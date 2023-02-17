//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

namespace EVP
{

[CustomEditor(typeof(VehicleController))]
public class VehicleControllerInspector : Editor
	{
	TextureCanvas m_canvas = null;
	const int m_graphWidth = 242;
	const int m_graphHeight = 64;

	VehicleController m_target;

	static bool m_showAdvanced = false;
	static bool m_showInput = false;


	public override void OnInspectorGUI ()
		{
		serializedObject.Update();
		m_target = (VehicleController)target;

		InspectorTools.BeginContent(115);
		InspectorTools.SetMinLabelWidth(140);

		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("wheels"), true);

		InspectorTools.ResetMinLabelWidth();
		// Center of Mass

		InspectorTools.PropertyField(serializedObject, "centerOfMassMode", "Mode", "How the center of mass is configured");

		if (m_target.centerOfMassMode == VehicleController.CenterOfMassMode.Parametric)
			{
			InspectorTools.PropertyField(serializedObject, "centerOfMassPosition", "Position", "Relative longitudinal position. 0 = rear, 1 = front, 0.5 = middle");
			InspectorTools.PropertyField(serializedObject, "centerOfMassHeightOffset", "Height", "Height distance (m) relative to vehicle's reference frame");
			}
		else
			{
			InspectorTools.PropertyField(serializedObject, "centerOfMassTransform", "Transform", "A Transform in the vehicle to be used as center of mass");
			}

		// Setup

		string forwardSpeedText = GetSpeedText(m_target.maxSpeedForward);
		string reverseSpeedText = GetSpeedText(m_target.maxSpeedReverse);

		InspectorTools.PropertyField(serializedObject, "maxSpeedForward", "Max Speed Forward", forwardSpeedText + "\n\nMaximum theoretical forward speed (m/s). The force produced by the drive wheels decreases with speed according to the graph below. The effective top speed will also be limited by the Aerodynamic Drag.");
		InspectorTools.PropertyField(serializedObject, "maxSpeedReverse", "Max Speed Reverse", reverseSpeedText + "\n\nMaximum theoretical speed (m/s) in reverse. ");
		InspectorTools.PropertyField(serializedObject, "tireFriction", "Tire friction", "Coefficient of friction for the tires. Can be biased to front or rear.");
		InspectorTools.PropertyField(serializedObject, "antiRoll", "Anti-roll", "% of anti roll effect. Prevents the vehicle to bank on corners or rolling over. Vehicle runs flat on anti-roll = 1.0");
		InspectorTools.PropertyField(serializedObject, "maxSteerAngle", "Max Steer Angle", "Maximimum steer angle (degrees) for the wheels marked as Steer");
		InspectorTools.SetMinLabelWidth(143);
		InspectorTools.PropertyField(serializedObject, "aeroDrag", "Aerodynamic Drag", "Drag coefficient. Produces a resistive force opposing the velocity squared. Applied at the position specified by the Aerodynamics balance. This setting limits the effective top speed.");
		InspectorTools.PropertyField(serializedObject, "aeroDownforce", "Aerodynamic Downforce", "Downforce coefficient. Produces a vertical downforce based on the velocity squared. Applied at the position specified by the Aerodynamics balance. The suspension will get compressed with the speed.");
		InspectorTools.ResetMinLabelWidth();
		InspectorTools.PropertyField(serializedObject, "rollingResistance", "Rolling resistance", "% of brake resistance applied for stopping the vehicle when no other input is applied.");

		// Motor

		InspectorTools.SetMinLabelWidth(150);
		InspectorTools.PropertyField(serializedObject, "maxDriveForce", "Max Drive Force", "Maximum drive force (N) applied at the wheels marked as Drive on throttle input. The force decreases with speed according to the graph below. The Drive Balance allows applying different forces at front and rear wheels.");
		InspectorTools.PropertyField(serializedObject, "maxDriveSlip", "Max Drive Slip", "Maximum amount of slip (m/s) when applying drive force. Slip is the relative speed of the tire over the ground. The slip can limited by Traction Control.");
		InspectorTools.PropertyField(serializedObject, "driveForceToMaxSlip", "Drive Force To Max Slip", "Excess of drive force (N) that takes the wheel to the maximum drive slip when applying throttle.");
		InspectorTools.ResetMinLabelWidth();
		InspectorTools.PropertyField(serializedObject, "forceCurveShape", "Force Curve Shape", "Shape of the curve that decreases the drive force with speed, as seen in the graph below.");

