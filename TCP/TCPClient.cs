using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using MPGameCore.TCP;
using MPGameCore.Utils;

namespace MPGameCore.TCP
{
    class TCPClient : TCPConnection
	{
		public bool isConnected = false;

		public int conIndex = -1;

		#region Public Methods
		public override void StartIt()
		{
			StartConnection();
		}

		public void StartConnection()
		{
			Socket cli_connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint srvr_ep = new IPEndPoint(IPAddress.Parse(ipAddr), portNumber);

			theState = new TCPState(cli_connection);

			try
			{
				connectionStablished.Reset();
				cli_connection.BeginConnect(srvr_ep, new AsyncCallback(ConnectionCallback), theState);
				connectionStablished.WaitOne();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		public override void SendMessage_(string mssg, int conIndex = 0)
		{
			if (isConnected)
			{
				mssg = mssg + "<EOF>";
				byte[] byteData = Encoding.ASCII.GetBytes(mssg);
				theState.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), theState);
			}
		}

		public override void BroadCastMessage(string mssg, int index = -1)
		{
			Console.WriteLine("You are trying to broadcast from a client! This is not cool man!");
		}

		public void ShutDown()
		{
			theState.workSocket.Shutdown(SocketShutdown.Both);
			theState.workSocket.Close();
			Console.WriteLine("socket shutdown");
			isConnected = false;
		}

		public override void FlushBuffer()
		{
			if (theState.messageBuffer.Count > 0)
			{
				SendMessage_(Packer.Pack(theState.messageBuffer.ToArray()));
				theState.messageBuffer.Clear();
			}
		}

		public override void AddMessage(TCPMessage mssg, int conIndex)
		{
			theState.messageBuffer.Add(mssg);
		}
		#endregion

		protected override void ReceiveCallback(IAsyncResult ar)
		{
			String content = String.Empty;

			TCPState state = (TCPState)ar.AsyncState;
			Socket handler = state.workSocket;

			int bytesRead = handler.EndReceive(ar);

			if (bytesRead > 0)
			{
				state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
				content = state.sb.ToString();

				int indexOfEOF = content.IndexOf("<EOF>");

				if (indexOfEOF > -1)
				{
					// A mssg is received
					content = content.Substring(0, indexOfEOF);
					if (conIndex != -1)
					{
						OnReceivedDataHandler(content);
						state.sb.Clear();
					}
					else
					{
						try
						{
							Console.WriteLine("received first packet");
							conIndex = Convert.ToInt32(content);
							OnConnectedHandler(conIndex + "");
						}
						catch { }
					}

					// start receiving the next message
					StartReceiving(state);
				}
				else
				{
					StartReceiving(state);
				}
			}
			else
			{
				// It's theoritically not possible to get here but I put it here just in case
				StartReceiving(state);
			}
		}

		#region Private Methods
		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				TCPState state = (TCPState)ar.AsyncState;
				int bytesSent = state.workSocket.EndSend(ar);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
		}

		private void ConnectionCallback(IAsyncResult ar)
		{
			connectionStablished.Set();
			Console.WriteLine("Connection stablished");
			isConnected = true;

			TCPState state = (TCPState)ar.AsyncState;
			StartReceiving(state);
		}

		private void StartReceiving(TCPState state)
		{
			state.workSocket.BeginReceive(state.buffer, 0, TCPState.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
		}
		#endregion
	}
}
