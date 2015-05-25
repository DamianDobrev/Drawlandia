using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Script.Serialization;
using DrawlandiaApp.Models;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace DrawlandiaApp.Signalr.hubs
{
    public class RoomsHub : Hub
    {
        DrawlandiaContext db = new DrawlandiaContext();

        public void GetAllRooms()
        {
            var rooms = db.Rooms.OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    Id = r.Id,
                    Name = r.Name,
                    Players = r.Players,
                    HasPassword = !String.IsNullOrEmpty(r.Password)
                });
            var jsonSerialiser = new JavaScriptSerializer();
            var jsonRooms = jsonSerialiser.Serialize(rooms);
            Clients.Caller.initializeRooms(jsonRooms);
        }

        public void UpdateRoomsToAll()
        {
            var rooms = db.Rooms.OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    Id = r.Id,
                    Name = r.Name,
                    Players = r.Players,
                    HasPassword = !String.IsNullOrEmpty(r.Password)
                });
            var jsonSerialiser = new JavaScriptSerializer();
            var jsonRooms = jsonSerialiser.Serialize(rooms);
            Clients.All.initializeRooms(jsonRooms);
        }

        public void GoToRoom(int id)
        {
            var rooms = db.Rooms;
            var ourRoom = rooms.FirstOrDefault(r => r.Id == id);

            //Check if room exists
            if (ourRoom == null)
            {
                Clients.Caller.errorWithMsg("No such room.");
                return;
            }

            var jsonSerialiser = new JavaScriptSerializer();
            var outputRoom = new
                {
                    Id = ourRoom.Id,
                    Name = ourRoom.Name,
                    Players = ourRoom.Players,
                    HasPassword = !String.IsNullOrEmpty(ourRoom.Password)
                };

            var outputJson = jsonSerialiser.Serialize(outputRoom);
            Clients.Caller.initRoom(outputJson);
        }

        public void CreateRoom(string name, string password, string creatorName)
        {
            var rooms = db.Rooms;
            if (String.IsNullOrEmpty(creatorName))
            {
                Clients.Caller.errorWithMsg("Invalid name, please refresh the page.");
                return;
            }

            if (rooms.Any(room => room.Name == name))
            {
                Clients.Caller.errorWithMsg("Sorry, but it seems like there is already a room with that name :(");
                return;
            }

            var roomToCreate = new Room(name, password);

            var player = new Player(Context.ConnectionId, creatorName);
            roomToCreate.Players = new List<Player>();
            roomToCreate.Players.Add(player);
            rooms.Add(roomToCreate);
            db.Rooms.Add(roomToCreate);
            db.SaveChanges();

            Groups.Add(Context.ConnectionId, name);

            var roomOutput = rooms.FirstOrDefault(r => r.Name == roomToCreate.Name);
            GoToRoom(roomOutput.Id);

            UpdateRoomsToAll();
        }

        public void JoinRoom(int roomId, string password, string playerName)
        {
            var rooms = db.Rooms;
            var players = db.Players;

            var roomToJoin = rooms.FirstOrDefault(room => room.Id == roomId);

            //Check if player exists in db
            if (players.Any(p => p.ConnectionId == Context.ConnectionId))
            {
                Clients.Caller.errorWithMsg("You are already in a room.");
                return;
            }

            //Check player name
            if (String.IsNullOrEmpty(playerName))
            {
                Clients.Caller.errorWithMsg("Player name is empty.");
                return;
            }

            //Check if room exists or game started
            if (roomToJoin == null || roomToJoin.State == RoomState.Started)
            {
                Clients.Caller.errorWithMsg("The room no longer exists or the game has started.");
                return;
            }

            //Check if people in room reached 6
            if (roomToJoin.Players.Count >= 6)
            {
                Clients.Caller.errorWithMsg("Room is full.");
                return;
            }

            //Authorization
            if (!String.IsNullOrEmpty(roomToJoin.Password) && roomToJoin.Password != password)
            {
                Clients.Caller.errorWithMsg("Wrong password.");
                return;
            }

            var playerToBeJoined = new Player(Context.ConnectionId, playerName);

            roomToJoin.Players.Add(playerToBeJoined);
            Groups.Add(Context.ConnectionId, roomToJoin.Name);
            db.SaveChanges();

            Clients.Group(roomToJoin.Name).updatePlayers(roomToJoin.Players);
            GoToRoom(roomToJoin.Id);
        }

        public void LeaveRoom(int roomId)
        {
            var rooms = db.Rooms;
            var roomToLeave = rooms.FirstOrDefault(room => room.Id == roomId);

            //Check if room exists
            if (roomToLeave == null)
            {
                Clients.Caller.errorWithMsg("No such room.");
                return;
            }

            var playerLeaving = roomToLeave.Players.FirstOrDefault(pl => pl.ConnectionId == Context.ConnectionId);

            //Check if player exists in the room
            if (playerLeaving == null)
            {
                Clients.Caller.errorWithMsg("You are not in that room.");
                return;
            }

            //Check if game has started
            if (roomToLeave.State == RoomState.Started)
            {
                Clients.Caller.errorWithMsg("You can't leave a game that has already started.");
                return;
            }

            //Leave and remove room if no one left
            roomToLeave.Players.Remove(playerLeaving);
            if (!roomToLeave.Players.Any())
            {
                rooms.Remove(roomToLeave);
            }
            db.Players.Remove(playerLeaving);
            db.SaveChanges();

            Groups.Remove(Context.ConnectionId, roomToLeave.Name);

            Clients.Group(roomToLeave.Name).updatePlayers(roomToLeave.Players);

            Clients.Group(roomToLeave.Name).playSound("Disconnect");

            Clients.Caller.redirectToLobby();

            UpdateRoomsToAll();
            
        }

        public void SendMessage(string message)
        {
            var player = db.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            if (player == null)
            {
                Clients.Caller.errorWithMsg("Player doesn't exists, please refresh the page.");
                return;
            }

            var room = db.Rooms.FirstOrDefault(r => r.Id == player.RoomId);

            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            if (room == null)
            {
                Clients.Caller.errorWithMsg("Something went wrong...");
                return;
            }

            Clients.Group(room.Name).addMessage(player.Name, message);
        }

        //public void StartGame(string roomName)
        //{
        //    var roomToStart = rooms.FirstOrDefault(room => room.Name == roomName);

        //    //if (roomToStart != null && roomToStart.Players[0].ConnectionId == Context.ConnectionId)
        //    //{
        //    //    roomToStart.State = State.Started;
        //    //}
        //}
        public override Task OnDisconnected(bool stopCalled)
        {
            var players = db.Players;
            var rooms = db.Rooms;
            var dcPlayer = players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            //Check if dc player is not in db
            if (dcPlayer == null)
            {
                return Clients.Caller.errorWithMsg("Something went terribly wrong");
            }

            LeaveRoom(dcPlayer.RoomId);

            return Clients.Caller.errorWithMsg("Error");
        }
    }
}