using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace NetworkServerCommunicator
{
	public class TCPMessageServer : TCPComServerEvents, TCPComServerInterface
	{
		

		#region Class member variables

		TcpListener mTcpListener;
		Socket mSocket;
		IPAddress mLocalMachineIPAddr;
		AsyncCallback mAcceptSocketCallback;

		//====================THREAD UNSAFE OBJECTS====================
		NetworkStream mNetStream;
		//=============================================================

		#endregion

		#region Static methods

		private static string GetLocalIP()
		{
			IPHostEntry host;
			string localIP = "?";
			host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily.ToString() == AddressFamily.InterNetwork.ToString())
				{
					localIP = ip.ToString();
				}
			}
			return localIP;
		}

		#endregion

		#region Class constructor

		public TCPMessageServer()
		{
			string localIP = GetLocalIP();

			if (localIP.Equals("?"))
				throw new Exception("Could not get the local machine's IP");

			mLocalMachineIPAddr = IPAddress.Parse(localIP);
		}

		#endregion

		#region Class methods

		private void BeginSocketAccepting()
		{
			mAcceptSocketCallback = new AsyncCallback(AcceptSocketCallback);

			mTcpListener.BeginAcceptSocket(mAcceptSocketCallback, mTcpListener);
		}

		private void AcceptSocketCallback(IAsyncResult ar)
		{

		}

		#endregion

		#region TCPComServerInterfaces

		public string GetPacket()
		{
			throw new NotImplementedException();
		}

		public void SendPacket(string packet)
		{
			throw new NotImplementedException();
		}

		public void SendPackets(List<string> packets)
		{
			throw new NotImplementedException();
		}

		public void StartListening(int PortNumber, int KeepAlivePacketPeriod)
		{
			mTcpListener = new TcpListener(mLocalMachineIPAddr, PortNumber);
			
			mTcpListener.Start();
			BeginSocketAccepting();
		}

		public void Stop()
		{

		}

		#endregion
	}
}
