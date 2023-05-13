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
    public TextMeshProUGUI modelNameText;
    public TextMeshProUGUI roadNameText; 
    public TextMeshProUGUI activityText; 
    public TextMeshProUGUI distanceTravelledText; 
    
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
            vehicleAutoDrive.Agent.Context.OnRoadChanged += UpdateRoadName;
            UpdateRoadName();
            UpdateActivityText();
            UpdateModelNameText();
        }
    }


    private void UpdateModelNameText()
    {
        modelNameText.text = Regex.Replace(vehicleAutoDrive.gameObject.name, @"\d|\s*\(Clone\)", "");
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
}