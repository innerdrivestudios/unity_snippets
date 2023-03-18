using UnityEngine;

namespace InnerDriveStudios.Util
{
	public static class MaterialExtensions
	{
		public static void SetColor(this Material pMaterial, Color pColor)
		{
			pMaterial.SetVector("_Color", pColor);
		}

		public static void SetEmissiveColor(this Material pMaterial, Color pColor)
		{
			pMaterial.SetVector("_EmissionColor", pColor);
		}

	}
}
