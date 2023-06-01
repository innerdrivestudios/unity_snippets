using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class ColliderUtility
	{
		private const string SUB_MENU = "Colliders/";

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Remove all colliders from selected objects")]
		private static void RemoveAllColliders()
		{
			foreach (GameObject gameObject in Selection.gameObjects)
			{
                foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
                {
                    Undo.DestroyObjectImmediate(collider);
                }
			}
		}

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding box collider")]
		private static void AddBoundingBoxCollider()
		{
            if (!Common.GetBounds(Selection.activeTransform, out Bounds bounds)) return;

			BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(Selection.activeGameObject);

			boxCollider.center = Selection.activeTransform.InverseTransformPoint(bounds.center);
			Vector3 size = Selection.activeTransform.InverseTransformVector(bounds.extents * 2);
			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);
			size.z = Mathf.Abs(size.z);
			boxCollider.size = size;
		}

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding capsule collider")]
		private static void AddBoundingCapsuleCollider()
		{
            if (!Common.GetBounds(Selection.activeTransform, out Bounds bounds)) return;

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

			capsuleCollider.height = height * 2;
			capsuleCollider.direction = direction;
			capsuleCollider.radius = extents.magnitude;
		}

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding sphere collider")]
		private static void AddBoundingSphereCollider()
		{
            if (!Common.GetBounds(Selection.activeTransform, out Bounds bounds)) return;

			SphereCollider sphereCollider = Undo.AddComponent<SphereCollider>(Selection.activeGameObject);
			sphereCollider.center = Selection.activeTransform.InverseTransformPoint(bounds.center);
			Vector3 extents = Selection.activeTransform.InverseTransformVector(bounds.extents);

			float height = extents.x;
			if (extents.y > extents.x && extents.y > extents.z)
			{
				height = extents.y;
			}
			else if (extents.z > extents.x && extents.z > extents.y)
			{
				height = extents.z;
			}
	
			sphereCollider.radius = height;
		}

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding capsule collider", true)]
		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding box collider", true)]
		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding sphere collider", true)]
		private static bool IsGameObjectSelected()
		{
			return Selection.activeGameObject != null;
		}
	}
}
