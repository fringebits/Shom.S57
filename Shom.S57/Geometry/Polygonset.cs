using SimpleLogger;
using System;
using System.Collections.Generic;

namespace S57
{
    public class PolygonSet : Geometry
    {
        public List<Area> Areas { get; private set; } = new List<Area>();

        public override string ToString()
        {
            return $"PolygonSet Areas={this.Areas.Count}";
        }
    }
}

