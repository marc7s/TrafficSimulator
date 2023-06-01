using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using User;
using VehicleBrain;

// Import the TextMesh Pro namespace

public class VehicleInfoDisplay : MonoBehaviour
{
    public AutoDrive vehicleAutoDrive;
    public FuelConsumption vehicleFuelConsumption;
    
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private InfoField RoadNameField;
    [SerializeField] private InfoField ActivityField; 
    [SerializeField] private InfoField TotalDistanceField;
    [SerializeField] private InfoField FuelAmountField;
    
    private void Start()
    {
        UserSelectManager.Instance.OnSelectedGameObject += ToggledVehicle;

        // Update the info since a vehicle may currently be selected
        ToggledVehicle(UserSelectManager.Instance.SelectedGameObject);
    }

    private void Update()
    {
        if(vehicleAutoDrive != null) 
            UpdateTotalDistanceText();
    }

    private void ToggledVehicle(Selectable selectable)
    {
        if (selectable == null)
        {
            if(vehicleAutoDrive != null)
                Unsubscribe(vehicleAutoDrive);
           
            vehicleAutoDrive = null;
            ClearVehicleInfo();
            return;
        }

        bool isOtherAutoDrive = selectable.gameObject.GetComponent<AutoDrive>() && !selectable.gameObject.GetComponent<AutoDrive>().Equals(vehicleAutoDrive);
        
        if (vehicleAutoDrive == null || isOtherAutoDrive)
        {
            if (vehicleAutoDrive != null)
                Unsubscribe(vehicleAutoDrive);

            vehicleAutoDrive = selectable.GetComponent<AutoDrive>();
            vehicleFuelConsumption = selectable.GetComponent<FuelConsumption>();
            Subscribe(vehicleAutoDrive);

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

        vehicleAutoDrive.Agent.Context.OnActivityChanged += UpdateActivityText;
        vehicleAutoDrive.Agent.Context.OnRoadChanged += UpdateRoadName;
    }

    private void Unsubscribe(AutoDrive autoDrive)
    {
        if(autoDrive == null)
            return;

        vehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;
        vehicleAutoDrive.Agent.Context.OnRoadChanged -= UpdateRoadName;
    }

    private void ClearVehicleInfo()
    {
        Name.enabled = false;
        FuelAmountField.Hide();
        RoadNameField.Hide();
        TotalDistanceField.Hide();

        // The vehicle activity text is the most centered, so use it temporarily until a vehicle is selected
        ActivityField.DisplayNoHeader("No vehicle selected");
    }

    private void UpdateFuelAmountText()
    {
        FuelAmountField.Display(vehicleFuelConsumption.CurrentFuelAmount, "L", 1);
    }
    
    private void UpdateActivityText()
    {
        ActivityField.Display(vehicleAutoDrive.GetVehicleActivityDescription());
    }

    private void UpdateName()
    {
        Name.enabled = true;
        Name.text = vehicleAutoDrive.name;
    }

    private void UpdateRoadName()
    {
        RoadNameField.Display(vehicleAutoDrive.Agent.Context.CurrentRoad?.name ?? "N/A");
    }

    private void UpdateTotalDistanceText()
    {
        TotalDistanceField.Display(vehicleAutoDrive.TotalDistance / 1000, "km", 2);
    }
}