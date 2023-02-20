using UnityEngine;
using System.Collections.Generic;

namespace RoadGenerator
{
    /// <summary>A generic implementation of a node</summary>
    public class Node<T> where T : Node<T>
    {
        protected T _next;
        protected T _prev;
        protected Vector3 _position;
        protected Quaternion _rotation;

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

        /// <summary>Returns the last node in the linked list</summary>
        public T Last
        {
            get
            {
                T curr = (T)this;
                
                while(curr.Next != null)
                {
                    curr = curr.Next;
                }
                return curr;
            }
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

        /// <summary>Reverses the linked nodes. Returns the head of the reversed nodes</summary>
        public T Reverse()
        {
            T curr = (T)this;
            T prev = null;
            T next = null;
            
            while(curr != null)
            {
                next = curr.Next;
                curr.Next = prev;
                curr.Prev = next;
                prev = curr;
                curr = next;
            }
            return prev;
        }
    }
}