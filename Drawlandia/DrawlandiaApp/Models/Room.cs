using System;
using System.Collections.Generic;

namespace DrawlandiaApp.Models
{
    class Room
    {
        public Room(string name, string password)
        {
            this.Id = Guid.NewGuid();
            this.Name = name;
            this.Password = password;
            this.Players = new List<Player>();
            this.State = State.NotStarted;
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public List<Player> Players { get; set; }

        public State State { get; set; }
    }
}
