using UnityEngine;

namespace InnerDriveStudios.Util
{
	public class MinMaxRangeDemo : MonoBehaviour
	{
		public MinMaxRange range;

		private void Awake()
		{
			Debug.Log("The range is from:" + range.min + " to " + range.max);
		}
	}
}
