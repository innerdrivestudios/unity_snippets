using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class ColliderUtility
	{
		private const string subMenu = "Colliders/";

		[MenuItem(Settings.menuPath + subMenu + "Remove all colliders from selected objects")]
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

		[MenuItem(Settings.menuPath + subMenu + "Add bounding box collider")]
		private static void AddBoundingBoxCollider()
		{
			Bounds bounds;
			if (!Common.GetBounds(Selection.activeGameObject, out bounds)) return;

			BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(Selection.activeGameObject);

			boxCollider.center = Selection.activeTransform.InverseTransformPoint(bounds.center);
			boxCollider.size = Selection.activeTransform.InverseTransformVector(bounds.extents * 2);
		}

		[MenuItem(Settings.menuPath + subMenu + "Add bounding capsule collider")]
		private static void AddBoundingCapsuleCollider()
		{
			Bounds bounds;
			if (!Common.GetBounds(Selection.activeGameObject, out bounds)) return;

			CapsuleCollider capsuleCollider = Undo.AddComponent<CapsuleCollider>(Selection.activeGameObject);

			capsuleCollider.center = Selection.activeTransform.InverseTransformPoint(bounds.center);
			Vector3 extents = Selection.activeTransform.InverseTransformVector(bounds.extents);

			//deduct direction and use other dimensions to calculate the radius
			int direction = 0;
			float height = extents.x;
			if (extents.y > extents.x && extents.y > extents.z) {
				direction = 1;
				height = extents.y;
				extents.y = 0;
			}
			else if (extents.z > extents.x && extents.z > extents.y) {
				direction = 2;
				height = extents.z;
				extents.z = 0;
			}

			capsuleCollider.height = height;
			capsuleCollider.direction = direction;
			capsuleCollider.radius = extents.magnitude;
		}

		[MenuItem(Settings.menuPath + subMenu + "Add bounding sphere collider")]
		private static void AddBoundingSphereCollider()
		{
			Bounds bounds;
			if (!Common.GetBounds(Selection.activeGameObject, out bounds)) return;

			SphereCollider sphereCollider = Undo.AddComponent<SphereCollider>(Selection.activeGameObject);
			sphereCollider.center = Selection.activeTransform.InverseTransformPoint(bounds.center);
			Vector3 extents = Selection.activeTransform.InverseTransformVector(bounds.extents);

			//we want to get the inner dimension of the box and not the outer (which would be extents.magnitude)
			//to get the inner dimension, we ignore the smallest dimension
			if (extents.x < extents.y && extents.x > extents.z)
			{
				extents.x = 0;
			}
			else if (extents.y > extents.z && extents.y > extents.x)
			{
				extents.y = 0;
			}
			else
			{
				extents.z = 0;
			}

			sphereCollider.radius = extents.magnitude;
		}

		[MenuItem(Settings.menuPath + subMenu + "Add bounding capsule collider", true)]
		[MenuItem(Settings.menuPath + subMenu + "Add bounding box collider", true)]
		[MenuItem(Settings.menuPath + subMenu + "Add bounding sphere collider", true)]
		private static bool IsGameObjectSelected()
		{
			return Selection.activeGameObject != null;
		}
	}
}
