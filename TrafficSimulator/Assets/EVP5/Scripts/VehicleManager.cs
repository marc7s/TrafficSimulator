//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

namespace EVP
{

public class VehicleManager : MonoBehaviour
	{
	public VehicleController[] vehicles = new VehicleController[0];
	public int defaultVehicle = 0;

	public KeyCode previousVehicleKey = KeyCode.PageUp;
	public KeyCode nextVehicleKey = KeyCode.PageDown;
	public KeyCode alternateNextVehicleKey = KeyCode.Tab;

	public VehicleCameraController cameraController;
	public bool overrideVehicleComponents = true;


	int m_currentVehicleIdx = -1;
	VehicleController m_currentVehicle = null;

	VehicleStandardInput m_commonInput = null;
	VehicleTelemetry m_commonTelemetry = null;


	void OnEnable ()
		{
		m_commonInput = GetComponent<VehicleStandardInput>();
		m_commonTelemetry = GetComponent<VehicleTelemetry>();
		}


	void Start ()
		{
		foreach (VehicleController vehicle in vehicles)
			DisableVehicle(vehicle);

		SelectVehicle(defaultVehicle);
		}


	void Update ()
		{
		if (Input.GetKeyDown(previousVehicleKey)) SelectPreviousVehicle();
		if (Input.GetKeyDown(nextVehicleKey) || Input.GetKeyDown(alternateNextVehicleKey))
			SelectNextVehicle();
		}


	public void SelectVehicle (int vehicleIdx)
		{
		if (vehicleIdx > vehicles.Length) return;

		// Disable current vehicle, if any

		if (m_currentVehicle != null)
			{
			DisableVehicle(m_currentVehicle);
			m_currentVehicle = null;
			}

		// Select new vhicle. Leave no vehicle selected if idx < 1.

		if (vehicleIdx >= 0)
			{
			m_currentVehicle = vehicles[vehicleIdx];
			EnableVehicle(m_currentVehicle);
			}

		m_currentVehicleIdx = vehicleIdx;
		}


	public void SelectPreviousVehicle ()
		{
		int newVehicleIdx = m_currentVehicleIdx - 1;

		if (newVehicleIdx < 0)
			newVehicleIdx = vehicles.Length-1;

		if (newVehicleIdx >= 0)
			SelectVehicle(newVehicleIdx);
		}


	public void SelectNextVehicle ()
		{
		int newVehicleIdx = m_currentVehicleIdx + 1;

		if (newVehicleIdx >= vehicles.Length)
			newVehicleIdx = 0;

		SelectVehicle(newVehicleIdx < vehicles.Length? newVehicleIdx : -1);
		}


    //----------------------------------------------------------------------------------------------


	void EnableVehicle (VehicleController vehicle)
		{
		if (vehicle == null) return;

		SetupVehicleComponents(vehicle, true);

		if (cameraController != null)
			cameraController.target = vehicle.transform;
		}


	void DisableVehicle (VehicleController vehicle)
		{
		if (vehicle == null) return;

		SetupVehicleComponents(vehicle, false);
		vehicle.throttleInput = 0.0f;
		vehicle.brakeInput = 1.0f;
		}


	void SetupVehicleComponents (VehicleController vehicle, bool enabled)
		{
		VehicleTelemetry vehicleTelemetry = vehicle.GetComponent<VehicleTelemetry>();
		VehicleStandardInput vehicleInput = vehicle.GetComponent<VehicleStandardInput>();
		VehicleDamage vehicleDamage = vehicle.GetComponent<VehicleDamage>();

		if (vehicleInput != null)
			{
			if (m_commonInput != null)
				{
				if (overrideVehicleComponents)
					{
					vehicleInput.enabled = false;
					m_commonInput.enabled = true;
					m_commonInput.target = enabled? vehicle : null;
					}
				else
					{
					vehicleInput.enabled = enabled;
					m_commonInput.enabled = false;
					}
				}
			else
				{
				vehicleInput.enabled = enabled;
				}
			}
		else
			{
			if (m_commonInput != null)
				{
				m_commonInput.enabled = true;
				m_commonInput.target = enabled? vehicle : null;
				}
			}

		if (vehicleTelemetry != null)
			{
			if (m_commonTelemetry != null)
				{
				if (overrideVehicleComponents)
					{
					vehicleTelemetry.enabled = false;
					m_commonTelemetry.enabled = true;
					m_commonTelemetry.target = enabled? vehicle : null;
					}
				else
					{
					vehicleTelemetry.enabled = enabled;
					m_commonTelemetry.enabled = false;
					}
				}
			else
				{
				vehicleTelemetry.enabled = enabled;
				}
			}
		else
			{
			if (m_commonTelemetry != null)
				{
				m_commonTelemetry.enabled = true;
				m_commonTelemetry.target = enabled? vehicle : null;
				}
			}

		if (vehicleDamage != null)
			{
			vehicleDamage.enableRepairKey = enabled;
			}
		}
	}
}