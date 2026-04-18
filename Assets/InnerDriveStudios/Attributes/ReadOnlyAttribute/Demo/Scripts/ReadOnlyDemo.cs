using UnityEngine;

namespace InnerDriveStudios.Util
{
    public class ReadOnlyDemo : MonoBehaviour
    {
        [ReadOnly] [SerializeField] private float time;

		private void Update()
		{
			time = Time.time;
		}
	}
}
