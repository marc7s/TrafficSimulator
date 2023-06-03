using UnityEngine;


public class RoadColorSwitch : MonoBehaviour
{
    public void OnSwitchToggle(bool value)
    {
        GameObject.Find("RoadSystem")?.GetComponent<RoadColorManager>()?.SetRoadColor(value);
    }
}