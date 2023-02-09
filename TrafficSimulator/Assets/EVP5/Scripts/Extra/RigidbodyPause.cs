//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections;

namespace EVP
{

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyPause : MonoBehaviour
	{
	public bool pause = false;
	public KeyCode key = KeyCode.P;

	Rigidbody m_rigidbody;


	bool m_pausedState = false;
	Vector3 m_velocity = Vector3.zero;
	Vector3 m_angularVelocity = Vector3.zero;

	// Enabling / disabling vehicle and wheelcolliders should be unnecesary in Unity 5.2+
	// (patch pending)

	VehicleController m_vehicle;


	void OnEnable ()
		{
		m_rigidbody = GetComponent<Rigidbody>();
		m_vehicle = GetComponent<VehicleController>();
		}


	void FixedUpdate ()
		{
		if (pause && !m_pausedState)
			{
			m_velocity = m_rigidbody.velocity;
			m_angularVelocity = m_rigidbody.angularVelocity;

			m_pausedState = true;
			m_rigidbody.isKinematic = true;

			if (m_vehicle)
				{
				m_vehicle.enabled = false;
				DisableWheelColliders();
				}
			}
		else
		if (!pause && m_pausedState)
			{
			m_rigidbody.isKinematic = false;

			if (m_vehicle)
				{
				EnableWheelColliders();
				m_vehicle.enabled = true;
				}

			m_rigidbody.AddForce(m_velocity, ForceMode.VelocityChange);
			m_rigidbody.AddTorque(m_angularVelocity, ForceMode.VelocityChange);

			m_pausedState = false;
			}
		}


	void DisableWheelColliders ()
		{
		WheelCollider[] colliders = GetComponentsInChildren<WheelCollider>();

		foreach (WheelCollider wheel in colliders)
			wheel.enabled = false;
		}


	void EnableWheelColliders ()
		{
		WheelCollider[] colliders = GetComponentsInChildren<WheelCollider>();

		foreach (WheelCollider wheel in colliders)
			wheel.enabled = true;
		}


	void Update ()
		{
		if (Input.GetKeyDown(key)) pause = !pause;
		}
	}

}