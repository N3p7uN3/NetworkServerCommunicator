using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkServerCommunicator
{
    public enum ConnectionState
	{
		Disconnected,
		Listening,
		Connected
	};

	public abstract class TCPComServerEvents
	{
		public delegate void PacketReadyEventHandler();
		public event PacketReadyEventHandler PacketReady;

		public delegate void ConnectionStatusChangedEventHandler(
			ConnectionState statusChange,
			string Details);
		public event ConnectionStatusChangedEventHandler ConnectionStatusChanged;
	}
	
	public interface TCPComServerInterface
    {
		//Reading packets
		
		string GetPacket();

		//Sending packets
		void SendPacket(string packet);
		void SendPackets(List<string> packets);

		

		void StartListening(
			int PortNumber,
			int KeepAlivePacketPeriod);
		void Stop();


    }
}
