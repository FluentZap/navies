using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Net_Navis
{
    partial class Navi_Main
    {
        enum Headers : int
        {
            SendingUpdate,
            RequestJoin,
            GroupAppend,
            Approved,
            Denied,
            Invalid,
            Error
        }

        private Dictionary<string, Client> peers = new Dictionary<string, Client>();
        private Dictionary<Client, int> peerListeningPorts = new Dictionary<Client, int>();
        private System.Net.Sockets.TcpListener listener = null;
        private bool networkActive = false;
        private const int DEFAULT_PORT = 11994;
        private const int MAX_PEERS = 4;
        private int peerCount = 0;
        private string networkName;
        private int listenerPort;
        private Client networkCaptain = null; // if null, then we are the network captain

        void StartNetwork(string name, int port = DEFAULT_PORT)
        {
            if (networkActive)
                return;

            networkActive = true;
            networkName = name;
            listenerPort = port;
            if (listener == null)
                listener = new TcpListener(IPAddress.Any, port);
            listener.Start(MAX_PEERS); // set backlog size to MAX_PEERS 
            Console.WriteLine("listener started on port " + port + " with name " + name);
        }

        void StopNetwork()
        {
            if (!networkActive)
                return;

            networkActive = false;
            listener.Stop();

            foreach (Client peer in peers.Values)
                peer.Close();

            peers.Clear();
            Client_Navi.Clear();
            peerListeningPorts.Clear();
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
            newPeer.Write((int)Headers.RequestJoin);
            Headers response = (Headers)newPeer.ReadInt32();

            if (response == Headers.Invalid) // we did not connect to the captain
            {
                Console.WriteLine("redirected to captain");
                host = newPeer.ReadString(); // receive the captain ip
                port = newPeer.ReadInt32(); // receive the captain port
                newPeer.Close();
                newPeer = new Client(new TcpClient(host, port));
                newPeer.Write((int)Headers.RequestJoin);
                response = (Headers)newPeer.ReadInt32();
            }

            if (response != Headers.Approved) // group was full
            {
                newPeer.Close();
                return false;
            }

            newPeer.Write(networkName); // send our name
            newPeer.Write(listenerPort); // send listener's port

            if ((Headers)newPeer.ReadInt32() != Headers.Approved) // name was taken
            {
                newPeer.Close();
                return false;
            }

            // read number of soon-to-be incoming connections
            int incomingConnectionCount = newPeer.ReadInt32();

            // wait for pending connections
            Client c;
            HashSet<Client> otherPending = new HashSet<Client>();
            while (incomingConnectionCount > 0)
            {
                c = new Client(listener.AcceptTcpClient());
                response = (Headers)c.ReadInt32();
                if (response == Headers.GroupAppend)
                {
                    addPeer(c.ReadString(), c, c.ReadInt32());
                    incomingConnectionCount -= 1;
                }
                else if (response == Headers.RequestJoin)
                    otherPending.Add(c); // deal with these later
                else
                    c.Close();
            }

            // lastly, add the captain we are talking to
            addPeer(newPeer.ReadString(), newPeer, port);
            networkCaptain = newPeer; // set them as captain

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

                request = (Headers)newPeer.ReadInt32();
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
                newPeer.Write((int)Headers.Invalid);
                newPeer.Write(networkCaptain.IPAddress); // send ip address of captain
                newPeer.Write(peerListeningPorts[networkCaptain]); // send port too
                newPeer.Close();
                Console.WriteLine("referred to captain");
                return false;
            }

            if (peerCount == MAX_PEERS) // full
            {
                newPeer.Write((int)Headers.Denied);
                newPeer.Close();
                return false;
            }

            newPeer.Write((int)Headers.Approved);

            // read name
            string name = newPeer.ReadString();
            // read port
            int port = newPeer.ReadInt32();

            // check to see if the name is taken
            if (peers.ContainsKey(name) || name == networkName) // name is taken
            {
                newPeer.Write((int)Headers.Invalid);
                newPeer.Close();
                return false;
            }

            newPeer.Write((int)Headers.Approved);

            // send number of peers who are about to connect
            newPeer.Write(peerCount);

            // tell the peers to connect
            foreach (Client peer in peers.Values)
            {
                peer.Write((int)Headers.GroupAppend);
                peer.Write(name);
                peer.Write(newPeer.IPAddress);
                peer.Write(port);
            }

            // send our name
            newPeer.Write(networkName);

            // add them to our peers
            addPeer(name, newPeer, port);

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
                    // send our update
                    peer.Write((int)Headers.SendingUpdate);
                    sendUpdate(peer, Host_Navi);

                    // read info
                    while (peer.Available >= 4) // 4 bytes is the size of a header (int32) from the Headers enum
                    {
                        request = (Headers)peer.ReadInt32();

                        if (request == Headers.SendingUpdate)
                        {
                            // read update
                            readPeerUpdate(peer, navi);
                        }
                        else if (request == Headers.GroupAppend)
                        {
                            string newPeerName = peer.ReadString();
                            string ip = peer.ReadString();
                            int port = peer.ReadInt32();
                            Client newPeer = new Client(new TcpClient(ip, port));
                            newPeer.Write((int)Headers.GroupAppend);
                            newPeer.Write(networkName);
                            newPeer.Write(listenerPort);
                            toAdd.Add(newPeerName, newPeer);
                        }
                    }
                }
                catch (System.IO.IOException) // other end disconnected
                {
                    toRemove.Add(name);
                    Client_Navi.Remove(name);
                    peerListeningPorts.Remove(peer);
                    peer.Close();
                    --peerCount;
                    // if it was the captain who disconnected, select new captain alphabetically
                    if (networkCaptain == peer)
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
                            networkCaptain = peers[lowest];
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
                addPeer(name, toAdd[name], toAdd[name].Port);
        }

        private void addPeer(string name, Client peer, int port)
        {
            NetNavi_Type localObject = new NetNavi_Type();
            // send AND receive first update (for draw function)
            sendUpdate(peer, Host_Navi);
            readPeerUpdate(peer, localObject);
            
            localObject = Navi_resources.Get_Data(localObject.NaviID, localObject.NAVIEXEID);

            //// send update again to salt it, baby
            //peer.Write((int)Headers.SendingUpdate);
            //sendUpdate(peer, Host_Navi);

            // add to containers
            peers.Add(name, peer);
            Client_Navi.Add(name, localObject);
            peerListeningPorts.Add(peer, port);

            ++peerCount;
            Console.WriteLine("Client " + name + " successfully added");
            Console.WriteLine(peer.IPAddress);
        }

        private void sendUpdate(Client peer, NetNavi_Type navi)
        {            
            byte[] buffer = new byte[128];
            navi.Get_Compact_buffer().CopyTo(buffer, 5);
            peer.WriteSpecial(buffer);
        }

        private void readPeerUpdate(Client peer, NetNavi_Type navi)
        {            
            byte[] buffer = peer.ReadSpecial();
            navi.Set_Compact_buffer(buffer);
        }

    }
}
