
namespace S57
{
    using MapCore;
    using Shom.S57.Geometry;
    using Utilities;
    using SimpleLogger;

    public abstract class Geometry : IGeometry, IOutToLog
    {
        public virtual void OutToLog(int depth)
        {
            Logger.Log($"{this}");
        }

        //public abstract BoundingBox BoundingBox { get; }
    }
}
