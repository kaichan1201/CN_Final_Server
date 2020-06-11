using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
namespace server
{
    public class ServerSideClient
    {
        public static int dataBufferSize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public ServerSideClient(int _client_id) {
            id = _client_id;
            tcp = new TCP(_client_id);
            udp = new UDP(_client_id);
        }

        public class TCP
        {
            public TcpClient clientSocket;
            private readonly int id;
            private NetworkStream stream;
            private Packet receivedPacket;
            private byte[] receiveBuffer;

            public TCP(int _client_id) {id = _client_id;}

            public void ReceiveConnect(TcpClient _clientSocket) {
                clientSocket = _clientSocket;
                clientSocket.ReceiveBufferSize = dataBufferSize;
                clientSocket.SendBufferSize = dataBufferSize;

                stream = clientSocket.GetStream();

                receivedPacket = new Packet();
                receiveBuffer = new byte[dataBufferSize];
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, _ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the Server!");
            }

            public void SendData(Packet _packet) {
                try {
                    if (clientSocket != null) {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception _ex) {
                    System.Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }

            private void _ReceiveCallback(IAsyncResult _result) {
                try
                {
                    int _dataLength = stream.EndRead(_result);
                    if (_dataLength <= 0) {
                        Server.clients[id]._Disconnect();
                        return;
                    }

                    byte[] _data = new byte[_dataLength];
                    Array.Copy(receiveBuffer, _data, _dataLength);
                    
                    receivedPacket.Reset(_HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, _ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    Server.clients[id]._Disconnect();
                }
            }
            private bool _HandleData(byte[] _data) {
                try {
                    int _packetLength = 0;
                    receivedPacket.SetBytes(_data);

                    if (receivedPacket.UnreadLength() >= 4) {
                        _packetLength = receivedPacket.ReadInt(); // read the (remaining) length of the packet
                        if (_packetLength <= 0) {return true;}
                    }

                    while (_packetLength > 0 && _packetLength <= receivedPacket.UnreadLength()) {
                        byte[] _packetBytes = receivedPacket.ReadBytes(_packetLength);
                        // schedule the corresponding packet handler onto the main thread
                        ThreadManager.ExecuteOnMainThread(() => {
                            try {
                                using (Packet _packet = new Packet(_packetBytes)) {
                                    int _packetId = _packet.ReadInt();
                                    Server.packetHandlers[_packetId](id, _packet);
                                }
                            }
                            catch {
                                Console.WriteLine("WTFFF");
                            }
                        });

                        _packetLength = 0;
                        if (receivedPacket.UnreadLength() >= 4) {
                            _packetLength = receivedPacket.ReadInt(); // read the (remaining) length of the packet
                            if (_packetLength <= 0) {return true;}
                        }
                    }

                    if (_packetLength <= 1) {return true;}
                    return false;
                }
                catch {
                    Console.WriteLine("WTF");
                    return true;
                }
            }

            public void Disconnect() {
                clientSocket.Close();
                stream = null;
                receivedPacket = null;
                receiveBuffer = null;
                clientSocket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id) {id = _id;}

            public void ReceiveConnect(IPEndPoint _endPoint) {
                endPoint = _endPoint;
            }

            public void SendData(Packet _packet) {
                // Server.SendUDPData(endPoint, _packet);
                Server.SendUDPData(endPoint, _packet, id);
            }

            public void HandleData(Packet _packet) {  // can be moved to Server.cs
                int _packetLength = _packet.ReadInt();
                byte[] _packetBytes = _packet.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() => {
                    using (Packet _newPacket = new Packet(_packetBytes)) {
                        int _packetId = _newPacket.ReadInt();
                        Server.packetHandlers[_packetId](id, _newPacket);
                    };
                });
            }

            public void Disconnect() {
                endPoint = null;
            }
        }

        public void SendIntoGame(string _playerName) {
            player = new Player(id, _playerName);
            GameLogic.currentPlayers++;
            // update info
            foreach(ServerSideClient _client in Server.clients.Values) {
                if (_client.player != null) {
                    if (_client.id != id) {
                        ServerSend.SpawnPlayer(id, _client.player); // info of all other players -> new player
                    }
                    ServerSend.SpawnPlayer(_client.id, player); // info of new player -> all other players
                }
            }
        }

        public void _Disconnect() {

            if (tcp.clientSocket != null) {
                Console.WriteLine($"{tcp.clientSocket.Client.RemoteEndPoint} has disconnected.");
                if (player.isReady) {
                    GameLogic.readyPlayers--;
                }
                GameLogic.currentPlayers--;
                player = null;
                tcp.Disconnect();
                udp.Disconnect();
                ServerSend.KickPlayerToAllExcept(id);
            }
        }
    }
}