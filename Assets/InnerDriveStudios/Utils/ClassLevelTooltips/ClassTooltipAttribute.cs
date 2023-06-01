using System;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	//
	// Summary:
	//     Specify a tooltip for a field in the Inspector window.
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class ClassTooltipAttribute : PropertyAttribute
	{
		//
		// Summary:
		//     The tooltip text.
		public readonly string tooltip;

		//
		// Summary:
		//     Specify a tooltip for a field.
		//
		// Parameters:
		//   tooltip:
		//     The tooltip text.
		public ClassTooltipAttribute(string pTooltip)
		{
			tooltip = pTooltip;
		}
	}
}