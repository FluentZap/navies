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
            Bye,
            Approved,
            Denied,
            Invalid,
            Error
        }

        private Dictionary<string, Client> peers = new Dictionary<string, Client>();
        private System.Net.Sockets.TcpListener listener = null;
        private bool networkActive = false;
        private static const int PORT = 11994;
        private static const int MAX_PEERS = 1;
        private int peerCount = 0;
        private string networkName;

        public void StartNetwork(string name)
        {
            if (networkActive)
                return;

            networkActive = true;
            networkName = name;
            if (listener == null)
                listener = new TcpListener(IPAddress.Any, PORT);
            listener.Start();
        }

        public void StopNetwork()
        {
            if (!networkActive)
                return;

            networkActive = false;
            listener.Stop();

            foreach (Client peer in peers.Values)
            {
                peer.Write((int)Headers.Bye);
                peer.Close();
            }
            peers = new Dictionary<string, Client>();
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
            newPeer.Write(networkName);

            if ((Headers)newPeer.ReadInt32() != Headers.Approved)
            {
                newPeer.Close();
                return false;
            }

            string name = newPeer.ReadString();
            if (peers.ContainsKey(name))
            {
                newPeer.Write((int)Headers.Invalid);
                newPeer.Close();
                return false;
            }

            peers.Add(name, newPeer);
            ++peerCount;
            return true;
        }

        private void checkIncomingPeers()
        {
            Client newPeer;
            while (listener.Pending())
            {
                newPeer = new Client(listener.AcceptTcpClient());
                registerPeer(newPeer);
            }
        }

        private void registerPeer(Client newPeer)
        {
            string name = newPeer.ReadString(); // get the incoming connection's name

            if (peers.ContainsKey(name)) // if we already have a user with the same name
            {
                newPeer.Write((int)Headers.Invalid);
                newPeer.Close();
                return;
            }

            if (peerCount == MAX_PEERS) // full
            {
                newPeer.Write((int)Headers.Denied);
                newPeer.Close();
                return;
            }

            newPeer.Write((int)Headers.Approved);
            newPeer.Write(networkName); // send our name
            if ((Headers)newPeer.ReadInt32() != Headers.Approved) // if they have a user with the same name
            {
                newPeer.Close();
                return;
            }

            peers.Add(name, newPeer);
            ++peerCount;
        }

        private void handlePeers()
        {
            Client peer;
            Headers request;
            HashSet<string> toRemove = new HashSet<string>();

            foreach (string name in peers.Keys)
            {
                peer = peers[name];
                
                // send update
                peer.Write((int)Headers.SendingUpdate);
                sendUpdate(peer);

                // read info
                while (peer.Available >= 4) // 4 bytes is the size of a header (int32) from the Headers enum
                {
                    request = (Headers)peer.ReadInt32();

                    if (request == Headers.Bye)
                    {
                        toRemove.Add(name);
                        peer.Close();
                        break;
                    }
                    else if (request == Headers.SendingUpdate)
                    {
                        readPeerUpdate(peer);
                    }
                }
            }

            // remove anyone who disconnected
            foreach (string name in toRemove)
                peers.Remove(name);
        }

        private void sendUpdate(Client peer)
        {
            byte[] buffer = new byte[72];
            Host_Navi.Get_Compact_buffer().CopyTo(buffer, 5);
            peer.WriteSpecial(buffer);
        }

        public void readPeerUpdate(Client peer)
        {
            byte[] buffer = peer.ReadSpecial();
            Other_Navi.Set_Compact_buffer(buffer);
        }

    }
}
