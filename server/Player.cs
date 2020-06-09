using System.Numerics;
using System;
namespace server
{
    public class Player
    {
        public int id;
        public string username;
        public int health;
        public Vector3 position;
        public Quaternion rotation;
        public Quaternion upperRotation;
        public bool isReady;
        public int score;
        public int deathCount;

        public Player(int _id, string _username) {
            id = _id;
            username = _username;
            health = 100;
            position = new Vector3(0, 0, 0);
            rotation = Quaternion.Identity;
            isReady = false;
            score = 0;
            deathCount = 0;
        }

        public void UpdatePositionRotation(Vector3 _position, Quaternion _rotation, Quaternion _upperRotation) {
            position = _position;
            rotation = _rotation;
            upperRotation = _upperRotation;
            ServerSend.PositionRotationToAllExcept(this);
        }

        public void UpdateIsReady(bool _isReady) {
            // updates whenever player changes isReady status
            isReady = _isReady;

            if (_isReady)
                GameLogic.readyPlayers++;
            else
                GameLogic.readyPlayers--;

            ServerSend.IsReadyToAllExcept(this);
        }
    
        public void Respawn() {
            health = 100;
        }
    }
}