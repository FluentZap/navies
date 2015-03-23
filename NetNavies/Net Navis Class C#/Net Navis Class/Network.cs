using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Net_Navis
{
    public class Navi_Network_TCP
    {        


        enum Headers : byte
        {
            NaviUpdate,
            RequestJoin,
            GroupAppend,
            Approved,
            DeniedFull,
            RedirectToCaptian,
            Error,
            Bye
        }

        private const int DEFAULT_PORT = 51300;
        private const int MAX_PEERS = 15;
        
        public Dictionary<string, Client> peers = new Dictionary<string, Client>();
        private Navi_Main NaviData;
        private System.Net.Sockets.TcpListener listener = null;
        private bool networkActive = false;
        private int peerCount = 0;
        private string networkName = null;
        private int listenerPort;
        public string networkCaptain = null; // if null, then we are the network captain
        private int pending_peers = 0;



        public Navi_Network_TCP(Navi_Main Data)
        {
            NaviData = Data;
            StartNetwork();
        }

        
        public bool StartNetwork()
        {
            if (networkActive)
                return true;
            //Find an open port
            if (listener == null)
                listenerPort = findOpenPort(DEFAULT_PORT);
            //If no port can be found return
            if (listenerPort == -1)
            {Console.WriteLine("No available port found in port range " + DEFAULT_PORT + " To " + DEFAULT_PORT + 500); networkActive = false; return false;}
                   
            listener.Start(MAX_PEERS); // set backlog size to MAX_PEERS 
            Console.WriteLine("listener started on port " + listenerPort);            
            
            networkActive = true;
            return true;
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
                if (port >= (DEFAULT_PORT + 500)) return -1;
                return findOpenPort(port + 1);
            }
        }

        public void StopNetwork()
        {
            if (!networkActive)
                return;

            networkActive = false;
            listener.Stop();

            foreach (Client peer in peers.Values)
                packet_Bye(peer);
            peers.Clear();     
            peerCount = 0;
            networkCaptain = null;
            Console.WriteLine("Network stopped");
        }     
        
        //add update loop

        public void DoNetworkEvents()
        {
            if (!networkActive)
                return;
            
            handleIncomingPeers();
            handlePeers();
        }

        public void ConnectToPeer(string host, int port)
        {
            if (!networkActive || peerCount > 0)
                return;
            requestNetworkJoin(host, port);
        }

        private void requestNetworkJoin(string host, int port)
        {
            Client newPeer;
            try
            {
                newPeer = new Client(new TcpClient(host, port));
            }
            catch (SocketException)
            {
                Console.WriteLine("Connection timed out");
                return;
            }
            if (!packet_RequestJoin(newPeer)) return;            
        }

        private void handleIncomingPeers()
        {
            Client peer;            
            while (listener.Pending())
            {
                peer = new Client(listener.AcceptTcpClient());
                Console.WriteLine("incomming client"); 
                string name = peer.IPAddress + ":" + peer.Port; // figure out name                
                peers.Add(name, peer); // add them to our peers
                peerCount++;
            }
        }

        private void handlePeers()
        {
            Dictionary<string, Client>.KeyCollection Keys = peers.Keys;
            bool update = true;
            foreach (string name in Keys)
            {
                if (peers[name].Available > 0)
                    handlePacket(peers[name]);
                if (!peers[name].PendingUpdate) update = false;
            }            
            

            if (update)
            {
                foreach (string name in Keys)
                {
                    if (!NaviData.Client_Navi[name].Initialised) { initialiseNaviClient(peers[name]); NaviData.Client_Navi[name].Initialised = true; }
                    peers[name].PendingUpdate = false;
                    packet_NaviUpdate(peers[name], NaviData.Host_Navi);
                }
                NaviData.Advance_Clients();
            }            
        }

        private void initialiseNaviClient(Client peer)
        {
            NetNavi_Network_Type buffer = NaviData.Client_Navi[peer.Name].NetBuffer;
            NetNavi_Type navi = Navi_resources.Get_Data(buffer.NaviID, buffer.NAVIEXEID);
            buffer.ProcessBuffer(navi);
            NaviData.Client_Navi[peer.Name] = navi;
        }


        private void addPeer(string name, Client peer)
        {            
            peers.Add(name, peer);
            ++peerCount;
            Console.WriteLine("Client " + name + " successfully added");
        }
              
        private void disconnectPeer(Client peer)
        {
            peers.Remove(peer.Name);
            peer.Close();
            --peerCount;
            // if it was the captain who disconnected, select new captain alphabetically            
            if (peer.Name == networkCaptain)
            {
                string lowest = null;
                foreach (string s in NaviData.Client_Navi.Keys)
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
            Console.WriteLine(peer.Name + " disconnected");
        }


        private void handlePacket(Client peer)
        {
            Headers header;

            header = (Headers)peer.ReadByte();

            if (header == Headers.RedirectToCaptian) // we did not connect to the captain
                handle_RedirectToCaptian(peer);
            
            if (header == Headers.RequestJoin)
                handle_RequestJoin(peer);

            if (!peer.Authenticated) return;

            if (header == Headers.Approved)
                handle_Approved(peer);

            if (header == Headers.NaviUpdate)
                handle_NaviUpdate(peer);

            if (header == Headers.GroupAppend)
                handle_GroupAppend(peer);

            if (header == Headers.Bye)
                disconnectPeer(peer);
        }


        #region RecievePackets

        private void handle_RedirectToCaptian(Client peer)
        {
            string host = peer.ReadString(); // receive the captain ip
            int port = peer.ReadInt32(); // receive the captain port
            Console.WriteLine("Redirecting to captain at " + host + ":" + port);
            peer.Close();

            peer = new Client(new TcpClient(host, port));
            if (!packet_RequestJoin(peer)) return;
        }

        private void handle_RequestJoin(Client peer)
        {
            if (networkCaptain != null) // we are not the captain
                if (!packet_RedirectToCaptian(peer)) { disconnectPeer(peer); return; }                
            
            if (peerCount == MAX_PEERS) // full
                if (!packet_DeniedFull(peer)) { disconnectPeer(peer); return; }
            
            int port = peer.ReadInt32(); // read port
            string name = peer.IPAddress + ":" + peer.Port; // figure out name                
            if (!packet_ApproveClient(peer, name, port)) return; // Approve and send them their name
            Console.WriteLine("Client " + peer.IPAddress + " Authenticated");
            peer.Authenticated = true;
        }

        private void handle_Approved(Client peer)
        {
            networkName = peer.ReadString(); // read our network name                    
            pending_peers = peer.ReadInt32(); // read number of soon-to-be incoming connections
            networkCaptain = peer.IPAddress + ":" + peer.Port; // set them as captain            
            Console.WriteLine("Connected to host" + peer.IPAddress);            
            addPeer(networkCaptain, peer);
        }

        private void handle_GroupAppend(Client peer)
        {
            string ip = peer.ReadString();
            int port = peer.ReadInt32();
            string newPeerName = ip + ":" + port;
            Client newPeer = new Client(new TcpClient(ip, port));
            addPeer(newPeerName, newPeer);            
            pending_peers--;
        }

        private void handle_NaviUpdate(Client peer)
        {
            byte[] buffer = peer.ReadByteArray(Navi_Main.COMPACT_BUFFER_SIZE);
            if (!NaviData.Client_Navi.ContainsKey(peer.Name))
                NaviData.Client_Navi.Add(peer.Name, new NetNavi_Type()); //adds blank to fill later
            NaviData.Client_Navi[peer.Name].Set_Navi_Network(buffer, 0);            
            peer.PendingUpdate = true;
        }

        #endregion


        #region SendPackets
        //Packet ends connection
        private bool packet_Bye(Client peer)
        {
            try
            {
                peer.Write((byte)Headers.Bye);
                peer.Flush();
                peer.Close();
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
        }

        private bool packet_RedirectToCaptian(Client peer)
        {
            try
            {
                peer.Write((byte)Headers.RedirectToCaptian);
                string[] s = networkCaptain.Split(':');
                peer.Write(s[0]); // send ip address of captain
                peer.Write(Convert.ToInt32(s[1])); // send port
                peer.Flush();
                peer.Close();
                Console.WriteLine(peer.IPAddress + " referred to captain");
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
            
        }

        private bool packet_DeniedFull(Client peer)
        {
            try
            {
            peer.Write((byte)Headers.DeniedFull);
            peer.Flush();
            peer.Close();
            Console.WriteLine(peer.IPAddress + " Denied connection, connection limit reached");
            return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
        }

        private bool packet_GroupAppend(Client peer, int port)
        {
            try
            {                
                peer.Write((byte)Headers.GroupAppend);
                peer.Write(peer.IPAddress);
                peer.Write(port);
                peer.Flush();
                packet_NaviUpdate(peer, NaviData.Host_Navi);
                Console.WriteLine("Peer Update Sent to: " + peer.IPAddress);
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
        }

        private bool packet_ApproveClient(Client peer, string name, int port)
        {
            try
            {                                
                // send Approved Packet and Clients name
                peer.Write((byte)Headers.Approved);
                // send client it's name
                peer.Write(name);                
                // send number of peers who are about to connect
                peer.Write(peerCount);
                peer.Flush();

                // tell the peers to connect
                foreach (Client clientpeers in peers.Values)
                    if (!packet_GroupAppend(clientpeers, port)) return false;                
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
        }

        private bool packet_NaviUpdate(Client peer, NetNavi_Type navi)
        {
            try
            {
                byte[] buffer = new byte[Navi_Main.COMPACT_BUFFER_SIZE];
                navi.Get_Navi_Network().CopyTo(buffer, 0);
                peer.Write((byte)Headers.NaviUpdate);
                peer.WriteByteArray(buffer);
                peer.Flush();
                Console.WriteLine("Peer Update Sent to: " + peer.IPAddress);
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }

        }

        private bool packet_RequestJoin(Client peer)
        {
            try
            {
                peer.Write((byte)Headers.RequestJoin);
                peer.Write(listenerPort); // send listener port
                peer.Flush();
                Console.WriteLine("Connecting to " + peer.IPAddress);
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
        }

        #endregion

    }
}