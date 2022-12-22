using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class ColliderUtility
	{
		[MenuItem(Settings.menuPath+"Remove all colliders from selection")]
		private static void RemoveAllColliders()
		{
			foreach (GameObject gameObject in Selection.gameObjects)
			{
				Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
				for (int i = 0; i < colliders.Length; i++)
				{
					GameObject.DestroyImmediate(colliders[i], true);
				}
			}
		}
	}
}
