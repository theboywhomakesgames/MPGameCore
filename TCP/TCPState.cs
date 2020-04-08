using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using MPGameCore.TCP;
using MPGameCore.Utils;

namespace MPGameCore.TCP
{
    public class TCPState
    {
        public Socket workSocket;
        public const int BufferSize = 512;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb;

        public List<TCPMessage> messageBuffer = new List<TCPMessage>();

        public TCPState(Socket s)
        {
            workSocket = s;
            sb = new StringBuilder();
        }
    }
}
