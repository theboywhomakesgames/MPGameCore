using System;
using System.Collections.Generic;
using System.Text;
using MPGameCore.TCP;
using MPGameCore.Utils;

namespace MPGameCore
{
    class Initializer
    {
        private static TCPConnection srv;

        public static void Main(string[] args)
        {
            srv = new TCPServer();

            srv.ipAddr = "127.0.0.1";
            srv.portNumber = 11000;

            srv.BindEventHandler(OnReceiveData, 0);
            srv.BindEventHandler(OnConnectionEstablished, 1);

            srv.StartIt();

            srv.AddMessage(new TCPMessage("Hello from srv", TCPConnection.GenRandString(10)), -1);
        }

        public static void OnConnectionEstablished(string data)
        {
            Console.WriteLine("New Connection");
        }

        public static void OnReceiveData(string data)
        {
            Console.WriteLine("New Data Received: " + data);
        }
    }
}
