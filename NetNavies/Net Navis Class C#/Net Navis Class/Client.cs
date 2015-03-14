using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Net_Navis
{
    public class Client
    {
        public int MAX_STRING_LENGTH = 4092; // 4096 - 4

        private TcpClient client;
        private NetworkStream stream;
        private static BinaryFormatter formatter = new BinaryFormatter();
        private static ASCIIEncoding asciiEncoder = new ASCIIEncoding();

        private object writeLock = new object();
        private bool active = true;

        private MemoryStream readBuffer;
        private MemoryStream writeBuffer;

        public bool Active
        {
            get { return active; }
        }
        public int Available
        {
            get { return (int)(readBuffer.Length - readBuffer.Position); }
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
            readBuffer = new MemoryStream();
            writeBuffer = new MemoryStream();
        }

        public void Close()
        {
            lock (writeLock)
            {
                if (active)
                {
                    stream.Close();
                    client.Close();
                    readBuffer.Close();
                    writeBuffer.Close();
                    active = false;
                }
            }
        }

        public void Write(byte b)
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] byteData = new byte[1] { b };
                    writeBuffer.Write(byteData, 0, 1);
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
                    writeBuffer.Write(byteData, 0, byteData.Length);
                }
            }
        }

        public void Write(UInt64 integer)
        {
            lock (writeLock)
            {
                if (active)
                {
                    byte[] byteData = BitConverter.GetBytes(integer);
                    writeBuffer.Write(byteData, 0, byteData.Length);
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
                    writeBuffer.Write(byteData, 0, byteData.Length);
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
                    writeBuffer.Write(byteData, 0, byteData.Length);
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
                    writeBuffer.Write(byteData, 0, byteData.Length);
                }
            }
        }

        public void WriteByteArray(byte[] buffer)
        {
            lock (writeLock)
            {
                if (active)
                {
                    writeBuffer.Write(buffer, 0, buffer.Length);
                }
            }
        }

        // commented out because we're never going to use it and i haven't figured out how to make it work with the buffer system
        //public void WriteObject(params object[] data)
        //{
        //    lock (writeLock)
        //    {
        //        if (active)
        //        {
        //            foreach (object o in data)
        //                formatter.Serialize(writeBuffer, o);
        //        }
        //    }
        //}

        public void Flush()
        {
            lock (writeLock)
            {
                if (active)
                {
                    if (writeBuffer.Length > 0)
                    {
                        writeBuffer.Position = 0;
                        writeBuffer.CopyTo(stream);
                        writeBuffer = new MemoryStream();
                    }
                }
            }
        }

        public byte ReadByte()
        {
            byte[] buffer = new byte[1];
            if (readBuffer.Length - readBuffer.Position < 1)
            {
                readBuffer = new MemoryStream();
                blockingRead();
            }
            readBuffer.Read(buffer, 0, 1);
            return buffer[0];
        }

        public Int32 ReadInt32()
        {
            return BitConverter.ToInt32(ReadByteArray(4), 0);
        }

        public UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadByteArray(8), 0);
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(ReadByteArray(4), 0);
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(ReadByteArray(8), 0);
        }

        public string ReadString()
        {
            int size = ReadInt32();
            if (size < 0 || size > MAX_STRING_LENGTH)
                throw new ArgumentOutOfRangeException();
            return asciiEncoder.GetString(ReadByteArray(size));
        }

        // commented out because we're never going to use it and i haven't figured out how to make it work with the buffer system
        //public object ReadObject()
        //{
        //    object o = formatter.Deserialize(readBuffer);
        //    return o;
        //}

        public byte[] ReadByteArray(int amount)
        {
            byte[] buffer = new byte[amount];
            int amountRead = readBuffer.Read(buffer, 0, amount);
            while (amountRead < amount)
            {
                readBuffer = new MemoryStream();
                blockingRead();
                amountRead += readBuffer.Read(buffer, amountRead, amount - amountRead);
            }
            return buffer;
        }

        public void NonBlockingRead()
        {
            byte[] buffer = new byte[4096];
            int amountRead;
            while (stream.DataAvailable)
            {
                amountRead = stream.Read(buffer, 0, 4096);
                long position = readBuffer.Position;
                readBuffer.Write(buffer, 0, amountRead);
                readBuffer.Position = position;
            }
        }

        private void blockingRead()
        {
            byte[] buffer = new byte[4096];
            int amountRead;
            do
            {
                amountRead = stream.Read(buffer, 0, 4096);
                long position = readBuffer.Position;
                readBuffer.Write(buffer, 0, amountRead);
                readBuffer.Position = position;
            } while (stream.DataAvailable);
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
                    readBuffer = new MemoryStream();
                }
            }
        }

        //public void Echo()
        //{
        //    byte[] buffer = new byte[4096];
        //    int amountRead;
        //    amountRead = stream.Read(buffer, 0, 4096);
        //    if (amountRead <= 0)
        //        throw new EndOfStreamException();
        //    lock (writeLock)
        //    {
        //        if (active)
        //            stream.Write(buffer, 0, amountRead);
        //    }
        //}
    }
}
