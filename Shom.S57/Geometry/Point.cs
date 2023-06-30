﻿using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace S57
{
    public class Point : Geometry
    {
        public Point()
        {
            X = 0;
            Y = 0;
        }

        public Point( double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X;
        public double Y;

        public override string ToString()
        {
            return $"Point ({X}, {Y})";
        }
    }
}
