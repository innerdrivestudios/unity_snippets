using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class Common
	{
		///note that you can be in both play mode and one of these edit modes, since you can edit in play mode
		public enum EditMode { Normal, Prefab };

		/// <summary>
		/// Returns whether the application is in play mode
		/// </summary>
		/// <returns></returns>
		public static bool IsInPlayMode()
		{
			return Application.isPlaying;
		}

		/// <summary>
		/// Returns the amount of selected items in the hierarchy, project window, etc.
		/// </summary>
		/// <returns></returns>
		public static int GetSelectedObjectsCount()
		{
			return Selection.objects.Length;
		}

		/// <summary>
		/// Are we in normal edit mode (not editing a prefab), or any sort of prefab edit mode.
		/// </summary>
		/// <returns></returns>
		public static EditMode GetEditMode()
		{
			return PrefabStageUtility.GetCurrentPrefabStage() == null ? EditMode.Normal : EditMode.Prefab;
		}

		/// <summary>
		/// Returns true is the GameObject is not null and in a scene, but not (part of) a prefab instance.
		/// Note that this also returns true for any non prefab parts in prefab edit mode.
		/// If you want to exclude prefab edit mode, include the GetEditMode into your test.
		/// </summary>
		/// <param name="pGameObject"></param>
		/// <returns></returns>
		public static bool IsNonPrefabGameObjectInstance(GameObject pGameObject)
		{
			return pGameObject != null && pGameObject.scene.IsValid() && !PrefabUtility.IsPartOfAnyPrefab(pGameObject);
		}

		/// <summary>
		/// Is the transform a regular transform and not a RectTransform?
		/// </summary>
		/// <param name="pTransform"></param>
		/// <returns></returns>
		public static bool IsRegularTransform(Transform pTransform)
		{
			return pTransform != null && !(pTransform is RectTransform);
		}

		public static bool IsRootTransform(Transform pTransform)
		{
			return pTransform.parent == null;
		}

		public static bool IsEmpty(Transform pTransform)
		{
			return pTransform.GetComponents<Component>().Length == 1;
		}

		public static bool HasMeshRenderer(Transform pTransform)
		{
			return pTransform.GetComponent<MeshRenderer>() != null;
		}

		/// <summary> Returns false when the gameobject or its children do not contain any meshrenderer components</summary>
		public static bool GetBounds(Transform pRoot, out Bounds bounds)
		{
			MeshRenderer[] meshRenderers = pRoot.GetComponentsInChildren<MeshRenderer>();

			if (meshRenderers.Length == 0)
			{
				bounds = new Bounds();
				return false;
			}

			bounds = meshRenderers[0].bounds;
			for (int i = 1; i < meshRenderers.Length; i++)
			{
				bounds.Encapsulate(meshRenderers[i].bounds);
			}

			return true;
		}

	}
}
