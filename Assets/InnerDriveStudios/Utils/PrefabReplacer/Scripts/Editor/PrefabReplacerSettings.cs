using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	/**
	 * Settings for the replacer window.
	 */
	public class PrefabReplacerSettings : ScriptableObject
	{
		[Tooltip("Each selected object will be replaced with a random prefab from this list")]
		public GameObject[] replacers;
		[Tooltip("Only replace selected object that have this given script attached to it, leave empty to replace all selected objects")]
		public MonoScript hasToBeOfType;        

		[Header("General Settings")]
		public bool keepCurrentLocalScale = true;

		[Header("Rotation Settings")]
		public bool keepCurrentLocalRotation = true;
		public float randomAngleToAdd = 360;
		public float angleSnap = 90;
		public Vector3 rotationAxis = Vector3.up;
	}
}