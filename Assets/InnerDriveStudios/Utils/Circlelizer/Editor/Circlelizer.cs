using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	/**
	 * Utility to position a number of selected objects in a circle with a given radius and facing.
	 */
	public class Circlelizer : EditorWindow
	{
		[MenuItem(Settings.MENU_PATH + "Circlelizer")]
		private static void Init()
		{
			Circlelizer window = GetWindow<Circlelizer>("Circlelizer");
			window.minSize = new Vector2(350, 200);
			window.Show();
		}

		//define all the values the user can set and use [SerializeField] to indicate which fields
		//we'd like to serialize in OnEnable/OnDisable using JSON

		[SerializeField] private Vector3 centerPoint = Vector3.zero;
		[SerializeField] private float startRotation = 0;
		[SerializeField] private Quaternion facing = Quaternion.identity;
		[SerializeField] private float radius = 5;
		[SerializeField] private bool spiral = false;
		[SerializeField] private float rotations = 1;
		[SerializeField] private bool lookAtCenter = false;

		//by default liveUpdate is disabled since we want this to be an active decision
		private bool liveUpdate = false;

		//we update this flag after each selection change
		private enum SelectionStatus { None, Invalid, Ok };
		private SelectionStatus status = SelectionStatus.None;

		//define some default facings using the new C# 7.0 value tuple definition (gotta love it !).
		//This is similar to a dictionary but without the lookups.
		private static (string, Vector3)[] facings = {
			("-X", Vector3.left), ("X", Vector3.right),
			("-Y", Vector3.down), ("Y", Vector3.up),
			("-Z", Vector3.back), ("Z", Vector3.forward)
		};

		private static string saveDataIdentifier = MethodBase.GetCurrentMethod().DeclaringType.Name;

		//enable a scenegui for this editor window and update some settings the moment we activate
		private void OnEnable()
		{
			//load the data if it exists or load the defaults and overwrite local values 
			string data = EditorPrefs.GetString(saveDataIdentifier, JsonUtility.ToJson(this, false));
			JsonUtility.FromJsonOverwrite(data, this);

			SceneView.duringSceneGui += OnSceneGUI;
			OnSelectionChange();
		}

		private void OnDisable()
		{
			//convert serialized fields to json and store it as a string
			string data = JsonUtility.ToJson(this, false);
			EditorPrefs.SetString(saveDataIdentifier, data);
			Debug.Log("Saved:" + data + " to " + saveDataIdentifier);

			SceneView.duringSceneGui -= OnSceneGUI;
		}

		//anytime the selection in the scene changes, we check whether the selection is valid for our purposes,
		//repaint our window based on the new status, and update the layout of the selected objects based on our settings
		private void OnSelectionChange()
		{
			UpdateSelectionStatus();
			Repaint();
			if (liveUpdate) UpdateLayout();
		}

		//OnGUI code is always horrible :(
		//This implements anything that must be draw in the window itself.
		//The window code MUST be in OnGUI
		private void OnGUI()
		{
			//vector3field layouts title above input by default, so that's why we need more cowbe.. code.
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Position");
			centerPoint = EditorGUILayout.Vector3Field("", centerPoint);
			EditorGUILayout.EndHorizontal();

			startRotation = EditorGUILayout.FloatField("Start rotation", startRotation);
			
			radius = EditorGUILayout.FloatField("Radius", radius);
			lookAtCenter = EditorGUILayout.Toggle("Look at center?", lookAtCenter);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Reset facing:");
			foreach ((string, Vector3) facing in facings)
			{
				if (GUILayout.Button(facing.Item1)) this.facing = Quaternion.FromToRotation(Vector3.forward, facing.Item2);
			}
			EditorGUILayout.EndHorizontal();

			spiral = EditorGUILayout.Toggle("Spiral?", spiral);
			rotations = EditorGUILayout.FloatField("Rotations?", rotations);
			liveUpdate = EditorGUILayout.Toggle("Live update ?", liveUpdate);

			switch (status)
			{
				case SelectionStatus.None:
					EditorGUILayout.HelpBox("Select some objects in the scene to get started!", MessageType.Info);
					break;
				case SelectionStatus.Invalid:
					EditorGUILayout.HelpBox("Circlelizer only works on scene objects !", MessageType.Warning);
					break;
				case SelectionStatus.Ok:
					if (!liveUpdate && GUILayout.Button("Circlelize!")) UpdateLayout();
					if (liveUpdate && GUI.changed) UpdateLayout();
					break;
			}
		}

		//Everything that has to be drawn in the SceneView HAS to go into onSceneGUI
		private void OnSceneGUI(SceneView sceneView)
		{
			//show the rotation handles 
			facing = Handles.RotationHandle(facing, centerPoint);
			facing.Normalize();

			//show the center point handles but respect the tools.pivotRotation setting
			centerPoint = Handles.PositionHandle(
				centerPoint,
				Tools.pivotRotation == PivotRotation.Local ? facing : Quaternion.identity
			);

			//Show a nice disc so that we can see the facing and radius more clearly
			Vector3 up = facing * Vector3.up;
			Vector3 right = facing * Vector3.right;
			Vector3 forward = facing * Vector3.forward;

			Handles.color = new Color(0, 1, 0, 0.1f);
			if (status == SelectionStatus.None) Handles.color = new Color(1, 0.8f, 0, 0.1f);
			if (status == SelectionStatus.Invalid) Handles.color = new Color(1, 0, 0, 0.1f);
			//use 359 instead 360 so we can see the starting point along the circle's edge
			Handles.DrawSolidArc(centerPoint, forward, right, 359, radius);

			//add a dot so we can change the radius (another 2 hours of my life I'll never get back ;))
			Handles.color = new Color(0, 1, 0);
			Vector3 currentPos = centerPoint + radius * right;
			EditorGUI.BeginChangeCheck();
			Vector3 newPos = Handles.Slider(currentPos, right, HandleUtility.GetHandleSize(currentPos) * 0.5f, Handles.SphereHandleCap, 0);
			if (EditorGUI.EndChangeCheck()) radius = Vector3.Dot(newPos - centerPoint, right);

			//if we changed anything in the sceneview repaint the editor window
			if (GUI.changed)
			{
				Repaint();
				if (liveUpdate) UpdateLayout();
			}
		}

		private void UpdateLayout()
		{
			if (status != SelectionStatus.Ok) return;

			int count = Selection.gameObjects.Length;                       //cache the nr of objects
			if (count == 0) return;

			float angleStep = rotations * 2 * Mathf.PI / count;            //calculate the angle step per object
			Vector3 right = facing * Vector3.right;                        //calculate the right vector for this facing
			Vector3 up = facing * Vector3.up;                              //calculate the up vector for this facing
			Vector3 forward = facing * Vector3.forward;

			for (int i = 0; i < count; i++)
			{
				float angle = i * angleStep + Mathf.Deg2Rad * startRotation;
				float radius = spiral ? (i * this.radius / count) : this.radius;
				//apply basic 2d rotation formula using different basis vectors
				Selection.gameObjects[i].transform.position =
					centerPoint + Mathf.Cos(angle) * right * radius + Mathf.Sin(angle) * up * radius;

				if (lookAtCenter)
				{
					//we are using the forward as the up normal ;)
					Selection.gameObjects[i].transform.LookAt(centerPoint, forward);
				}
			}
		}

		private void UpdateSelectionStatus()
		{
			int count = Selection.gameObjects.Length;

			if (count == 0)
			{
				status = SelectionStatus.None;
				return;
			}

			for (int i = 0; i < count; i++)
			{
				if (!Selection.gameObjects[i].scene.IsValid())
				{
					status = SelectionStatus.Invalid;
					return;
				}
			}

			status = SelectionStatus.Ok;
		}
	}
}
