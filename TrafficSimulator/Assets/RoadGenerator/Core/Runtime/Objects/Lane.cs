using UnityEngine;
using System;

namespace RoadGenerator
{
    public enum LaneSide
    {
        PRIMARY,
        SECONDARY
    }
    
    public class LaneType
    {
        public readonly LaneSide Side;
        public readonly int Index;
        public LaneType(LaneSide side, int index) => (this.Side, this.Index) = (side, Math.Abs(index));
    }
	
    public class Lane
	{
        private VertexPath _path;
        private Road _road;
        private LaneType _type;
        public Lane(LaneType type, VertexPath path) => (this._type, this._path) = (type, path);

        public VertexPath Path
        {
            get => _path;
        }
        public LaneType Type
        {
            get => _type;
        }
    }
}