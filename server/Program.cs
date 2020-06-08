using System.Data;
using System;
using System.Threading;

namespace server
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            isRunning = true;
            
            Thread mainThread = new Thread(new ThreadStart(_MainThread));
            mainThread.Start();
            
            Server.Start(12, Constants.SERVER_PORT);
        }

        private static void _MainThread() {
            Console.WriteLine($"Main Thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning) {
                while (_nextLoop < DateTime.Now) {
                    GameLogic.Update();
                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);
                    
                    if (_nextLoop > DateTime.Now) {
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }
            }
        }
    }
}
