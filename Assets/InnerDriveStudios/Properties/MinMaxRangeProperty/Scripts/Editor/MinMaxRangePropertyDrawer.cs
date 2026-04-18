using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    /// <summary>
    /// Custom PropertyDrawer for the <see cref="MinMaxRange"/> struct.
    ///
    /// Renders the range as two side-by-side fields ("Min" and "Max") in a single row.
    ///
    /// Layout:
    /// [Main Label] [Min Field] [Max Field]
    ///
    /// Implementation details:
    /// - Uses EditorGUI.PropertyField to preserve Unity's default field behavior
    ///   (including context menus, prefab overrides, and multi-object editing).
    /// - Temporarily overrides EditorGUIUtility.labelWidth to ensure labels fit
    ///   within the constrained half-width rects.
    /// - Restores labelWidth afterward to avoid affecting other inspector fields.
    /// </summary>
    [CustomPropertyDrawer(typeof(MinMaxRange))]
    public class MinMaxRangePropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draws the MinMaxRange property in the inspector.
        /// </summary>
        /// <param name="rect">The total rect allocated for this property.</param>
        /// <param name="property">The serialized MinMaxRange property.</param>
        /// <param name="label">The label shown in the inspector.</param>
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            // Ensures prefab override logic and context menus apply to the full property
            EditorGUI.BeginProperty(rect, label, property);

            // Draw the main label and retrieve remaining space for custom layout
            rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

            // Layout configuration
            float spacing = 8f;                         // Space between Min and Max fields
            float halfWidth = (rect.width - spacing) * 0.5f;

            // Define rects for Min and Max fields
            Rect minRect = new Rect(rect.x, rect.y, halfWidth, rect.height);
            Rect maxRect = new Rect(rect.x + halfWidth + spacing, rect.y, halfWidth, rect.height);

            // Access child properties
            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            // Store current label width so we can restore it after drawing
            float oldLabelWidth = EditorGUIUtility.labelWidth;

            try
            {
                // Reduce label width so "Min"/"Max" fit inside compact rects
                EditorGUIUtility.labelWidth = 30f;

                // Draw the fields with labels (enables drag-scrubbing on labels)
                EditorGUI.PropertyField(minRect, minProp, new GUIContent("Min"));
                EditorGUI.PropertyField(maxRect, maxProp, new GUIContent("Max"));
            }
            finally
            {
                // Always restore label width to avoid side effects on other fields
                EditorGUIUtility.labelWidth = oldLabelWidth;
            }

            // End property wrapper
            EditorGUI.EndProperty();
        }
    }
}