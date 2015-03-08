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
        public enum Headers : int
        {
            SendingUpdate,
            Approved,
            Denied,
            Invalid,
            Error
        }

        private Dictionary<string, Client> peers = new Dictionary<string, Client>();
        private System.Net.Sockets.TcpListener listener = null;
        private bool networkActive = false;
        private const int PORT = 11994;
        private const int MAX_PEERS = 2;
        private int peerCount = 0;
        private string networkName;        

        //private bool isNetworkCaptain;
        //private IPEndPoint networkCaptainIP;

        public void StartNetwork(string name, int port = PORT)
        {
            if (networkActive)
                return;

            networkActive = true;
            networkName = name;
            if (listener == null)
                listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("listener started on port " + port + " with name " + name);
        }

        public void StopNetwork()
        {
            if (!networkActive)
                return;

            networkActive = false;
            listener.Stop();

            foreach (Client peer in peers.Values)
                peer.Close();
            peers.Clear();
            Client_Navi.Clear();
            peerCount = 0;
            Console.WriteLine("Network stopped");
        }

        public void DoNetworkEvents()
        {
            if (!networkActive)
                return;

            checkIncomingPeers();
            handlePeers();
        }

        public bool ConnectToPeer(string host)
        {
            if (peerCount == MAX_PEERS)
                return false;

            TcpClient c = new TcpClient(host, PORT);
            Client newPeer = new Client(c);

            Console.WriteLine("Connecting... writing name");
            newPeer.Write(networkName);

            if ((Headers)newPeer.ReadInt32() != Headers.Approved)
            {
                newPeer.Close();
                Console.WriteLine("our name was not approved");
                return false;
            }

            string name = newPeer.ReadString();
            if (peers.ContainsKey(name))
            {
                newPeer.Write((int)Headers.Invalid);
                newPeer.Close();
                Console.WriteLine("their name was taken");
                return false;
            }
            newPeer.Write((int)Headers.Approved);

            NetNavi_Type localObject = new NetNavi_Type();
            localObject = Navi_resources.Get_Data("Raven", 0);            
            
            // send first update
            sendUpdate(newPeer, Host_Navi);
            // receive first update
            readPeerUpdate(newPeer, localObject);

            // add to containers
            peers.Add(name, newPeer);
            Client_Navi.Add(name, localObject);

            ++peerCount;
            Console.WriteLine("Client " + name + " successfully added");
            Console.WriteLine(newPeer.RemoteIP);
            return true;
        }

        private void checkIncomingPeers()
        {
            Client newPeer;
            while (listener.Pending())
            {
                newPeer = new Client(listener.AcceptTcpClient());
                Console.WriteLine("Incomming Client");
                registerPeer(newPeer);
            }
        }

        private void registerPeer(Client newPeer)
        {
            string name = newPeer.ReadString(); // get the incoming connection's name
            Console.WriteLine("name is " + name);
            if (peers.ContainsKey(name)) // if we already have a user with the same name
            {
                newPeer.Write((int)Headers.Invalid);
                newPeer.Close();
                Console.WriteLine("name taken");
                return;
            }

            if (peerCount == MAX_PEERS) // full
            {
                newPeer.Write((int)Headers.Denied);
                newPeer.Close();
                Console.WriteLine("full");
                return;
            }

            newPeer.Write((int)Headers.Approved);
            Console.WriteLine("approved");

            newPeer.Write(networkName); // send our name
            if ((Headers)newPeer.ReadInt32() != Headers.Approved) // if they have a user with the same name
            {
                newPeer.Close();
                Console.WriteLine("our name was not approved");
                return;
            }

            NetNavi_Type localObject = new NetNavi_Type();
            localObject = Navi_resources.Get_Data("Raven", 0);            

            // send first update
            sendUpdate(newPeer, Host_Navi);
            // receive first update
            readPeerUpdate(newPeer, localObject);

            // add to containers
            peers.Add(name, newPeer);
            Client_Navi.Add(name, localObject);

            ++peerCount;
            Console.WriteLine("client " + name + " added");
            Console.WriteLine(newPeer.RemoteIP);
        }

        private void handlePeers()
        {
            Client peer;
            Headers request;
            HashSet<string> toRemove = new HashSet<string>();
            NetNavi_Type navi;

            foreach (string name in peers.Keys)
            {
                peer = peers[name];
                navi = Client_Navi[name];
                
                try
                {
                    // send update
                    peer.Write((int)Headers.SendingUpdate);
                    sendUpdate(peer, Host_Navi);

                    // read info
                    while (peer.Available >= 4) // 4 bytes is the size of a header (int32) from the Headers enum
                    {
                        request = (Headers)peer.ReadInt32();

                        if (request == Headers.SendingUpdate)
                        {
                            readPeerUpdate(peer, navi);
                        }
                    }
                }
                catch (System.IO.IOException) // other end disconnected
                {
                    toRemove.Add(name);
                    Client_Navi.Remove(name);
                    peer.Close();
                    --peerCount;
                    Console.WriteLine(name + " disconnected");
                }
            }

            // remove any peers who disconnected
            // (can't edit the container during iteration)
            foreach (string name in toRemove)
                peers.Remove(name);
        }

        private void sendUpdate(Client peer, NetNavi_Type navi)
        {            
            byte[] buffer = new byte[128];
            navi.Get_Compact_buffer().CopyTo(buffer, 5);
            peer.WriteSpecial(buffer);
        }

        public void readPeerUpdate(Client peer, NetNavi_Type navi)
        {            
            byte[] buffer = peer.ReadSpecial();
            navi.Set_Compact_buffer(buffer);
        }

    }
}
