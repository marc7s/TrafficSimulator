using UnityEngine;
using System.Collections;

public class VehicleNitro : MonoBehaviour
	{
	public enum Mode { Acceleration, Impulse };
	public Mode mode = Mode.Acceleration;
	public float value = 10.0f;
	public float maxVelocity = 50.0f;
	public KeyCode key = KeyCode.N;


	Rigidbody m_rigidbody;


	void OnEnable ()
		{
		m_rigidbody = GetComponent<Rigidbody>();
		}


	void Update ()
		{
		if (mode == Mode.Impulse)
			{
			if (Input.GetKeyDown(key) && m_rigidbody.velocity.magnitude < maxVelocity)
			m_rigidbody.AddRelativeForce(Vector3.forward * value, ForceMode.VelocityChange);
			}
		}


	void FixedUpdate ()
		{
		if (mode == Mode.Acceleration)
			{
			if (Input.GetKey(key) && m_rigidbody.velocity.magnitude < maxVelocity)
				m_rigidbody.AddRelativeForce(Vector3.forward * value, ForceMode.Acceleration);
			}
		}
	}
