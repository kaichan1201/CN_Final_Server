using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace server
{
    class Server
    {
        public static int MaxPlayers {get; private set;}
        // public static int Port {get; private set;}
        public static Dictionary<int, ServerSideClient> clients = new Dictionary<int, ServerSideClient>();

        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers; 
        // private static TcpListener tcpListener;
        // private static UdpClient udpListener;

        private static Dictionary<int, MyTcpListener> tcpManagers = new Dictionary<int, MyTcpListener>();
        private static Dictionary<int, MyUdpClient> udpManagers = new Dictionary<int, MyUdpClient>();

        public static void Start(int _maxPlayers) {
            MaxPlayers = _maxPlayers;
            // Port = _port;


            Console.WriteLine("Starting server...");
            _InitializeServerData();

            // IPAddress _ip_addr = IPAddress.Parse(Constants.SERVER_IP);
            // tcpListener = new TcpListener(_ip_addr, Port);
            // tcpListener.Start();
            // tcpListener.BeginAcceptTcpClient(new AsyncCallback(_TCPConnectCallback), null);

            // udpListener = new UdpClient(Port);
            // udpListener.BeginReceive(_UDPReceiveCallback, null);

            // Console.WriteLine($"Server started on {_ip_addr}:{Port}");

            for (int i=1; i<=MaxPlayers; i++) {
                IPAddress _ip_addr = IPAddress.Parse(Constants.SERVER_IP);
                tcpManagers.Add(i, new MyTcpListener(_ip_addr, RouterServer.ports[i]));
                tcpManagers[i].tcpListener.Start();
                tcpManagers[i].tcpListener.BeginAcceptTcpClient(new AsyncCallback(tcpManagers[i].TCPConnectCallback), null);

                udpManagers.Add(i, new MyUdpClient(RouterServer.ports[i]));
                udpManagers[i].udpListener.BeginReceive(udpManagers[i].UDPReceiveCallback, null);

                Console.WriteLine($"Server started on {_ip_addr}:{RouterServer.ports[i]}");
            }
        }

        // private static void _TCPConnectCallback(IAsyncResult _result) {
        //     TcpClient _accepted_client = tcpListener.EndAcceptTcpClient(_result);
        //     tcpListener.BeginAcceptTcpClient(new AsyncCallback(_TCPConnectCallback), null);
        //     Console.WriteLine($"Incoming connection from {_accepted_client.Client.RemoteEndPoint}...");
            
        //     for (int i = 1; i <= MaxPlayers; i++) {
        //         if (clients[i].tcp.clientSocket == null) {
        //             clients[i].tcp.ReceiveConnect(_accepted_client);
        //             return;
        //         }
        //     }
        //     Console.WriteLine($"{_accepted_client.Client.RemoteEndPoint} failed to connect: server full!");
        // }

        // private static void _UDPReceiveCallback(IAsyncResult _result) {
        //     try {
        //         IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //         // receive data as well as setup the _clientEndPoint
        //         byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);

        //         udpListener.BeginReceive(_UDPReceiveCallback, null);

        //         if (_data.Length < 4) {return;}

        //         using (Packet _packet = new Packet(_data)) {
        //             int _clientId = _packet.ReadInt();  // get the _clientId that we inserted
        //             if (_clientId == 0) {return;}
        //             // new connection
        //             if (clients[_clientId].udp.endPoint == null) { 
        //                 clients[_clientId].udp.ReceiveConnect(_clientEndPoint);
        //                 return;
        //             }
        //             // handle data if endPoints are the same
        //             if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString()) {
        //                 clients[_clientId].udp.HandleData(_packet);
        //             }
        //         }
        //     }
        //     catch (Exception _ex) {
        //         Console.WriteLine($"Error receiving UDP data: {_ex}");
        //     }
        // }
        
        // public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet) {
        //     try {
        //         if (_clientEndPoint != null) {
        //             udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
        //         }
        //     }
        //     catch (Exception _ex) {
        //         Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
        //     }
        // }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet, int _id) {
            try {
                if (_clientEndPoint != null) {
                    udpManagers[_id].udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex) {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }

        private static void _InitializeServerData() {
            for (int i = 1; i <= MaxPlayers; i++) {clients.Add(i, new ServerSideClient(i));}
            packetHandlers = new Dictionary<int, PacketHandler>() {
                {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived},
                {(int)ClientPackets.playerIsReady, ServerHandle.PlayerIsReady},
                {(int)ClientPackets.startGame, ServerHandle.StartGame},
                {(int)ClientPackets.playerPositionRotation, ServerHandle.PlayerPositionRotation},
                {(int)ClientPackets.playerAnimBool, ServerHandle.PlayerAnimBool},
                {(int)ClientPackets.playerAnimInt, ServerHandle.PlayerAnimInt},
                {(int)ClientPackets.playerShoot, ServerHandle.PlayerShoot},
                {(int)ClientPackets.playerAddScore, ServerHandle.PlayerAddScore},
                {(int)ClientPackets.playerRespawn, ServerHandle.PlayerRespawn},
            };
            Console.WriteLine("Initialized packets.");
        }

        class MyTcpListener
        {
            public TcpListener tcpListener;
            public MyTcpListener(IPAddress _ip_addr, int _port) {
                tcpListener = new TcpListener(_ip_addr, _port);
            }
            public void TCPConnectCallback(IAsyncResult _result) {
                TcpClient _accepted_client = tcpListener.EndAcceptTcpClient(_result);
                tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
                Console.WriteLine($"Incoming connection from {_accepted_client.Client.RemoteEndPoint}...");
            
                for (int i = 1; i <= MaxPlayers; i++) {
                    if (clients[i].tcp.clientSocket == null) {
                        clients[i].tcp.ReceiveConnect(_accepted_client);
                        return;
                    }
                }
                Console.WriteLine($"{_accepted_client.Client.RemoteEndPoint} failed to connect: server full!");
            }
        }

        class MyUdpClient 
        {
            public UdpClient udpListener;
            public MyUdpClient(int _port) {
                udpListener = new UdpClient(_port);
            }
            public void UDPReceiveCallback(IAsyncResult _result) {
            try {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                // receive data as well as setup the _clientEndPoint
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);

                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4) {return;}

                using (Packet _packet = new Packet(_data)) {
                    int _clientId = _packet.ReadInt();  // get the _clientId that we inserted
                    if (_clientId == 0) {return;}
                    // new connection
                    if (clients[_clientId].udp.endPoint == null) { 
                        clients[_clientId].udp.ReceiveConnect(_clientEndPoint);
                        return;
                    }
                    // handle data if endPoints are the same
                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString()) {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex) {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
            }
        }
        }
    }
}