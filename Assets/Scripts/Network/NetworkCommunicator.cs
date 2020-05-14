using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking.Types;

namespace DatastrikeNetwork
{
    public class NetworkCommunicator
    {
        public const int SEND_BUFFER_SIZE = 2048;
        public const int RECV_BUFFER_SIZE = 2048;

        public class SendState
        {
            public Socket workSocket = null;
            public byte[] sendBuffer = new byte[SEND_BUFFER_SIZE];
        }

        public class RecvState
        {
            public Socket workSocket = null;
            public byte[] recvBuffer = new byte[RECV_BUFFER_SIZE];
        }

        private static bool connectionFound = false;

        private static byte[] messageToSend = new byte[SEND_BUFFER_SIZE];
        private static int currentMessageLength = 2;

        private static SendState sendState = null;
        private static RecvState recvState = null;

        // Initializes server, listens for a client connection.
        public static void RunServer()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint localEndPoint = new IPEndPoint(ip, 27317);

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(5);

                while (!connectionFound)
                {
                    listener.BeginAccept(new AsyncCallback(ServerAcceptCallback), listener);
                    Thread.Sleep(1000);
                }
            }

            catch (Exception e)
            {
                NetworkIdentity.ConsolePrint(e.ToString());
            }
        }

        // When a connection is accepted, initialize state objects then start queuing data for sending (and get ready to receive)
        private static void ServerAcceptCallback(IAsyncResult ar)
        {
            connectionFound = true;

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            SendState ss = new SendState();
            ss.workSocket = handler;
            sendState = ss;

            RecvState rs = new RecvState();
            rs.workSocket = handler;
            recvState = rs;

            NetworkClock.StartCommunication();
        }

        // Called by NetworkClock to cause the current data in messageToSend to be sent.
        public static void SendDataBuffer()
        {
            messageToSend[currentMessageLength] = 255;
            messageToSend[currentMessageLength + 1] = 255;
            messageToSend[currentMessageLength + 2] = 255;
            currentMessageLength += 3;
            sendState.sendBuffer = messageToSend;
            sendState.workSocket.BeginSend(sendState.sendBuffer, 0, currentMessageLength, 0, new AsyncCallback(SendCallback), sendState.workSocket);
        }

        // After data is sent, empty messageToSend.
        private static void SendCallback(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
            messageToSend = new byte[SEND_BUFFER_SIZE];
            BitConverter.GetBytes((ushort)2).CopyTo(messageToSend, 0);
            currentMessageLength = 2;
        }

        // Triggered by NetworkClock to start receiving data
        public static void RecvDataBuffer()
        {
            recvState.workSocket.BeginReceive(recvState.recvBuffer, 0, RECV_BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), recvState);
        }

        // Check if message is valid. If so, distribute messages to their appropriate network identities. Else, get the rest if the message.
        private static void ReceiveCallback(IAsyncResult ar)
        {
            RecvState state = (RecvState)ar.AsyncState;

            int bytesRead = state.workSocket.EndReceive(ar);
            ushort messageLength = BitConverter.ToUInt16(state.recvBuffer, 0);

            bool hasTerminator = state.recvBuffer[messageLength] == 255 && state.recvBuffer[messageLength + 1] == 255 && state.recvBuffer[messageLength + 2] == 255;

            if (bytesRead > 2 && hasTerminator)
            {
                NetworkSerializer.DistributeMessages(state.recvBuffer);
                state.recvBuffer = new byte[RECV_BUFFER_SIZE];
            }

            else
            {
                state.workSocket.BeginReceive(state.recvBuffer, 0, RECV_BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
            }
        }

        // Initializes client and attempts to connect to a listening server.
        public static void RunClient()
        {
            try
            {
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEndPoint = new IPEndPoint(ip, 27317);

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                client.BeginConnect(remoteEndPoint, new AsyncCallback(ClientConnectCallback), client);
            }

            catch (Exception e)
            {
                NetworkIdentity.ConsolePrint(e.ToString());
            }
        }

        // Equivalent to ServerAcceptCallback
        private static void ClientConnectCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);

            SendState ss = new SendState();
            ss.workSocket = client;
            sendState = ss;

            RecvState rs = new RecvState();
            rs.workSocket = client;
            recvState = rs;

            NetworkClock.StartCommunication();
        }

        // Adds a new data entry to messageToSend.
        public static void UpdateSendMessage(byte[] message)
        {
            message.CopyTo(messageToSend, currentMessageLength);
            currentMessageLength += message.Length;
            BitConverter.GetBytes((ushort)currentMessageLength).CopyTo(messageToSend, 0);
        }
    }
}
