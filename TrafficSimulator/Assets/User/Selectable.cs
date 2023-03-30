using UnityEngine;

namespace User
{
    /// <summary>
    ///     Communication interface to allow a simulation object to be selected by the user. 
    /// </summary>
    public abstract class Selectable : MonoBehaviour
    {
        public abstract void Select();
        public abstract void Deselect();
    }
}