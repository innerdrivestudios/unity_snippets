using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerDriveStudios.Util
{
	public static class MissingComponentUtility
	{
        private const string SUB_MENU = "Missing Components/";

		[MenuItem(Settings.MENU_PATH+SUB_MENU+"Select all non prefab game objects with missing components")]
		private static void SelectAllNonPrefabGameObjectsWithMissingComponents()
		{
            //determine our root game objects to investigate
            GameObject[] gameObjectsToTest =
                    Common.GetEditMode() == Common.EditMode.Normal ?
                    SceneManager.GetActiveScene().GetRootGameObjects():
                    new[] { PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot};

            List<GameObject> goWithMissingComponents = new();

            foreach (GameObject go in gameObjectsToTest)
			{
                RecursivelyFindAllGameObjectsWithMissingComponents(go, goWithMissingComponents);
            }
            
            Selection.objects = goWithMissingComponents.ToArray<Object>(); //Convert to Object to avoid co-variant array conversion
		}

		private static void RecursivelyFindAllGameObjectsWithMissingComponents(GameObject go, List<GameObject> goWithMissingComponents)
		{
            //should we add the current object to the list?
            if (Common.IsNonPrefabGameObjectInstance(go) && GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
            {
                goWithMissingComponents.Add(go);
            }

            //repeat for all children
            foreach (Transform t in go.transform)
			{
                RecursivelyFindAllGameObjectsWithMissingComponents(t.gameObject, goWithMissingComponents);
			}
		}

        [MenuItem(Settings.MENU_PATH+SUB_MENU+"Select all prefab assets with missing components")]
        public static void SelectAllPrefabAssetsWithMissingComponents()
        {
            // Get all prefab assets in the project
            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            // Convert the guids to paths
            string[] paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            List<Object> objectsToSelect = new();

            foreach (string path in paths)
			{
                GameObject go = (GameObject)AssetDatabase.LoadMainAssetAtPath(path);
                if (go.GetComponentsInChildren<Component>().Any(x => x == null))
				{
                    objectsToSelect.Add(go);
				}
            }
       
            Selection.objects = objectsToSelect.ToArray();
        }

        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Remove missing components from all selected objects")]
        private static void RemoveMissingComponentsFromAllSelectedObjects()
        {
            /*
            GameObject[] gameObjectsToTest = Selection.gameObjects;
            List<GameObject> goWithMissingComponents = new List<GameObject>();

            foreach (GameObject go in gameObjectsToTest)
            {
                recursivelyFindAllGameObjectsWithMissingComponents(go, goWithMissingComponents);
            }

            foreach (GameObject go in goWithMissingComponents)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }
            */

            foreach (GameObject go in Selection.gameObjects)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }

        }

    }
}
