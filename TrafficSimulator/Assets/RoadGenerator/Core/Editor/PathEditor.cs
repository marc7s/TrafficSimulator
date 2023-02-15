using System.Collections.Generic;
using RoadGenerator;
using RoadGenerator.Utility;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RoadGeneratorEditor
{
    /// Editor class for the creation of Bezier and Vertex paths

    [CustomEditor(typeof(PathCreator))]
	public class PathEditor : Editor
	{

		#region Fields

		// Interaction:
		const float segmentSelectDistanceThreshold = 10f;
		const float screenPolylineMaxAngleError = .3f;
		const float screenPolylineMinVertexDst = .01f;

		// Help messages:
		const string helpInfo = "Shift-click to add or insert new points. Control-click to delete points. For more detailed infomation, please refer to the documentation.";
		static readonly string[] spaceNames = { "3D (xyz)", "2D (xy)", "Top-down (xz)" };
		static readonly string[] tabNames = { "Bézier Path", "Vertex Path" };
		const string constantSizeTooltip = "If true, anchor and control points will keep a constant size when zooming in the editor.";

		// Display
		const int inspectorSectionSpacing = 10;
		const float constantHandleScale = .01f;
		const float normalsSpacing = .2f;
		GUIStyle boldFoldoutStyle;

		// References:
		PathCreator creator;
		Road thisRoad;
		Editor globalDisplaySettingsEditor;
		ScreenSpacePolyLine screenSpaceLine;
		ScreenSpacePolyLine.MouseInfo pathMouseInfo;
		GlobalDisplaySettings globalDisplaySettings;
		PathHandle.HandleColours splineAnchorColours;
		PathHandle.HandleColours splineControlColours;
		Dictionary<GlobalDisplaySettings.HandleType, Handles.CapFunction> capFunctions;
		ArcHandle anchorAngleHandle = new ArcHandle();
		VertexPath normalsVertexPath;

		// State variables:
		int selectedSegmentIndex;
		int draggingHandleIndex;
		int mouseOverHandleIndex;
		int handleIndexToDisplayAsTransform;

		bool shiftLastFrame;
		bool hasUpdatedScreenSpaceLine;
		bool hasUpdatedNormalsVertexPath;
		bool editingNormalsOld;

		Vector3 transformPos;
		Vector3 transformScale;
		Quaternion transformRot;

		Color handlesStartCol;

		// Constants
		const int bezierPathTab = 0;
		const int vertexPathTab = 1;

		#endregion

		#region Inspectors

		public override void OnInspectorGUI()
		{
			// Initialize GUI styles
			if (boldFoldoutStyle == null)
			{
				boldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
				boldFoldoutStyle.fontStyle = FontStyle.Bold;
			}

			Undo.RecordObject(creator, "Path settings changed");

			// Draw Bezier and Vertex tabs
			int tabIndex = GUILayout.Toolbar(data.tabIndex, tabNames);
			if (tabIndex != data.tabIndex)
			{
				data.tabIndex = tabIndex;
				TabChanged();
			}

			// Draw inspector for active tab
			switch (data.tabIndex)
			{
				case bezierPathTab:
					DrawBezierPathInspector();
					break;
				case vertexPathTab:
					DrawVertexPathInspector();
					break;
			}

			// Notify of undo/redo that might modify the path
			if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
			{
				data.PathModifiedByUndo();
			}
		}

		void DrawBezierPathInspector()
		{
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				// Path options:
				data.showPathOptions = EditorGUILayout.Foldout(data.showPathOptions, new GUIContent("Bézier Path Options"), true, boldFoldoutStyle);
				if (data.showPathOptions)
				{
					bezierPath.Space = (PathSpace)EditorGUILayout.Popup("Space", (int)bezierPath.Space, spaceNames);
					bezierPath.ControlPointMode = BezierPath.ControlMode.Automatic;
					bezierPath.AutoControlLength = EditorGUILayout.Slider(new GUIContent("Control Spacing"), bezierPath.AutoControlLength, 0, 1);

					bezierPath.IsClosed = EditorGUILayout.Toggle("Closed Path", bezierPath.IsClosed);
					data.showTransformTool = EditorGUILayout.Toggle(new GUIContent("Enable Transforms"), data.showTransformTool);

					Tools.hidden = !data.showTransformTool;

					// Check if out of bounds (can occur after undo operations)
					if (handleIndexToDisplayAsTransform >= bezierPath.NumPoints)
					{
						handleIndexToDisplayAsTransform = -1;
					}

					// If a point has been selected
					if (handleIndexToDisplayAsTransform != -1)
					{
						EditorGUILayout.LabelField("Selected Point:");

						using (new EditorGUI.IndentLevelScope())
						{
							var currentPosition = creator.bezierPath[handleIndexToDisplayAsTransform];
							var newPosition = EditorGUILayout.Vector3Field("Position", currentPosition);
							if (newPosition != currentPosition)
							{
								Undo.RecordObject(creator, "Move point");
								creator.bezierPath.MovePoint(handleIndexToDisplayAsTransform, newPosition);
							}
							// Don't draw the angle field if we aren't selecting an anchor point/not in 3d space
							if (handleIndexToDisplayAsTransform % 3 == 0 && creator.bezierPath.Space == PathSpace.xyz)
							{
								var anchorIndex = handleIndexToDisplayAsTransform / 3;
								var currentAngle = creator.bezierPath.GetAnchorNormalAngle(anchorIndex);
								var newAngle = EditorGUILayout.FloatField("Angle", currentAngle);
								if (newAngle != currentAngle)
								{
									Undo.RecordObject(creator, "Set Angle");
									creator.bezierPath.SetAnchorNormalAngle(anchorIndex, newAngle);
								}
							}
						}
					}

					if (data.showTransformTool & (handleIndexToDisplayAsTransform == -1))
					{
						if (GUILayout.Button("Centre Transform"))
						{

							Vector3 worldCentre = bezierPath.CalculateBoundsWithTransform(creator.transform).center;
							Vector3 transformPos = creator.transform.position;
							if (bezierPath.Space == PathSpace.xy)
							{
								transformPos = new Vector3(transformPos.x, transformPos.y, 0);
							}
							else if (bezierPath.Space == PathSpace.xz)
							{
								transformPos = new Vector3(transformPos.x, 0, transformPos.z);
							}
							Vector3 worldCentreToTransform = transformPos - worldCentre;

							if (worldCentre != creator.transform.position)
							{
								//Undo.RecordObject (creator, "Centralize Transform");
								if (worldCentreToTransform != Vector3.zero)
								{
									Vector3 localCentreToTransform = MathUtility.InverseTransformVector(worldCentreToTransform, creator.transform, bezierPath.Space);
									for (int i = 0; i < bezierPath.NumPoints; i++)
									{
										bezierPath.SetPoint(i, bezierPath.GetPoint(i) + localCentreToTransform, true);
									}
								}

								creator.transform.position = worldCentre;
								bezierPath.NotifyPathModified();
							}
						}
					}

					if (GUILayout.Button("Reset Path"))
					{
						Undo.RecordObject(creator, "Reset Path");
						bool in2DEditorMode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D;
						data.ResetBezierPath(creator.transform.position, in2DEditorMode);
						EditorApplication.QueuePlayerLoopUpdate();
					}

					GUILayout.Space(inspectorSectionSpacing);
				}

				data.showNormals = EditorGUILayout.Foldout(data.showNormals, new GUIContent("Normals Options"), true, boldFoldoutStyle);
				if (data.showNormals)
				{
					bezierPath.FlipNormals = EditorGUILayout.Toggle(new GUIContent("Flip Normals"), bezierPath.FlipNormals);
					if (bezierPath.Space == PathSpace.xyz)
					{
						bezierPath.GlobalNormalsAngle = EditorGUILayout.Slider(new GUIContent("Global Angle"), bezierPath.GlobalNormalsAngle, 0, 360);

						if (GUILayout.Button("Reset Normals"))
						{
							Undo.RecordObject(creator, "Reset Normals");
							bezierPath.FlipNormals = false;
							bezierPath.ResetNormalAngles();
						}
					}
					GUILayout.Space(inspectorSectionSpacing);
				}

				// Editor display options
				data.showDisplayOptions = EditorGUILayout.Foldout(data.showDisplayOptions, new GUIContent("Display Options"), true, boldFoldoutStyle);
				if (data.showDisplayOptions)
				{
					data.showPathBounds = GUILayout.Toggle(data.showPathBounds, new GUIContent("Show Path Bounds"));
					data.showPerSegmentBounds = GUILayout.Toggle(data.showPerSegmentBounds, new GUIContent("Show Segment Bounds"));
					data.displayAnchorPoints = GUILayout.Toggle(data.displayAnchorPoints, new GUIContent("Show Anchor Points"));
					if (!(bezierPath.ControlPointMode == BezierPath.ControlMode.Automatic && globalDisplaySettings.hideAutoControls))
					{
						data.displayControlPoints = GUILayout.Toggle(data.displayControlPoints, new GUIContent("Show Control Points"));
					}
					data.keepConstantHandleSize = GUILayout.Toggle(data.keepConstantHandleSize, new GUIContent("Constant Point Size", constantSizeTooltip));
					data.bezierHandleScale = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("Handle Scale"), data.bezierHandleScale));
					DrawGlobalDisplaySettingsInspector();
				}

				if (check.changed)
				{
					SceneView.RepaintAll();
					EditorApplication.QueuePlayerLoopUpdate();
				}
			}
		}

		void DrawVertexPathInspector()
		{

			GUILayout.Space(inspectorSectionSpacing);
			EditorGUILayout.LabelField("Vertex count: " + creator.path.NumPoints);
			GUILayout.Space(inspectorSectionSpacing);

			data.showVertexPathOptions = EditorGUILayout.Foldout(data.showVertexPathOptions, new GUIContent("Vertex Path Options"), true, boldFoldoutStyle);
			if (data.showVertexPathOptions)
			{
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					data.vertexPathMaxAngleError = EditorGUILayout.Slider(new GUIContent("Max Angle Error"), data.vertexPathMaxAngleError, 0, 45);
					data.vertexPathMinVertexSpacing = EditorGUILayout.Slider(new GUIContent("Min Vertex Dst"), data.vertexPathMinVertexSpacing, 0, 1);

					GUILayout.Space(inspectorSectionSpacing);
					if (check.changed)
					{
						data.VertexPathSettingsChanged();
						SceneView.RepaintAll();
						EditorApplication.QueuePlayerLoopUpdate();
					}
				}
			}

			data.showVertexPathDisplayOptions = EditorGUILayout.Foldout(data.showVertexPathDisplayOptions, new GUIContent("Display Options"), true, boldFoldoutStyle);
			if (data.showVertexPathDisplayOptions)
			{
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					data.showNormalsInVertexMode = GUILayout.Toggle(data.showNormalsInVertexMode, new GUIContent("Show Normals"));
					data.showBezierPathInVertexMode = GUILayout.Toggle(data.showBezierPathInVertexMode, new GUIContent("Show Bezier Path"));

					if (check.changed)
					{
						SceneView.RepaintAll();
						EditorApplication.QueuePlayerLoopUpdate();
					}
				}
				DrawGlobalDisplaySettingsInspector();
			}
		}

		void DrawGlobalDisplaySettingsInspector()
		{
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				data.globalDisplaySettingsFoldout = EditorGUILayout.InspectorTitlebar(data.globalDisplaySettingsFoldout, globalDisplaySettings);
				if (data.globalDisplaySettingsFoldout)
				{
					CreateCachedEditor(globalDisplaySettings, null, ref globalDisplaySettingsEditor);
					globalDisplaySettingsEditor.OnInspectorGUI();
				}
				if (check.changed)
				{
					UpdateGlobalDisplaySettings();
					SceneView.RepaintAll();
				}
			}
		}

		#endregion

		#region Scene GUI

		void OnSceneGUI()
		{
			if (!globalDisplaySettings.visibleBehindObjects)
			{
				Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			}

			EventType eventType = Event.current.type;

			using (var check = new EditorGUI.ChangeCheckScope())
			{
				handlesStartCol = Handles.color;
				switch (data.tabIndex)
				{
					case bezierPathTab:
						if (eventType != EventType.Repaint && eventType != EventType.Layout)
						{
							ProcessBezierPathInput(Event.current);
						}

						DrawBezierPathSceneEditor();
						break;
					case vertexPathTab:
						if (eventType == EventType.Repaint)
						{
							DrawVertexPathSceneEditor();
						}
						break;
				}

				// Don't allow clicking over empty space to deselect the object
				if (eventType == EventType.Layout)
				{
					HandleUtility.AddDefaultControl(0);
				}

				if (check.changed)
				{
					EditorApplication.QueuePlayerLoopUpdate();
				}
			}

			SetTransformState();
		}

		void DrawVertexPathSceneEditor()
		{

			Color bezierCol = globalDisplaySettings.bezierPath;
			bezierCol.a *= .5f;

			if (data.showBezierPathInVertexMode)
			{
				for (int i = 0; i < bezierPath.NumSegments; i++)
				{
					Vector3[] points = bezierPath.GetPointsInSegment(i);
					for (int j = 0; j < points.Length; j++)
					{
						points[j] = MathUtility.TransformPoint(points[j], creator.transform, bezierPath.Space);
					}
					Handles.DrawBezier(points[0], points[3], points[1], points[2], bezierCol, null, 2);
				}
			}

			Handles.color = globalDisplaySettings.vertexPath;

			for (int i = 0; i < creator.path.NumPoints; i++)
			{
				int nextIndex = (i + 1) % creator.path.NumPoints;
				if (nextIndex != 0 || bezierPath.IsClosed)
				{
					Handles.DrawLine(creator.path.GetPoint(i), creator.path.GetPoint(nextIndex));
				}
			}

			if (data.showNormalsInVertexMode)
			{
				Handles.color = globalDisplaySettings.normals;
				Vector3[] normalLines = new Vector3[creator.path.NumPoints * 2];
				for (int i = 0; i < creator.path.NumPoints; i++)
				{
					normalLines[i * 2] = creator.path.GetPoint(i);
					normalLines[i * 2 + 1] = creator.path.GetPoint(i) + creator.path.localNormals[i] * globalDisplaySettings.normalsLength;
				}
				Handles.DrawLines(normalLines);
			}
		}
		void ProcessBezierPathInput(Event e)
		{
			// when the handles are being moves, update the intersections
			if (draggingHandleIndex != -1 && e.type == EventType.MouseUp)
			{
				UpdateIntersections();
			}
			
			// Find which handle mouse is over. Start by looking at previous handle index first, as most likely to still be closest to mouse
			int previousMouseOverHandleIndex = (mouseOverHandleIndex == -1) ? 0 : mouseOverHandleIndex;
			mouseOverHandleIndex = -1;
			for (int i = 0; i < bezierPath.NumPoints; i += 3)
			{

				int handleIndex = (previousMouseOverHandleIndex + i) % bezierPath.NumPoints;
				float handleRadius = GetHandleDiameter(globalDisplaySettings.anchorSize * data.bezierHandleScale, bezierPath[handleIndex]) / 2f;
				Vector3 pos = MathUtility.TransformPoint(bezierPath[handleIndex], creator.transform, bezierPath.Space);
				float dst = HandleUtility.DistanceToCircle(pos, handleRadius);
				if (dst == 0)
				{
					mouseOverHandleIndex = handleIndex;
					break;
				}
			}

			// Shift-left click (when mouse not over a handle) to split or add segment
			if (mouseOverHandleIndex == -1)
			{
				if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
				{
					// If the mouse isn't over an object, return
					if (!IsMouseOverObject(e))
						return;
					UpdatePathMouseInfo();
					// Insert point along selected segment
					if (selectedSegmentIndex != -1 && selectedSegmentIndex < bezierPath.NumSegments)
					{
						Vector3 newPathPoint = pathMouseInfo.closestWorldPointToMouse;
						newPathPoint = MathUtility.InverseTransformPoint(newPathPoint, creator.transform, bezierPath.Space);
						Undo.RecordObject(creator, "Split segment");
						bezierPath.SplitSegment(newPathPoint, selectedSegmentIndex, pathMouseInfo.timeOnBezierSegment);
					}
					// If path is not a closed loop, add new point on to the end of the path
					else if (!bezierPath.IsClosed)
					{
						// If control/command are held down, the point gets pre-pended, so we want to check distance
						// to the endpoint we are adding to
						var pointIdx = e.control || e.command ? 0 : bezierPath.NumPoints - 1;
						// insert new point at same dst from scene camera as the point that comes before it (for a 3d path)
						var endPointLocal = bezierPath[pointIdx];
						var endPointGlobal =
							MathUtility.TransformPoint(endPointLocal, creator.transform, bezierPath.Space);
						var distanceCameraToEndpoint = (Camera.current.transform.position - endPointGlobal).magnitude;
						var newPointGlobal =
							MouseUtility.GetMouseWorldPosition(bezierPath.Space, distanceCameraToEndpoint);
						var newPointLocal =
							MathUtility.InverseTransformPoint(newPointGlobal, creator.transform, bezierPath.Space);

						Undo.RecordObject(creator, "Add segment");
						if (e.control || e.command)
						{
							bezierPath.AddSegmentToStart(newPointLocal);
						}
						else
						{
							bezierPath.AddSegmentToEnd(newPointLocal);
						}
					}
					UpdateIntersections();
				}
			}

			// Control click or backspace/delete to remove point
			if (e.keyCode == KeyCode.Backspace || e.keyCode == KeyCode.Delete || ((e.control || e.command) && e.type == EventType.MouseDown && e.button == 0))
			{
				if (mouseOverHandleIndex != -1)
				{
					Undo.RecordObject(creator, "Delete segment");
					bezierPath.DeleteSegment(mouseOverHandleIndex);
					if (mouseOverHandleIndex == handleIndexToDisplayAsTransform)
					{
						handleIndexToDisplayAsTransform = -1;
					}
					mouseOverHandleIndex = -1;
					Repaint();
				}
				//CreateIntersectionIfRoadsAreCrossing();
			}

			// Holding shift and moving mouse (but mouse not over a handle/dragging a handle)
			if (draggingHandleIndex == -1 && mouseOverHandleIndex == -1)
			{
				bool shiftDown = e.shift && !shiftLastFrame;
				if (shiftDown || ((e.type == EventType.MouseMove || e.type == EventType.MouseDrag) && e.shift))
				{
					UpdatePathMouseInfo();
					bool notSplittingAtControlPoint = pathMouseInfo.timeOnBezierSegment > 0 && pathMouseInfo.timeOnBezierSegment < 1;
					if (pathMouseInfo.mouseDstToLine < segmentSelectDistanceThreshold && notSplittingAtControlPoint)
					{
						if (pathMouseInfo.closestSegmentIndex != selectedSegmentIndex)
						{
							selectedSegmentIndex = pathMouseInfo.closestSegmentIndex;
							HandleUtility.Repaint();
						}
					}
					else
					{
						selectedSegmentIndex = -1;
						HandleUtility.Repaint();
					}

				}
			}

			shiftLastFrame = e.shift;

		}

		Road GetRoad(PathCreator pathCreator)
		{
			RoadSystem roadSystem = GameObject.Find("RoadSystem").GetComponent<RoadSystem>();
			Road thisRoad = null;
			foreach (Road road in roadSystem.Roads) {
				if (road.RoadObject.GetComponent<PathCreator>() == pathCreator) {
					thisRoad = road;
					break;
				}
			}
			if (thisRoad == null) {
				Debug.Log("ERROR, Road not found");
				return null;
			}
			return thisRoad;
		}
		void UpdateIntersections() {
			RoadSystem roadSystem = thisRoad.RoadSystem;
			Vector3[] segment1 = creator.bezierPath.GetPointsInSegment(creator.bezierPath.NumSegments - 1);
			int startingVertexIndexThisRoad = creator.path.GetPointClosestPointIndex(segment1[0]);
			int endingVertexIndexThisRoad = creator.path.GetPointClosestPointIndex(segment1[3], false);
			List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();

			foreach(Road road in roadSystem.Roads) {
                if (road == thisRoad)
                    continue;

				PathCreator pathCreator = road.RoadObject.GetComponent<PathCreator>();

				// If the two road bounds are not overlapping, then intersection is not possible
				if (!creator.bezierPath.PathBounds.Intersects(pathCreator.bezierPath.PathBounds)) {
					continue;
				}

				// Loop through all bezier anchor points of the other road
				for (int i = 0; i < pathCreator.bezierPath.NumSegments; i++) 
				{
					Vector3[] segment2 = pathCreator.bezierPath.GetPointsInSegment(i);
					// If the two segment bounds are not overlapping, then intersection is not possible
					if (!IsBezierPathIntersectionPossible(segment1, segment2)) {
						continue;
					}
					intersectionPointDatas.AddRange(GetBezierPathIntersections(startingVertexIndexThisRoad, endingVertexIndexThisRoad, pathCreator, segment2));
				}
            }

			for (int i = 0; i < intersectionPointDatas.Count; i++) {
				CreateIntersectionAtPosition(intersectionPointDatas[i]);
			}
		}

		/// <summary> Quick check find out if the bezier paths could be intersecting  </summary>
		bool IsBezierPathIntersectionPossible(Vector3[] segment1, Vector3[] segment2) {
			// If the rectangle made up of the bezier control points are not overlapping with each other, then the bezier path is not overlapping
			Bounds bound1 = CubicBezierUtility.CalculateSegmentBounds(segment1[0], segment1[1], segment1[2], segment1[3]);
			Bounds bound2 = CubicBezierUtility.CalculateSegmentBounds(segment2[0], segment2[1], segment2[2], segment2[3]);
            return bound1.Intersects(bound2);
        }

		/// <summary> Check if the bezier paths are intersecting </summary>
		List<IntersectionPointData> GetBezierPathIntersections(int startingVertexIndexThisRoad, int endingVertexIndexThisRoad, PathCreator otherRoadPathCreator, Vector3[] otherRoadSegmentPoints)
		{
		List<IntersectionPointData> intersectionsPoints = new List<IntersectionPointData>();
		Vector3 otherRoadSegmentAnchorPoint1 = otherRoadSegmentPoints[0];
		Vector3 otherRoadSegmentAnchorPoint2 = otherRoadSegmentPoints[3];

		int startingVertexIndexOtherRoad = otherRoadPathCreator.path.GetPointClosestPointIndex(otherRoadSegmentAnchorPoint1);
		int endingVertexIndexOtherRoad = otherRoadPathCreator.path.GetPointClosestPointIndex(otherRoadSegmentAnchorPoint2, false);

		for (int i = 0; i < endingVertexIndexThisRoad - startingVertexIndexThisRoad; i++)
		{
			for (int j = 0; j < endingVertexIndexOtherRoad - startingVertexIndexOtherRoad; j++)
			{
			Vector2 thisRoadVertexPoint1 = Get2DPoint(creator.path.GetPoint(startingVertexIndexThisRoad + i));
			Vector2 thisRoadVertexPoint2 = Get2DPoint(creator.path.GetPoint(startingVertexIndexThisRoad + i+1));
			Vector2 otherRoadVertexPoint1 = Get2DPoint(otherRoadPathCreator.path.GetPoint(startingVertexIndexOtherRoad + j));
			Vector2 otherRoadVertexPoint2 = Get2DPoint(otherRoadPathCreator.path.GetPoint(startingVertexIndexOtherRoad + j+1));
			// If the vertex lines are intercecting
			if (LineUtil.IntersectLineSegments2D(thisRoadVertexPoint1, thisRoadVertexPoint2, otherRoadVertexPoint1, otherRoadVertexPoint2, out Vector2 intersectionPoint))
			{
				// The intersections rotation is the rotation of the active roads vertex line at the intersection point
				Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, new Vector3(thisRoadVertexPoint1.x, 0, thisRoadVertexPoint1.y) - new Vector3(thisRoadVertexPoint2.x, 0, thisRoadVertexPoint2.y));
				IntersectionPointData intersectionPointData = new IntersectionPointData(intersectionPoint, rotation, creator, otherRoadPathCreator);
				intersectionsPoints.Add(intersectionPointData);
			}
            }
		}
		return intersectionsPoints;
		}
		Vector2 Get2DPoint(Vector3 point) {
			return new Vector2(point.x, point.z);
		}

		void CreateIntersectionAtPosition(IntersectionPointData intersectionPointData)
		{
			RoadSystem roadSystem = thisRoad.RoadSystem;
			Vector3 intersectionPoint3D = new Vector3(intersectionPointData.intersectionPoint.x, 0, intersectionPointData.intersectionPoint.y);
			Intersection intersection = roadSystem.AddNewIntersection(intersectionPoint3D, intersectionPointData.rotation);
			PathCreator otherRoadCreator = intersectionPointData.Road2PathCreator;
			intersection.Road2PathCreator = otherRoadCreator;
			intersection.Road1PathCreator = creator;
			intersection.Road1 = thisRoad;
			intersection.Road2 = GetRoad(otherRoadCreator);

			screenSpaceLine = new ScreenSpacePolyLine(bezierPath, creator.transform, screenPolylineMaxAngleError, screenPolylineMinVertexDst);
			int segmentIndex = screenSpaceLine.ClosestSegmentIndexFromPosition(intersectionPoint3D);
			Vector3 segmentStartPoint = bezierPath.GetPointsInSegment(segmentIndex)[0];
			float distanceAtIntersection = creator.path.GetClosestDistanceAlongPath(intersectionPoint3D);
			Vector3 position = creator.path.GetPointAtDistance(distanceAtIntersection + intersection.IntersectionLength/2);
			Vector3 postion2 = creator.path.GetPointAtDistance(distanceAtIntersection - intersection.IntersectionLength/2);
			intersection.Road1AnchorPoint1 = position;
			intersection.Road1AnchorPoint2 = postion2;
			if (Vector3.Distance(segmentStartPoint, intersection.Road1AnchorPoint1) < Vector3.Distance(segmentStartPoint, intersection.Road1AnchorPoint2))
			{
				bezierPath.SplitSegment(intersection.Road1AnchorPoint1, segmentIndex, creator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint1));
				bezierPath.SplitSegment(intersection.Road1AnchorPoint2, segmentIndex, creator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint2));
			}
			else
			{
				bezierPath.SplitSegment(intersection.Road1AnchorPoint2, segmentIndex, creator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint2));
				bezierPath.SplitSegment(intersection.Road1AnchorPoint1, segmentIndex + 1, creator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint1));
			}

			ScreenSpacePolyLine screenSpaceLineOtherRoad = new ScreenSpacePolyLine(otherRoadCreator.bezierPath, otherRoadCreator.transform, screenPolylineMaxAngleError, screenPolylineMinVertexDst);
			segmentIndex = screenSpaceLineOtherRoad.ClosestSegmentIndexFromPosition(intersectionPoint3D);
			segmentStartPoint = otherRoadCreator.bezierPath.GetPointsInSegment(segmentIndex)[0];

			distanceAtIntersection = otherRoadCreator.path.GetClosestDistanceAlongPath(intersectionPoint3D);
			position = otherRoadCreator.path.GetPointAtDistance(distanceAtIntersection + intersection.IntersectionLength/2);
			postion2 = otherRoadCreator.path.GetPointAtDistance(distanceAtIntersection - intersection.IntersectionLength/2);
			intersection.Road2AnchorPoint1 = position;
			intersection.Road2AnchorPoint2 = postion2;
			if (Vector3.Distance(segmentStartPoint, position) < Vector3.Distance(segmentStartPoint, postion2))
			{
				otherRoadCreator.bezierPath.SplitSegment(position, segmentIndex, otherRoadCreator.path.GetClosestTimeOnPath(position));
				otherRoadCreator.bezierPath.SplitSegment(postion2, segmentIndex + 1, otherRoadCreator.path.GetClosestTimeOnPath(postion2));
			}
			else
			{
				otherRoadCreator.bezierPath.SplitSegment(postion2, segmentIndex, otherRoadCreator.path.GetClosestTimeOnPath(postion2));
				otherRoadCreator.bezierPath.SplitSegment(position, segmentIndex + 1, otherRoadCreator.path.GetClosestTimeOnPath(position));
			}
			DeleteAnchorsInsideIntersectionBounds(intersection);
		}
		void DeleteAnchorsInsideIntersectionBounds(Intersection intersection)
		{
			float distanceToInterfaceAnchor = Vector3.Distance(intersection.IntersectionPosition, intersection.Road1AnchorPoint1);
			Bounds intersectionBounds = new Bounds(intersection.IntersectionPosition, new Vector3(distanceToInterfaceAnchor*2.5f, 1, distanceToInterfaceAnchor*2.5f));
			// If there is an existing handle in the intersection area, delete it
			for (int i = 0; i < intersection.Road2PathCreator.bezierPath.NumPoints; i += 3)
			{
				int handleIndex1 = i % intersection.Road2PathCreator.bezierPath.NumPoints;
				// don't delete the intersection handles
				if (intersection.Road2PathCreator.bezierPath[handleIndex1] == intersection.Road2AnchorPoint1 || intersection.Road2PathCreator.bezierPath[handleIndex1] == intersection.Road2AnchorPoint2) 
				{
					continue;
				}
				if (intersectionBounds.Contains(intersection.Road2PathCreator.bezierPath[handleIndex1])){
					intersection.Road2PathCreator.bezierPath.DeleteSegment(handleIndex1);
				}
			}
			for (int i = 0; i < intersection.Road1PathCreator.bezierPath.NumPoints; i += 3)
			{
				int handleIndex1 = i % intersection.Road1PathCreator.bezierPath.NumPoints;
				// don't delete the intersection handles
				if (intersection.Road1PathCreator.bezierPath[handleIndex1] == intersection.Road1AnchorPoint1 || intersection.Road1PathCreator.bezierPath[handleIndex1] == intersection.Road1AnchorPoint2) 
				{
					continue;
				}
				if (intersectionBounds.Contains(intersection.Road1PathCreator.bezierPath[handleIndex1])){
					intersection.Road1PathCreator.bezierPath.DeleteSegment(handleIndex1);
				}
			}
			
		}
		
		bool IsMouseOverObject(Event e){
			// Raycast to the mouse position
			Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			RaycastHit hitData;
			Physics.Raycast(mouseRay, out hitData);
			Debug.DrawRay(mouseRay.origin, mouseRay.direction * 10000);
			if (Physics.Raycast(mouseRay, out hitData))
        	{
				return true;
			}
			return false;
		}

		void DrawBezierPathSceneEditor()
		{
			bool displayControlPoints = data.displayControlPoints && (bezierPath.ControlPointMode != BezierPath.ControlMode.Automatic || !globalDisplaySettings.hideAutoControls);
			Bounds bounds = bezierPath.CalculateBoundsWithTransform(creator.transform);

			if (Event.current.type == EventType.Repaint)
			{
				for (int i = 0; i < bezierPath.NumSegments; i++)
				{
					Vector3[] points = bezierPath.GetPointsInSegment(i);
					for (int j = 0; j < points.Length; j++)
					{
						points[j] = MathUtility.TransformPoint(points[j], creator.transform, bezierPath.Space);
					}

					if (data.showPerSegmentBounds)
					{
						Bounds segmentBounds = CubicBezierUtility.CalculateSegmentBounds(points[0], points[1], points[2], points[3]);
						Handles.color = globalDisplaySettings.segmentBounds;
						Handles.DrawWireCube(segmentBounds.center, segmentBounds.size);
					}

					// Draw lines between control points
					if (displayControlPoints)
					{
						Handles.color = (bezierPath.ControlPointMode == BezierPath.ControlMode.Automatic) ? globalDisplaySettings.handleDisabled : globalDisplaySettings.controlLine;
						Handles.DrawLine(points[1], points[0]);
						Handles.DrawLine(points[2], points[3]);
					}

					// Draw path
					bool highlightSegment = (i == selectedSegmentIndex && Event.current.shift && draggingHandleIndex == -1 && mouseOverHandleIndex == -1);
					Color segmentCol = (highlightSegment) ? globalDisplaySettings.highlightedPath : globalDisplaySettings.bezierPath;
					Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, 2);
				}

				if (data.showPathBounds)
				{
					Handles.color = globalDisplaySettings.bounds;
					Handles.DrawWireCube(bounds.center, bounds.size);
				}

				// Draw normals
				if (data.showNormals)
				{
					if (!hasUpdatedNormalsVertexPath)
					{
						normalsVertexPath = new VertexPath(bezierPath, creator.transform, normalsSpacing);
						hasUpdatedNormalsVertexPath = true;
					}

					if (editingNormalsOld != data.showNormals)
					{
						editingNormalsOld = data.showNormals;
						Repaint();
					}

					Vector3[] normalLines = new Vector3[normalsVertexPath.NumPoints * 2];
					Handles.color = globalDisplaySettings.normals;
					for (int i = 0; i < normalsVertexPath.NumPoints; i++)
					{
						normalLines[i * 2] = normalsVertexPath.GetPoint(i);
						normalLines[i * 2 + 1] = normalsVertexPath.GetPoint(i) + normalsVertexPath.GetNormal(i) * globalDisplaySettings.normalsLength;
					}
					Handles.DrawLines(normalLines);
				}
			}

			if (data.displayAnchorPoints)
			{
				for (int i = 0; i < bezierPath.NumPoints; i += 3)
				{
					DrawHandle(i);
				}
			}
			if (displayControlPoints)
			{
				for (int i = 1; i < bezierPath.NumPoints - 1; i += 3)
				{
					DrawHandle(i);
					DrawHandle(i + 1);
				}
			}
		}

		void DrawHandle(int i)
		{
			Vector3 handlePosition = MathUtility.TransformPoint(bezierPath[i], creator.transform, bezierPath.Space);

			float anchorHandleSize = GetHandleDiameter(globalDisplaySettings.anchorSize * data.bezierHandleScale, bezierPath[i]);
			float controlHandleSize = GetHandleDiameter(globalDisplaySettings.controlSize * data.bezierHandleScale, bezierPath[i]);

			bool isAnchorPoint = i % 3 == 0;
			bool isInteractive = isAnchorPoint || bezierPath.ControlPointMode != BezierPath.ControlMode.Automatic;
			float handleSize = (isAnchorPoint) ? anchorHandleSize : controlHandleSize;
			bool doTransformHandle = i == handleIndexToDisplayAsTransform;

			PathHandle.HandleColours handleColours = (isAnchorPoint) ? splineAnchorColours : splineControlColours;
			if (i == handleIndexToDisplayAsTransform)
			{
				handleColours.defaultColour = (isAnchorPoint) ? globalDisplaySettings.anchorSelected : globalDisplaySettings.controlSelected;
			}
			var cap = capFunctions[(isAnchorPoint) ? globalDisplaySettings.anchorShape : globalDisplaySettings.controlShape];
			PathHandle.HandleInputType handleInputType;
			handlePosition = PathHandle.DrawHandle(handlePosition, bezierPath.Space, isInteractive, handleSize, cap, handleColours, out handleInputType, i);

			if (doTransformHandle)
			{
				// Show normals rotate tool 
				if (data.showNormals && Tools.current == Tool.Rotate && isAnchorPoint && bezierPath.Space == PathSpace.xyz)
				{
					Handles.color = handlesStartCol;

					int attachedControlIndex = (i == bezierPath.NumPoints - 1) ? i - 1 : i + 1;
					Vector3 dir = (bezierPath[attachedControlIndex] - handlePosition).normalized;
					float handleRotOffset = (360 + bezierPath.GlobalNormalsAngle) % 360;
					anchorAngleHandle.radius = handleSize * 3;
					anchorAngleHandle.angle = handleRotOffset + bezierPath.GetAnchorNormalAngle(i / 3);
					Vector3 handleDirection = Vector3.Cross(dir, Vector3.up);
					Matrix4x4 handleMatrix = Matrix4x4.TRS(
						handlePosition,
						Quaternion.LookRotation(handleDirection, dir),
						Vector3.one
					);

					using (new Handles.DrawingScope(handleMatrix))
					{
						// draw the handle
						EditorGUI.BeginChangeCheck();
						anchorAngleHandle.DrawHandle();
						if (EditorGUI.EndChangeCheck())
						{
							Undo.RecordObject(creator, "Set angle");
							bezierPath.SetAnchorNormalAngle(i / 3, anchorAngleHandle.angle - handleRotOffset);
						}
					}

				}
				else
				{
					handlePosition = Handles.DoPositionHandle(handlePosition, Quaternion.identity);
				}

			}

			switch (handleInputType)
			{
				case PathHandle.HandleInputType.LMBDrag:
					draggingHandleIndex = i;
					handleIndexToDisplayAsTransform = -1;
					Repaint();
					break;
				case PathHandle.HandleInputType.LMBRelease:
					draggingHandleIndex = -1;
					handleIndexToDisplayAsTransform = -1;
					Repaint();
					break;
				case PathHandle.HandleInputType.LMBClick:
					draggingHandleIndex = -1;
					if (Event.current.shift)
					{
						handleIndexToDisplayAsTransform = -1; // disable move tool if new point added
					}
					else
					{
						if (handleIndexToDisplayAsTransform == i)
						{
							handleIndexToDisplayAsTransform = -1; // disable move tool if clicking on point under move tool
						}
						else
						{
							handleIndexToDisplayAsTransform = i;
						}
					}
					Repaint();
					break;
				case PathHandle.HandleInputType.LMBPress:
					if (handleIndexToDisplayAsTransform != i)
					{
						handleIndexToDisplayAsTransform = -1;
						Repaint();
					}
					break;
			}

			Vector3 localHandlePosition = MathUtility.InverseTransformPoint(handlePosition, creator.transform, bezierPath.Space);

			if (bezierPath[i] != localHandlePosition)
			{
				Undo.RecordObject(creator, "Move point");
				bezierPath.MovePoint(i, localHandlePosition);

			}

		}

		#endregion

		#region Internal methods

		void OnDisable()
		{
			Tools.hidden = false;
		}

		void OnEnable()
		{
			creator = (PathCreator)target;
			thisRoad = GetRoad(creator);
			bool in2DEditorMode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D;
			creator.InitializeEditorData(in2DEditorMode);

			data.bezierCreated -= ResetState;
			data.bezierCreated += ResetState;
			Undo.undoRedoPerformed -= OnUndoRedo;
			Undo.undoRedoPerformed += OnUndoRedo;

			LoadDisplaySettings();
			UpdateGlobalDisplaySettings();
			ResetState();
			SetTransformState(true);
		}

		void SetTransformState(bool initialize = false)
		{
			Transform t = creator.transform;
			if (!initialize)
			{
				if (transformPos != t.position || t.localScale != transformScale || t.rotation != transformRot)
				{
					data.PathTransformed();
				}
			}
			transformPos = t.position;
			transformScale = t.localScale;
			transformRot = t.rotation;
		}

		void OnUndoRedo()
		{
			hasUpdatedScreenSpaceLine = false;
			hasUpdatedNormalsVertexPath = false;
			selectedSegmentIndex = -1;

			Repaint();
		}

		void TabChanged()
		{
			SceneView.RepaintAll();
			RepaintUnfocusedSceneViews();
		}

		void LoadDisplaySettings()
		{
			globalDisplaySettings = GlobalDisplaySettings.Load();

			capFunctions = new Dictionary<GlobalDisplaySettings.HandleType, Handles.CapFunction>();
			capFunctions.Add(GlobalDisplaySettings.HandleType.Circle, Handles.CylinderHandleCap);
			capFunctions.Add(GlobalDisplaySettings.HandleType.Sphere, Handles.SphereHandleCap);
			capFunctions.Add(GlobalDisplaySettings.HandleType.Square, Handles.CubeHandleCap);
		}

		void UpdateGlobalDisplaySettings()
		{
			var gds = globalDisplaySettings;
			splineAnchorColours = new PathHandle.HandleColours(gds.anchor, gds.anchorHighlighted, gds.anchorSelected, gds.handleDisabled);
			splineControlColours = new PathHandle.HandleColours(gds.control, gds.controlHighlighted, gds.controlSelected, gds.handleDisabled);

			anchorAngleHandle.fillColor = new Color(1, 1, 1, .05f);
			anchorAngleHandle.wireframeColor = Color.grey;
			anchorAngleHandle.radiusHandleColor = Color.clear;
			anchorAngleHandle.angleHandleColor = Color.white;
		}

		void ResetState()
		{
			selectedSegmentIndex = -1;
			draggingHandleIndex = -1;
			mouseOverHandleIndex = -1;
			handleIndexToDisplayAsTransform = -1;
			hasUpdatedScreenSpaceLine = false;
			hasUpdatedNormalsVertexPath = false;

			bezierPath.OnModified -= OnPathModifed;
			bezierPath.OnModified += OnPathModifed;

			SceneView.RepaintAll();
			EditorApplication.QueuePlayerLoopUpdate();
		}

		void OnPathModifed()
		{
			hasUpdatedScreenSpaceLine = false;
			hasUpdatedNormalsVertexPath = false;

			RepaintUnfocusedSceneViews();
		}

		void RepaintUnfocusedSceneViews()
		{
			// If multiple scene views are open, repaint those which do not have focus.
			if (SceneView.sceneViews.Count > 1)
			{
				foreach (SceneView sv in SceneView.sceneViews)
				{
					if (EditorWindow.focusedWindow != (EditorWindow)sv)
					{
						sv.Repaint();
					}
				}
			}
		}

		void UpdatePathMouseInfo()
		{

			if (!hasUpdatedScreenSpaceLine || (screenSpaceLine != null && screenSpaceLine.TransformIsOutOfDate()))
			{
				screenSpaceLine = new ScreenSpacePolyLine(bezierPath, creator.transform, screenPolylineMaxAngleError, screenPolylineMinVertexDst);
				hasUpdatedScreenSpaceLine = true;
			}
			pathMouseInfo = screenSpaceLine.CalculateMouseInfo();
		}

		float GetHandleDiameter(float diameter, Vector3 handlePosition)
		{
			float scaledDiameter = diameter * constantHandleScale;
			if (data.keepConstantHandleSize)
			{
				scaledDiameter *= HandleUtility.GetHandleSize(handlePosition) * 2.5f;
			}
			return scaledDiameter;
		}

		BezierPath bezierPath
		{
			get
			{
				return data.bezierPath;
			}
		}

		PathCreatorData data
		{
			get
			{
				return creator.EditorData;
			}
		}

		bool editingNormals
		{
			get
			{
				return Tools.current == Tool.Rotate && handleIndexToDisplayAsTransform % 3 == 0 && bezierPath.Space == PathSpace.xyz;
			}
		}

		#endregion

	}

}
public class IntersectionPointData {
	public Vector2 intersectionPoint;
	public Quaternion rotation;

