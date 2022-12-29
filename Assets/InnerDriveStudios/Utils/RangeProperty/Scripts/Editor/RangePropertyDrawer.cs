using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    [CustomPropertyDrawer(typeof(Range))]
    public class RangePropertyDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(rect, label, property);

            // Draw label and get the rectangle for the remaining space 
            rect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), label);

            // Calculate rects
            Rect minRect = rect;
            Rect maxRect = rect;
            minRect.width = maxRect.width = (rect.width / 2) - 5;
            maxRect.x += rect.width / 2 + 5;

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(minRect, property.FindPropertyRelative("min"), GUIContent.none);
            EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("max"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}