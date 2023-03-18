using UnityEngine;

namespace InnerDriveStudios.Util
{
    public static class TransformExtensions
    {
        public static void SnapToGrid(this Transform transform, float pGridSize)
        {
            // Calculate the new position by rounding the transform's position to the nearest multiple of the grid size
            Vector3 newPosition = transform.position;
            newPosition.x = Mathf.Round(newPosition.x / pGridSize) * pGridSize;
            newPosition.y = Mathf.Round(newPosition.y / pGridSize) * pGridSize;
            newPosition.z = Mathf.Round(newPosition.z / pGridSize) * pGridSize;

            // Update the transform's position to the snapped position
            transform.position = newPosition;
        }

        public static Transform[] GetChildren(this Transform transform)
		{
            int childCount = transform.childCount;
            Transform[] children = new Transform[childCount];
            for (int i = 0; i < childCount; i++) children[i] = transform.GetChild(i);
            return children;
		}
    }
}
