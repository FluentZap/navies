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
        private bool authenticated = false;
        private string name;
        private byte[] writeBuffer;
        private int writeLength = 0;
        public const int WRITE_BUFFER_SIZE = 4096;
        private byte[] readBuffer;
        public int readPosition = 0;
        private int readLength = 0;
        private const int READ_BUFFER_SIZE = 4096;

        public bool PendingUpdate = false;

        public bool Authenticated
        {
            get { return authenticated; }
            set { this.authenticated = value; }
        }
        public string Name
        {
            get { return name; }
            set { this.name = value; }
        }
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
            readBuffer = new byte[READ_BUFFER_SIZE];
            writeBuffer = new byte[WRITE_BUFFER_SIZE];
            authenticated = false;
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

        public void Write(byte b)
        {
            lock (writeLock)
            {
                if (active)
                {
                    writeBuffer[writeLength++] = b;
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
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;
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
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;
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
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;
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
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;
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

                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;
                }
            }
        }

        public void WriteByteArray(byte[] buffer)
        {
            lock (writeLock)
            {
                if (active)
                {
                    buffer.CopyTo(writeBuffer, writeLength);
                    writeLength += buffer.Length;
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
                    if (writeLength > 0)
                    {
                        stream.Write(writeBuffer, 0, writeLength);
                        writeLength = 0;
                    }
                }
            }
        }

        public byte ReadByte()
        {
            if (readPosition == READ_BUFFER_SIZE)
                readPosition = 0;
            if (readLength == 0)
                blockingRead();
            --readLength;
            return readBuffer[readPosition++];
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

            int i = 0;
            while (i < amount)
            {
                if (readPosition == READ_BUFFER_SIZE)
                    readPosition = 0;
                if (readLength == 0)
                    blockingRead();
                buffer[i++] = readBuffer[readPosition++];
                --readLength;
            }

            return buffer;
        }

        public void NonBlockingRead()
        {
            byte[] buffer = new byte[4096];
            int amountRead;
            while (stream.DataAvailable)
            {
                amountRead = stream.Read(buffer, 0, buffer.Length);

                // overflows buffer
                if (amountRead + readLength > READ_BUFFER_SIZE)
                    throw new InternalBufferOverflowException();

                int i = 0;
                int j = (readPosition + readLength) % READ_BUFFER_SIZE;
                while (i < amountRead)
                {
                    if (j == READ_BUFFER_SIZE)
                        j = 0;
                    readBuffer[j++] = buffer[i++];
                }
                readLength += amountRead;
            }
        }

        private void blockingRead()
        {
            byte[] buffer = new byte[READ_BUFFER_SIZE];
            int amountRead;
            do
            {
                amountRead = stream.Read(buffer, 0, buffer.Length);

                // overflows buffer
                if (amountRead + readLength > READ_BUFFER_SIZE)
                    throw new InternalBufferOverflowException();

                int i = 0;
                int j = (readPosition + readLength) % READ_BUFFER_SIZE;
                while (i < amountRead)
                {
                    if (j == READ_BUFFER_SIZE)
                        j = 0;
                    readBuffer[j++] = buffer[i++];
                }
                readLength += amountRead;
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
                    readPosition = 0;
                    readLength = 0;
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
