namespace S57
{
    using SimpleLogger;
    using System.Collections.Generic;

    public class FlatPolygon : PointArray
    {
        public List<int> HolesIndices { get; private set; } = new List<int>();

        public override string ToString()
        {
            return $"FlatPolygon Points={this.Points.Count}, Holes={this.HolesIndices.Count}";
        }
    }
}
