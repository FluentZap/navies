using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
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
            PeerConnect,
            Approved,
            DeniedFull,
            RedirectToCaptian,
            Error,
            Bye
        }

        private const int DEFAULT_PORT = 53300 ;
        private const int MAX_PEERS = 15;

        private const int MAX_PACKET_BUFFER = 4;
        
        public Dictionary<string, Client> peers = new Dictionary<string, Client>();
        private Navi_Main NaviData;
        private System.Net.Sockets.TcpListener listener = null;
        private bool networkActive = false;
        private int peerCount = 0;
        private string networkName = null;
        private int listenerPort;
        public string networkCaptain = null; // if null, then we are the network captain
        private int pending_peers = 0;
        public bool NetworkHold;
        public int PacketAhead = 0;
        

        public string name
        {
            get { return networkName; }            
        }


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

            networkName = getUniqueNaviName(NaviData.Host_Navi.NaviID, NaviData.Host_Navi.NAVIEXEID);
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

            string name = newPeer.IPAddress + ":" + newPeer.Port; // figure out name            
            addPeer(name, newPeer); // add them to our peers

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
                addPeer(name, peer); // add them to our peers

            }
        }

        private void handlePeers()
        {
            bool update = true;
            string[] Keys = new string[peers.Keys.Count]; peers.Keys.CopyTo(Keys, 0);
            foreach (string name in Keys)
                if (!peers[name].isActive()) 
                    disconnectPeer(peers[name]);


            Keys = new string[peers.Keys.Count]; peers.Keys.CopyTo(Keys, 0);           
            foreach (string name in Keys)
                if (peers[name].Available >= 4)
                    handlePacket(peers[name]);

            Keys = new string[peers.Keys.Count]; peers.Keys.CopyTo(Keys, 0);
            foreach (string name in Keys)
                if (!peers[name].PendingUpdate) update = false;

            if (PacketAhead < MAX_PACKET_BUFFER)
                sendPeerUpdate();

            if (update)
            {
                advancePeers();
                NetworkHold = false;
            }
        }

        private void sendPeerUpdate()
        {
            string[] Keys = new string[peers.Keys.Count]; peers.Keys.CopyTo(Keys, 0);            
            foreach (string name in Keys)
            {                                
                packet_NaviUpdate(peers[name], NaviData.Host_Navi);
            }
            NaviData.Host_Navi.Activated_Ability = -1;
            PacketAhead++;
        }
        

        private void advancePeers()
        {            
            string[] Keys = new string[peers.Keys.Count]; peers.Keys.CopyTo(Keys, 0);
            foreach (string name in Keys)
            {
                if(NaviData.Client_Navi[name].NetBuffer.Count == 0)
                    peers[name].PendingUpdate = false;                
            }
            //if (!NaviData.Client_Navi[name].Initialised) { initialiseNaviClient(peers[name]); NaviData.Client_Navi[name].Initialised = true; }
                        
            NaviData.Advance_Clients();
            foreach (NetNavi_Type navi in NaviData.Client_Navi.Values)
                navi.Activated_Ability = -1;
            PacketAhead--;
        }


        private void initialiseNaviClient(Client peer, Navi_Name_ID NaviID, ulong NAVIEXEID)
        {            
            NetNavi_Type navi = Navi_resources.Get_Data(NaviID, NAVIEXEID);            
            navi.Initialised = true;
            NaviData.Client_Navi[peer.Name] = navi;
        }

        private void addPeer(string name, Client peer)
        {
            peer.Name = name;
            peers.Add(name, peer);            
            ++peerCount;
            if (!NaviData.Client_Navi.ContainsKey(peer.Name))
                NaviData.Client_Navi.Add(peer.Name, Navi_resources.Get_Data(Navi_Name_ID.Junker, 0)); //adds blank to fill later

            Console.WriteLine("Peer " + name + " successfully added");
        }

        private void changePeerName(string name, string newName)
        {            
            Client temp = peers[name];
            NetNavi_Type tempNavi = NaviData.Client_Navi[name];
            temp.Name = newName;            
            peers.Remove(name);
            peers.Add(newName, temp);            
            NaviData.Client_Navi.Remove(name);
            NaviData.Client_Navi.Add(newName, tempNavi);            
            Console.WriteLine("Changed " + name + " to " + newName);
        }
              
        private void disconnectPeer(Client peer)
        {
            peers.Remove(peer.Name);
            NaviData.Client_Navi.Remove(peer.Name);
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

            if (header == Headers.PeerConnect)
                handle_PeerConnect(peer);

            if (header == Headers.Approved)
                handle_Approved(peer);

            if (header == Headers.DeniedFull)
                disconnectPeer(peer);

            if (header == Headers.Bye)
                disconnectPeer(peer);

            if (!peer.Authenticated) return;

            if (header == Headers.NaviUpdate)
                handle_NaviUpdate(peer);

            if (header == Headers.GroupAppend)
                handle_GroupAppend(peer);
            
        }

        
        //Captain should only call this
        private string getUniqueNaviName(Navi_Name_ID name, ulong EXEID)
        {
            string peerName = name.ToString() + EXEID.ToString();
            string hostName = NaviData.Host_Navi.ToString() + NaviData.Host_Navi.NAVIEXEID.ToString();
            
            for(int x = 0; x < 500; x++)
                if (!peers.ContainsKey(peerName + "-" + x.ToString()))
                    if (networkName != (peerName + "-" + x.ToString()))
                        return peerName + "-" + x.ToString();

            return null;
        }

        #region RecievePackets

        private void handle_RedirectToCaptian(Client peer)
        {
            string host = peer.ReadString(); // receive the captain ip
            int port = peer.ReadInt32(); // receive the captain port
            Console.WriteLine("Redirecting to captain at " + host + ":" + port);
            disconnectPeer(peer);
            
            Client newPeer = new Client(new TcpClient(host, port));
            
            string name = newPeer.IPAddress + ":" + newPeer.Port; // figure out name
            addPeer(name, newPeer); // add them to our peers
            
            if (!packet_RequestJoin(newPeer)) return;
        }

        private void handle_RequestJoin(Client peer)
        {
            if (networkCaptain != null) // we are not the captain
                if (!packet_RedirectToCaptian(peer)) { disconnectPeer(peer); return; }                
            
            if (peerCount == MAX_PEERS) // full
                if (!packet_DeniedFull(peer)) { disconnectPeer(peer); return; }
            
            int port = peer.ReadInt32(); // read port
            Navi_Name_ID name = (Navi_Name_ID)peer.ReadInt32(); // read name
            ulong EXEID = peer.ReadUInt64(); // read EXEID           

            string peerName = getUniqueNaviName(name, EXEID);
            if (peerName == null) { disconnectPeer(peer); return; }

            if (!packet_ApproveClient(peer, peerName, port)) return; // Approve and send them their name
            
            changePeerName(peer.Name, peerName);
            initialiseNaviClient(peer, name, EXEID);
            Console.WriteLine("Client " + peerName + " at " + peer.IPAddress + " Authenticated");
            peer.Authenticated = true;
        }

        private void handle_Approved(Client peer)
        {
            networkName = peer.ReadString(); // read our network name
            networkCaptain = peer.ReadString(); // read captains name

            Navi_Name_ID name = (Navi_Name_ID)peer.ReadInt32(); // read name
            ulong EXEID = peer.ReadUInt64(); // read EXEID           

            NaviData.Program_Step = peer.ReadUInt64(); // read the current program step            
            pending_peers = peer.ReadInt32(); // read number of soon-to-be incoming connections            
            initialiseNaviClient(peer, name, EXEID);

            Console.WriteLine("Connected to host " + peer.IPAddress);
            peer.Authenticated = true;
            changePeerName(peer.Name, networkCaptain);
            //Send first update to captain
            packet_NaviUpdate(peer, NaviData.Host_Navi);
        }

        private void handle_GroupAppend(Client peer)
        {
            string ip = peer.ReadString();
            int port = peer.ReadInt32();
            string newPeerName = peer.ReadString();
            Client newPeer = new Client(new TcpClient(ip, port));
            addPeer(newPeerName, newPeer);            
            packet_PeerConnect(newPeer);
            newPeer.Authenticated = true;
        }

        private void handle_NaviUpdate(Client peer)
        {
            byte[] buffer = peer.ReadByteArray(Navi_Main.COMPACT_BUFFER_SIZE);            
            NaviData.Client_Navi[peer.Name].write_netBuffer(buffer, 0);            
            peer.PendingUpdate = true;
        }

        private void handle_PeerConnect(Client peer)
        {
            string newPeerName = peer.ReadString();            
            changePeerName(peer.Name, newPeerName);            
            
            //Get first update
            byte[] buffer = peer.ReadByteArray(Navi_Main.COMPACT_BUFFER_SIZE);                        
            NaviData.Client_Navi[peer.Name].write_netBuffer(buffer, 0);
            peer.PendingUpdate = true;
            
            //initialise the navi
            initialiseNaviClient(peer, NaviData.Client_Navi[peer.Name].NetBuffer.Peek().NaviID, NaviData.Client_Navi[peer.Name].NetBuffer.Peek().NAVIEXEID);

            //Send update
            packet_NaviUpdate(peer, NaviData.Host_Navi);
            peer.Authenticated = true;
            Console.WriteLine("Peer " + peer.Name + " at " + peer.IPAddress + " Authenticated");
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
                peer.Write(peers[networkCaptain].IPAddress);
                peer.Write(peers[networkCaptain].Port);
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
                peer.Write(peer.Name);
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
                // send client our (The captians) name ,NaviID, EXEID
                peer.Write(networkName);                
                peer.Write((int)NaviData.Host_Navi.NaviID);
                peer.Write(NaviData.Host_Navi.NAVIEXEID);                

                // send the current program step
                peer.Write(NaviData.Program_Step);
                // send number of peers who are about to connect
                peer.Write(peerCount);                                
                
                peer.Flush();
                
                

                // tell the peers to connect
                foreach (Client clientpeers in peers.Values)
                    if (clientpeers != peer)
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
                navi.get_netBuffer().CopyTo(buffer, 0);
                peer.Write((byte)Headers.NaviUpdate);
                peer.WriteByteArray(buffer);
                peer.Flush();
                //Console.WriteLine("Peer Update Sent to: " + peer.Name);
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
                peer.Write((int)NaviData.Host_Navi.NaviID); // send our NaviName
                peer.Write(NaviData.Host_Navi.NAVIEXEID); // send our NaviEXEID
                peer.Flush();
                Console.WriteLine("Connecting to " + peer.IPAddress);
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }
        }

        private bool packet_PeerConnect(Client peer)
        {
            try
            {
                peer.Write((byte)Headers.PeerConnect);
                peer.Write(networkName); // send our name
                
                //send first navi update
                byte[] buffer = new byte[Navi_Main.COMPACT_BUFFER_SIZE];                
                NaviData.Host_Navi.get_netBuffer().CopyTo(buffer, 0);
                peer.WriteByteArray(buffer);

                peer.Flush();
                Console.WriteLine("Captain told us to connect to peer " + peer.IPAddress);
                return true;
            }
            catch (SocketException)
            { Console.WriteLine("Could not send Packet"); disconnectPeer(peer); return false; }

        }

        #endregion

    }
}