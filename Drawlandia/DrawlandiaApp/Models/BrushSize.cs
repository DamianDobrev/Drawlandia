using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DrawlandiaApp.Models
{
    public class BrushSize
    {
        public BrushSize()
        {
            
        }

        public BrushSize(int size)
        {
            this.Size = size;
        }

        public int Id { get; set; }

        public int Size { get; set; }
    }
}