		Rect graphRect = AllocateRectForGraphic();

		// Brakes

		InspectorTools.SetMinLabelWidth(150);
		InspectorTools.PropertyField(serializedObject, "maxBrakeForce", "Max Brake Force", "Maximum brake force (N) applied at the wheels marked as Brake on brake input. The Brake Balance allows applying different brakes at front and rear wheels.");
		InspectorTools.PropertyField(serializedObject, "brakeForceToMaxSlip", "Brake Force To Max Slip", "Excess of brake force (N) that takes the wheel to the maximum brake slip when applying brakes.");
		InspectorTools.ResetMinLabelWidth();

		EditorGUILayout.Space();
		InspectorTools.SetMinLabelWidth(125);
		InspectorTools.PropertyField(serializedObject, "brakeMode", "Brake Mode", "How the brakes affect the wheel slip. Slip: specify maximum amount of brake slip in m/s. Ratio: the maximum slip is a ratio (%) of the current speed.");
		if (m_target.brakeMode == VehicleController.BrakeMode.Slip)
			InspectorTools.PropertyField(serializedObject, "maxBrakeSlip", "Max Brake Slip", "Maximum amount of slip (m/s) when appling brake force. Slip is the relative speed of the tire over the ground.");
		else
			InspectorTools.PropertyField(serializedObject, "maxBrakeRatio", "Max Brake Ratio", "Wheel is allowed to slip at the given ratio (%) of the current speed as effect of the brake");

		EditorGUILayout.Space();
		InspectorTools.PropertyField(serializedObject, "handbrakeMode");
		if (m_target.handbrakeMode == VehicleController.BrakeMode.Slip)
			InspectorTools.PropertyField(serializedObject, "maxHandbrakeSlip", "Max Handbrake Slip", "Maximum amount of slip (m/s) when appling handbrake. Slip is the relative speed of the tire over the ground.");
		else
			InspectorTools.PropertyField(serializedObject, "maxHandbrakeRatio", "Max Handbrake Ratio", "Wheel is allowed to slip at the given ratio (%) of the current speed as effect of the handbrake");
		InspectorTools.ResetMinLabelWidth();

		// Balance

		InspectorTools.PropertyField(serializedObject, "driveBalance", "Drive Balance", "Balance of the drive force. 0 = rear, 1 = front, 0.5 = 50-50%. Only affects wheels set as Drive.");
		InspectorTools.InfoLabel(" ", string.Format("Rear: {0}  Front: {1}",
			VehicleController.GetRampBalancedValue(m_target.maxDriveForce, m_target.driveBalance, 0.0f).ToString("0"),
			VehicleController.GetRampBalancedValue(m_target.maxDriveForce, m_target.driveBalance, 1.0f).ToString("0")
			));

		InspectorTools.PropertyField(serializedObject, "brakeBalance", "Brake Balance", "Balance of the brake force. 0 = rear, 1 = front, 0.5 = 50-50%. Only affects wheels set as Brake.");
		InspectorTools.InfoLabel(" ", string.Format("Rear: {0}  Front: {1}",
			VehicleController.GetRampBalancedValue(m_target.maxBrakeForce, m_target.brakeBalance, 0.0f).ToString("0"),
			VehicleController.GetRampBalancedValue(m_target.maxBrakeForce, m_target.brakeBalance, 1.0f).ToString("0")
			));


		InspectorTools.PropertyField(serializedObject, "tireFrictionBalance", "Tire Friction", "Front - rear friction tweaking. >0.5 = bias to front, <0.5 = bias to rear.");
		InspectorTools.InfoLabel(" ", string.Format("Rear: {0}%  Front: {1}%",
			VehicleController.GetBalancedValue(100.0f, m_target.tireFrictionBalance, 0.0f).ToString("0"),
			VehicleController.GetBalancedValue(100.0f, m_target.tireFrictionBalance, 1.0f).ToString("0")
			));

