using UnityEngine;
using RoadGenerator;
using System.Collections.Generic;

namespace POIs
{
    public enum RoadSideParkingType
    {
        Length,
        FullRoad
    }

    public class RoadsideParking : Parking
    {
        [Header("Settings")]
        public float ParkingLength = _parkingSize.y * 3;
        public RoadSideParkingType ParkingType = RoadSideParkingType.Length;
        public bool AllowStartingSmoothEdge = true;
        public bool AllowEndingSmoothEdge = true;

        private List<RoadNode> _spanNodes = new List<RoadNode>();

        private int _sideCoef => (LaneSide == LaneSide.Primary ? -1 : 1) * (int)RoadNode.Road.RoadSystem.DrivingSide;
        private float _smoothEdgeOffset => _parkingSize.y / 2;
        private bool _hasStartingSmoothEdge = true;
        private bool _hasEndingSmoothEdge = true;
        Dictionary<RoadNode, POINode> _parkingSpaceMap = new Dictionary<RoadNode, POINode>();

        protected override void ParkingSetup()
        {
            _useCustomSize = true;
        }

        protected override Vector3 GetSize()
        {
            return new Vector3(2.15f, 0.1f, ParkingType == RoadSideParkingType.Length || RoadNode == null ? ParkingLength : RoadNode.Road.Length);
        }

