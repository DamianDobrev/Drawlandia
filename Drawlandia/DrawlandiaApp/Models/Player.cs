using System.ComponentModel.DataAnnotations.Schema;

namespace DrawlandiaApp.Models
{
    public class Player
    {
        public Player()
        {
            this.Score = 0;
        }

        public Player(string connectionIdentifier, string name)
        {
            this.ConnectionId = connectionIdentifier;
            this.Name = name;
            this.Score = 0;
        }

        public int Id { get; set; }

        public string ConnectionId { get; set; }

        public string Name { get; set; }

        public int CurrentGameId { get; set; }

        [ForeignKey("CurrentGameId")]
        public virtual Game CurrentGame { get; set; }

        public int Score { get; set; }

        public PlayerState PlayerState { get; set; }
    }
}