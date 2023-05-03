using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	/**
	 * This editor overwrites the default editor for all ScriptableObjects types to 
	 * add a tooltip to the script's name based on the already accepted class level tooltip attribute.
	 * 
	 * This specific editor just extends the ClassLevelTooltips since I didn't know another way to accomplish the same thing,
	 * except by doing typeof(Object) which feels like an accident waiting to happen ;).
	 */
	[CustomEditor(typeof(ScriptableObject), true)]
	public class ScriptableObjectClassLevelTooltips : ClassLevelTooltips	{}
}