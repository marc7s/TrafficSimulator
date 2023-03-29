using UnityEngine;
using System.Collections.Generic;

namespace RoadGenerator
{
    /// <summary>A generic implementation of a node</summary>
    public abstract class Node<T> where T : Node<T>
    {
        protected T _next;
        protected T _prev;
        protected Vector3 _position;
        protected Quaternion _rotation;
        protected float _distanceToPrevNode;
        protected string _id;

        /// <summary>Gets the next node</summary>
        public virtual T Next
        {
            get => _next;
            set => _next = value;
        }
        
        /// <summary>Returns the previous node</summary>
        public virtual T Prev
        {
            get => _prev;
            set => _prev = value;
        }
        
        /// <summary>Returns the position of this node</summary>
        public Vector3 Position
        {
            get => _position;
        }

        /// <summary>Returns the rotation of this node</summary>
        public Quaternion Rotation
        {
            get => _rotation;
        }

        /// <summary>Returns the number of linked nodes starting at this node</summary>
        public int Count
        {
            get
            {
                int count = 1;
                T curr = (T)this;
                
                while(curr.Next != null)
                {
                    count++;
                    curr = curr.Next;
                }
                return count;
            }
        }

        /// <summary>Returns the ID of this node</summary>
        public string ID
        {
            get => _id;
        }

        /// <summary>Returns the first node in the linked list</summary>
        public T First
        {
            get
            {
                T curr = (T)this;
                
                while(curr.Prev != null)
                {
                    curr = curr.Prev;
                }
                return curr;
            }
        }

        /// <summary>Returns the last node in the linked list</summary>
        public T Last
        {
            get
            {
                T curr = (T)this;
                
                while(curr.Next != null)
                {
                    // If the linked list is closed, return the node before this one
                    if (curr.Next == (T)this)
                        return curr;
                    curr = curr.Next;
                }
                return curr;
            }
        }

        /// <summary>Returns the distance to the next node</summary>
        public float DistanceToPrevNode
        {
            get => _distanceToPrevNode;
            set => _distanceToPrevNode = value;
        }

        /// <summary>Returns all linked node positions as an array</summary>
        public Vector3[] GetPositions()
        {
            List<Vector3> points = new List<Vector3>();
            T curr = (T)this;
            
            while(curr != null)
            {
                points.Add(curr.Position);
                curr = curr.Next;
            }
            return points.ToArray();
        }

        /// <summary>Returns all linked node rotations as an array</summary>
        public Quaternion[] GetRotations()
        {
            List<Quaternion> rotations = new List<Quaternion>();
            T curr = (T)this;
            
            while(curr != null)
            {
                rotations.Add(curr.Rotation);
                curr = curr.Next;
            }
            return rotations.ToArray();
        }

        /// <summary>Reverses the linked nodes. Returns the head of the reversed nodes</summary>
        public T Reverse()
        {
            T curr = Copy();
            T prev = null;
            T next = null;
            
            while(curr != null)
            {
                next = curr.Next?.Copy();
                
                // Recalculate the distance to the previous node
                if(next != null)
                    curr._distanceToPrevNode = Vector3.Distance(next.Position, curr.Position);
                
                curr.Next = prev;
                curr.Prev = next;
                prev = curr;
                curr = next;
            }
            // This will be the first node so reset the distance to previous node
            prev._distanceToPrevNode = 0;
            
            return prev;
        }

        /// <summary>Override the generic equals for this class</summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as T);
        }

        /// <summary>Define a custom equality function between Vehicles</summary>
        public bool Equals(T other)
        {
            return other != null && other.ID == ID;
        }

        /// <summary>Override the hashcode function for this class</summary>
        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public abstract T Copy();
    }
}