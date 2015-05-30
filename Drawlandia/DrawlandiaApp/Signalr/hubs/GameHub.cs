using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using DrawlandiaApp.Models;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Web.DynamicData.ModelProviders;

namespace DrawlandiaApp.Signalr.hubs
{
    public class GameHub : Hub
    {
        DrawlandiaContext db = new DrawlandiaContext();

        public void GetAllGames()
        {
            var games = db.Games.OrderByDescending(g => g.Id)
                .Select(g => new
                {
                    Id = g.Id,
                    Name = g.Name,
                    PlayersCount = g.Players.Count,
                    HasPassword = !String.IsNullOrEmpty(g.Password)
                });
            var jsonSerialiser = new JavaScriptSerializer();
            var jsonGames = jsonSerialiser.Serialize(games);
            Clients.Caller.initializeGames(jsonGames);
        }

        public void GoToGame(int id)
        {
            var games = db.Games;
            var ourGame = games.FirstOrDefault(g => g.Id == id);

            //Check if game exists
            if (ourGame == null)
            {
                Clients.Caller.errorWithMsg("No such room.");
                return;
            }

            var jsonSerialiser = new JavaScriptSerializer();

            var outputPlayers = GetPlayersOutput(ourGame.Players);

            var outputGame = new
                {
                    Id = ourGame.Id,
                    Name = ourGame.Name,
                    Players = outputPlayers,
                    HasPassword = !String.IsNullOrEmpty(ourGame.Password)
                };

            var outputJson = jsonSerialiser.Serialize(outputGame);
            Clients.Caller.initGame(outputJson);
        }

        public void CreateGame(string name, string password, string creatorName)
        {
            var games = db.Games;
            if (String.IsNullOrEmpty(creatorName))
            {
                Clients.Caller.errorWithMsg("Invalid name, please refresh the page.");
                return;
            }

            if (games.Any(room => room.Name == name))
            {
                Clients.Caller.errorWithMsg("Sorry, but it seems like there is already a room with that name :(");
                return;
            }

            var gameToCreate = new Game(name, password);

            var player = new Player(Context.ConnectionId, creatorName);
            gameToCreate.Players = new List<Player>();
            gameToCreate.Players.Add(player);
            gameToCreate.EndOfCurrentTurn = DateTime.Now;
            games.Add(gameToCreate);
            db.SaveChanges();

            Groups.Add(Context.ConnectionId, name);

            var gameOutput = games.FirstOrDefault(r => r.Name == gameToCreate.Name);
            GoToGame(gameOutput.Id);

            UpdateLobby();
        }

        public void JoinGame(int gameId, string password, string playerName)
        {
            var games = db.Games;
            var gameToJoin = games.FirstOrDefault(g => g.Id == gameId);

            //Check if game exists or game started
            if (gameToJoin == null || gameToJoin.State == GameState.Started)
            {
                Clients.Caller.errorWithMsg("The room no longer exists or the game has started.");
                return;
            }


            //Check player name
            if (String.IsNullOrEmpty(playerName))
            {
                Clients.Caller.errorWithMsg("Player name is empty please refresh the page.");
                return;
            }

            //Check if people in game reached 6
            if (gameToJoin.Players.Count >= 6)
            {
                Clients.Caller.errorWithMsg("Room is full.");
                return;
            }

            //Authorization
            if (!String.IsNullOrEmpty(gameToJoin.Password) && gameToJoin.Password != password)
            {
                Clients.Caller.errorWithMsg("Wrong password.");
                return;
            }

            var playerToBeJoined = new Player(Context.ConnectionId, playerName);

            gameToJoin.Players.Add(playerToBeJoined);
            Groups.Add(Context.ConnectionId, gameToJoin.Name);
            db.SaveChanges();
            GoToGame(gameToJoin.Id);
            UpdatePlayersInGame(gameToJoin);
        }

        public void LeaveGame(int roomId)
        {
            var games = db.Games;
            var gameToLeave = games.FirstOrDefault(room => room.Id == roomId);

            //Check if game exists
            if (gameToLeave == null)
            {
                Clients.Caller.errorWithMsg("No such game.");
                return;
            }

            var playerLeaving = gameToLeave.Players.FirstOrDefault(pl => pl.ConnectionId == Context.ConnectionId);

            //Check if player exists in the room
            if (playerLeaving == null)
            {
                Clients.Caller.errorWithMsg("You are not in that game.");
                return;
            }

            //Check if game has started
            if (gameToLeave.State == GameState.Started)
            {
                Clients.Caller.errorWithMsg("You can't leave a game that has already started.");
                return;
            }

            //Leave and remove game if no one left
            gameToLeave.Players.Remove(playerLeaving);
            if (!gameToLeave.Players.Any())
            {
                games.Remove(gameToLeave);
            }
            db.Players.Remove(playerLeaving);
            db.SaveChanges();

            Groups.Remove(Context.ConnectionId, gameToLeave.Name);

            Clients.Group(gameToLeave.Name).updatePlayers(GetPlayersOutput(gameToLeave.Players));

            Clients.Group(gameToLeave.Name).playSound("Disconnect");

            Clients.Caller.redirectToLobby();

            UpdatePlayersInGame(gameToLeave);

            UpdateLobby();
        }

