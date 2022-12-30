using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	/**
	 * Settings for the replacer window.
	 */
	public class PrefabReplacerSettings : ScriptableObject
	{
		public GameObject[] replacers;          //objects to pick from while replacing (should be prefabs)
		public MonoScript hasToBeOfType;        //only replace selected object that have this given script attached to it

		[Header("Rotation settings")]
		public bool keepCurrentLocalScale = true;
		public bool keepCurrentLocalRotation = true;
		[Space(20)]
		public float randomAngleToAdd = 360;
		public float angleSnap = 90;
		public Vector3 rotationAxis = Vector3.up;
	}
}