        protected override void GenerateParkingSpots()
        {
            if(RoadNode == null)
                return;
            
            CalculateSpanNodes();
            _parkingSpots.Clear();
            _parkingSpaceMap.Clear();
            
            int parkingSpots = Mathf.FloorToInt(Size.z / _parkingSize.y);
            float forwardOffsetDelta = Size.z / parkingSpots;
            float sideOffset = _parkingSize.x / 2;

            RoadNode startNode = _spanNodes[_hasStartingSmoothEdge ? 1 : 0];
            RoadNode endNode = _spanNodes[_spanNodes.Count - 1 - (_hasEndingSmoothEdge ? 1 : 0)];
            Vector3 startPos = GetEdgePosition(startNode, sideOffset);
            Vector3 endPos = GetEdgePosition(endNode, sideOffset);
            POINode lastParkingSpot = null;

            //_parkingSpaceEdges.Add(startNode);
            
            // Go through all span nodes, ignoring the smooth edge nodes
            for(int i = 1; i < _spanNodes.Count - 1; i++)
            {
                RoadNode curr = _spanNodes[i];

                Vector3 newPosition = GetEdgePosition(curr, sideOffset);
                bool first = lastParkingSpot == null;

                //if(Vector3.Distance(GetEdgePosition(_parkingSpaceEdges[_parkingSpaceEdges.Count - 1], sideOffset), newPosition) >= forwardOffsetDelta)
                //    _parkingSpaceEdges.Add(curr);
                
                if(first || Vector3.Distance(lastParkingSpot.Position, newPosition) >= forwardOffsetDelta)
                {
                    // Do not add the first spot if it is too close to the start
                    if(first && Vector3.Distance(startPos, newPosition) < _parkingSize.y / 2)
                        continue;

                    // Do not add the spot if it is too close to the end
                    if(Vector3.Distance(endPos, newPosition) < _parkingSize.y / 2)
                        continue;
                    
                    Quaternion rotation = curr.Rotation * (LaneSide == LaneSide.Secondary ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
                    POINode parkingSpot = new POINode(newPosition, rotation);
                    _parkingSpots.Add(parkingSpot);
                    lastParkingSpot = parkingSpot;

                    _parkingSpaceMap.Add(curr, parkingSpot);
                }
            }
            
            if(ParkingType == RoadSideParkingType.FullRoad)
                DistanceAlongRoad = RoadNode.Road.Length / 2;
        }

        private float? GetDistanceToNextParkingSpot(RoadNode roadNode, bool primaryDirection)
        {
            RoadNode curr = primaryDirection ? roadNode.Next : roadNode.Prev;
            float distance = 0;
            while(curr != null)
            {
                distance += Vector3.Distance(curr.Position, primaryDirection ? curr.Prev.Position : curr.Next.Position);
                if(_parkingSpaceMap.ContainsKey(curr))
                    return distance;
                curr = primaryDirection ? curr.Next : curr.Prev;
            }
            return null;
        }
        protected override void CreateParkingMesh()
        {
            if(_spanNodes.Count == 0)
                return;
            
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();
            bool parkingStart = true;
            POINode prevParking = _parkingSpots[0];
            bool reachedEndSpot = false;
            float distance = 0;
            bool primaryDirection = LaneSide == LaneSide.Primary;
            float? distanceToNextParkingSpot = GetDistanceToNextParkingSpot(_spanNodes[1], primaryDirection);
            float distanceToNextParkingSpotOriginal = distanceToNextParkingSpot.Value;
            RoadNode prev = _spanNodes[_hasStartingSmoothEdge ? 1 : 0];

            for(int i = 0; i < _spanNodes.Count; i++)
            {
                RoadNode vertNode = _spanNodes[i];
                Vector3 roadSide = GetEdgePosition(vertNode);
                Vector3 outside = GetEdgePosition(vertNode, _parkingSize.x);

                bool first = i == 0;
                bool last = i == _spanNodes.Count - 1;
                
                // Add a single inner vertex if it is a smooth edge, otherwise add both
                if((first && _hasStartingSmoothEdge) || (last && _hasEndingSmoothEdge))
                {
                    verts.Add(roadSide);
                    float uv = first && primaryDirection ? 1 : 0;

                    uvs.Add(new Vector2(0, uv));
                }
                else
                {
                    verts.Add(roadSide);
                    verts.Add(outside);
                    bool parkingEdge = _parkingSpaceMap.ContainsKey(vertNode);

                    if(i != 0)
                        distanceToNextParkingSpot -= vertNode.DistanceToPrevNode;

                    float forwardUV = primaryDirection ? 1 : 0;
                    if (distanceToNextParkingSpot != null)
                        forwardUV = distanceToNextParkingSpot.Value / distanceToNextParkingSpotOriginal;
                    



                    Debug.Log("Distance to next parking spot: " + distanceToNextParkingSpot);

                   // Debug.Log(forwardUV);
                    float uvY;

                    if (parkingEdge)
                        uvY = parkingStart ? 0 : 1;
                    else
                        uvY = parkingStart ? forwardUV : 1 - forwardUV;
           

                    
                    if(parkingEdge)
                    {
                        prevParking = _parkingSpaceMap[vertNode];
                        parkingStart = !parkingStart;
  
                        distanceToNextParkingSpot = GetDistanceToNextParkingSpot(vertNode, primaryDirection);
                        if (distanceToNextParkingSpot == null && !reachedEndSpot)
                        {
                            reachedEndSpot = true;
                            distanceToNextParkingSpot = _parkingSize.y;
                        }

                        distanceToNextParkingSpotOriginal = distanceToNextParkingSpot ?? -1;
                        //distance = 0;
                    }



                    uvs.Add(new Vector2(0, uvY));
                    uvs.Add(new Vector2(1, uvY));
                }
            }

            for(int i = 0; i < verts.Count; i++)
                normals.Add(Vector3.up);

            // Add smooth entry curve
            if(_hasStartingSmoothEdge)
                tris.AddRange(new List<int>{ 0, 1, 2 });

            int startIndex = _hasStartingSmoothEdge ? 1 : 0;
            int endOffset = _hasEndingSmoothEdge ? 1 : 0;
            int vertIndex = startIndex;
            
            for(int i = startIndex; i < _spanNodes.Count - 1 - endOffset; i++, vertIndex += 2)
            {
                tris.AddRange(new List<int>{ vertIndex, vertIndex + 2, vertIndex + 1 });
                tris.AddRange(new List<int>{ vertIndex + 1, vertIndex + 2, vertIndex + 3 });
            }

            int lastLeft = verts.Count - 3;
            
            // Add smooth exit curve
            if(_hasEndingSmoothEdge)
                tris.AddRange(new List<int>{ lastLeft, lastLeft + 2, lastLeft + 1 });

            _mesh.Clear();
            _mesh.vertices = verts.ToArray();
            _mesh.normals = normals.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(tris, 1);
        }

        /// <summary> Returns the position at the edge of the road </summary>
        private Vector3 GetEdgePosition(RoadNode node, float offset = 0)
        {
            return node.Position + node.Normal * _sideCoef * (offset + node.Road.RoadWidth / 2);
        }

        private float GetEdgeDistance(RoadNode node1, RoadNode node2)
        {
            return Vector3.Distance(GetEdgePosition(node1), GetEdgePosition(node2));
        }

        private void CalculateSpanNodes()
        {
            if(ParkingType == RoadSideParkingType.FullRoad)
            {
                _spanNodes = GetFullRoadSpanNodes();
                return;
            }

            List<RoadNode> nodes = new List<RoadNode>();
            RoadNode curr = RoadNode.Prev;
            RoadNode prev = RoadNode;
            float distance = 0;
            const float buffer = 1.5f;
            float offset = _parkingSize.x / 2 + buffer;

            // Add the nodes behind
            while(curr != null && distance <= Size.z / 2 + offset)
            {
                nodes.Add(curr);
                distance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Prev;
            }

            float smoothEdgeDistance = 0;
            
            // Move back until the smooth edge node
            while(curr != null && smoothEdgeDistance <= _smoothEdgeOffset)
            {
                smoothEdgeDistance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Prev;
            }
            
            // Set the starting smooth edge depending on if we have enough distance in this direction
            _hasStartingSmoothEdge = AllowStartingSmoothEdge && smoothEdgeDistance >= _smoothEdgeOffset && curr != null;
            
            // Add the smooth edge node
            if(_hasStartingSmoothEdge)
                nodes.Add(curr);
            
            nodes.Reverse();
            
            // Add the center node
            nodes.Add(RoadNode);

            curr = RoadNode.Next;
            prev = RoadNode;
            distance = 0;
            
            // Add the nodes in front
            while(curr != null && distance <= Size.z / 2 + offset)
            {
                nodes.Add(curr);
                distance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Next;
            }

            smoothEdgeDistance = 0;

            // Move forward until the smooth edge node
            while(curr != null && smoothEdgeDistance <= _smoothEdgeOffset)
            {
                smoothEdgeDistance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Next;
            }

            // Set the starting smooth edge depending on if we have enough distance in this direction
            _hasEndingSmoothEdge = AllowEndingSmoothEdge && smoothEdgeDistance >= _smoothEdgeOffset && curr != null;

            // Add the smooth edge node
            if(_hasEndingSmoothEdge)
                nodes.Add(curr);

            if(LaneSide == LaneSide.Secondary)
                nodes.Reverse();

            _spanNodes = nodes;
        }

        private List<RoadNode> GetFullRoadSpanNodes()
        {
            List<RoadNode> nodes = new List<RoadNode>();
            RoadNode prev = RoadNode.First;
            RoadNode curr = prev.Next;

            RoadNode lastBeforeSmoothEdge = null;

            bool roadTooShortForSmoothEdges = RoadNode.Road.Length < _smoothEdgeOffset * 2 + _parkingSize.y * 2;
            _hasStartingSmoothEdge = AllowStartingSmoothEdge && !roadTooShortForSmoothEdges;
            _hasEndingSmoothEdge = AllowEndingSmoothEdge && !roadTooShortForSmoothEdges;

            float distance = 0;

            if(_hasEndingSmoothEdge)
            {
                prev = RoadNode.Last;
                curr = prev.Prev;

                // Find the last node before the ending smooth edge
                distance = 0;
                
                while(curr != null && distance <= _smoothEdgeOffset)
                {
                    distance += GetEdgeDistance(curr, prev);
                    prev = curr;
                    curr = curr.Prev;
                }
                
                lastBeforeSmoothEdge = curr;
            }

            prev = RoadNode.First;
            curr = prev;

            if(_hasStartingSmoothEdge)
            {
                // Add the first smooth edge node
                nodes.Add(RoadNode.First);

                prev = RoadNode.First;
                curr = prev.Next;
                
                // Move the curr node forward until the smooth edge is over
                distance = 0;
                while(curr != null && distance <= _smoothEdgeOffset)
                {
                    distance += GetEdgeDistance(curr, prev);
                    prev = curr;
                    curr = curr.Next;
                }
            }

            // Add all nodes between the smooth edges
            while(curr != null && curr != lastBeforeSmoothEdge)
            {
                nodes.Add(curr);
                curr = curr.Next;
            }

            // Add the last smooth edge node
            if(_hasEndingSmoothEdge)
                nodes.Add(RoadNode.Last);

            if(LaneSide == LaneSide.Secondary)
                nodes.Reverse();
            
            return nodes;
        }
    }
}