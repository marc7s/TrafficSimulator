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
public class RigidbodyImpulse : MonoBehaviour
	{
	public float velocity = 6.0f;
	public Vector3 direction = Vector3.up;
	public KeyCode key = KeyCode.E;

	Rigidbody m_rigidbody;


	void OnEnable ()
		{
		m_rigidbody = GetComponent<Rigidbody>();
		}


	void Update ()
		{
		if (Input.GetKeyDown(key))
			m_rigidbody.AddForce(direction.normalized * velocity, ForceMode.VelocityChange);
		}
	}

}