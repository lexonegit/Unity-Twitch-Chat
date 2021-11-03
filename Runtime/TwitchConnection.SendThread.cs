using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Incredulous.Twitch
{

    internal partial class TwitchConnection
    {
        private ConcurrentQueue<string> priorityOutputQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();

        private const int twitchRateLimitSleepTime = 1750;

        /// <summary>
        /// The IRC output process which will run on the send thread.
        /// </summary>
        private void SendProcess()
        {
            var stream = tcpClient.GetStream();

            //Read loop
            while (continueThreads)
            {
                int sleepTime = writeInterval;

                if (!priorityOutputQueue.IsEmpty)
                {
                    // Send all outputs from priorityOutputQueue
                    while (priorityOutputQueue.TryDequeue(out var output))
                        stream.WriteLine(output, debugIRC);
                }
                else if (!outputQueue.IsEmpty)
                {
                    // Send next output from outputQueue
                    if (outputQueue.TryDequeue(out var output))
                    {
                        stream.WriteLine(output, debugIRC);
                        sleepTime = twitchRateLimitSleepTime;
                    }
                }

                // Sleep for a short while before checking again
                Thread.Sleep(sleepTime);
            }

            if (debugThreads)
                Debug.LogWarning("Exited send thread.");
        }

        /// <summary>
        /// Sends a ping to the server.
        /// </summary>
        public void Ping() => SendCommand("PING :tmi.twitch.tv", true);

        /// <summary>
        /// Queues a command to be sent to the IRC server. All prioritzed commands will be sent before non-prioritized commands.
        /// </summary>
        public void SendCommand(string command, bool prioritized = false)
        {
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

            // Place message in queue
            outputQueue.Enqueue("PRIVMSG #" + twitchCredentials.channel + " :" + message);
        }
    }

}