using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Net_Navis
{
    public class Client
    {
        public int MAX_STRING_LENGTH = 4092;

        private TcpClient client;
        private NetworkStream stream;
        private static BinaryFormatter formatter = new BinaryFormatter();
        private static ASCIIEncoding asciiEncoder = new ASCIIEncoding();

        private object writeLock = new object();
        private bool active = true;

        public bool Active
        {
            get { return active; }
        }
        public int Available
        {
            get { return client.Available; }
        }
        public string IPAddress
        {
            get { return (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString(); }
        }
        public int Port
        {
            get { return (client.Client.RemoteEndPoint as IPEndPoint).Port; }
        }

        // Constructor
        public Client(TcpClient client)
        {
            this.client = client;
            client.NoDelay = true;
            stream = client.GetStream();
        }

        public void Close()
        {
            lock (writeLock)
            {
                if (active)
                {
                    stream.Close();
                    client.Close();
                    active = false;
                }
            }
        }

        public void Write(Int32 integer)
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] byteData = BitConverter.GetBytes(integer);
                    stream.Write(byteData, 0, byteData.Length);
                }
            }
        }

        public void Write(float number)
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] byteData = BitConverter.GetBytes(number);
                    stream.Write(byteData, 0, byteData.Length);
                }
            }
        }

        public void Write(double number)
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] byteData = BitConverter.GetBytes(number);
                    stream.Write(byteData, 0, byteData.Length);
                }
            }
        }

        public void Write(string data)
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] byteData = asciiEncoder.GetBytes(data);
                    Write(byteData.Length);
                    stream.Write(byteData, 0, byteData.Length);
                }
            }
        }

        public void WriteObject(params object[] data)
        {
            lock (writeLock)
            {
                if (active)
                {
                    foreach (object o in data)
                        formatter.Serialize(stream, o);
                }
            }
        }

        public Int32 ReadInt32()
        {
            byte[] buffer = new byte[4];
            if (stream.Read(buffer, 0, 4) <= 0)
                throw new System.IO.EndOfStreamException();
            return BitConverter.ToInt32(buffer, 0);
        }

        public float ReadFloat()
        {
            byte[] buffer = new byte[4];
            if (stream.Read(buffer, 0, 4) <= 0)
                throw new System.IO.EndOfStreamException();
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble()
        {
            byte[] buffer = new byte[8];
            if (stream.Read(buffer, 0, 8) <= 0)
                throw new System.IO.EndOfStreamException();
            return BitConverter.ToDouble(buffer, 0);
        }

        public string ReadString()
        {
            int size = ReadInt32();
            if (size < 0 || size > MAX_STRING_LENGTH)
                throw new ArgumentOutOfRangeException();
            byte[] buffer = new byte[size];
            if (stream.Read(buffer, 0, size) <= 0)
                throw new System.IO.EndOfStreamException();
            return asciiEncoder.GetString(buffer);
        }

        public object ReadObject()
        {
            return formatter.Deserialize(stream);
        }

        public void DumpReadBuffer()
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] buffer = new byte[4096];
                    while (stream.DataAvailable)
                        stream.Read(buffer, 0, 4096);
                }
            }
        }

        public void Echo()
        {
            byte[] buffer = new byte[4096];
            int amountRead;
            amountRead = stream.Read(buffer, 0, 4096);
            if (amountRead == 0)
                throw new System.IO.EndOfStreamException();
            lock (writeLock)
            {
                if (active)
                    stream.Write(buffer, 0, amountRead);
            }
        }

        public void WriteSpecial(byte[] buffer)
        {
            lock (writeLock)
            {
                if (active)
                {
                    stream.Write(buffer, 0, 128);
                }
            }
        }

        public byte[] ReadSpecial()
        {
            byte[] buffer = new byte[128];
            if (stream.Read(buffer, 0, 128) <= 0)
                throw new System.IO.EndOfStreamException();
            return buffer;
        }
    }
}
