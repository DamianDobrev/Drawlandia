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

            //Authorization
            if (!String.IsNullOrEmpty(roomToJoin.Password) && roomToJoin.Password != password)
            {
                Clients.Caller.errorWithMsg("Wrong password.");
                return;
            }

            var playerToBeJoined = new Player(Context.ConnectionId, playerName);
            roomToJoin.Players = new List<Player>();
            roomToJoin.Players.Add(playerToBeJoined);
            Groups.Add(Context.ConnectionId, roomToJoin.Name);
            db.SaveChanges();

            GoToRoom(roomToJoin.Id);
        }

        public void LeaveRoom(int roomId)
        {
            var roomToLeave = db.Rooms.FirstOrDefault(room => room.Id == roomId);

            //Check if room exists
            if (roomToLeave == null)
            {
                Clients.Caller.errorWithMsg("No such room.");
                return;
            }

            //Check if game has started
            if (roomToLeave.State == RoomState.Started)
            {
                Clients.Caller.errorWithMsg("You can't leave a game that has already started.");
                return;
            }

            //Leave
            var playerLeaving = roomToLeave.Players.FirstOrDefault(pl => pl.ConnectionId == Context.ConnectionId);
            roomToLeave.Players.Remove(playerLeaving);
            Groups.Remove(Context.ConnectionId, roomToLeave.Name);

            GetAllRooms();
        }

        //public void StartGame(string roomName)
        //{
        //    var roomToStart = rooms.FirstOrDefault(room => room.Name == roomName);

        //    //if (roomToStart != null && roomToStart.Players[0].ConnectionId == Context.ConnectionId)
        //    //{
        //    //    roomToStart.State = State.Started;
        //    //}
        //}
    }
}