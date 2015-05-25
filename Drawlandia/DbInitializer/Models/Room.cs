using System;
using System.Collections.Generic;

namespace DbInitializer.Models
{
    public class Room
    {
        public Room(string name, string password)
        {
            this.Name = name;
            this.Password = password;
            this.State = State.NotStarted;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public ICollection<Player> Players { get; set; }

        public State State { get; set; }
    }
}
