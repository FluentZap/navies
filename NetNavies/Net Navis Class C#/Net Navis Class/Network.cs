using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Net_Navis
{
    partial class Navi_Main
    {
        enum Headers : byte
        {
            SendingUpdate,
            RequestJoin,
            GroupAppend,
            Approved,
            Denied,
            Invalid,
            Error,
            Bye
        }

        private const int DEFAULT_PORT = 11994;
        private const int MAX_PEERS = 15;
        
        private Dictionary<string, Client> peers = new Dictionary<string, Client>();
        private System.Net.Sockets.TcpListener listener = null;
        private bool networkActive = false;
        private int peerCount = 0;
        private string networkName = null;
        private int listenerPort;
        private string networkCaptain = null; // if null, then we are the network captain

        private LinkedList<byte[]> updateBuffer = new LinkedList<byte[]>();



        void StartNetwork()
        {
            if (networkActive)
                return;

            networkActive = true;
            if (listener == null)
                listenerPort = findOpenPort(DEFAULT_PORT);
            else
                listener.Start(MAX_PEERS); // set backlog size to MAX_PEERS 

            Console.WriteLine("listener started on port " + listenerPort);
        }

        // Recursive
        private int findOpenPort(int port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start(MAX_PEERS); // set backlog size to MAX_PEERS
                return port;
            }
            catch (SocketException)
            {
                return findOpenPort(port + 1);
            }
        }

        void StopNetwork()
        {
            if (!networkActive)
                return;

            networkActive = false;
            listener.Stop();

            foreach (Client peer in peers.Values)
            {
                peer.Write((byte)Headers.Bye);
                peer.Close();
            }

            peers.Clear();
            Client_Navi.Clear();
            updateBuffer.Clear();
            peerCount = 0;
            networkCaptain = null;
            Console.WriteLine("Network stopped");
        }

        void DoNetworkEvents()
        {
            if (!networkActive)
                return;

            handleIncomingPeers();
            handlePeers();
        }

        bool ConnectToPeer(string host, int port)
        {
            if (!networkActive || peerCount > 0)
                return false;
            return requestNetworkJoin(host, port);
        }

        private bool requestNetworkJoin(string host, int port)
        {
            Client newPeer = new Client(new TcpClient(host, port));
            newPeer.Write((byte)Headers.RequestJoin);
            Headers response = (Headers)newPeer.ReadByte();

            if (response == Headers.Invalid) // we did not connect to the captain
            {
                Console.WriteLine("redirected to captain");
                host = newPeer.ReadString(); // receive the captain ip
                port = newPeer.ReadInt32(); // receive the captain port
                newPeer.Close();
                newPeer = new Client(new TcpClient(host, port));
                newPeer.Write((byte)Headers.RequestJoin);
                response = (Headers)newPeer.ReadByte();
            }

            if (response != Headers.Approved) // group was full
            {
                newPeer.Close();
                return false;
            }

            newPeer.Write(listenerPort); // send listener port
            networkName = newPeer.ReadString(); // read our network name

            // read number of soon-to-be incoming connections
            int incomingConnectionCount = newPeer.ReadInt32();

            // wait for pending connections
            Client c;
            string name;
            HashSet<Client> otherPending = new HashSet<Client>();
            while (incomingConnectionCount > 0)
            {
                c = new Client(listener.AcceptTcpClient());
                response = (Headers)c.ReadByte();
                if (response == Headers.GroupAppend)
                {
                    name = c.IPAddress + ":" + c.ReadInt32(); // figure out their network name
                    addPeer(name, c);
                    incomingConnectionCount -= 1;
                }
                else if (response == Headers.RequestJoin)
                    otherPending.Add(c); // deal with these later
                else
                    c.Close();
            }

            // sync the program step
            Program_Step = newPeer.ReadUInt64();

            // lastly, add the captain we are talking to
            networkCaptain = newPeer.IPAddress + ":" + port; // set them as captain
            addPeer(networkCaptain, newPeer);

            // deal with the other connections that came in while we were handling the GroupAppend
            foreach (Client pending in otherPending)
                receiveNetworkJoinRequest(pending);

            return true;
        }

        private void handleIncomingPeers()
        {
            Client newPeer;
            Headers request;
            while (listener.Pending())
            {
                newPeer = new Client(listener.AcceptTcpClient());
                Console.WriteLine("incomming client");

                request = (Headers)newPeer.ReadByte();
                if (request == Headers.RequestJoin)
                {
                    if (!receiveNetworkJoinRequest(newPeer))
                        Console.WriteLine("unable to add client");
                }
                else
                    newPeer.Close();
            }
        }

        private bool receiveNetworkJoinRequest(Client newPeer)
        {
            if (networkCaptain != null) // we are not the captain
            {
                newPeer.Write((byte)Headers.Invalid);
                string[] s = networkCaptain.Split(':');
                newPeer.Write(s[0]); // send ip address of captain
                newPeer.Write(Convert.ToInt32(s[1])); // send port
                newPeer.Close();
                Console.WriteLine("referred to captain");
                return false;
            }

            if (peerCount == MAX_PEERS) // full
            {
                newPeer.Write((byte)Headers.Denied);
                newPeer.Close();
                return false;
            }

            newPeer.Write((byte)Headers.Approved);

            // read port
            int port = newPeer.ReadInt32();
            // figure out name
            string name = newPeer.IPAddress + ":" + port;
            newPeer.Write(name); // send them their name

            // send number of peers who are about to connect
            newPeer.Write(peerCount);

            // tell the peers to connect
            foreach (Client peer in peers.Values)
            {
                peer.Write((byte)Headers.GroupAppend);
                peer.Write(newPeer.IPAddress);
                peer.Write(port);
            }
            
            // send program step
            newPeer.Write(Program_Step);

            // add them to our peers
            addPeer(name, newPeer);

            return true;
        }

        private void handlePeers()
        {
            Client peer;
            Headers request;
            HashSet<string> toRemove = new HashSet<string>();
            Dictionary<string, Client> toAdd = new Dictionary<string, Client>();
            NetNavi_Type navi;

            foreach (string name in peers.Keys)
            {
                peer = peers[name];
                navi = Client_Navi[name];
                
                try
                {
                    do
                    {
                        // read info
                        while (peer.Available >= 1) // 1 byte is the size of a header (byte) from the Headers enum
                        {
                            request = (Headers)peer.ReadByte();

                            if (request == Headers.SendingUpdate)
                                readPeerUpdate(peer); // read update
                            else if (request == Headers.GroupAppend)
                            {
                                string ip = peer.ReadString();
                                int port = peer.ReadInt32();
                                string newPeerName = ip + ":" + port;
                                Client newPeer = new Client(new TcpClient(ip, port));
                                newPeer.Write((byte)Headers.GroupAppend);
                                newPeer.Write(listenerPort);
                                toAdd.Add(newPeerName, newPeer);
                            }
                            else if (request == Headers.Bye)
                                throw new System.IO.IOException();
                        }

                        while (updateBuffer.Count == 0 && peer.Available == 0)
                            Thread.Yield();

                    } while (updateBuffer.Count == 0);

                    // read update
                    byte[] buffer = updateBuffer.First.Value;
                    updateBuffer.RemoveFirst();
                    navi.Set_Compact_buffer(buffer, 8);
                    ulong step = BitConverter.ToUInt64(buffer, 0);

                    Console.WriteLine(updateBuffer.Count);

                    // send our update
                    peer.Write((byte)Headers.SendingUpdate);
                    sendUpdate(peer, Host_Navi);
                }
                catch (System.IO.IOException) // other end disconnected
                {
                    toRemove.Add(name);
                    Client_Navi.Remove(name);
                    peer.Close();
                    --peerCount;
                    // if it was the captain who disconnected, select new captain alphabetically
                    if (name == networkCaptain)
                    {
                        string lowest = null;
                        foreach (string s in Client_Navi.Keys)
                        {
                            if (lowest == null)
                                lowest = s;
                            if (s.CompareTo(lowest) < 0)
                                lowest = s;
                        }
                        if (lowest == null || networkName.CompareTo(lowest) < 0) // our name is the first
                            networkCaptain = null; // set ourself as captain
                        else
                            networkCaptain = lowest;
                    }
                    Console.WriteLine(name + " disconnected");
                }
            }

            // remove any peers who disconnected
            // (can't edit the container during iteration)
            foreach (string name in toRemove)
                peers.Remove(name);

            // add any peers who we connected to with a GroupAppend
            // (can't edit the container during iteration)
            foreach (string name in toAdd.Keys)
                addPeer(name, toAdd[name]);
        }

        private void addPeer(string name, Client peer)
        {
            NetNavi_Type localObject = new NetNavi_Type();
            // send AND receive first update (for draw function)
            sendUpdate(peer, Host_Navi);
            readPeerUpdate(peer);

            // fill the buffer
            byte[] buffer = updateBuffer.First.Value;
            updateBuffer.Clear();
            for (ulong i = 3; i > 0; --i)
            {
                BitConverter.GetBytes(Program_Step - i).CopyTo(buffer, 0);
                byte[] newBuffer = new byte[NetNavi_Type.COMPACT_BUFFER_SIZE + 8];
                buffer.CopyTo(newBuffer, 0);
                updateBuffer.AddLast(newBuffer);
            }

            localObject.Set_Compact_buffer(buffer, 8);

            localObject = Navi_resources.Get_Data(localObject.NaviID, localObject.NAVIEXEID);

            // add to containers
            peers.Add(name, peer);
            Client_Navi.Add(name, localObject);

            ++peerCount;
            Console.WriteLine("Client " + name + " successfully added");
        }

        private void sendUpdate(Client peer, NetNavi_Type navi)
        {
            byte[] buffer = new byte[NetNavi_Type.COMPACT_BUFFER_SIZE + 8];
            BitConverter.GetBytes(Program_Step).CopyTo(buffer, 0);
            navi.Get_Compact_buffer().CopyTo(buffer, 8);
            peer.WriteByteArray(buffer);
        }

        private void readPeerUpdate(Client peer)
        {            
            byte[] buffer = peer.ReadByteArray(NetNavi_Type.COMPACT_BUFFER_SIZE + 8);            
            updateBuffer.AddLast(buffer);
        }

    }
}
