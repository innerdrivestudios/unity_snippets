using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace InnerDriveStudios.Util
{
	public class StagedScreenshotUtility : MonoBehaviour
	{
		[Tooltip("Path to screenshot folder, relative to project, starts with Assets. Easiest to find by just selecting a path and pressing Alt+Ctrl+C")]
		[SerializeField] private string subdirectory;
		[Tooltip("Where to move the object before screenshotting? View this as the chair to sit in before getting your photo taken ;)")]
		[SerializeField] private Transform anchor;

		[SerializeField] private bool copyAnchorScale = false;
		[SerializeField] private bool copyAnchorRotation = false;
		[SerializeField] private bool useGameViewSize = false;

		[SerializeField] private int width = 256;
		[SerializeField] private int height = 256;

		public static StagedScreenshotUtility Instance { get; private set; }

		public bool isScreenshotProcessing { get; private set; } = false;
		public string subPath { get; private set; }

		private IEnumerator Start()
		{
			//enable our texture processing importer
			isScreenshotProcessing = true;
			Instance = this;

			//gather objects to process
			List<GameObject> objectsToScreenShot = new List<GameObject>();
			foreach (Transform child in transform)
			{
				if (child.gameObject.activeSelf)
				{
					objectsToScreenShot.Add(child.gameObject);
					child.gameObject.SetActive(false);
				}
			}

			//We use a separate camera so we can still see what is happening in the game view
			GameObject screenshotCameraGO = new GameObject("Temporary Screenshot Camera");
			Camera screenshotCamera = screenshotCameraGO.AddComponent<Camera>();
			screenshotCamera.CopyFrom(Camera.main);

			//Capture each object
			foreach (GameObject go in objectsToScreenShot)
			{
				//path relative to project
				subPath = subdirectory + "/" + go.name + ".png";
				string path = Application.dataPath.Replace("/Assets", "/") + subPath;
				Debug.Log("Screenshotting " + path);
				go.transform.position = anchor.position;
				if (copyAnchorRotation) go.transform.rotation = anchor.rotation;
				if (copyAnchorScale) go.transform.localScale = anchor.localScale;
				go.SetActive(true);

				//transparent capture, turns out this is easiest to do using just an additional camera + rendertexture
				RenderTexture renderTexture = new RenderTexture(useGameViewSize?Screen.width:width, useGameViewSize?Screen.height:height, 32, RenderTextureFormat.ARGB32);
				screenshotCamera.targetTexture = renderTexture;
				yield return new WaitForEndOfFrame();
				//Instead of: ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);

				//Now convert the render texture to a png we can save
				Texture2D texture = toTexture2D(renderTexture);
				File.WriteAllBytes(path, texture.EncodeToPNG());

				yield return new WaitForSeconds(1);
				go.SetActive(false);
				AssetDatabase.ImportAsset(subdirectory + "/" + go.name + ".png");
			}
			yield return new WaitForSeconds(1);
			EditorApplication.ExitPlaymode();
			isScreenshotProcessing = false;
		}

		Texture2D toTexture2D(RenderTexture pRenderTexture)
		{
			Texture2D texture = new Texture2D(pRenderTexture.width, pRenderTexture.height, TextureFormat.RGBA32, false);
			// ReadPixels looks at the active RenderTexture.
			RenderTexture.active = pRenderTexture;
			texture.ReadPixels(new Rect(0, 0, pRenderTexture.width, pRenderTexture.height), 0, 0);
			texture.Apply();
			//texture = FlipTexture(texture);
			return texture;
		}

	}

	class ScreenshotProcessor: AssetPostprocessor
	{
		void OnPreprocessTexture()
		{
			if (StagedScreenshotUtility.Instance != null && StagedScreenshotUtility.Instance.isScreenshotProcessing && assetPath == StagedScreenshotUtility.Instance.subPath)
			{
				Debug.Log("Importing:" + assetPath);
				TextureImporter textureImporter = (TextureImporter)assetImporter;
				textureImporter.textureType = TextureImporterType.Sprite;
				textureImporter.alphaIsTransparency = true;
				textureImporter.mipmapEnabled = false;
				textureImporter.npotScale = TextureImporterNPOTScale.None;
			}
		}
	}
}

#endif

/*
Texture2D FlipTexture(Texture2D original, bool upSideDown = true)
{

	Texture2D flipped = new Texture2D(original.width, original.height);

	int xN = original.width;
	int yN = original.height;


	for (int i = 0; i < xN; i++)
	{
		for (int j = 0; j < yN; j++)
		{
			flipped.SetPixel(i, yN - j - 1, original.GetPixel(i, j));
		}
	}
	flipped.Apply();

	return flipped;
}
*/