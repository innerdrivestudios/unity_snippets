using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{

	/**
	 * This is a simple tool to replace gameobjects in your scene with a random prefab from your selection.
	 */
	public class PrefabReplacer : EditorWindow
	{
		[MenuItem(Settings.menuPath + "Prefab Replacer")]
		private static void init()
		{
			PrefabReplacer window = GetWindow<PrefabReplacer>("Prefab Replacer");
			window.minSize = new Vector2(350, 200);
			window.Show();
		}

		private PrefabReplacerSettings settings;
		private Editor settingsEditor;

		private GameObject[] _selectedObjects = null;

		//we update this flag after each selection change
		private enum SelectionStatus { NONE, INVALID, OK };
		private SelectionStatus status = SelectionStatus.NONE;

		private void OnEnable()
		{
			//load settings, we can enter a path directly, but the location of this script might change
			//try to find the path using monoscript, this works since editor derives from scriptable object
			string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
			//This is important otherwise you'll overwrite this script (been there done that ;)) and don't forget the .asset!
			path = path.Replace(".cs", "_data.asset");

			settings = AssetDatabase.LoadAssetAtPath<PrefabReplacerSettings>(path);
			if (!settings)
			{
				settings = CreateInstance<PrefabReplacerSettings>();
				AssetDatabase.CreateAsset(settings, path);
			}

			OnSelectionChange();
		}

		//anytime the selection in the scene changes, we check whether the selection is valid for our purposes,
		//repaint our window based on the new status, and update the layout of the selected objects based on our settings
		private void OnSelectionChange()
		{
			updateSelectionStatus();
			Repaint();
		}

		void OnGUI()
		{
			//show the editor
			Editor.CreateCachedEditor(settings, null, ref settingsEditor);
			settingsEditor.OnInspectorGUI();

			GUI.enabled = status == SelectionStatus.OK;

			if (GUILayout.Button("Replace selected"))
			{
				if (settings.replacers != null && settings.replacers.Length > 0)
				{
					List<GameObject> newSelection = new List<GameObject>();

					System.Type typeFilter = null;
					if (settings.hasToBeOfType != null) typeFilter = settings.hasToBeOfType.GetClass();

					for (int i = 0; i < _selectedObjects.Length; i++)
					{
						GameObject selectedObject = _selectedObjects[i];

						//if our selection was deleted during the loop skip it
						if (selectedObject == null) continue;

						//if a typefilter has been specified and our gameObject doesn't have that type skip it
						if (typeFilter != null && selectedObject.GetComponent(typeFilter) == null) continue;

						//Undo.DestroyObjectImmediate
						GameObject go = PrefabUtility.InstantiatePrefab(settings.replacers[UnityEngine.Random.Range(0, settings.replacers.Length)]) as GameObject;
						go.transform.parent = selectedObject.transform.parent;
						go.transform.position = selectedObject.transform.position;

						//currently prefab has scale from replacement, but using original localScale is more logical
						if (settings.keepCurrentLocalScale)
						{
							go.transform.localScale = selectedObject.transform.localScale;
						}

						//currently prefab has rotation from replacement, but using original rotation is more logical
						if (settings.keepCurrentLocalRotation)
						{
							go.transform.localRotation = selectedObject.transform.localRotation;
						}

						if (settings.randomAngleToAdd != 0)
						{
							float angle = Random.Range(0, settings.randomAngleToAdd);
							if (settings.angleSnap != 0) angle = Mathf.Round(angle / settings.angleSnap) * settings.angleSnap;
							go.transform.localRotation *= Quaternion.AngleAxis(angle, settings.rotationAxis);
						}

						Undo.RegisterCreatedObjectUndo(go, "Replacer");
						Undo.DestroyObjectImmediate(selectedObject);

						newSelection.Add(go);

/*		
		public bool addAdditionalRotation = false;
		public Vector3 rotationAxis = Vector3.up;
		public Range randomAngleToAdd;
		public bool integerAnglesOnly = true;
		public int angleMultiplier = 1;
*/
	}

					Selection.objects = newSelection.ToArray();
				}
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Close", GUILayout.Width(100), GUILayout.Height(30)))
			{
				Close();
			}

			EditorGUILayout.EndHorizontal();
		}

		private void updateSelectionStatus()
		{
			_selectedObjects = Selection.gameObjects;

			int count = _selectedObjects.Length;                       //cache the nr of objects

			if (count == 0)
			{
				status = SelectionStatus.NONE;
				return;
			}

			for (int i = 0; i < count; i++)
			{
				if (!_selectedObjects[i].scene.IsValid())
				{
					status = SelectionStatus.INVALID;
					return;
				}
			}

			status = SelectionStatus.OK;
		}

	}

}