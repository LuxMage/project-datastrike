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
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using System.Linq;

namespace DatastrikeNetwork
{
    public class NetworkCommunicator
    {

        private class UdpState
        {
            public UdpClient u;
            public IPEndPoint e;
        }

        // UdpClient and Relay data
        private static IPEndPoint localEndPoint = null;
        private static IPEndPoint relayEndPoint = null;
        private static UdpClient udpClient = null;

        // Relay initialization variables
        private static bool initMessageSent = false;
        private static bool initMessageRecv = false;
        private static byte[] initMessage = null;

        private static List<byte> mts = new List<byte>();
        private static byte[] mrcv = null;

        private static int sendSeq = 0;
        private static int recvSeq = 0;

        public static void RunHost()
        {
            InitializeServerConnection();

            // Initialization successful, start communication
            NetworkClock.StartCommunication();
        }

        public static void RunClient()
        {
            InitializeServerConnection();

            NetworkClock.StartCommunication();
        }

        private static void InitializeServerConnection()
        {
            // Get local IP address
            var host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localIP = IPAddress.Parse("0.0.0.0");

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip;
                }
            }

            // Initialize endpoints and UdpClient object
            localEndPoint = new IPEndPoint(localIP, 27317);
            relayEndPoint = new IPEndPoint(IPAddress.Parse(""), 27317); // Insert server IP address here

            udpClient = new UdpClient(localEndPoint);

            // Set default host for udpClient
            try
            {
                udpClient.Connect(relayEndPoint);
            }

            catch (Exception e)
            {
                NetworkClock.ConsolePrint(e.ToString());
            }

            while (true)
            {
                // Send relay connect message
                UdpState initState = new UdpState();
                initState.u = udpClient;
                initState.e = relayEndPoint;
                byte[] synMessage = new byte[] { 1 };

                udpClient.BeginSend(synMessage, synMessage.Length, new AsyncCallback(HostInitSendCallback), initState);

                while (!initMessageSent)
                {
                    Thread.Sleep(100);
                }

                // Wait until we receive the all-clear from the relay server to continue
                udpClient.BeginReceive(new AsyncCallback(HostInitRecvCallback), initState);
                NetworkClock.ConsolePrint("Waiting on other client...");

                while (!initMessageRecv)
                {
                    Thread.Sleep(100);
                }

                if (initMessage[0] != 3)
                {
                    NetworkClock.ConsolePrint("Unsuccessful initialization, trying again...");
                    continue;
                }

                break;
            }
        }

        public static void SendDataBuffer()
        {
            // Calculate checksum (sum of binary complements)
            int checksum = 0;

            foreach (byte b in mts)
            {
                int complement = (~b) & 255;
                checksum += complement;
            }

            // Assemble full message with sequence number, checksum, and content length
            List<byte> fm = new List<byte>();
            fm.AddRange(BitConverter.GetBytes(sendSeq));
            fm.AddRange(BitConverter.GetBytes(checksum));
            fm.AddRange(BitConverter.GetBytes(mts.Count));

            mts.InsertRange(0, fm);

            byte[] fullMessage = mts.ToArray();

            UdpState state = new UdpState();
            state.u = udpClient;
            state.e = relayEndPoint;

            udpClient.BeginSend(fullMessage, fullMessage.Length, new AsyncCallback(SendCallback), state);
            sendSeq++;
        }

        public static void RecvDataBuffer()
        {
            UdpState state = new UdpState();
            state.u = udpClient;
            state.e = relayEndPoint;

            udpClient.BeginReceive(new AsyncCallback(RecvCallback), state);
        }

        public static void UpdateSendMessage(byte[] message)
        {
            mts.AddRange(message);
        }

        private static void HostInitSendCallback(IAsyncResult ar)
        {
            UdpClient u = ((UdpState)ar.AsyncState).u;
            u.EndSend(ar);
            initMessageSent = true;
        }

        private static void HostInitRecvCallback(IAsyncResult ar)
        {
            UdpState state = (UdpState)ar.AsyncState;
            initMessage = state.u.EndReceive(ar, ref state.e);
            initMessageRecv = true;
        }

        private static void SendCallback(IAsyncResult ar)
        {
            UdpClient udpClient = ((UdpState)ar.AsyncState).u;
            udpClient.EndSend(ar);
        }

        private static void RecvCallback(IAsyncResult ar)
        {
            UdpState state = (UdpState)ar.AsyncState;
            mrcv = state.u.EndReceive(ar, ref state.e);

            // Check the sequence number
            int supposedSeq = BitConverter.ToInt32(mrcv, 0);

            if (supposedSeq < recvSeq)
                return;

            recvSeq = supposedSeq;

            int supposedChecksum = BitConverter.ToInt32(mrcv, 4);
            int calculatedChecksum = 0;
            int length = BitConverter.ToInt32(mrcv, 8);

            for (int i = 12; i < mrcv.Length; i++)
            {
                int complement = (~mrcv[i]) & 255;
                calculatedChecksum += complement;
            }

            if (calculatedChecksum != supposedChecksum)
                return;

            NetworkSerializer.DistributeMessages(mrcv);
        }
    }
}
