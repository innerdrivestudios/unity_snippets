using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class Common
	{
		//note that you can be in both play mode and one of these edit modes, since you can edit in play mode
		public enum EditMode { NORMAL, PREFAB };

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
			return Selection.count;
		}

		/// <summary>
		/// Are we in normal edit mode (not editing a prefab), or any sort of prefab edit mode.
		/// </summary>
		/// <returns></returns>
		public static EditMode GetEditMode()
		{
			return PrefabStageUtility.GetCurrentPrefabStage() == null ? EditMode.NORMAL : EditMode.PREFAB;
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
			return pGameObject != null && pGameObject.scene != null && !PrefabUtility.IsPartOfAnyPrefab(pGameObject);
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

		public static bool IsEmpty (Transform pTransform)
		{
			return pTransform.GetComponents<Component>().Length == 1;
		}

		public static bool HasMeshRenderer(Transform pTransform)
		{
			return pTransform.GetComponent<MeshRenderer>() != null;
		}

	}
}
