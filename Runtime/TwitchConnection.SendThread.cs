using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Incredulous.Twitch
{

    internal partial class TwitchConnection
    {
        private ConcurrentQueue<string> priorityOutputQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// The IRC output process which will run on the send thread.
        /// </summary>
        private void SendProcess()
        {
            var stream = tcpClient.GetStream();
            RateLimit rateLimit;

            //Read loop
            while (continueThreads)
            {
                // Send prioritized outputs
                while (priorityOutputQueue.TryDequeue(out var priorityOutput))
                    stream.WriteLine(priorityOutput, debugIRC);

                // Get the current rate limit
                lock (rateLimitLock)
                    rateLimit = chatRateLimit;

                // Clear timestamps that are older than the rate limit period
                var minTime = DateTime.Now - rateLimit.timeSpan;
                while (outputTimestamps.TryPeek(out var next) && next < minTime)
                    outputTimestamps.TryDequeue(out _);

                // Send outputs up to the rate limit
                while (!outputQueue.IsEmpty && outputTimestamps.Count < rateLimit.count)
                {
                    if (outputQueue.TryDequeue(out var output))
                    {
                        stream.WriteLine(output, debugIRC);
                        outputTimestamps.Enqueue(DateTime.Now);
                    }
                }

                // Sleep for a short while before checking again
                Thread.Sleep(writeInterval);
            }

            if (debugThreads)
                Debug.LogWarning("Exited send thread.");
        }

        /// <summary>
        /// Sends a ping to the server.
        /// </summary>
        public void Ping() => SendCommand("PING :tmi.twitch.tv");

        /// <summary>
        /// Queues a command to be sent to the IRC server. Prioritzed commands will be sent without regard for rate limits.
        /// </summary>
        public void SendCommand(string command, bool prioritized = false)
        {
            // For non-prioritized outputs, send a warning if the output will surpass the rate limit
            if (!prioritized)
            {
                lock (rateLimitLock)
                {
                    if (outputTimestamps.Count + 1 > chatRateLimit.count)
                        alertQueue.Enqueue(ConnectionAlert.RateLimitWarning);
                }
            }

            // Place command in respective queue
            if (prioritized)
                priorityOutputQueue.Enqueue(command);
            else
                outputQueue.Enqueue(command);
        }

        /// <summary>
        /// Sends a chat message.
        /// </summary>
        public void SendChatMessage(string message)
        {
            // Message can't be empty
            if (message.Length <= 0)
                return;

            // Send a warning if the output will surpass the rate limit
            lock (rateLimitLock)
            {
                if (outputTimestamps.Count + 1 > chatRateLimit.count)
                    alertQueue.Enqueue(ConnectionAlert.RateLimitWarning);
            }

            // Place message in queue
            outputQueue.Enqueue("PRIVMSG #" + twitchCredentials.channel + " :" + message);
        }
    }

}