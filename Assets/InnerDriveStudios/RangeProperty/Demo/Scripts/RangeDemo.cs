using InnerDriveStudios.Util;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public class RangeDemo : MonoBehaviour
	{
		public Range range;

		private void Awake()
		{
			Debug.Log("The range is from:" + range.min + " to " + range.max);
		}
	}
}
