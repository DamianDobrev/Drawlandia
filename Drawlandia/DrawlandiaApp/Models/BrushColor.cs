using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DrawlandiaApp.Models
{
    public class BrushColor
    {
        public BrushColor()
        {
            
        }

        public BrushColor(string colorHex)
        {
            this.ColorHex = colorHex;
        }
        public int Id { get; set; }

        public string ColorHex { get; set; }
    }
}