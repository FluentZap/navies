using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
        
        private bool authenticated = false;
        //public bool PendingUpdate = false;
        private string name;
        private byte[] writeBuffer;
        private int writeLength = 0;
        public const int WRITE_BUFFER_SIZE = 4096;

        private byte[] readBuffer;
        public int readPosition = 0;
        private int readLength = 0;
        private const int READ_BUFFER_SIZE = 4096;
        
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
                    stream.Close();
                    client.Close();                    
        }

        public void Write(byte b)
        {
                    writeBuffer[writeLength++] = b;            
        }

        public void Write(Int32 integer)
        {
                    byte[] byteData = BitConverter.GetBytes(integer);
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;            
        }

        public void Write(UInt64 integer)
        {
                    byte[] byteData = BitConverter.GetBytes(integer);
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;            
        }

        public void Write(float number)
        {
                    byte[] byteData = BitConverter.GetBytes(number);
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;            
        }

        public void Write(double number)
        {
                    byte[] byteData = BitConverter.GetBytes(number);
                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;            
        }

        public void Write(string data)
        {
                    byte[] byteData = asciiEncoder.GetBytes(data);

                    Write(byteData.Length);

                    byteData.CopyTo(writeBuffer, writeLength);
                    writeLength += byteData.Length;            
        }

        public void WriteByteArray(byte[] buffer)
        {
                    buffer.CopyTo(writeBuffer, writeLength);
                    writeLength += buffer.Length;            
        }


        public void Flush()
        {
            if (writeLength > 0)
            {
                stream.Write(writeBuffer, 0, writeLength);
                writeLength = 0;
            }
        }

        public byte ReadByte()
        {                        
            return (byte)stream.ReadByte();
        }

        public Int32 ReadInt32()
        {
            byte[] b = new byte[4];
            stream.Read(b, 0, 4);
            return BitConverter.ToInt32(b, 0);
        }

        public UInt64 ReadUInt64()
        {
            byte[] b = new byte[8];
            stream.Read(b, 0, 8);
            return BitConverter.ToUInt64(b, 0);
        }

        public float ReadFloat()
        {
            byte[] b = new byte[4];
            stream.Read(b, 0, 4);
            return BitConverter.ToSingle(b, 0);            
        }

        public double ReadDouble()
        {
            byte[] b = new byte[8];
            stream.Read(b, 0, 8);
            return BitConverter.ToDouble(b, 0);
        }

        public string ReadString()
        {
            int size = ReadInt32();
            if (size < 0 || size > MAX_STRING_LENGTH)
                throw new ArgumentOutOfRangeException();            
            byte[] b = new byte[size];
            stream.Read(b, 0, size);
            return asciiEncoder.GetString(b);
        }

        public byte[] ReadByteArray(int amount)
        {            
            byte[] b = new byte[amount];
            stream.Read(b, 0, amount);
            return b;
        }                
        
        public bool isActive()
        {

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections().Where(x => x.LocalEndPoint.Equals(client.Client.LocalEndPoint) && x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint)).ToArray();

            if (tcpConnections != null && tcpConnections.Length > 0)
            {
                TcpState stateOfConnection = tcpConnections.First().State;
                if (stateOfConnection == TcpState.Established)
                {
                    return true;
                    // Connection is OK
                }
                else
                {
                    return false;
                    // No active tcp Connection to hostName:port
                }

            }
            return false;
        }


    }
}
