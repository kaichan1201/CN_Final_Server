using System.Numerics;
using System;
namespace server
{
    public class ServerSend
    {
        private static void _SendTCPData(int _toClient, Packet _packet) {
            _packet.WriteLength();
            Server.clients[_toClient].tcp.SendData(_packet);
        }
        private static void _SendTCPDataToAll(Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++) {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
        private static void _SendTCPDataToAll(int _exceptClient, Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++) {
                if (i != _exceptClient) {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }
        
        private static void _SendUDPData(int _toClient, Packet _packet) {
            _packet.WriteLength();
            Server.clients[_toClient].udp.SendData(_packet);
        }
        private static void _SendUDPDataToAll(Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++) {
                Server.clients[i].udp.SendData(_packet);
            }
        }
        private static void _SendUDPDataToAll(int _exceptClient, Packet _packet) {
            _packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++) {
                if (i != _exceptClient) {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }
 
        #region Packets

        #region TCP
        public static void Welcome(int _toClient, string _msg) {
            using (Packet _packet = new Packet((int)ServerPackets.welcome)) {
                _packet.Write(_msg);
                _packet.Write(_toClient);
                _SendTCPData(_toClient, _packet);
            }
        }
        public static void SpawnPlayer(int _toClient, Player _player) {
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer)) {
                _packet.Write(_player.id);
                _packet.Write(_player.username);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);
                _SendTCPData(_toClient, _packet);
            }
        }
        public static void IsReadyToAllExcept(Player _player) {
            using (Packet _packet = new Packet((int)ServerPackets.isReadyToAllExcept)) {
                _packet.Write(_player.id);
                _packet.Write(_player.isReady);
                _SendTCPDataToAll(_player.id, _packet);
            }
        }
        public static void StartGameToAll() {
            // start the actual game
            using (Packet _packet = new Packet((int)ServerPackets.startGameToAll)) {
                _SendTCPDataToAll(_packet);
            }
        }
        public static void ShootToAll(int _id, int _hitId, Vector3 _hitPoint, int _hitHealth) {
            using (Packet _packet = new Packet((int)ServerPackets.shootToAll)) {
                _packet.Write(_id);
                _packet.Write(_hitId);
                _packet.Write(_hitPoint);
                _packet.Write(_hitHealth);
                _SendTCPDataToAll(_packet);
            }
        }
        #endregion

        #region UDP
        public static void PositionRotationToAllExcept(Player _player) {
            // send position & rotation info to all (except original player)
            using (Packet _packet = new Packet((int)ServerPackets.positionRotationToAllExcept)) {
                _packet.Write(_player.id);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);
                _packet.Write(_player.upperRotation);
                _SendUDPDataToAll(_player.id, _packet);
            }
        }
        public static void AnimToAllExceptBool(int _id, int _index, bool _activate) {
            using (Packet _packet = new Packet((int)ServerPackets.animToAllExceptBool)) {
                _packet.Write(_id);
                _packet.Write(_index);
                _packet.Write(_activate);
                _SendTCPDataToAll(_id, _packet);
            }
        }
        public static void AnimToAllExceptInt(int _id, int _index, int _activate) {
            using (Packet _packet = new Packet((int)ServerPackets.animToAllExceptInt)) {
                _packet.Write(_id);
                _packet.Write(_index);
                _packet.Write(_activate);
                _SendTCPDataToAll(_id, _packet);
            }
        }
        #endregion

        #endregion
    }
}