using UnityEngine;
using EVP;


// Execution order must be:
//
//	EVP.VehicleStandardInput
//	FollowHeading
//	< Default Time >


public class FollowHeading : MonoBehaviour
	{
	[Range(-180, 180)]
	public float heading = 0.0f;			// Degrees, 0 = "north" (World +Z)

	VehicleController m_vehicle;


	void OnEnable ()
		{
		m_vehicle = GetComponent<VehicleController>();
		}


	void FixedUpdate ()
		{
		float deltaAngle = Mathf.DeltaAngle(transform.eulerAngles.y, heading);
		float targetSteer = Mathf.Clamp(deltaAngle / m_vehicle.maxSteerAngle, -1.0f, +1.0f);

		m_vehicle.steerInput += targetSteer;
		}
	}
