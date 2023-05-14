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
        ClearVehicleInfo();
    }

    private void Update()
    {
        if(vehicleAutoDrive != null) 
            UpdateDistanceTravelledText();
    }

    private void ToggledVehicle(Selectable selectable)
    {
        if (selectable == null)
        {
            vehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;
            vehicleAutoDrive.Agent.Context.OnRoadChanged -= UpdateRoadName;
            vehicleAutoDrive = null;
            ClearVehicleInfo();
            return;
        }

        bool isOtherAutoDrive = selectable.gameObject.GetComponent<AutoDrive>() && !selectable.gameObject.GetComponent<AutoDrive>().Equals(vehicleAutoDrive);
        
        if (vehicleAutoDrive == null || isOtherAutoDrive)
        {
            if (vehicleAutoDrive != null) 
                vehicleAutoDrive.Agent.Context.OnActivityChanged -= UpdateActivityText;

            vehicleAutoDrive = selectable.GetComponent<AutoDrive>();
            vehicleAutoDrive.Agent.Context.OnActivityChanged += UpdateActivityText;
            vehicleAutoDrive.Agent.Context.OnRoadChanged += UpdateRoadName;
            UpdateRoadName();
            UpdateActivityText();
            UpdateModelNameText();
        }
    }

    private void ClearVehicleInfo()
    {
        modelNameText.text = "";
        roadNameText.text = "";
        distanceTravelledText.text = "";

        // The vehicle selected text is the most centered, so use it temporarily until a vehicle is selected
        activityText.text = "No vehicle selected";
    }

    private string Header(string text)
    {
        return $"<b><color=black>{text}: </color></b>";
    }

    private void UpdateModelNameText()
    {
        modelNameText.text = Header("Model") + Regex.Replace(vehicleAutoDrive.gameObject.name, @"\d|\s*\(Clone\)", "");
    }
    
    private void UpdateActivityText()
    {
        activityText.text = Header("Activity") + vehicleAutoDrive.GetVehicleActivityDescription();
    }

    private void UpdateRoadName()
    {
        roadNameText.text = Header("Road") + vehicleAutoDrive.Agent.Context.CurrentRoad?.name ?? "N/A";
    }

    private void UpdateDistanceTravelledText()
    {
        distanceTravelledText.text = Header("Distance") + (vehicleAutoDrive.TotalDistance / 1000).ToString("0.00") + " km";
    }
}