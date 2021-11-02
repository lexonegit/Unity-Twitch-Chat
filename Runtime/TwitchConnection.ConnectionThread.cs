using System.Threading;
using System.Net.Sockets;

namespace Incredulous.Twitch
{

    internal partial class TwitchConnection
    {
        private const int connectionCheckInterval = 500;

        /// <summary>
        /// Thread process which checks whether the socket is still connected to the network.
        /// </summary>
        private void CheckConnectionProcess()
        {
            /* TODO
             * Unclear why this is necessary. Sometimes, right after a new TcpClient is created, the socket says
             * it has been shutdown. This catches that case and reconnects.
            */

            var socket = tcpClient.Client;

            while (continueThreads)
            {
                // Alert if the socket is disconnected
                if (!CheckSocketConnection(socket))
                {
                    connectionAlertQueue.Enqueue(ConnectionAlert.ConnectionInterrupted);
                    break;
                }
                else
                {
                    Thread.Sleep(connectionCheckInterval);
                }
            }
        }

        /// <summary>
        /// Checks whether the socket is still connected to the network.
        /// </summary>
        private bool CheckSocketConnection(Socket socket)
        {
            var poll = socket.Poll(1000, SelectMode.SelectRead);
            var avail = (socket.Available == 0);
            if ((poll && avail) || !socket.Connected)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

}