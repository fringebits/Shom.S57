using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace S57
{
    public class Area : PointArray
    {
        public override string ToString()
        {
            return $"Area {this.Points.Count}";
        }
    }
}