        public void SendMessage(string message)
        {
            var currentGameId = db.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId).CurrentGameId;
            var game = db.Games.FirstOrDefault(g => g.Id == currentGameId);

            if (game == null)
            {
                Clients.Caller.errorWithMsg("Game does not exist");
                return;
            }

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            if (player == null)
            {
                Clients.Caller.errorWithMsg("Player doesn't exists, please refresh the page.");
                return;
            }

            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            Clients.Group(game.Name).addMessage(player.Name, message);

            if (game.State == GameState.Started)
            {
                CheckMessage(game, message);
            }
        }

        public void StartGame()
        {
            var player = db.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            var game = db.Games.FirstOrDefault(g => g.Id == player.CurrentGame.Id);

            if (game == null || player == null)
            {
                Clients.Caller.errorWithMsg("Something went wrong! Please create a new game.");
                return;
            }

            if (game.State == GameState.Started)
            {
                Clients.Caller.errorWithMsg("Game has already started.");
                return;
            }

            game.State = GameState.Started;

            db.SaveChanges();

            Clients.Group(game.Name).cutLegs();

            NewRound(game);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var players = db.Players;
            var dcPlayer = players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            //Check if dc player is not in db
            if (dcPlayer == null)
            {
                return Clients.Caller.errorWithMsg("Something went terribly wrong");
            }

            if (dcPlayer.PlayerState == PlayerState.InRoom)
            {
                LeaveGame(dcPlayer.CurrentGame.Id);
            }
            else
            {
                var game = dcPlayer.CurrentGame;
                var gameId = game.Id;
                if (game.Players.All(p => p.PlayerState == PlayerState.Disconnected))
                {
                    db.Players.RemoveRange(dcPlayer.CurrentGame.Players);
                    db.Games.Remove(db.Games.FirstOrDefault(g => g.Id == gameId));
                    db.SaveChanges();
                }
                else
                {
                    UpdateDcPlayer(dcPlayer);
                }
            }

            return Clients.Caller.errorWithMsg("Error");
        }

        private void UpdateDcPlayer(Player dcPlayer)
        {
            dcPlayer.PlayerState = PlayerState.Disconnected;
            db.SaveChanges();
            var game = dcPlayer.CurrentGame;
            Clients.Group(game.Name).updatePlayers(GetPlayersOutput(game.Players));
        }

        private void CheckMessage(Game game, string message)
        {
            var players = game.Players;
            var currentDrawer = players.FirstOrDefault(p => p.Id == GetNextIdFromMap(game.TurnsMap));

            if (game.CurrentWord != message || game.WordIsGuessed)
            {
                return;
            }

            game.WordIsGuessed = true;

            var sender = game.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            if (sender == null || sender.Id == currentDrawer.Id)
            {
                return;
            }

            sender.Score += 10;
            currentDrawer.Score += 15;

            game.TurnsMap = RemoveElementFromMap(game.TurnsMap);

            db.SaveChanges();

            var outputMessage = String.Format("Player {0} guessed the word: {1}, starting new round.", sender.Name, game.CurrentWord);
            Clients.Group(game.Name).onGuessedWord(outputMessage, GetPlayersOutput(game.Players));

            NewRound(game);
        }

        private void NewRound(Game game)
        {
            var players = game.Players;

            //New game

            if (game.TurnsMap == null)
            {
                foreach (var player in players)
                {
                    player.PlayerState = PlayerState.InStartedGame;
                }
                game.TurnsMap = GenerateTurnsMap(players);
            }

            int currentDrawerId;

            try
            {
                currentDrawerId = GetNextIdFromMap(game.TurnsMap);
            }
            catch (Exception)
            {
                EndGame(game);
                return;
            }

            var currentPlayer = players.FirstOrDefault(p => p.Id == currentDrawerId);

            if (currentPlayer.PlayerState == PlayerState.Disconnected)
            {
                game.TurnsMap = RemoveElementFromMap(game.TurnsMap);
                db.SaveChanges();
                NewRound(game);
                return;
            }

            //Set properties

            game.CurrentWord = GenerateRandomWord();
            game.CurrentPattern = GeneratePattern(game.CurrentWord);
            game.EndOfCurrentTurn = DateTime.Now.AddSeconds(40);
            game.WordIsGuessed = false;

            db.SaveChanges();

            var currentDrawer = players.FirstOrDefault(p => p.Id == currentDrawerId);
            if (currentDrawer == null)
            {
                Clients.Group(game.Name).errorWithMsg("Something went terribly wrong, please refresh the page.");
                return;
            }

            //Send the word and pattern to clients
            Clients.Client(currentDrawer.ConnectionId).becomeDrawer(game.CurrentWord);
            Clients.AllExcept(currentDrawer.ConnectionId).becomeGuesser(game.CurrentPattern);
        }