		InspectorTools.PropertyField(serializedObject, "aeroBalance", "Aerodynamics", "Longitudinal relative position (%) of the aerodynamics application point");
		InspectorTools.InfoLabel(" ", string.Format("Rear: {0}%  Front: {1}%",
			VehicleController.GetBalancedValue(50.0f, m_target.aeroBalance, 0.0f).ToString("0"),
			VehicleController.GetBalancedValue(50.0f, m_target.aeroBalance, 1.0f).ToString("0")
			));

		InspectorTools.PropertyField(serializedObject, "handlingBias", "Handling Bias", "Impose additional oversteer - understeed behaviour");
		if (m_target.handlingBias > 0.5001f)
			InspectorTools.InfoLabel(" ", string.Format("Oversteer +{0}%", (int)(Mathf.InverseLerp(0.5f, 1.0f, m_target.handlingBias) * 100)));
		else if (m_target.handlingBias < 0.4999f)
			InspectorTools.InfoLabel(" ", string.Format("Understeer +{0}%", (int)(Mathf.InverseLerp(0.5f, 0.0f, m_target.handlingBias) * 100)));
		else
			InspectorTools.InfoLabel(" ", "Neutral");

		// Driving Aids

		InspectorTools.PropertyField(serializedObject, "tractionControl", "Traction Control", "Limits the amount of wheel slip when applying throttle. The value is the ratio (%) of Max Drive Slip allowed.");
		InspectorTools.PropertyField(serializedObject, "tractionControlRatio", " ");
		InspectorTools.PropertyField(serializedObject, "brakeAssist", "Brake Assist", "Limits the amount of wheel slip when applying brakes. The value is the ratio (%) of Brake Slip (either Slip or Ratio) allowed.");
		InspectorTools.PropertyField(serializedObject, "brakeAssistRatio", " ");
		InspectorTools.PropertyField(serializedObject, "steeringLimit", "Steering Limit", "Limits the steering angle with speed. The value is the % of effectiveness.");
		InspectorTools.PropertyField(serializedObject, "steeringLimitRatio", " ");
		InspectorTools.PropertyField(serializedObject, "steeringAssist", "Steering Assist", "Helps the wheels to point to the actual direction of movement. Useful when drifting. The value is the % of effectiveness.");
		InspectorTools.PropertyField(serializedObject, "steeringAssistRatio", " ");

		// Advanced / optimization

		m_showAdvanced = InspectorTools.LayoutFoldout(m_showAdvanced, "Advanced Settings");
		if (m_showAdvanced)
			{
			InspectorTools.SetMinLabelWidth(130);
			InspectorTools.PropertyField(serializedObject, "wheelUpdateRate", "Wheel Update Rate", "When to update the wheel's visual position and rotation.");
			InspectorTools.PropertyField(serializedObject, "wheelPositionMode", "Wheel Position Mode", "Visual precision of the contact among the wheel mesh and the ground. Doesn't affect the physics behaviour.");

			InspectorTools.PropertyField(serializedObject, "sleepVelocity", "Sleep Velocity", "Braked wheels moving below this speed are allowed to 'sleep' (= keep stationary)");
			InspectorTools.PropertyField(serializedObject, "defaultGroundGrip", "Default Ground Grip", "Grip value to be applied when the contacted surface has no ground material assigned");
			InspectorTools.PropertyField(serializedObject, "defaultGroundDrag", "Default Ground Drag", "Drag value to be applied when the contacted surface has no ground material assigned");

			InspectorTools.SetMinLabelWidth(200);
			InspectorTools.PropertyField(serializedObject, "disallowRuntimeChanges", "Disallow Runtime Changes", "Can be enabled if mass, center of mass and suspension are not expected to change in runtime. Saves some calculations.");
			InspectorTools.PropertyField(serializedObject, "disableSteerAngleCorrection", "Disable Steer Angle Correction", "Can be enabled if all wheels point in the vehicle's forward direction. Saves some calculations.");
			InspectorTools.PropertyField(serializedObject, "showCollisionGizmos", "Show Collision Gizmos", "Draw some debug lines in the Editor when the vehicle collides with other objects");
			InspectorTools.ResetMinLabelWidth();

			EditorGUILayout.Space();
			}

