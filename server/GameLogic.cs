using System;
using System.Text;
using System.Collections.Generic;
namespace server
{
    public class GameLogic
    {
        public static int readyPlayers = 0;
        public static int currentPlayers = 0;
        public static bool isGameStarted = false;
        public static int[] score = new int[2];
        public static void Reset() {
            readyPlayers = 0;
            currentPlayers = 0;
            isGameStarted = false;
        }
        public static void Update() {
            ThreadManager.UpdateMain();
        }
    }
}