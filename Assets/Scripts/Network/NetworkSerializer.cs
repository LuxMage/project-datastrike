using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Networking.Types;

namespace DatastrikeNetwork
{
    public enum NetworkEventType { TriggerEvent = 1, UpdatePosition };
    public enum NetworkSubeventType { Null };

    public class NetworkSerializer
    {
        private static Hashtable networkedObjects = new Hashtable();

        // All game objects with a NetworkIdentity call this to add them to the networkedObject hash table
        public static ushort Register(object o)
        {
            for (ushort i = 1; i < UInt16.MaxValue; i++)
            {
                if (!networkedObjects.ContainsKey(i))
                {
                    networkedObjects.Add(i, o);
                    return i;
                }
            }

            return UInt16.MaxValue;
        }

        // Creates a message based on the event type
        public static void AssembleMessage(ushort netObjectId, NetworkEventType netEventType, NetworkSubeventType netSubeventType, object data)
        {
            byte[] message = null;

            switch (netEventType)
            {
                case NetworkEventType.TriggerEvent:
                    break;

                case NetworkEventType.UpdatePosition:
                    message = new byte[53];
                    SerializeUShort(netObjectId, message, 0);
                    SerializeUShort((ushort)netEventType, message, 2);
                    message[4] = (byte)netSubeventType;
                    Transform typedObject = (Transform)data;
                    SerializeTransform(typedObject, message, 5);
                    break;
            }

            NetworkCommunicator.UpdateSendMessage(message);
        }

        // Takes the full data sent from the other client and distributes them to each corresponding network identity
        public static void DistributeMessages(byte[] sendData)
        {
            int i = 2;
            ushort sendDataLength = BitConverter.ToUInt16(sendData, 0);
            int increment = 0;
            while (i < sendDataLength - 3)
            {
                NetworkEvent currentEvent = DisassembleMessage(sendData, i, out increment);
                currentEvent.GetNetworkIdentity().dataQueue.Add(currentEvent);
                i += increment;
            }
        }

        // Takes part of the full data and disassembles it into a NetworkEvent
        public static NetworkEvent DisassembleMessage(byte[] message, int startIndex, out int newIndex)
        {
            ushort objId = DeserializeUShort(message, startIndex);
            NetworkEventType netEventType = (NetworkEventType)DeserializeUShort(message, startIndex + 2);
            NetworkSubeventType netSubeventType = (NetworkSubeventType)message[startIndex + 4];
            object data = null;
            newIndex = 1;

            switch (netEventType)
            {
                case NetworkEventType.TriggerEvent:
                    break;

                case NetworkEventType.UpdatePosition:
                    data = DeserializeTransform(message, startIndex + 5);
                    newIndex = 41;
                    break;

                default:
                    break;
            }

            NetworkEvent returnVal = new NetworkEvent(objId, netEventType, netSubeventType, data);

            return returnVal;
        }

        // Serialization Functions
        // ------------------------------------
        // 4 bytes
        public static void SerializeInt(int i, byte[] message, int startIndex)
        {
            byte[] si = BitConverter.GetBytes(i);
            si.CopyTo(message, startIndex);
        }

        // 2 bytes
        public static void SerializeUShort(ushort s, byte[] message, int startIndex)
        {
            byte[] sus = BitConverter.GetBytes(s);
            sus.CopyTo(message, startIndex);
        }

        // 4 bytes
        public static void SerializeFloat(float f, byte[] message, int startIndex)
        {
            byte[] sf = BitConverter.GetBytes(f);
            sf.CopyTo(message, startIndex);
        }

        // 1 byte
        public static void SerializeBool(bool b, byte[] message, int startIndex)
        {
            byte[] sb = BitConverter.GetBytes(b);
            sb.CopyTo(message, startIndex);
        }