		// In-editor input

		m_showInput = InspectorTools.LayoutFoldout(m_showInput, "Vehicle Controls", "Used by input scripts. Exposed here for debug & tests in runtime");
		if (m_showInput)
			{
			InspectorTools.PropertyField(serializedObject, "steerInput", "Steer Input", "-1 left, 0 center, +1 right.");
			InspectorTools.PropertyField(serializedObject, "throttleInput", "Throttle Input", "0 = no throttle, 1 = full throttle forwards, -1 = full throttle reverse");
			InspectorTools.PropertyField(serializedObject, "brakeInput", "Brake Input", "0 = no brakes, 1 = full brakes");
			InspectorTools.PropertyField(serializedObject, "handbrakeInput", "Handbrake Input", "0 = no handbrake, 1 = full handbrake");
			}

		serializedObject.ApplyModifiedProperties();

		GUIDrawForceGraph(graphRect);
		InspectorTools.EndContent();
		}



	Rect AllocateRectForGraphic ()
		{
		Rect graphRect = EditorGUILayout.GetControlRect(false, m_graphHeight + 5);
		graphRect.y += 5;
		graphRect.height -= 5;

		// Graph legend info

		GUIContent legend = new GUIContent("Hover here to legend",
			"Horizontal scale: speed (m/s)\n" +
			"Vertical scale: force (N)\n\n" +
			"Green: max drive force per drive wheel\n" +
			"Dashed white: max speed"
			);

		GUIStyle tmpStyle = new GUIStyle(EditorStyles.miniLabel);
		tmpStyle.alignment = TextAnchor.UpperCenter;
		tmpStyle.normal.textColor = Color.gray;
		EditorGUILayout.LabelField(legend, tmpStyle);

		return graphRect;
		}



	void GUIDrawForceGraph (Rect graphRect)
		{
		if (m_canvas == null || Event.current.type != EventType.Ignore)
			{
			if (m_canvas == null)
				{
				m_canvas = new TextureCanvas(m_graphWidth, m_graphHeight);
				m_canvas.alpha = 0.75f;
				m_canvas.color = Color.black;
				m_canvas.Clear();
				m_canvas.Save();
				}
			else
				{
				m_canvas.Restore();
				}

			m_target = (VehicleController)serializedObject.targetObject;

			// Calculate the dimmensions of the speed graph

			float maxSpeed = Mathf.Max(m_target.maxSpeedForward * 1.1f, 1.0f);
			float maxForce = Mathf.Max(m_target.maxDriveForce * 1.1f, 1.0f);

			// Set dimensions and draw grids

			m_canvas.rect = new Rect(0.0f, 0.0f, maxSpeed, maxForce);
			m_canvas.color = Color.green*0.1f;
			m_canvas.Grid(1.0f, 100.0f);
			m_canvas.color = Color.green*0.4f;
			m_canvas.Grid(10.0f, 1000.0f);

			// Origin lines

			m_canvas.color = Color.grey;
			m_canvas.HorizontalLine(0.0f);
			m_canvas.VerticalLine(0.0f);

			// Force graph

			CommonTools.BiasLerpContext biasCtx = new CommonTools.BiasLerpContext();

			m_canvas.color = Color.green;
			if (m_target.maxSpeedForward >= 0.01f)
				m_canvas.Function(x => m_target.maxDriveForce * CommonTools.BiasedLerp(1.0f - x/m_target.maxSpeedForward, m_target.forceCurveShape, biasCtx));

			// Limit lines

			m_canvas.color = Color.white;
			m_canvas.lineType = TextureCanvas.LineType.Dashed;
			m_canvas.Line(m_target.maxSpeedForward, 0.0f, m_target.maxSpeedForward, m_target.maxDriveForce);
			m_canvas.lineType = TextureCanvas.LineType.Solid;
			}

		// Non-scaled, horizontally centered, bottom-aligned, shadow effect

		m_canvas.EditorGUIDraw(graphRect);
		}


	string GetSpeedText (float speed)
		{
		return string.Format("{0} m/s = {1} km/h = {2} mph", speed, speed*3.6f, speed*2.237f);
	    }
	}
}