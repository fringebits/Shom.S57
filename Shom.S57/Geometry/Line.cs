using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace S57
{
    public class Line : PointArray
    {
        public override string ToString()
        {
            return $"Line {this.Points.Count}";
        }
    }
}
