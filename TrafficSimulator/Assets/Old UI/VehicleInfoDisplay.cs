using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using User;
using VehicleBrain;

// Import the TextMesh Pro namespace

public class VehicleInfoDisplay : MonoBehaviour
{
    [HideInInspector] public AutoDrive VehicleAutoDrive;
    [HideInInspector] public FuelConsumption VehicleFuelConsumption;
    
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private InfoField _roadNameField;
    [SerializeField] private InfoField _activityField; 
    [SerializeField] private InfoField _totalDistanceField;
    [SerializeField] private InfoField _fuelAmountField;
    
    private void Start()
    {
        UserSelectManager.Instance.OnSelectedGameObject += ToggledVehicle;

        // Update the info since a vehicle may currently be selected
        ToggledVehicle(UserSelectManager.Instance.SelectedGameObject);
    }

    private void Update()
    {
        if(VehicleAutoDrive != null) 
            UpdateTotalDistanceText();
    }

    private void ToggledVehicle(Selectable selectable)
    {
        if (selectable == null)
        {
            if(VehicleAutoDrive != null)
                Unsubscribe(VehicleAutoDrive);
           
            VehicleAutoDrive = null;
            ClearVehicleInfo();
            return;
        }

        bool isOtherAutoDrive = selectable.gameObject.GetComponent<AutoDrive>() && !selectable.gameObject.GetComponent<AutoDrive>().Equals(VehicleAutoDrive);
        
        if (VehicleAutoDrive == null || isOtherAutoDrive)
        {
            if (VehicleAutoDrive != null)
                Unsubscribe(VehicleAutoDrive);

            VehicleAutoDrive = selectable.GetComponent<AutoDrive>();
            VehicleFuelConsumption = selectable.GetComponent<FuelConsumption>();
            Subscribe(VehicleAutoDrive);

            UpdateName();
            UpdateRoadName();
            UpdateActivityText();
            UpdateFuelAmountText();
        }
    }

    private void Subscribe(AutoDrive autoDrive)
    {
        if(autoDrive == null)
            return;

        VehicleAutoDrive.Agent.Context.OnActivityChanged += UpdateActivityText;
        VehicleAutoDrive.Agent.Context.OnRoadChanged += UpdateRoadName;
    }

    private void Unsubscribe(AutoDrive autoDrive)
    {
        if(autoDrive == null)
            return;

        VehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;
        VehicleAutoDrive.Agent.Context.OnRoadChanged -= UpdateRoadName;
    }

    private void ClearVehicleInfo()
    {
        _name.enabled = false;
        _fuelAmountField.Hide();
        _roadNameField.Hide();
        _totalDistanceField.Hide();

        // The vehicle activity text is the most centered, so use it temporarily until a vehicle is selected
        _activityField.DisplayNoHeader("No vehicle selected");
    }

    private void UpdateFuelAmountText()
    {
        _fuelAmountField.Display(VehicleFuelConsumption.CurrentFuelAmount, "L", 1);
    }
    
    private void UpdateActivityText()
    {
        _activityField.Display(VehicleAutoDrive.GetVehicleActivityDescription());
    }

    private void UpdateName()
    {
        _name.enabled = true;
        _name.text = VehicleAutoDrive.name;
    }

    private void UpdateRoadName()
    {
        _roadNameField.Display(VehicleAutoDrive.Agent.Context.CurrentRoad?.name ?? "N/A");
    }

    private void UpdateTotalDistanceText()
    {
        _totalDistanceField.Display(VehicleAutoDrive.TotalDistance / 1000, "km", 2);
    }
}