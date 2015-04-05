using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace NetworkServerCommunicator
{
	public class NetworkStreamSender
	{
		#region Public class events

		public delegate void CommunicationFailureEventHandler();
		public event CommunicationFailureEventHandler CommunicationFailure;

		#endregion

		#region Member variables

		CancellationTokenSource mCancelThreadToken;
		EventWaitHandle mPacketSentWaitHandle;
		EventWaitHandle mPacketReadyToBeSentWaitHandle;
		EventWaitHandle mWaitForThreadToCompleteWaitHandle;
		//====================THREAD UNSAFE OBJECTS====================
		NetworkStream mNetStream;
		object mNetStreamLock;
		int mEndOfPacketChar;
		Queue<string> mSendPacketQueue;
		Encoding mPacketEncoding;
		Thread mThread;
		//=============================================================

		#endregion

		#region Class constructor

		public NetworkStreamSender(
			NetworkStream netStream,
			int endOfPacketChar,
			Encoding packetEncoding)
		{
			mNetStream = netStream;
			mNetStreamLock = new object();
			mEndOfPacketChar = endOfPacketChar;
			mSendPacketQueue = new Queue<string>();
			mPacketEncoding = packetEncoding;

			mCancelThreadToken = new CancellationTokenSource();
			mPacketSentWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
			mPacketReadyToBeSentWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
			mWaitForThreadToCompleteWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

			mThread = new Thread(ThreadLoop);
			mThread.Start();
		}

		#endregion

		#region Class methods

		public void EnqueuePacket(string packet, bool waitForPacketToSend)
		{
			Monitor.Enter(mSendPacketQueue);
			//Now thread safe.
			mSendPacketQueue.Enqueue((string)packet.Clone());
			Monitor.Exit(mSendPacketQueue);
			//Queue is now thread unsafe.  Signal the thread it has a packet ready to be sent.

			SignalPacketIsReady();

			if (waitForPacketToSend)
				mPacketSentWaitHandle.WaitOne();
		}

		public void EnqueuePackets(List<string> packets, bool waitForPacketsToSend)
		{

			Monitor.Enter(mSendPacketQueue);
			//Now thread safe

			foreach (string packet in packets)
				mSendPacketQueue.Enqueue(packet);

			Monitor.Exit(mSendPacketQueue);

			SignalPacketIsReady();

			if (waitForPacketsToSend)
				mPacketSentWaitHandle.WaitOne();
		}

		private void SignalPacketIsReady()
		{
			SignalThreadFrameCanProceed();
		}

		private void SignalThreadFrameCanProceed()
		{
			mPacketReadyToBeSentWaitHandle.Set();
		}

		public void Stop()
		{
			mCancelThreadToken.Cancel();
			SignalThreadFrameCanProceed();

			mWaitForThreadToCompleteWaitHandle.WaitOne();

			//All thread unsafe objects are now thread safe, as we have verified the spawned thread
			//has completed.
		}

		private void ComFailureThreadSafe()
		{
			CommunicationFailureEventHandler tmp = CommunicationFailure;

			if (tmp != null)
				tmp();
		}

		private void ThreadLoop()
		{
			bool exitWithError = false;
			
			while (!mCancelThreadToken.IsCancellationRequested)
			{
				
				try 
				{	        
					ThreadFrame();
				}
				catch (SocketException)
				{
					exitWithError = true;
					break;
				}
			}

			if (exitWithError)
				ComFailureThreadSafe();

			BeforeThreadSignalsComplete();

			//Signal that the thread has completed, if someone is waiting for this signal.
			mWaitForThreadToCompleteWaitHandle.Set();
		}

		protected virtual void BeforeThreadSignalsComplete()
		{

		}

		private void ThreadFrame()
		{
			//Wait for a signal that packets are ready.
			mPacketReadyToBeSentWaitHandle.WaitOne();
			//Signal received, reset it for next time.
			mPacketReadyToBeSentWaitHandle.Reset();

			Debug.Print("got past the wait");

			Monitor.Enter(mSendPacketQueue);

			if (mSendPacketQueue.Count > 0)
			{
				//Packets are queued to be sent, let's get a lock on the network stream and send them.
				Monitor.Enter(mNetStreamLock);
				
				while (mSendPacketQueue.Count > 0)
				{
					string packet = mSendPacketQueue.Dequeue();
					try 
					{	        
						SendPacket(packet);
					}
					catch (Exception)
					{
						Monitor.Exit(mNetStreamLock);
						Monitor.Exit(mSendPacketQueue);
						throw new System.Net.Sockets.SocketException();
					}
				}

				AfterSendingPackets();

				//Signal that the packets have been sent.
				mPacketSentWaitHandle.Set();

				Monitor.Exit(mNetStreamLock);
			}

			Monitor.Exit(mSendPacketQueue);


		}

		protected virtual void AfterSendingPackets()
		{

		}
		/// <summary>
		/// Writes the packet to mNetStream.  Assumes the caller has a lock on mNetStream!
		/// </summary>
		/// <param name="packet"></param>
		protected virtual void SendPacket(string packet)
		{
			if (!packet.Equals(""))
			{
				mNetStream.Write(mPacketEncoding.GetBytes(packet), 0, packet.Length);
				mNetStream.WriteByte((byte)mEndOfPacketChar);

				mNetStream.Flush();
			}
		}

		#endregion
	}
}
