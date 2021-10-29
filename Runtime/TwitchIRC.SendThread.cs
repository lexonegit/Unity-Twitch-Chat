using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Lexonegit.UnityTwitchChat
{

    public partial class TwitchIRC
    {
        /// <summary>
        /// The prioritized queue for outputs to the IRC server. This queue will be fully emptied before reading from the main output queue.
        /// </summary>
        private ConcurrentQueue<string> priorityOutputQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// The main output queue for outputs to the IRC server
        /// </summary>
        private ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// The IRC output process which will run on the send thread.
        /// </summary>
        private void IRCOutputProc()
        {
            Debug.Log("IRCOutput Thread (Send) started");

            var stream = client.GetStream();

            //Read loop
            while (connected)
            {
                int sleepTime = writeInterval;

                if (!priorityOutputQueue.IsEmpty)
                {
                    // Send all outputs from priorityOutputQueue
                    while (priorityOutputQueue.TryDequeue(out var output))
                        stream.WriteLine(output, settings.debugIRC);
                }
                else if (!outputQueue.IsEmpty)
                {
                    // Send next output from outputQueue
                    if (outputQueue.TryDequeue(out var output))
                    {
                        stream.WriteLine(output, settings.debugIRC);
                        sleepTime = twitchRateLimitSleepTime;
                    }
                }

                // Sleep for a short while before checking again
                Thread.Sleep(sleepTime);
            }

            Debug.LogWarning("IRCOutput Thread (Send) exited");
        }
    }

}