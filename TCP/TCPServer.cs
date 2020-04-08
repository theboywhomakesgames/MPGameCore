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
    class TCPServer : TCPConnection
	{
		public static bool listening = true;

		public List<TCPState> connectionsState = new List<TCPState>();

		public delegate void StartListeningAsync();

		#region public methods
		public override void StartIt()
		{
			StartListening();
		}

		public void StartListening()
		{
			Socket srvr_listenersoc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint srvr_ep = new IPEndPoint(IPAddress.Parse(ipAddr), portNumber);
			srvr_listenersoc.Bind(srvr_ep);
			srvr_listenersoc.Listen(100);
			theState = new TCPState(srvr_listenersoc);
			Console.WriteLine("server set, waiting for connectoin");

			try
			{
				while (listening)
				{
					connectionStablished.Reset();
					srvr_listenersoc.BeginAccept(new AsyncCallback(AcceptCallback), srvr_listenersoc);
					connectionStablished.WaitOne();
					Console.WriteLine("connection accepted by server");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				StartListening();
			}
		}

		public override void FlushBuffer()
		{
			if (connectionsState.Count > 0)
			{
				int i = 0;
				foreach (TCPState connection in connectionsState)
				{
					if (connection.messageBuffer.Count > 0)
					{
						SendMessage_(Packer.Pack(connection.messageBuffer.ToArray()), i);
						connection.messageBuffer.Clear();
					}

					i++;
				}
			}
		}

		public override void SendMessage_(string mssg, int conIndex = 0)
		{
			if (connectionsState.Count > 0)
			{
				mssg = mssg + "<EOF>";
				byte[] byteData = Encoding.ASCII.GetBytes(mssg);
				connectionsState[conIndex].workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connectionsState[conIndex]);
			}
			else
			{
				Console.WriteLine("no connections found but you're trying to broadcast a message");
			}
		}

		public override void BroadCastMessage(string mssg, int index = -1)
		{
			if (connectionsState.Count > 0)
			{
				int i = 0;
				foreach (TCPState connection in connectionsState)
				{
					if (index > -1 && i == index)
					{
						i++;
						continue;
					}
					mssg = mssg + "<EOF>";
					byte[] byteData = Encoding.ASCII.GetBytes(mssg);
					connection.workSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), connection);
					i++;
				}
			}
			else
			{
				Console.WriteLine("no connections found but you're trying to broadcast a message");
			}
		}

		public void ShutDown()
		{
			connectionsState[0].workSocket.Shutdown(SocketShutdown.Both);
			connectionsState[0].workSocket.Close();
			Console.WriteLine("socket shutdown");
		}

		public override void AddMessage(TCPMessage mssg, int conIndex)
		{
			if (conIndex >= 0)
			{
				connectionsState[conIndex].messageBuffer.Add(mssg);
			}
			else
			{
				foreach (TCPState con in connectionsState)
				{
					con.messageBuffer.Add(mssg);
				}
			}
		}
		#endregion

		protected override void ReceiveCallback(IAsyncResult ar)
		{
			try
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
						OnReceivedDataHandler(content);
						state.sb.Clear();

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
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				StartReceiving((TCPState)ar.AsyncState);
			}
		}

		#region private methods
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

		private void AcceptCallback(IAsyncResult ar)
		{
			connectionStablished.Set();

			Socket listener = (Socket)ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			TCPState conState = new TCPState(handler);
			connectionsState.Add(conState);

			int conIndex = connectionsState.Count - 1;
			SendMessage_(conIndex + "", conIndex);

			OnConnectedHandler(conIndex + "");

			StartReceiving(conState);
		}

		private void StartReceiving(TCPState state)
		{
			state.workSocket.BeginReceive(state.buffer, 0, TCPState.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
		}
		#endregion
	}
}
