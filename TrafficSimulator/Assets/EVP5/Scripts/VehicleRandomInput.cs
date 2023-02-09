//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;

namespace EVP
{

public class VehicleRandomInput : MonoBehaviour
	{
	public float steerInterval = 2.0f;
	public float steerIntervalTolerance = 1.0f;
	public float steerChangeRate = 1.0f;
	[Range(0,1)]
	public float steerStraightRandom = 0.4f;

	[Space(5)]
	public float throttleInterval = 5.0f;
	public float throttleIntervalTolerance = 2.0f;
	public float throttleChangeRate = 3.0f;
	[Range(0,1)]
	public float throttleForwardRandom = 0.8f;

	float m_targetSteer = 0.0f;
	float m_nextSteerTime = 0.0f;
	float m_targetThrottle = 0.0f;
	float m_targetBrake = 0.0f;
	float m_nextThrottleTime = 0.0f;

	VehicleController m_vehicle;


	void OnEnable ()
		{
		m_vehicle = GetComponent<VehicleController>();
		}


	void Update ()
		{
		// Set a random steer value

		if (Time.time > m_nextSteerTime)
			{
			if (Random.value < steerStraightRandom)
				m_targetSteer = 0.0f;
			else
				m_targetSteer = Random.Range (-1.0f, 1.0f);

			m_nextSteerTime = Time.time + steerInterval + Random.Range(-steerIntervalTolerance, steerIntervalTolerance);
			}

		// Set a random throttle-brake value.
		// At low speed chances are that the vehicle has encountered an obstacle.
		// If so, we increase the probability of going reverse.

		if (Time.time > m_nextThrottleTime)
			{
			float forwardRandom = throttleForwardRandom;
			float speed = m_vehicle.cachedRigidbody.velocity.magnitude;

			if (speed < 0.1f && m_targetBrake < 0.001f && m_targetThrottle >= 0.0f)
				forwardRandom *= 0.4f;

			if (Random.value < forwardRandom)
				{
				m_targetThrottle = Random.value;
				m_targetBrake = 0.0f;
				}
			else
				{
				if (speed < 0.5f)
					{
					m_targetBrake = 0.0f;
					m_targetThrottle = -Random.value;
					}
				else
					{
					m_targetThrottle = 0.0f;
					m_targetBrake = Random.value;
					}
				}

			m_nextThrottleTime = Time.time + throttleInterval + Random.Range(-throttleIntervalTolerance, throttleIntervalTolerance);
			}

		// Apply the input progressively

		m_vehicle.steerInput = Mathf.MoveTowards(m_vehicle.steerInput, m_targetSteer, steerChangeRate * Time.deltaTime);
		m_vehicle.throttleInput = Mathf.MoveTowards(m_vehicle.throttleInput, m_targetThrottle, throttleChangeRate * Time.deltaTime);
		m_vehicle.brakeInput = m_targetBrake;
		m_vehicle.handbrakeInput = 0.0f;
		}


	void OnCollisionEnter (Collision collision)
		{
		if (enabled && collision.contacts.Length > 0)
			{
			// Front / rear collisions reduce the waiting time for taking the next throttle decision.

			float colRatio = Vector3.Dot(transform.forward, collision.contacts[0].normal);
			if (colRatio > 0.8f || colRatio < -0.8f)
				m_nextThrottleTime -= throttleInterval * 0.5f;

			// Sideways collisions reduce the waiting time for taking the next steering decision

			if (colRatio > -0.4f && colRatio < 0.4f)
				m_nextSteerTime -= steerInterval * 0.5f;
			}
		}
	}
}