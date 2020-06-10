using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace server
{
    class RouterServer
    {
        public static int MaxPlayers {get; private set;}
        public static int Port {get; private set;}
        public static int ClientCount;
        public static Dictionary<int, RouterServerSideClient> clients = new Dictionary<int, RouterServerSideClient>();
        public static Dictionary<int, int> ports = new Dictionary<int, int>();

        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers; 
        private static TcpListener tcpListener;

        public static void Start(int _maxPlayers) {
            MaxPlayers = _maxPlayers;
            Port = Constants.ROUTER_SERVER_PORT;
            ClientCount = 0;

            Console.WriteLine("Starting router server...");
            _InitializeServerData();

            IPAddress _ip_addr = IPAddress.Parse(Constants.SERVER_IP);
            tcpListener = new TcpListener(_ip_addr, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(_TCPConnectCallback), null);

            Console.WriteLine($"Router Server started on {_ip_addr}:{Port}");
        }

        private static void _TCPConnectCallback(IAsyncResult _result) {
            TcpClient _accepted_client = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(_TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection to router server from {_accepted_client.Client.RemoteEndPoint}...");
            
            for (int i = 1; i <= MaxPlayers; i++) {
                if (clients[i].tcp.clientSocket == null) {
                    clients[i].tcp.ReceiveConnect(_accepted_client);
                    return;
                }
            }
            Console.WriteLine($"{_accepted_client.Client.RemoteEndPoint} failed to connect: router server full!");
        }

        private static void _InitializeServerData() {
            for (int i = 1; i <= MaxPlayers; i++) {
                clients.Add(i, new RouterServerSideClient(i));
                ports.Add(i, Constants.ROUTER_SERVER_PORT + i);
            }
            packetHandlers = new Dictionary<int, PacketHandler>() {
                {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived},
            };
            Console.WriteLine("Initialized packets.");
        }
    }
}