	public PathCreator Road1PathCreator;
    public PathCreator Road2PathCreator;

    public IntersectionPointData(Vector2 intersectionPoint, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator)
    {
		
        this.intersectionPoint = intersectionPoint;
        this.rotation = rotation;
		this.Road1PathCreator = road1PathCreator;
        this.Road2PathCreator = road2PathCreator;
    }
}
public static class LineUtil {
    public static void Swap<T>(ref T lhs, ref T rhs) {
        T temp = lhs;
        lhs = rhs;
        rhs = temp;
    }

    public static bool Approximately(float a, float b, float tolerance = 1e-5f) {
        return Mathf.Abs(a - b) <= tolerance;
    }

    public static float CrossProduct2D(Vector2 a, Vector2 b) {
        return a.x * b.y - b.x * a.y;
    }

    /// <summary>
    /// Determine whether 2 lines intersect, and give the intersection point if so.
    /// </summary>
    /// <param name="p1start">Start point of the first line</param>
    /// <param name="p1end">End point of the first line</param>
    /// <param name="p2start">Start point of the second line</param>
    /// <param name="p2end">End point of the second line</param>
    /// <param name="intersection">If there is an intersection, this will be populated with the point</param>
    /// <returns>True if the lines intersect, false otherwise.</returns>
    public static bool IntersectLineSegments2D(Vector2 p1start, Vector2 p1end, Vector2 p2start, Vector2 p2end,
        out Vector2 intersection) {
        // Consider:
        //   p1start = p
        //   p1end = p + r
        //   p2start = q
        //   p2end = q + s
        // We want to find the intersection point where :
        //  p + t*r == q + u*s
        // So we need to solve for t and u
        var p = p1start;
        var r = p1end - p1start;
        var q = p2start;
        var s = p2end - p2start;
        var qminusp = q - p;

        float cross_rs = CrossProduct2D(r, s);

        if (Approximately(cross_rs, 0f)) {
            // Parallel lines
            if (Approximately(CrossProduct2D(qminusp, r), 0f)) {
                // Co-linear lines, could overlap
                float rdotr = Vector2.Dot(r, r);
                float sdotr = Vector2.Dot(s, r);
                // this means lines are co-linear
                // they may or may not be overlapping
                float t0 = Vector2.Dot(qminusp, r / rdotr);
                float t1 = t0 + sdotr / rdotr;
                if (sdotr < 0) {
                    // lines were facing in different directions so t1 > t0, swap to simplify check
                    Swap(ref t0, ref t1);
                }

                if (t0 <= 1 && t1 >= 0) {
                    // Nice half-way point intersection
                    float t = Mathf.Lerp(Mathf.Max(0, t0), Mathf.Min(1, t1), 0.5f);
                    intersection = p + t * r;
                    return true;
                } else {
                    // Co-linear but disjoint
                    intersection = Vector2.zero;
                    return false;
                }
            } else {
                // Just parallel in different places, cannot intersect
                intersection = Vector2.zero;
                return false;
            }
        } else {
            // Not parallel, calculate t and u
            float t = CrossProduct2D(qminusp, s) / cross_rs;
            float u = CrossProduct2D(qminusp, r) / cross_rs;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) {
                intersection = p + t * r;
                return true;
            } else {
                // Lines only cross outside segment range
                intersection = Vector2.zero;
                return false;
            }
        }
    }
}