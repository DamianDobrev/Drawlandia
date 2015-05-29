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
            games.Add(gameToCreate);
            gameToCreate.EndOfCurrentTurn = DateTime.Now;
            db.SaveChanges();

            Groups.Add(Context.ConnectionId, name);

            var gameOutput = games.FirstOrDefault(r => r.Name == gameToCreate.Name);
            GoToGame(gameOutput.Id);

            UpdateGamesToAll();
        }

        public void JoinGame(int gameId, string password, string playerName)
        {
            var games = db.Games;
            var players = db.Players;

            var gameToJoin = games.FirstOrDefault(g => g.Id == gameId);

            //Check if player exists in db
            if (players.Any(p => p.ConnectionIdentifier == Context.ConnectionId))
            {
                Clients.Caller.errorWithMsg("You are already in a game.");
                return;
            }

            //Check player name
            if (String.IsNullOrEmpty(playerName))
            {
                Clients.Caller.errorWithMsg("Player name is empty.");
                return;
            }

            //Check if game exists or game started
            if (gameToJoin == null || gameToJoin.State == GameState.Started)
            {
                Clients.Caller.errorWithMsg("The room no longer exists or the game has started.");
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

            Clients.Group(gameToJoin.Name).updatePlayers(GetPlayersOutput(gameToJoin.Players));
            GoToGame(gameToJoin.Id);
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

            var playerLeaving = gameToLeave.Players.FirstOrDefault(pl => pl.ConnectionIdentifier == Context.ConnectionId);

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

            UpdateGamesToAll();
        }

        public void SendMessage(string message)
        {
            var player = db.Players.FirstOrDefault(p => p.ConnectionIdentifier == Context.ConnectionId);

            if (player == null)
            {
                Clients.Caller.errorWithMsg("Player doesn't exists, please refresh the page.");
                return;
            }

            var game = db.Games.FirstOrDefault(g => g.Id == player.CurrentGame.Id);

            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            if (game == null)
            {
                Clients.Caller.errorWithMsg("Game does not exist");
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
            var player = db.Players.FirstOrDefault(p => p.ConnectionIdentifier == Context.ConnectionId);
            var game = db.Games.FirstOrDefault(g => g.Id == player.CurrentGame.Id);

            if (game == null || player == null)
            {
                Clients.Caller.errorWithMsg("Something went wrong! Please create a new game.");
                return;
            }

            game.State = GameState.Started;
            db.SaveChanges();

            NewRound(game);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var players = db.Players;
            var dcPlayer = players.FirstOrDefault(p => p.ConnectionIdentifier == Context.ConnectionId);

            //Check if dc player is not in db
            if (dcPlayer == null)
            {
                return Clients.Caller.errorWithMsg("Something went terribly wrong");
            }

            LeaveGame(dcPlayer.CurrentGame.Id);

            return Clients.Caller.errorWithMsg("Error");
        }

        private void CheckMessage(Game game, string message)
        {
            if (game.CurrentWord != message || game.WordIsGuessed)
            {
                return;
            }

            game.WordIsGuessed = true;
            var player = game.Players.FirstOrDefault(p => p.ConnectionIdentifier == Context.ConnectionId);
            player.Score += 10;
            var drawer = game.Players.FirstOrDefault(p => p.IsHisTurn);
            drawer.Score += 15;
            db.SaveChanges();

            var outputMessage = String.Format("Player {0} guessed the word: {1}, starting new round.", player.Name, game.CurrentWord);
            Clients.Group(game.Name).onGuessedWord(outputMessage);

            NewRound(game);
        }

        private void NewRound(Game game)
        {
            var players = game.Players;
            var currentDrawer = game.Players.FirstOrDefault(p => p.IsHisTurn);
            //If a new game

            if (currentDrawer == null)
            {
                players.FirstOrDefault().IsHisTurn = true;
            }

            else
            {
                //If the player is the last one, start next round from the first

                var currentDrawerIndex = players.ToList().FindIndex(p => p.Id == currentDrawer.Id);
                if (currentDrawerIndex == players.Count - 1)
                {
                    currentDrawer.IsHisTurn = false;
                    players.FirstOrDefault().IsHisTurn = true;
                    if (game.CurrentTurnNumber == 2)
                    {
                        EndGame(game);
                    }
                }
                else
                {
                    players.ToList()[currentDrawerIndex + 1].IsHisTurn = true;
                }
            }

            currentDrawer = game.Players.FirstOrDefault(p => p.IsHisTurn);

            //Set properties

            game.CurrentWord = GenerateRandomWord();
            game.CurrentPattern = GeneratePattern(game.CurrentWord);

            game.EndOfCurrentTurn = DateTime.Now.AddSeconds(40);
            game.WordIsGuessed = false;
            db.SaveChanges();

            //Send the word and pattern to clients
            Clients.Client(currentDrawer.ConnectionIdentifier).becomeDrawer(game.CurrentWord);
            Clients.AllExcept(currentDrawer.ConnectionIdentifier).becomeGuesser(game.CurrentPattern);
        }

        private void EndGame(Game game)
        {
            var outputMessage = GenerateEndGameMessage(game.Players.ToList());
            Clients.Group(game.Name).gameOver(outputMessage);
        }

        private IEnumerable<object> GetPlayersOutput(ICollection<Player> players)
        {
            return players.Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                IsHisTurn = p.IsHisTurn,
                Score = p.Score
            });
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

        private void UpdateGamesToAll()
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
            Clients.All.initializeRooms(jsonRooms);
        }

        private string GenerateRandomWord()
        {
            var allWords = db.Words.ToList();
            var rnd = new Random();
            return allWords[rnd.Next(1, allWords.Count)].WordText;
        }
    }
}