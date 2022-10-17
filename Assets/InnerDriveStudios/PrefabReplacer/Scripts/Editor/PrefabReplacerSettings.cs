using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.PrefabReplacer
{

	/**
	 * Settings for the replacer window.
	 */
	public class PrefabReplacerSettings : ScriptableObject
	{
		public GameObject[] replacers;          //objects to pick from while replacing (should be prefabs)
		public MonoScript hasToBeOfType;        //only replace selected object that have this given script attached to it
	}
}