        private string RemoveElementFromMap(string map)
        {
            var indexOfFirstComma = map.IndexOf(",", StringComparison.Ordinal);
            return map.Substring(indexOfFirstComma + 1, map.Length - indexOfFirstComma - 1);
        }

        private int GetNextIdFromMap(string map)
        {
            var indexOfFirstComma = map.IndexOf(",", StringComparison.Ordinal);
            return Int32.Parse(map.Substring(0, indexOfFirstComma));
        }

        private string GenerateTurnsMap(ICollection<Player> players)
        {
            var mapList = players.Select(player => player.Id.ToString()).ToList();
            var mapOutputList = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                mapOutputList.AddRange(mapList);
            }

            var mapArr = mapOutputList.ToArray();
            return String.Join(",", mapArr);
        }

        private void EndGame(Game game)
        {
            var outputMessage = GenerateEndGameMessage(game.Players.ToList());
            foreach (var player in game.Players.ToList())
            {
                if (player.PlayerState == PlayerState.InStartedGame)
                {
                    player.PlayerState = PlayerState.InRoom;
                }
                else
                {
                    game.Players.Remove(player);
                    db.Players.Remove(player);
                }
            }
            game.State = GameState.NotStarted;
            game.TurnsMap = null;
            game.CurrentPattern = null;
            game.CurrentWord = null;
            game.EndOfCurrentTurn = DateTime.Now;

            db.SaveChanges();

            Clients.Group(game.Name).gameOver(outputMessage);
        }

        private IEnumerable<object> GetPlayersOutput(ICollection<Player> players)
        {
            return players.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Score = p.Score,
                PlayerState = p.PlayerState
            }).OrderBy(p => p.Id);
        }

        private string GenerateEndGameMessage(List<Player> players)
        {
            var output = new StringBuilder();
            output.Append("Game over! Players: ");

            if (players == null || !players.Any())
            {
                return "";
            }

            var maxScorePlayer = players
                .OrderByDescending(p => p.Score)
                .FirstOrDefault();

            if (maxScorePlayer == null)
            {
                return "";
            }

            var maxScore = maxScorePlayer.Score;

            foreach (var player in players)
            {
                if (player.Score == maxScore)
                {
                    output.Append(String.Format("{0}, ", player.Name));
                }
            }

            output.Append(String.Format("won with score: {0}", maxScore));

            return output.ToString();
        }

        private string GeneratePattern(string randomWord)
        {
            StringBuilder output = new StringBuilder();
            foreach (var character in randomWord.ToCharArray())
            {
                if (character != ' ')
                {
                    output.Append('_');
                }
            }
            return output.ToString();
        }

        private void UpdateLobby()
        {
            var games = db.Games.OrderByDescending(g => g.Id)
                .Select(g => new
                {
                    Id = g.Id,
                    Name = g.Name,
                    PlayersCount = g.Players.Count,
                    HasPassword = !String.IsNullOrEmpty(g.Password)
                });
            var jsonSerialiser = new JavaScriptSerializer();
            var jsonRooms = jsonSerialiser.Serialize(games);
            Clients.Caller.initializeRooms(jsonRooms);
        }

        private string GenerateRandomWord()
        {
            var allWords = db.Words.ToList();
            var rnd = new Random();
            return allWords[rnd.Next(1, allWords.Count)].WordText;
        }

        private void UpdatePlayersInGame(Game game)
        {
            if (game.Players.Count <= 0)
            {
                return;
            }

            var gameOwnerConnectionId = game.Players.ToList()[0].ConnectionId;
            Clients.Group(game.Name).updatePlayers(GetPlayersOutput(game.Players));
            Clients.Client(gameOwnerConnectionId).becomeOwner();
            Clients.Group(game.Name, gameOwnerConnectionId).becomeOrdinaryPlayer();
        }

        //Drawing functions

        public void Draw(int clickX, int clickY, bool clickDrag, string color)
        {
            Clients.All.drawRemote(clickX, clickY, clickDrag, color);
        }

        public void Clear()
        {
            Clients.All.clearCanvas();
        }
    }
}