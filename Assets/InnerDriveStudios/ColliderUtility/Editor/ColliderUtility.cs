using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class ColliderUtility
	{
		[MenuItem(Settings.menuPath+"Remove all colliders from selected objects")]
		private static void RemoveAllColliders()
		{
			foreach (GameObject gameObject in Selection.gameObjects)
			{
				Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
				for (int i = 0; i < colliders.Length; i++)
				{
					Undo.DestroyObjectImmediate(colliders[i]);
				}
			}
		}
	}
}
