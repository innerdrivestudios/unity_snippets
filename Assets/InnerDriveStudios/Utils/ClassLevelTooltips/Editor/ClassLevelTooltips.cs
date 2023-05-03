using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	/**
	 * This editor overwrites the default editor for all MonoBehaviour types to 
	 * add a tooltip to the script's name based on the already accepted class level tooltip attribute.
	 * 
	 * @author J.C. Wichman
	 */
	[CustomEditor(typeof(MonoBehaviour), true)]
	public class ClassLevelTooltips : Editor
	{
		//the property we want to provide an updated editor for
		private const string scriptPropertyName = "m_Script";
		//to be used with the base method DrawPropertiesExcluding
		private string[] excludedPropertyNames = { scriptPropertyName };
		//cache as much as possible OnEnable
		private SerializedProperty scriptProperty;
		private GUIContent scriptPropertyLabel;

		//this pattern matches either a / or an * (which escaped looks like \*) and any extra spaces after a space.
		//(?<= ) is a positive lookbehind that matches a space character before the space characters we want to replace.
		//The + matches one or more spaces that come after the space character.
		//note the @ this allows us to simply use \ without having to do \\
		private string regexPattern1 = @"(\r?\n|/\*|\*/|\*)";
		private string regexPattern2 = @"((?<= ) +)";

		//private string regexPattern = @"(/|\*|\r?\n|(?<= ) +)";

		private void OnEnable()
		{
			//does the given target have a script property?
			scriptProperty = serializedObject.FindProperty(scriptPropertyName);
			if (scriptProperty == null) return;

			//if so, does it have a tooltip attribute?
			TooltipAttribute tooltipAttribute = GetClassLevelTooltip(target, true);
			if (tooltipAttribute == null) return;

			//if so, get its value and run it through our Regex
			string	tooltip = Regex.Replace(tooltipAttribute.tooltip, regexPattern1, "");
					tooltip = Regex.Replace(tooltip, regexPattern2, "").Trim();

			//now create a new script header with tooltip
			scriptPropertyLabel = new GUIContent("[?] " + scriptProperty.displayName, tooltip);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			//the updated/changed field
			GUI.enabled = false;
			EditorGUILayout.PropertyField(scriptProperty, scriptPropertyLabel);
			GUI.enabled = true;

			//everything BUT the updated/changed field
			DrawPropertiesExcluding(serializedObject, excludedPropertyNames);

			serializedObject.ApplyModifiedProperties();
		}

		private TooltipAttribute GetClassLevelTooltip(Object pObject, bool pForSubclassToo)
		{
			//use a bunch of reflection statements in a row to get the contents of the TooltipAttribute, if any
			object[] attributes = pObject.GetType().GetTypeInfo().GetCustomAttributes(typeof(TooltipAttribute), pForSubclassToo);
			return attributes.Length > 0 ? attributes[0] as TooltipAttribute : null;
		}
	}
}