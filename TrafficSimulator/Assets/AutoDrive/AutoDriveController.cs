using RoadGenerator;
using System;
using UnityEngine;

namespace VehicleBrain
{
    public interface IAutoDriveController<T> where T : System.Enum
    {
        abstract (T, bool) ShouldActImplementation(ref AutoDriveAgent agent);
        abstract Func<LaneNode, (T, bool)> EventAssessor(ref AutoDriveAgent agent, T type);
    }
    public abstract class AutoDriveController<T> : IAutoDriveController<T> where T : System.Enum
    {
        public abstract (T, bool) ShouldActImplementation(ref AutoDriveAgent agent);
        private readonly T[] _eventTypes = (T[])Enum.GetValues(typeof(T));

        public abstract Func<LaneNode, (T, bool)> EventAssessor(ref AutoDriveAgent agent, T type);
        public Action OnNavigationUpdate { get; set; }
        public Action<T> OnEvent;
        public Action OnEventClear;

        private T _lastEvent = default;
        private bool _lastEventNull = true;
        
        protected (T, bool) ShouldActAtNode(ref AutoDriveAgent agent, LaneNode node)
        {
            // Check all events and return true if any of them returns true
            foreach(T type in _eventTypes)
            {
                (T actingType, bool result) = EventAssessor(ref agent, type)(node);
                if(result)
                    return (actingType, true);
            }
            
            return (default, false);
        }

        public bool ShouldAct(ref AutoDriveAgent agent)
        {
            (T actingType, bool shouldAct) = ShouldActImplementation(ref agent);
            
            if(shouldAct)
                UpdateEvent(actingType);
            else
                ClearEvent();

            return shouldAct;
        }

        private void UpdateEvent(T type)
        {
            if(_lastEventNull || !type.Equals(_lastEvent))
            {
                OnEvent?.Invoke(type);
                _lastEventNull = false;
                _lastEvent = type;
            }
        }

        private void ClearEvent()
        {
            if(!_lastEventNull)
            {
                OnEventClear?.Invoke();
                _lastEventNull = true;
            }
        }
    }
}