using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrawlandiaApp.Models
{
    public class Game
    {
        public Game()
        {
            this.Players = new List<Player>();
            this.State = GameState.NotStarted;
            this.WordIsGuessed = false;
            this.CurrentWord = null;
        }

        public Game(string name, string password)
        {
            this.Name = name;
            this.Password = password;
            this.State = GameState.NotStarted;
            this.Players = new List<Player>();
            this.WordIsGuessed = false;
            this.CurrentWord = null;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public virtual ICollection<Player> Players { get; set; }

        public GameState State { get; set; }

        public string CurrentWord { get; set; }

        public string CurrentPattern { get; set; }

        public DateTime EndOfCurrentTurn { get; set; }

        public bool WordIsGuessed { get; set; }

        public string TurnsMap { get; set; }
    }
}
