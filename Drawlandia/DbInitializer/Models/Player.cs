using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DbInitializer.Models
{
    public class Player
    {
        public Player(string connectionId, string name)
        {
            this.ConnectionId = connectionId;
            this.Name = name;
        }

        public int Id { get; set; }

        public string ConnectionId { get; set; }

        public string Name { get; set; }

        public int RoomId { get; set; }
    }
}