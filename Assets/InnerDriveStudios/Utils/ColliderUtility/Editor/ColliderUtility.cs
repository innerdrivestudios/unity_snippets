using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    /**
     * Allows you to quickly remove all colliders from a (nested) (prefab) GameObject
     * and to quickly re-add one bounding box, sphere or capsule collider.
     * 
     * @author J.C.Wichman - InnerDriveStudios.com
     */
    public static class ColliderUtility
	{
		private const string SUB_MENU = "Colliders/";

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Remove all colliders from selected objects")]
		private static void RemoveAllCollidersFromSelectedObjects()
		{
			//Loop over all selected objects, both hierarchy and project window ...
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
			TransformMemento selectedTransform = new TransformMemento(Selection.activeTransform);
			selectedTransform.Reset();

            if (Common.GetWorldBounds(Selection.activeTransform, out Bounds worldBounds))
			{
				BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(Selection.activeGameObject);

				boxCollider.center = worldBounds.center;
				Vector3 size = worldBounds.size;
				size.x = Mathf.Abs(size.x);
				size.y = Mathf.Abs(size.y);
				size.z = Mathf.Abs(size.z);
				boxCollider.size = size;
            }

			selectedTransform.Restore();
        }

        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding capsule collider")]
		private static void AddBoundingCapsuleCollider()
		{
            TransformMemento selectedTransform = new TransformMemento(Selection.activeTransform);
			selectedTransform.Reset();

            if (Common.GetWorldBounds(Selection.activeTransform, out Bounds worldBounds))
            {
                CapsuleCollider capsuleCollider = Undo.AddComponent<CapsuleCollider>(Selection.activeGameObject);

                capsuleCollider.center = worldBounds.center;
                Vector3 extents = worldBounds.extents;
                extents.x = Mathf.Abs(extents.x);
                extents.y = Mathf.Abs(extents.y);
                extents.z = Mathf.Abs(extents.z);

                //deduct direction and use other dimensions to calculate the radius
                int direction = 0;
                float height = extents.x;
                if (extents.y > extents.x && extents.y > extents.z)
                {
                    direction = 1;
                    height = extents.y;
                    extents.y = 0;
                }
                else if (extents.z > extents.x && extents.z > extents.y)
                {
                    direction = 2;
                    height = extents.z;
                    extents.z = 0;
                }

                capsuleCollider.height = height * 2;
                capsuleCollider.direction = direction;
                capsuleCollider.radius = extents.magnitude;
            }

            selectedTransform.Restore();
		}

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding sphere collider")]
		private static void AddBoundingSphereCollider()
		{
            TransformMemento selectedTransform = new TransformMemento(Selection.activeTransform);
			selectedTransform.Reset();

            if (Common.GetWorldBounds(Selection.activeTransform, out Bounds worldBounds))
            {
                SphereCollider sphereCollider = Undo.AddComponent<SphereCollider>(Selection.activeGameObject);
                sphereCollider.center = worldBounds.center;
				Vector3 extents = worldBounds.extents;
                extents.x = Mathf.Abs(extents.x);
                extents.y = Mathf.Abs(extents.y);
                extents.z = Mathf.Abs(extents.z);

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

            selectedTransform.Restore();
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
