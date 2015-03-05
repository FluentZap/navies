using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
namespace Net_Navis
{
	//This module handles all the network interaction


	//Packet Layout
	//All packets
	//4 byte Program step
	//1 byte Packet type

	//Full sync

	//CommandSend

	public enum Packet_Type : byte
		{
			FullSync = 0,
			CommandSend = 1
		}


	partial class Navi_Main
	{


		


		private System.Net.Sockets.TcpListener Net_Host;
		private System.Net.Sockets.TcpClient Net_Client;

		private bool IsClient;

		private Dictionary<int, Client_Type> Client_List = new Dictionary<int, Client_Type>();

		public class Client_Type
		{
			public bool ReSync;
			public System.Net.Sockets.TcpClient Socket;

			public NetNavi_Type Client_Navi;
			public Client_Type(System.Net.Sockets.TcpClient Socket)
			{
				this.Socket = Socket;
				ReSync = true;
				Client_Navi = new NetNavi_Type();
			}

		}


		public bool Initialise_Network()
		{
			//Check for host if none start host
			try {
				Connect_As_Client();
				Console.WriteLine("Connected");
				return true;
			} catch {
				try {
					Host();
					Console.WriteLine("Hosted");
				} catch (Exception ex) {
					return false;
				}
				return true;
			}
			return false;
		}


		private void Connect_As_Client()
		{
			System.Net.IPAddress ip = System.Net.IPAddress.Parse("127.0.0.1");
			Net_Client = new System.Net.Sockets.TcpClient();
			Net_Client.Connect(ip, 52525);
			Console.WriteLine("Connecting");
			IsClient = true;
		}


		private void Host()
		{
			System.Net.IPAddress ip = System.Net.IPAddress.Parse("127.0.0.1");
			Net_Host = new System.Net.Sockets.TcpListener(ip, 52525);
			Net_Host.Start();
			Console.WriteLine("Hosting");
			IsClient = false;
		}

		public void CheckForConnections()
		{

			while (!(Net_Host.Pending() == false)) {
				int ID = 0;
				for (ID = 0; ID <= 1000; ID++) {
					if (!Client_List.ContainsKey(ID))
						break; // TODO: might not be correct. Was : Exit For
					if (ID == 1000)
						return;
				}
				Client_List.Add(ID, new Client_Type(Net_Host.AcceptTcpClient()));
			}
		}



		public void Handle_Clients()
		{
			foreach (KeyValuePair<int, Navi_Main.Client_Type> Client in Client_List) {				
				if (Client.Value.ReSync == true) {
					ServerResync(Client.Value.Socket);
					//Client.Value.ReSync = False
				}
			}
		}


		public void Update_To_Host()
		{
			if (Net_Client.Client.Available > 0) {
				byte[] b = new byte[72];
				Net_Client.GetStream().Read(b, 0, 71);
				Host_Navi.Set_Compact_buffer(b);
			}

		}


		public void DoNetworkEvents()
		{
			if (IsClient == false) {
				CheckForConnections();
				Handle_Clients();
			}

			if (IsClient == true) {
				Update_To_Host();
			}

		}



		public void ServerResync(System.Net.Sockets.TcpClient Socket)
		{
			byte[] Buffer = new byte[72];
			//Convert Data
			BitConverter.GetBytes(Program_Step).CopyTo(Buffer, 0);
			Buffer[4] = (byte)Packet_Type.FullSync;
			Host_Navi.Get_Compact_buffer().CopyTo(Buffer, 5);
			//65 bytes
			//Send Data
			//Socket.GetStream.BeginWrite(Buffer, 0, Buffer.Length, Nothing, Nothing)

			Socket.GetStream().Write(Buffer, 0, Buffer.Length - 1);
		}


	}
}
