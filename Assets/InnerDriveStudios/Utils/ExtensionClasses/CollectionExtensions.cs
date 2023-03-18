using System.Collections.Generic;
using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class CollectionExtensions
	{
		public static T GetRandomElement<T>(this List<T> pList)
		{
			return pList[Random.Range(0, pList.Count)];
		}

		public static T GetRandomElement<T>(this T[] pArray)
		{
			return pArray[Random.Range(0, pArray.Length)];
		}
	}
}
