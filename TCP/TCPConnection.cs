using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MPGameCore.TCP;
using MPGameCore.Utils;

namespace MPGameCore
{
    public abstract class TCPConnection
	{
		public static Random rnd = new Random();
		public static char[] chars = {
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K',
			'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
			'W', 'X', 'Y', 'Z', '1', '2', '3', '4', '5', '6', '7',
			'8', '9', '0'
		};

		public static string GenRandString(int length_)
		{
			string o = "";

			for (int i = 0; i < length_; i++)
			{				
				o += chars[rnd.Next(chars.Length)];
			}

			return o;
		}

		protected static ManualResetEvent connectionStablished = new ManualResetEvent(false);

		public delegate void tcpCallBack(string data);

		public event tcpCallBack OnRecieveData;
		public event tcpCallBack OnConnected;

		public string ipAddr;
		public int portNumber = 11000;
		public TCPState theState;

		protected abstract void ReceiveCallback(IAsyncResult ar);

		protected void OnReceivedDataHandler(string d)
		{
			OnRecieveData?.Invoke(d);
		}

		protected void OnConnectedHandler(string d)
		{
			OnConnected?.Invoke(d);
		}

		// 0 -> OnReceive , 1 -> OnConnected
		public void BindEventHandler(tcpCallBack cb, int which)
		{
			switch (which)
			{
				case 0:
					OnRecieveData += cb;
					break;
				case 1:
					OnConnected += cb;
					break;
			}
		}

		public abstract void FlushBuffer();
		public abstract void SendMessage_(string mssg, int conIndex = 0);
		public abstract void BroadCastMessage(string mssg, int index = -1);
		public abstract void StartIt();
		public abstract void AddMessage(TCPMessage mssg, int conIndex);
	}
}
