using System;
using System.Collections.Generic;

namespace DrawlandiaApp.Models
{
    public class Room
    {
        public Room()
        {
            this.Players = new List<Player>();
        }

        public Room(string name, string password)
        {
            this.Name = name;
            this.Password = password;
            this.State = RoomState.NotStarted;
            this.Players = new List<Player>();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        public RoomState State { get; set; }
    }
}
