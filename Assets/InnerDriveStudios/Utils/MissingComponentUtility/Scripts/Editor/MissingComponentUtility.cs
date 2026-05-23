using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerDriveStudios.Util
{
    /// <summary>
    /// Editor utility for finding and removing missing MonoBehaviour references.
    ///
    /// Supported/intended workflows:
    /// 1. Select all scene or prefab-stage GameObjects that contain missing components.
    /// 2. Select all prefab assets in the project that contain missing components.
    /// 3. Remove missing components from the currently selected GameObjects.
    ///
    /// Important:
    /// - The scene-object search is recursive.
    /// - The remove action itself is not recursive; it only removes missing components from the currently selected GameObjects.
    /// - In the intended workflow, this is fine, because the recursive search already
    ///   selects every GameObject that actually contains missing components.
    /// </summary>
    public static class MissingComponentUtility
    {
        private const string SUB_MENU = "Missing Components/";

        /// <summary>
        /// Recursively scans the active scene root objects, or the current prefab stage root,
        /// and selects every GameObject that has one or more missing MonoBehaviour references.
        /// </summary>
        [MenuItem(Settings.IDS_UTIL_PATH + SUB_MENU + "Select all game objects with missing components", priority = 0)]
        private static void SelectAllGameObjectsWithMissingComponents()
        {
            GameObject[] rootObjects = GetRootObjectsToScan();
            List<GameObject> gameObjectsWithMissingComponents = new List<GameObject>();

            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject == null)
                    continue;

                RecursivelyFindAllGameObjectsWithMissingComponents(rootObject, gameObjectsWithMissingComponents);
            }

            // Convert to Object[] explicitly to avoid array covariance issues.
            Selection.objects = gameObjectsWithMissingComponents.ToArray<Object>();
        }

        /// <summary>
        /// Recursively traverses the provided GameObject hierarchy and adds every GameObject
        /// with one or more missing MonoBehaviour references to the result list.
        /// </summary>
        /// <param name="pGameObject">The current GameObject to inspect.</param>
        /// <param name="pResults">The list that collects matching GameObjects.</param>
        private static void RecursivelyFindAllGameObjectsWithMissingComponents(GameObject pGameObject, List<GameObject> pResults)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(pGameObject) > 0)
            {
                pResults.Add(pGameObject);
            }

            foreach (Transform child in pGameObject.transform)
            {
                RecursivelyFindAllGameObjectsWithMissingComponents(child.gameObject, pResults);
            }
        }

        /// <summary>
        /// Searches all prefab assets in the project and selects the prefabs that contain
        /// one or more missing component references anywhere in their hierarchy.
        /// </summary>
        [MenuItem(Settings.IDS_UTIL_PATH + SUB_MENU + "Select all prefab assets with missing components", priority = 1)]
        private static void SelectAllPrefabAssetsWithMissingComponents()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            List<Object> prefabsWithMissingComponents = new List<Object>();

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefabAsset = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;

                if (prefabAsset == null) continue;

                if (PrefabHasMissingComponents(prefabAsset))
                {
                    prefabsWithMissingComponents.Add(prefabAsset);
                }
            }

            Selection.objects = prefabsWithMissingComponents.ToArray();
        }

        /// <summary>
        /// Removes missing MonoBehaviour references from all currently selected GameObjects.
        ///
        /// Note:
        /// This method does not recurse into children automatically.
        /// It assumes the selection already contains the exact GameObjects to clean,
        /// which is how the recursive selection command works.
        /// </summary>
        [MenuItem(Settings.IDS_UTIL_PATH + SUB_MENU + "Remove missing components from all selected objects", priority = 2)]
        private static void RemoveMissingComponentsFromAllSelectedObjects()
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            if (selectedObjects.Length == 0)
            {
                Debug.Log("MissingComponentUtility: No GameObjects selected.");
                return;
            }

            bool changedSomething = false;

            foreach (GameObject gameObject in selectedObjects)
            {
                if (gameObject == null)
                    continue;

                int missingCountBefore = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);

                if (missingCountBefore <= 0) continue;

                Undo.RegisterCompleteObjectUndo(gameObject, "Remove Missing Components");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                EditorUtility.SetDirty(gameObject);

                changedSomething = true;
            }

            if (!changedSomething) return;

            MarkCurrentContextDirty();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Returns the root GameObjects that should be scanned.
        /// In normal editing mode this is the active scene root objects.
        /// In prefab editing mode this is the prefab stage root.
        /// </summary>
        private static GameObject[] GetRootObjectsToScan()
        {
            if (Common.GetEditMode() == Common.EditMode.Normal)
            {
                return SceneManager.GetActiveScene().GetRootGameObjects();
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage == null || prefabStage.prefabContentsRoot == null)
            {
                return System.Array.Empty<GameObject>();
            }

            return new[] { prefabStage.prefabContentsRoot };
        }

        /// <summary>
        /// Checks whether a prefab asset contains one or more missing components
        /// anywhere in its hierarchy.
        /// </summary>
        private static bool PrefabHasMissingComponents(GameObject prefabAsset)
        {
            return prefabAsset.GetComponentsInChildren<Component>(true).Any(component => component == null);
        }

        /// <summary>
        /// Marks the active scene or prefab stage as dirty so Unity knows there are unsaved changes.
        /// </summary>
        private static void MarkCurrentContextDirty()
        {
            if (Common.GetEditMode() == Common.EditMode.Normal)
            {
                Scene activeScene = SceneManager.GetActiveScene();

                if (activeScene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }

                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null && prefabStage.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
        }
    }
}