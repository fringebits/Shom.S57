using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace S57
{
    public abstract class PointArray : Geometry
    {
        public List<Point> Points { get; private set; } = new List<Point>();
    }
}
