using System.Numerics;
using System;
using System.Text;
using System.Collections.Generic;
namespace server
{
    public class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet) {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.clientSocket.Client.RemoteEndPoint}" +
                              $" connected successfully and is now player {_fromClient}");

            if (_clientIdCheck != _fromClient) {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has" +
                                  $" assumed the wrong ID ({_clientIdCheck})!");
            }
            Server.clients[_fromClient].SendIntoGame(_username);
        }

        public static void PlayerIsReady(int _fromClient, Packet _packet) {
            bool _isReady = _packet.ReadBool();
            Server.clients[_fromClient].player.UpdateIsReady(_isReady);
        }

        public static void StartGame(int _fromClient, Packet _packet) {
            //Start the game
            Console.WriteLine($"{GameLogic.readyPlayers}, {GameLogic.currentPlayers}");
            if (GameLogic.readyPlayers == GameLogic.currentPlayers  && (!GameLogic.isGameStarted)) {
            // if (GameLogic.readyPlayers == GameLogic.currentPlayers) {
                ServerSend.StartGameToAll();
                GameLogic.isGameStarted = true;
            }
        }

        public static void PlayerPositionRotation(int _fromClient, Packet _packet) {
            Vector3 _position = _packet.ReadVector3();
            Quaternion _rotation = _packet.ReadQuaternion();
            Quaternion _upperRotation = _packet.ReadQuaternion();

            if (Server.clients[_fromClient].player != null) {
                Server.clients[_fromClient].player.UpdatePositionRotation(_position, _rotation, _upperRotation);
            }
        }

        public static void PlayerAnimBool(int _fromClient, Packet _packet) {
            int _index = _packet.ReadInt();
            bool _activate = _packet.ReadBool();

            ServerSend.AnimToAllExceptBool(_fromClient, _index, _activate);
        }

        public static void PlayerAnimInt(int _fromClient, Packet _packet) {
            int _index = _packet.ReadInt();
            int _activate = _packet.ReadInt();

            ServerSend.AnimToAllExceptInt(_fromClient, _index, _activate);
        }

        public static void PlayerShoot(int _fromClient, Packet _packet) {
            if(Server.clients[_fromClient].player != null) {
                int _hitId = _packet.ReadInt();
                Vector3 _hitPoint = _packet.ReadVector3();

                int _hitHealth = -1;
                if (_hitId != -1) {
                    Player _hitPlayer = Server.clients[_hitId].player;
                    _hitPlayer.health -= 5;
                    // TODO: Handle Death?
                    if (_hitPlayer.health == 0) {
                        _hitPlayer.deathCount += 1;
                    }
                    _hitHealth = _hitPlayer.health;
                }

                ServerSend.ShootToAll(_fromClient, _hitId, _hitPoint, _hitHealth);
            }
        }
    
        public static void PlayerAddScore(int _fromClient, Packet _packet) {
            if(Server.clients[_fromClient].player != null) {
                int _team = _packet.ReadInt();
                int _addScore = _packet.ReadInt();

                Console.WriteLine($"{GameLogic.score[_team]}, {_addScore}");
                Server.clients[_fromClient].player.score += _addScore;
                GameLogic.score[_team] += _addScore;
                ServerSend.NewScoreToAll(_team, GameLogic.score[_team]);

                if (GameLogic.score[_team] >= Constants.WIN_SCORE) {
                    ServerSend.WinToAll(_team);
                    //Restart game
                    // foreach(ServerSideClient _client in Server.clients.Values) {
                    //     if (_client.tcp.clientSocket != null){
                    //         _client._Disconnect();
                    //     }
                    // }
                    GameLogic.Reset();
                }
            }
        }

        public static void PlayerRespawn(int _fromClient, Packet _packet) {
            if(Server.clients[_fromClient].player != null) {
                Server.clients[_fromClient].player.Respawn();
                ServerSend.RespawnToAll(_fromClient);
            }
        }
    }
}