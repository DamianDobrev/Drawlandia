using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbInitializer
{
    class Program
    {
        static void Main(string[] args)
        {
            var db = new DrawlandiaContext();

            var anyPlayers = db.Players.Any(p => p.Name == "asd");
        }
    }
}
