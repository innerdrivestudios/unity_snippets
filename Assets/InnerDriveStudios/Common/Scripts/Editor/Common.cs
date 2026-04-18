using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    /**
     * Bunch of utility methods to make the other classes simpler.
     * 
     * @author J.C.Wichman - InnerDriveStudios.com
     */
    public static class Common
    {
        /// <summary>
        /// Indicates the current editing context.
        /// Note that you can be in both play mode and one of these edit modes, since you can edit in play mode.
        /// </summary>
        public enum EditMode { Normal, Prefab };

        /// <summary>
        /// Returns whether the application is currently in play mode.
        /// </summary>
        /// <returns>True if the application is in play mode; otherwise, false.</returns>
        public static bool IsInPlayMode()
        {
            return Application.isPlaying;
        }

        /// <summary>
        /// Returns the number of selected objects in the hierarchy, project window, and other Unity selections.
        /// </summary>
        /// <returns>The number of currently selected objects.</returns>
        public static int GetSelectedObjectsCount()
        {
            return Selection.objects.Length;
        }

        /// <summary>
        /// Returns whether Unity is in normal edit mode or prefab edit mode.
        /// </summary>
        /// <returns>The current edit mode.</returns>
        public static EditMode GetEditMode()
        {
            return PrefabStageUtility.GetCurrentPrefabStage() == null ? EditMode.Normal : EditMode.Prefab;
        }

        /// <summary>
        /// Returns true if the GameObject is not null, is part of a valid scene, and is not part of any prefab.
        /// Note that this also returns true for any non prefab parts in prefab edit mode.
        /// If you want to exclude prefab edit mode, include the GetEditMode into your test.
        /// </summary>
        /// <param name="pGameObject">The GameObject to test.</param>
        /// <returns>True if the GameObject is a non-prefab scene instance; otherwise, false.</returns>
        public static bool IsNonPrefabGameObjectInstance(GameObject pGameObject)
        {
            return pGameObject != null && pGameObject.scene.IsValid() && !PrefabUtility.IsPartOfAnyPrefab(pGameObject);
        }

        /// <summary>
        /// Returns whether the specified GameObject is a prefab asset.
        /// </summary>
        /// <param name="pGameObject">The GameObject to test.</param>
        /// <returns>True if the GameObject is a prefab asset; otherwise, false.</returns>
        public static bool IsPrefab(GameObject pGameObject)
        {
            return pGameObject != null && !pGameObject.scene.IsValid();
        }

        /// <summary>
        /// Returns whether the transform is a regular Transform and not a RectTransform.
        /// </summary>
        /// <param name="pTransform">The Transform to test.</param>
        /// <returns>True if the transform is not a RectTransform; otherwise, false.</returns>
        public static bool IsRegularTransform(Transform pTransform)
        {
            return pTransform != null && !(pTransform is RectTransform);
        }

        /// <summary>
        /// Returns whether the transform has no parent.
        /// </summary>
        /// <param name="pTransform">The Transform to test.</param>
        /// <returns>True if the transform has no parent; otherwise, false.</returns>
        public static bool IsRootTransform(Transform pTransform)
        {
            return pTransform.parent == null;
        }

        /// <summary>
        /// Returns whether the transform has no components other than Transform.
        /// </summary>
        /// <param name="pTransform">The Transform to test.</param>
        /// <returns>True if the transform only has its Transform component; otherwise, false.</returns>
        public static bool IsEmpty(Transform pTransform)
        {
            return pTransform.GetComponents<Component>().Length == 1;
        }

        /// <summary>
        /// Returns whether the given transform has a MeshRenderer component.
        /// </summary>
        /// <param name="pTransform">The Transform to test.</param>
        /// <returns>True if the transform has a MeshRenderer; otherwise, false.</returns>
        public static bool HasMeshRenderer(Transform pTransform)
        {
            return pTransform.GetComponent<MeshRenderer>() != null;
        }

        /// <summary>
        /// Calculates a single world-space Bounds that encapsulates all MeshRenderer components on the given transform and its children.
        /// </summary>
        /// <param name="pRoot">The root transform to evaluate.</param>
        /// <param name="bounds">The combined world-space bounds.</param>
        /// <returns>True if at least one MeshRenderer was found; otherwise, false.</returns>
        public static bool GetWorldBounds(Transform pRoot, out Bounds bounds)
        {
            MeshRenderer[] meshRenderers = pRoot.GetComponentsInChildren<MeshRenderer>();

            if (meshRenderers.Length == 0)
            {
                bounds = new Bounds();
                return false;
            }

            //This gets the world bounds of every MeshRenderer and makes sure to return ONE BIG
            //bounds object that encapsulates all of them

            bounds = meshRenderers[0].bounds;
            for (int i = 1; i < meshRenderers.Length; i++)
            {
                bounds.Encapsulate(meshRenderers[i].bounds);
            }

            return true;
        }

        /// <summary>
        /// Calculates a single bounds object in the local space of the given root transform that encapsulates all MeshRenderer components on the root and its children.
        /// </summary>
        /// <param name="pRoot">The root transform whose local space should be used.</param>
        /// <param name="bounds">The combined local-space bounds.</param>
        /// <returns>True if at least one MeshRenderer was found; otherwise, false.</returns>
        public static bool GetLocalBounds(Transform pRoot, out Bounds bounds)
        {
            MeshRenderer[] meshRenderers = pRoot.GetComponentsInChildren<MeshRenderer>();

            if (meshRenderers.Length == 0)
            {
                bounds = new Bounds();
                return false;
            }

            //Ok, so what we do here is we get the local bounds of every object... however...
            //We want to know the location and extends of these bounds in relation to the passed in pRoot
            //So we need to reinterpret the bounds local to meshRenderer[i] to figure out
            //what those bounds are relative to the root transform.

            bounds = ReinterpretBounds(pRoot, meshRenderers[0].transform, meshRenderers[0].localBounds);

            for (int i = 1; i < meshRenderers.Length; i++)
            {
                bounds.Encapsulate(
                    ReinterpretBounds(pRoot, meshRenderers[i].transform, meshRenderers[i].localBounds)
                );
            }

            return true;
        }

        /// <summary>
        /// Reinterprets bounds from one transform space into another transform space.
        /// </summary>
        /// <param name="pParentSpace">The target transform space.</param>
        /// <param name="pOriginalLocalBoundsSpace">The transform space the bounds are currently relative to.</param>
        /// <param name="pBounds">The bounds to reinterpret.</param>
        /// <returns>The bounds reinterpreted in the target transform space.</returns>
        private static Bounds ReinterpretBounds(Transform pParentSpace, Transform pOriginalLocalBoundsSpace, Bounds pBounds)
        {
            //First reinterpret the original center point, from local to world, world to parent (root)
            pBounds.center = pOriginalLocalBoundsSpace.TransformPoint(pBounds.center);
            pBounds.center = pParentSpace.InverseTransformPoint(pBounds.center);

            //Then reinterpret the bounds extents vector, from local to world, world to parent (root)
            pBounds.extents = pOriginalLocalBoundsSpace.TransformVector(pBounds.extents);
            pBounds.extents = pParentSpace.InverseTransformVector(pBounds.extents);
            return pBounds;
        }

        /// <summary>
        /// Returns whether the given GameObject is part of a prefab instance but is not the root object of that instance.
        /// </summary>
        /// <param name="obj">The GameObject to test.</param>
        /// <returns>True if the object is part of a prefab instance and is not its root; otherwise, false.</returns>
        public static bool IsPartOfPrefabInstanceButNotRoot(GameObject obj)
        {
            // Check if the object is part of a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                // Get the root of the prefab instance
                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj);

                // If the object is not the root, then it's part of the prefab but not the root
                return prefabRoot != obj;
            }

            // The object is not part of a prefab instance
            return false;
        }

    }
}