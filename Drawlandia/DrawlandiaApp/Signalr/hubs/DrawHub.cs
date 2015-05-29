using System;
using System.Linq;
using DrawlandiaApp.Models;
using Microsoft.AspNet.SignalR;

namespace DrawlandiaApp.Signalr.hubs
{
    public class DrawHub : Hub
    {
        DrawlandiaContext db = new DrawlandiaContext();

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