using UnityEngine;
using VehicleBrain;

namespace User
{
    /// <summary>
    ///     Communication interface to allow a car to be selected by the user. 
    /// </summary>
    [RequireComponent(typeof(AutoDrive))]
    public class CarSelectable : Selectable
    {
        private Outline _selectOutline;
        private AutoDrive _autoDrive;
        [field: SerializeField] public Transform FirstPersonPivot { get; private set; }

        private void Start()
        {
            _selectOutline = GetComponentInChildren<Outline>();
            _autoDrive = GetComponent<AutoDrive>();
            SetOutline(false);
        }

        public override void Select()
        {
            SetOutline(true);
            SetNavigationPathVisibility(true);
        }

        public override void Deselect()
        {
            SetOutline(false);
            SetNavigationPathVisibility(false);
        }

        private void SetOutline(bool isActive)
        {
            _selectOutline.enabled = isActive;
        }

        private void SetNavigationPathVisibility(bool active)
        {
            _autoDrive.Agent.Context.ShowNavigationPath = active;
        }
    }
}