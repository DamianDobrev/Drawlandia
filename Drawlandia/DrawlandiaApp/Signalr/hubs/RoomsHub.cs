using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using DrawlandiaApp.Models;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace DrawlandiaApp.Signalr.hubs
{
    public class RoomsHub : Hub
    {
        private List<Room> rooms = new List<Room>();
        public void GetAllRooms()
        {
            var jsonSerialiser = new JavaScriptSerializer();
            var jsonRooms = jsonSerialiser.Serialize(rooms);
            Clients.Caller.initializeRooms(jsonRooms);
        }

        public void GetRoomByName(string name)
        {
            var jsonSerialiser = new JavaScriptSerializer();
            if (rooms.Any(room => room.Name == name))
            {
                var jsonRooms = jsonSerialiser.Serialize(rooms.FirstOrDefault(room => room.Name == name));
                Clients.Caller.initializeRooms(jsonRooms);
            }
        }

        public Task CreateRoom(string name, string password, string creatorName)
        {
            if (rooms.Any(room => room.Name == name))
            {
                return null;
            }

            var roomToCreate = new Room(name, password);

            //First player is creator, if first player leaves -> the second one becomes the creator

            roomToCreate.Players.Add(new Player(Context.ConnectionId, creatorName));

            rooms.Add(roomToCreate);
            return Groups.Add(Context.ConnectionId, name); 
        }

        public Task JoinRoom(string name, string password, string playerName)
        {
            var roomToJoin = rooms.FirstOrDefault(room => room.Name == name);

            //Check if room exists
            if (roomToJoin == null)
            {
                return null;
            }

            //Check if game has started
            if (roomToJoin.State == State.Started)
            {
                return null;
            }

            //Authenticate
            if (roomToJoin.Password != password)
            {
                return null;
            }

            roomToJoin.Players.Add(new Player(Context.ConnectionId, playerName));
            return Groups.Add(Context.ConnectionId, name); 
        }

        public Task LeaveRoom(string roomName)
        {
            var roomToLeave = rooms.FirstOrDefault(room => room.Name == roomName);

            //Check if room exists
            if (roomToLeave == null)
            {
                return null;
            }

            //Check if game has started
            if (roomToLeave.State == State.Started)
            {
                return null;
            }

            //Leave
            var playerLeaving = roomToLeave.Players.FirstOrDefault(pl => pl.ConnectionId == Context.ConnectionId);

            roomToLeave.Players.Remove(playerLeaving);
            return Groups.Remove(Context.ConnectionId, roomName);
        }

        public void StartGame(string roomName)
        {
            var roomToStart = rooms.FirstOrDefault(room => room.Name == roomName);

            if (roomToStart != null && roomToStart.Players[0].ConnectionId == Context.ConnectionId)
            {
                roomToStart.State = State.Started;
            }
        }
    }
}