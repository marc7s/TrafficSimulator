using UnityEngine;

namespace User
{
    /// <summary>
    ///     Communication interface to allow a car to be selected by the user. 
    /// </summary>
    public class CarSelectable : Selectable
    {
        private Outline _selectOutline;
        [SerializeField] private Transform _firstPersonPivot;

        private void Start()
        {
            _selectOutline = GetComponentInChildren<Outline>();
            OutlineIsActive(false);
        }

        public override void Select()
        {
            OutlineIsActive(true);
        }

        public override void Deselect()
        {
            OutlineIsActive(false);
        }

        private void OutlineIsActive(bool isActive)
        {
            _selectOutline.enabled = isActive;
        } 
        
        public Transform GetFirstPersonPivot()
        {
            return _firstPersonPivot;
        }
    }
}