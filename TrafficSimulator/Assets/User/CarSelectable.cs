using UnityEngine;
using Car;

namespace User
{
    /// <summary>
    ///     Communication interface to allow a car to be selected by the user. 
    /// </summary>
    public class CarSelectable : Selectable
    {
        private Outline _selectOutline;
        [field: SerializeField] public Transform FirstPersonPivot { get; private set; }

        private void Start()
        {
            _selectOutline = GetComponentInChildren<Outline>();
            SetOutline(false);
        }

        public override void Select()
        {
            SetOutline(true);
        }

        public override void Deselect()
        {
            SetOutline(false);
        }

        private void SetOutline(bool isActive)
        {
            _selectOutline.enabled = isActive;
        }
    }
}