using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    public class PivotUtility
    {
        /// <summary>
        /// Sets the pivot of each valid selected transform to the bottom-center of its combined MeshRenderer bounds.
        /// Invalid selections are skipped and reported in a popup dialog.
        /// </summary>
        /// <remarks>
        /// A transform is considered valid if:
        /// - It is a regular Transform (not a RectTransform).
        /// - It does not have a MeshRenderer on the selected object itself.
        /// - It (or its children) contains at least one MeshRenderer.
        /// 
        /// The operation preserves the visual position of all child objects by offsetting them after moving the pivot.
        /// </remarks>
        [MenuItem(Settings.IDS_UTIL_PATH + "Bottom Center Pivot")]
        private static void BottomCenterPivot()
        {
            if (Common.IsInPlayMode())
            {
                EditorUtility.DisplayDialog("Error", Strings.ONLY_IN_EDITMODE, Strings.OK);
                return;
            }

            Transform[] selectedTransforms = Selection.transforms;

            if (selectedTransforms == null || selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No objects selected.", Strings.OK);
                return;
            }

            List<Transform> validTransforms = new List<Transform>();
            List<string> skippedMessages = new List<string>();

            foreach (Transform selectedTransform in selectedTransforms)
            {
                if (TryGetSkipReason(selectedTransform, out string skipReason))
                {
                    skippedMessages.Add($"{selectedTransform.name} error: {skipReason}.");
                    continue;
                }

                validTransforms.Add(selectedTransform);
            }

            if (validTransforms.Count > 0)
            {
                Undo.SetCurrentGroupName("Bottom Center Pivot");
                int undoGroup = Undo.GetCurrentGroup();

                foreach (Transform selectedTransform in validTransforms)
                {
                    ProcessTransform(selectedTransform);
                }

                Undo.CollapseUndoOperations(undoGroup);
            }

            if (skippedMessages.Count > 0)
            {
                StringBuilder popupMessage = new StringBuilder();
                foreach (string skippedMessage in skippedMessages)
                {
                    popupMessage.AppendLine(skippedMessage);
                }

                EditorUtility.DisplayDialog("Skipped Objects", popupMessage.ToString(), Strings.OK);
            }
        }

        /// <summary>
        /// Determines whether a transform should be skipped and provides the reason.
        /// </summary>
        /// <param name="selectedTransform">The transform to validate.</param>
        /// <param name="reason">The reason why the transform should be skipped, if applicable.</param>
        /// <returns>True if the transform should be skipped; otherwise, false.</returns>
        private static bool TryGetSkipReason(Transform selectedTransform, out string reason)
        {
            if (!Common.IsRegularTransform(selectedTransform))
            {
                reason = Strings.NOT_REGULAR_TRANSFORM;
                return true;
            }

            if (Common.HasMeshRenderer(selectedTransform))
            {
                reason = Strings.NO_MESHRENDERER_ALLOWED;
                return true;
            }

            /*
			if (!Common.IsNonPrefabGameObjectInstance(selectedTransform.gameObject))
			{
				reason = "it is not a simple GameObject instance";
				return true;
			}
			*/

            if (!Common.GetWorldBounds(selectedTransform, out Bounds bounds))
            {
                reason = Strings.CHILD_MESHRENDERERS_REQUIRED;
                return true;
            }

            reason = null;
            return false;
        }

        /// <summary>
        /// Moves the pivot of the given transform to the bottom-center of its world-space bounds,
        /// while preserving the world positions of all its direct children.
        /// </summary>
        /// <param name="selectedTransform">The transform to process.</param>
        private static void ProcessTransform(Transform selectedTransform)
        {
            if (!Common.GetWorldBounds(selectedTransform, out Bounds bounds)) return;

            //make sure we can undo everything
            Undo.RecordObject(selectedTransform, "Pivot point fix");
            List<Transform> children = new List<Transform>();
            foreach (Transform child in selectedTransform)
            {
                Undo.RecordObject(child, "Pivot point fix");
                children.Add(child);
            }

            //move pivot point down
            Vector3 originalPosition = selectedTransform.position;
            selectedTransform.position = bounds.center + Vector3.down * bounds.extents.y;

            //move all objects in the transform up
            Vector3 delta = originalPosition - selectedTransform.position;
            foreach (Transform child in children) child.position += delta;
        }
    }
}