        // 12 bytes
        public static void SerializeVector3(Vector3 vector, byte[] message, int startIndex)
        {
            SerializeFloat(vector.x, message, startIndex);
            SerializeFloat(vector.y, message, startIndex + 4);
            SerializeFloat(vector.z, message, startIndex + 8);
        }

        // 36 bytes
        public static void SerializeTransform(Transform transform, byte[] message, int startIndex)
        {
            SerializeVector3(transform.position, message, startIndex);
            SerializeVector3(transform.eulerAngles, message, startIndex + 12);
            SerializeVector3(transform.localScale, message, startIndex + 24);
        }

        // 48 bytes (includes movement data)
        public static void SerializeTransformAndMovement(Vector3[] transform, byte[] message, int startIndex)
        {
            SerializeVector3(transform[0], message, startIndex);
            SerializeVector3(transform[1], message, startIndex + 12);
            SerializeVector3(transform[2], message, startIndex + 24);
            SerializeVector3(transform[3], message, startIndex + 36);
        }

        // Deserialization Functions
        // ------------------------------------
        public static int DeserializeInt(byte[] message, int startIndex)
        {
            return BitConverter.ToInt32(message, startIndex);
        }

        public static ushort DeserializeUShort(byte[] message, int startIndex)
        {
            return BitConverter.ToUInt16(message, startIndex);
        }

        public static float DeserializeFloat(byte[] message, int startIndex)
        {
            return BitConverter.ToSingle(message, startIndex);
        }

        public static bool DeserializeBool(byte[] message, int startIndex)
        {
            return BitConverter.ToBoolean(message, startIndex);
        }

        public static Vector3 DeserializeVector3(byte[] message, int startIndex)
        {
            float rx = DeserializeFloat(message, startIndex);
            float ry = DeserializeFloat(message, startIndex + 4);
            float rz = DeserializeFloat(message, startIndex + 8);

            return new Vector3(rx, ry, rz);
        }

        public static Vector3[] DeserializeTransform(byte[] message, int startIndex)
        {
            Vector3[] transformComponents = new Vector3[3];
            transformComponents[0] = DeserializeVector3(message, startIndex);
            transformComponents[1] = DeserializeVector3(message, startIndex + 12);
            transformComponents[2] = DeserializeVector3(message, startIndex + 24);

            return transformComponents;
        }

        public static Vector3[] DeserializeTransformAndMovement(byte[] message, int startIndex)
        {
            Vector3[] components = new Vector3[4];
            components[0] = DeserializeVector3(message, startIndex);
            components[1] = DeserializeVector3(message, startIndex + 12);
            components[2] = DeserializeVector3(message, startIndex + 24);
            components[3] = DeserializeVector3(message, startIndex + 36);

            return components;
        }

        // Helper functions
        public static Hashtable GetNetworkedObjects()
        {
            return networkedObjects;
        }
    }

    // A class used to format a message received from the other client.
    public class NetworkEvent
    {
        private NetworkEventType networkEventType;
        private NetworkSubeventType networkSubeventType;
        private NetworkIdentity destNetId;
        private object data;

        public NetworkEvent(ushort networkedObjectId, NetworkEventType networkEventType, NetworkSubeventType networkSubeventType, object data)
        {
            this.destNetId = (NetworkIdentity)NetworkSerializer.GetNetworkedObjects()[networkedObjectId];
            this.networkEventType = networkEventType;
            this.networkSubeventType = networkSubeventType;
            this.data = data;
        }

        public NetworkEventType GetNetworkEventType()
        {
            return this.networkEventType;
        }

        public NetworkIdentity GetNetworkIdentity()
        {
            return this.destNetId;
        }

        public object GetData()
        {
            return this.data;
        }

        public override string ToString()
        {
            if (networkEventType == NetworkEventType.TriggerEvent)
            {
                return "TriggerEvent";
            }

            else if (networkEventType == NetworkEventType.UpdatePosition)
            {
                return "UpdatePosition";
            }

            else
            {
                return "Panic!";
            }
        }
    }
}
