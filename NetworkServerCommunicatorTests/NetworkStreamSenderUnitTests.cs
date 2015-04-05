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
		public const int ArtificialDelayms = 100;
		
		private List<string> mMockSendPackets;
		private int mTickCountAtBeginningofEndOfThread;
		private bool mArtificialDelay = false;

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

		public void EnableArtificialDelays()
		{
			mArtificialDelay = true;
		}

		protected override void BeforeThreadSignalsComplete()
		{
			if (mArtificialDelay)
				Thread.Sleep(ArtificialDelayms);
		}

		protected override void AfterSendingPackets()
		{
			if (mArtificialDelay)
				Thread.Sleep(ArtificialDelayms);
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
		public void TestSendsPacketsWithoutWaiting()
		{
			Instantiate();
			mNSS.EnableArtificialDelays();

			mNSS.EnqueuePacket("packet1", false);
			mNSS.EnqueuePacket("packet2", false);

			Thread.Sleep(TestNetworkStreamSender.ArtificialDelayms + 10);

			Assert.AreEqual(2, mNSS.GetTotalPacketsSent());
			Assert.IsTrue(mNSS.IsPacketSent("packet1"));
			Assert.IsTrue(mNSS.IsPacketSent("packet2"));

			mNSS.Stop();

		}

		[TestMethod]
		public void TestWaitingToSendPacket()
		{
			Instantiate();
			mNSS.EnableArtificialDelays();

			int start = Environment.TickCount;
			mNSS.EnqueuePacket("packet1", true);

			int diff = Environment.TickCount - start;

			Assert.IsTrue(diff >= (TestNetworkStreamSender.ArtificialDelayms - 20));
			Assert.AreEqual(1, mNSS.GetTotalPacketsSent());
			Assert.IsTrue(mNSS.IsPacketSent("packet1"));

			mNSS.Stop();

		}

		[TestMethod]
		public void TestWaitForStop()
		{
			Instantiate();
			mNSS.EnableArtificialDelays();

			int tickCountBeforeStopping = Environment.TickCount;

			mNSS.Stop();

			int diff = Environment.TickCount - tickCountBeforeStopping;

			Assert.IsTrue(diff >= (TestNetworkStreamSender.ArtificialDelayms - 20));
		}
	}
}
