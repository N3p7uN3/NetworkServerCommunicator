using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkServerCommunicator;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace NetworkServerCommunicatorTests
{
	#region Unit test class

	public class TestNetworkStreamSender : NetworkStreamSender
	{
		private List<string> mMockSendPackets;
		private int mTickCountAtBeginningofEndOfThread;

		public TestNetworkStreamSender(
			NetworkStream netStream,
			int endOfPacketChar,
			Encoding packetEncoding)
			: base(
			netStream,
			endOfPacketChar,
			packetEncoding)
		{
			mMockSendPackets = new List<string>();
		}

		protected override void BeforeThreadSignalsComplete()
		{

			Thread.Sleep(2000);
		}

		protected override void SendPacket(string packet)
		{
			Monitor.Enter(mMockSendPackets);

			mMockSendPackets.Add((string)packet.Clone());

			Monitor.Exit(mMockSendPackets);
		}

		public bool IsPacketSent(string packet)
		{
			Monitor.Enter(mMockSendPackets);
			
			foreach (string tmp in mMockSendPackets)
				if (tmp.Equals(packet))
				{
					Monitor.Exit(mMockSendPackets);
					
					return true;
				}

			Monitor.Exit(mMockSendPackets);
			return false;
		}

		public int GetTotalPacketsSent()
		{
			Monitor.Enter(mMockSendPackets);

			int sent = mMockSendPackets.Count;

			Monitor.Exit(mMockSendPackets);

			return sent;
		}

	}

	#endregion

	[TestClass]
	public class NetworkStreamSenderUnitTests
	{
		TestNetworkStreamSender mNSS;
		
		public NetworkStreamSenderUnitTests()
		{
			

		}

		private void Instantiate()
		{
			mNSS = new TestNetworkStreamSender(
				null,
				(int)'\n',
				new System.Text.UTF8Encoding());
		}
		
		[TestMethod]
		public void TestSendsPackets()
		{
			Instantiate();

			mNSS.EnqueuePacket("packet1", false);
			mNSS.EnqueuePacket("packet2", false);

			Assert.AreEqual(2, mNSS.GetTotalPacketsSent());
			Assert.IsTrue(mNSS.IsPacketSent("packet1"));
			Assert.IsTrue(mNSS.IsPacketSent("packet2"));



		}

		[TestMethod]
		public void TestWaitForStop()
		{
			Instantiate();
			int tickCountBeforeStopping = Environment.TickCount;

			mNSS.Stop();

			int diff = Environment.TickCount - tickCountBeforeStopping;

			Assert.IsTrue(diff >= 1800);
		}
	}
}
