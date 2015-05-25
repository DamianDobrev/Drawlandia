using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DrawlandiaApp.Models
{
    public class Player
    {
        public Player()
        {
            this.State = PlayerState.InRoom;
            this.Score = 0;
            this.IsHisTurn = false;
        }

        public Player(string connectionId, string name)
        {
            this.ConnectionId = connectionId;
            this.Name = name;
            this.State = PlayerState.InRoom;
            this.Score = 0;
            this.IsHisTurn = false;
        }

        public int Id { get; set; }

        public string ConnectionId { get; set; }

        public string Name { get; set; }

        public int RoomId { get; set; }

        public PlayerState State { get; set; }

        public int Score { get; set; }

        public bool IsHisTurn { get; set; }
    }
}