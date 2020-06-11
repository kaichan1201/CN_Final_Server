using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
namespace server
{
    public class RouterServerSideClient
    {
        public static int dataBufferSize = 4096;

        public int id;
        public TCP tcp;

        public RouterServerSideClient(int _client_id) {
            id = _client_id;
            tcp = new TCP(_client_id);
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
                if (RouterServer.ClientCount < RouterServer.MaxPlayers){
                    clientSocket = _clientSocket;
                    clientSocket.ReceiveBufferSize = dataBufferSize;
                    clientSocket.SendBufferSize = dataBufferSize;

                    stream = clientSocket.GetStream();

                    receivedPacket = new Packet();
                    receiveBuffer = new byte[dataBufferSize];
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, _ReceiveCallback, null);

                    int _port = RouterServer.ports[(RouterServer.ClientCount % RouterServer.MaxPlayers) + 1];
                    RouterServer.ClientCount += 1;

                    ServerSend.RouterWelcome(id, $"Assigned port {_port} by the Router Server!", _port);
                }
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
                        RouterServer.clients[id]._Disconnect();
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
                    RouterServer.clients[id]._Disconnect();
                }
            }
            private bool _HandleData(byte[] _data) {
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
                        using (Packet _packet = new Packet(_packetBytes)) {
                            int _packetId = _packet.ReadInt();
                            Server.packetHandlers[_packetId](id, _packet);
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

            public void Disconnect() {
                clientSocket.Close();
                stream = null;
                receivedPacket = null;
                receiveBuffer = null;
                clientSocket = null;
            }
        }
        private void _Disconnect() {

            Console.WriteLine($"{tcp.clientSocket.Client.RemoteEndPoint} has disconnected from router server.");
            tcp.Disconnect();
        }
    }
}