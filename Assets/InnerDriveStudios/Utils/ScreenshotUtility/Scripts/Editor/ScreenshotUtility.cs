using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerDriveStudios.Util
{
    /// <summary>
    /// Provides Unity Editor menu commands for capturing screenshots from the Game View
    /// and from the currently active Scene View.
    /// </summary>
    /// <remarks>
    /// This utility is intended to run only inside the Unity Editor. It creates a
    /// <c>Screenshots</c> folder next to the project's <c>Assets</c> folder and saves
    /// incrementally numbered PNG files using the active scene name and capture type.
    ///
    /// The Game View capture uses <see cref="ScreenCapture.CaptureScreenshot(string, int)"/>,
    /// which captures the visible Game View output, including UI. The Scene View capture
    /// renders the Scene View camera into a temporary render texture, which produces a
    /// cleaner image without editor overlays, handles, gizmos, or toolbar elements.
    /// </remarks>
    public static class ScreenshotUtility
    {
        /// <summary>
        /// Folder name used for screenshot output, relative to the Unity project root.
        /// </summary>
        /// <remarks>
        /// The project root is resolved from <see cref="Application.dataPath"/> by moving
        /// one directory up from the <c>Assets</c> folder. For example:
        /// <c>MyProject/Assets</c> becomes <c>MyProject/Screenshots</c>.
        /// </remarks>
        private const string OutputFolder = "Screenshots";

        /// <summary>
        /// Captures the currently visible Game View and saves it as a PNG file.
        /// </summary>
        /// <remarks>
        /// This menu command is registered under the custom editor menu path defined by
        /// <c>Settings.MENU_PATH</c>. The shortcut suffix <c>%&amp;s</c> maps to
        /// Ctrl+Alt+S on Windows/Linux and Cmd+Option+S on macOS.
        ///
        /// The capture includes the rendered Game View exactly as Unity outputs it,
        /// including canvases, post-processing, camera effects, and any visible UI.
        /// Unity writes the screenshot file asynchronously, so the file may not exist
        /// immediately on the same frame this method is called.
        /// (It actually might take up to several seconds before the screenshot appears).
        /// </remarks>
        [MenuItem(Settings.MENU_PATH + "Screenshot/Capture Game View %&s")]
        public static void CaptureGameView()
        {
            // Application.dataPath points at the project's Assets folder. Its parent is
            // the Unity project root. The null-forgiving operator is used because a valid
            // Unity project Assets path should always have a parent directory.
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string folderPath = Path.Combine(projectRoot, OutputFolder);
            Directory.CreateDirectory(folderPath);

            string sceneName = GetSceneNameSafe();
            string viewTag = "GameView";

            int nextIndex = GetNextIndex(folderPath, sceneName, viewTag);
            string filename = $"{sceneName}_{viewTag}_{nextIndex:0000}.png";
            string fullPath = Path.Combine(folderPath, filename);

            // Captures what is currently visible in the Game View, including UI.
            // The superSize value of 1 keeps the capture at the current Game View size.
            ScreenCapture.CaptureScreenshot(fullPath, superSize: 1);

            Debug.Log($"Saved {viewTag} screenshot to: {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        /// <summary>
        /// Captures the current Scene View camera output without editor overlays and saves it as a PNG file.
        /// </summary>
        /// <remarks>
        /// This menu command is registered under the custom editor menu path defined by
        /// <c>Settings.MENU_PATH</c>. The shortcut suffix <c>%#&amp;s</c> maps to
        /// Ctrl+Shift+Alt+S on Windows/Linux and Cmd+Shift+Option+S on macOS.
        ///
        /// Unlike <see cref="CaptureGameView"/>, this method does not capture the Scene View
        /// tab as displayed on screen. Instead, it renders <see cref="SceneView.lastActiveSceneView"/>'s
        /// camera into an off-screen render texture. This produces a clean scene render and
        /// intentionally excludes editor-only visual elements such as transform handles,
        /// toolbars, selection outlines, and gizmo overlays.
        /// </remarks>
        [MenuItem(Settings.MENU_PATH + "Screenshot/Capture Scene View (Clean) %#&s")]
        public static void CaptureSceneViewClean()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                Debug.LogWarning("No active SceneView found. Click the Scene view tab and try again.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string folderPath = Path.Combine(projectRoot, OutputFolder);
            Directory.CreateDirectory(folderPath);

            string sceneName = GetSceneNameSafe();
            string viewTag = "SceneView";

            int nextIndex = GetNextIndex(folderPath, sceneName, viewTag);
            string filename = $"{sceneName}_{viewTag}_{nextIndex:0000}.png";
            string fullPath = Path.Combine(folderPath, filename);

            // Render the Scene View camera into a texture. This excludes editor overlays,
            // gizmos, handles, and toolbar UI because only the camera's scene render is captured.
            SaveCameraRenderToPng(sceneView.camera, fullPath);

            Debug.Log($"Saved {viewTag} screenshot to: {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        /// <summary>
        /// Renders a camera into a temporary render texture and writes the result to a PNG file.
        /// </summary>
        /// <param name="pCamera">
        /// Camera to render. For Scene View screenshots, this is the active Scene View camera.
        /// </param>
        /// <param name="pFullPath">
        /// Absolute file path where the PNG should be written.
        /// </param>
        /// <remarks>
        /// The method temporarily replaces the camera's <see cref="Camera.targetTexture"/> and
        /// <see cref="RenderTexture.active"/> values, then restores both in a <c>finally</c> block.
        /// This is important because leaving either value changed can affect subsequent editor rendering,
        /// camera previews, or other tools that rely on the active render target.
        ///
        /// The temporary <see cref="RenderTexture"/> and <see cref="Texture2D"/> are destroyed with
        /// <see cref="Object.DestroyImmediate(Object)"/> because this code runs in the editor, outside
        /// normal play-mode object lifetime management.
        /// </remarks>
        private static void SaveCameraRenderToPng(Camera pCamera, string pFullPath)
        {
            Vector2 gameViewSize = GetMainGameViewSize();
            int width = Mathf.Max(1, Mathf.RoundToInt(gameViewSize.x));
            int height = Mathf.Max(1, Mathf.RoundToInt(gameViewSize.y));

            // Preserve global/editor render state so this utility does not leave the editor
            // in a modified rendering configuration after the screenshot is captured.
            var prevRT = RenderTexture.active;
            var prevTarget = pCamera.targetTexture;

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false);

            try
            {
                pCamera.targetTexture = rt;
                RenderTexture.active = rt;

                // Render the scene from the camera into the temporary render texture.
                // This does not include editor gizmos, handles, toolbars, or other Scene View UI.
                pCamera.Render();

                // Copy pixels from the active render texture into a CPU-readable Texture2D.
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(pFullPath, png);
            }
            finally
            {
                pCamera.targetTexture = prevTarget;
                RenderTexture.active = prevRT;

                if (rt != null) Object.DestroyImmediate(rt);
                if (tex != null) Object.DestroyImmediate(tex);
            }
        }

        /// <summary>
        /// Gets a filesystem-safe name for the currently active scene.
        /// </summary>
        /// <returns>
        /// The sanitized active scene name, or <c>Untitled</c> if the active scene is invalid
        /// or does not have a usable name.
        /// </returns>
        /// <remarks>
        /// Scene names are used as part of screenshot filenames, so invalid filename characters
        /// are replaced before the name is returned.
        /// </remarks>
        private static string GetSceneNameSafe()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && !string.IsNullOrWhiteSpace(scene.name))
                return Sanitize(scene.name);

            return "Untitled";
        }

        /// <summary>
        /// Finds the next available numeric suffix for a screenshot filename.
        /// </summary>
        /// <param name="pFolderPath">Directory that contains the screenshots.</param>
        /// <param name="pSceneName">Sanitized scene name used as the first part of the filename.</param>
        /// <param name="pViewTag">Capture source tag, such as <c>GameView</c> or <c>SceneView</c>.</param>
        /// <returns>
        /// One greater than the highest existing suffix matching the same scene and view tag.
        /// Returns <c>1</c> when no matching screenshot files exist.
        /// </returns>
        /// <remarks>
        /// This method scans only the top level of the screenshot folder and matches files using
        /// the format <c>{pSceneName}_{pViewTag}_0001.png</c>. The returned value is formatted by
        /// callers with four digits, for example <c>0001</c>, <c>0002</c>, and so on.
        /// </remarks>
        private static int GetNextIndex(string pFolderPath, string pSceneName, string pViewTag)
        {
            // Match files such as Scene_GameView_0001.png or Scene_SceneView_0001.png.
            string glob = $"{pSceneName}_{pViewTag}_*.png";
            string prefix = $"{pSceneName}_{pViewTag}_";

            int maxIndex = 0;

            foreach (var f in Directory.EnumerateFiles(pFolderPath, glob, SearchOption.TopDirectoryOnly))
            {
                string file = Path.GetFileNameWithoutExtension(f);
                if (!file.StartsWith(prefix)) continue;

                string numberPart = file.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int n))
                    maxIndex = Mathf.Max(maxIndex, n);
            }

            return maxIndex + 1;
        }

        /// <summary>
        /// Replaces characters that are invalid in filenames with underscores.
        /// </summary>
        /// <param name="name">Raw name to sanitize.</param>
        /// <returns>
        /// A filename-safe version of <paramref name="name"/>.
        /// </returns>
        /// <remarks>
        /// This prevents scene names containing characters such as slashes, colons, or other
        /// platform-specific invalid filename characters from producing invalid screenshot paths.
        /// </remarks>
        private static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }

        /// <summary>
        /// Gets the pixel size of Unity's main Game View.
        /// </summary>
        /// <returns>
        /// The main Game View size when Unity's internal editor API is available; otherwise,
        /// a fallback based on <see cref="Screen.width"/> and <see cref="Screen.height"/>.
        /// </returns>
        /// <remarks>
        /// Unity does not expose <c>GameView.GetSizeOfMainGameView</c> as a public API, so this
        /// method uses reflection against the editor assembly. If Unity changes or removes this
        /// internal method, the fallback keeps screenshot creation functional, though the output
        /// size may differ from the visible Game View.
        /// </remarks>
        private static Vector2 GetMainGameViewSize()
        {
            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var method = gameViewType?.GetMethod(
                "GetSizeOfMainGameView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            if (method != null)
            {
                return (Vector2)method.Invoke(null, null);
            }

            // Fallback path for Unity versions where the internal GameView reflection target
            // is unavailable or has changed.
            return new Vector2(Screen.width, Screen.height);
        }
    }
}
