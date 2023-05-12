using System;
using TMPro;
using UnityEngine;
using User;
using VehicleBrain;

// Import the TextMesh Pro namespace

public class VehicleInfoDisplay : MonoBehaviour
{
    public AutoDrive vehicleAutoDrive; // Reference to your vehicle script
    public TextMeshProUGUI roadNameText; // Use TextMeshProUGUI instead of Text
    public TextMeshProUGUI activityText; // Use TextMeshProUGUI instead of Text
    public TextMeshProUGUI distanceTravelledText; // Use TextMeshProUGUI instead of Text
    
    private void Start()
    {
        UserSelectManager.Instance.OnSelectedGameObject += ToggledVehicle;
    }

    private void Update()
    {
        if(vehicleAutoDrive != null) UpdateDistanceTravelledText();
    }

    private void ToggledVehicle(Selectable selectable)
    {
        if (selectable == null)
        {
            vehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;
            return;
        }
        if ((selectable.gameObject.GetComponent<AutoDrive>() &&
             !selectable.gameObject.GetComponent<AutoDrive>().Equals(vehicleAutoDrive))
            || vehicleAutoDrive == null)
        {
            if (vehicleAutoDrive != null) vehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;

            vehicleAutoDrive = selectable.GetComponent<AutoDrive>();
            vehicleAutoDrive.Agent.Context.OnActivityChanged += UpdateActivityText;
            UpdateActivityText();
        }
    }

    private void UpdateActivityText()
    {
        activityText.text = vehicleAutoDrive.GetVehicleActivityDescription();
    }

    private void UpdateRoadName()
    {
        roadNameText.text = vehicleAutoDrive.Agent.Context.CurrentRoad.name;
    }

    private void UpdateDistanceTravelledText()
    {
        distanceTravelledText.text = (vehicleAutoDrive.TotalDistance / 1000).ToString("0.00");
    }

    private void OnDestroy()
    {
        if(vehicleAutoDrive != null)
        {
            vehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;
            UserSelectManager.Instance.OnSelectedGameObject -= ToggledVehicle;
        }
    }
}