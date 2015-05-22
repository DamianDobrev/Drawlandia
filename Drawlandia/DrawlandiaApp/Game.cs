using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DrawlandiaApp.Models;
using System.Timers;

namespace DrawlandiaApp
{
    public class Game
    {
        public Game(List<Player> players)
        {
            this.players = players;
        }

        private const int MaxTurns = 5;

        private const int drawTimer = 30000;

        private const int examineWordTimer = 3000;

        private List<Player> players = new List<Player>();

        private int currentTurn = 1;

        private Timer timer;

        public void Start()
        {
            while (currentTurn < MaxTurns)
            {
                foreach (var player in players)
                {
                    timer = new Timer(drawTimer); //30 seconds
                }

                currentTurn++;
            }
        }

    }
}