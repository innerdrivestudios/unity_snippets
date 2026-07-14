using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    /// <summary>
    /// Unity Editor window that visualizes mesh data for the currently selected GameObject
    /// directly in the Scene view.
    /// </summary>
    /// <remarks>
    /// Supports meshes from both <see cref="MeshFilter"/> and <see cref="SkinnedMeshRenderer"/>.
    /// The window can draw vertices, triangle wireframes, normals, and UV coordinate labels.
    /// </remarks>
    public class MeshVisualizerWindow : EditorWindow
    {
        // Controls which mesh features are drawn in the Scene view.
        static bool showVertices = true;
        static bool showTriangles = true;
        static bool showNormals = false;
        static bool showUVs = false;

        // Controls how normal arrows are rendered.
        static bool normalDepthFade = true;
        static bool normalBackfaceCull = false;

        // Draws every Nth normal to reduce clutter on dense meshes.
        static int normalStep = 1;

        // Scene-view scale multipliers for visualization geometry.
        static float vertexSize = 0.06f;
        static float normalLength = 0.25f;
        static float normalLineThickness = 0.015f;
        static float normalHeadLength = 0.08f;
        static float normalHeadWidthMultiplier = 3f;

        // Default visualization colors.
        static Color vertexColor = Color.yellow;
        static Color wireColor = Color.green;
        static Color normalColor = Color.cyan;

        /// <summary>
        /// Opens the Mesh Visualizer editor window from the configured Unity menu path.
        /// </summary>
        [MenuItem(Settings.IDS_UTIL_PATH + "Mesh Visualizer")]
        public static void Open()
        {
            GetWindow<MeshVisualizerWindow>("Mesh Visualizer");
        }

        /// <summary>
        /// Subscribes this window to Scene view drawing when the editor window is enabled.
        /// </summary>
        void OnEnable()
        {
            SceneView.duringSceneGui += DrawScene;
        }

        /// <summary>
        /// Unsubscribes this window from Scene view drawing when the editor window is disabled.
        /// </summary>
        void OnDisable()
        {
            SceneView.duringSceneGui -= DrawScene;
        }

        /// <summary>
        /// Draws the Mesh Visualizer editor UI.
        /// </summary>
        /// <remarks>
        /// The UI shows basic mesh statistics for the selected object and exposes controls for
        /// enabling, disabling, coloring, and scaling each visualization type.
        /// </remarks>
        void OnGUI()
        {
            GameObject go = Selection.activeGameObject;
            Mesh mesh = GetMesh(go);

            EditorGUILayout.LabelField("Selected Object:", go ? go.name : "None");

            if (!mesh)
            {
                EditorGUILayout.HelpBox("Select a GameObject with a MeshFilter or SkinnedMeshRenderer.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Vertices:", mesh.vertexCount.ToString());
            EditorGUILayout.LabelField("Triangles:", (mesh.triangles.Length / 3).ToString());
            EditorGUILayout.LabelField("UVs:", $"UV0 ({mesh.uv.Length}), UV1 ({mesh.uv2.Length})");
            EditorGUILayout.LabelField("Vertex Colors:", mesh.colors != null && mesh.colors.Length > 0 ? "Yes" : "No");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Visualization Options", EditorStyles.boldLabel);

            showVertices = EditorGUILayout.Toggle("Show Vertices", showVertices);
            showTriangles = EditorGUILayout.Toggle("Show Triangles", showTriangles);
            showNormals = EditorGUILayout.Toggle("Show Normals", showNormals);
            showUVs = EditorGUILayout.Toggle("Show UVs", showUVs);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Vertex Settings", EditorStyles.boldLabel);

            vertexSize = EditorGUILayout.Slider("Vertex Size", vertexSize, 0.005f, 0.25f);
            vertexColor = EditorGUILayout.ColorField("Vertex Color", vertexColor);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Triangle Settings", EditorStyles.boldLabel);

            wireColor = EditorGUILayout.ColorField("Wireframe Color", wireColor);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Normal Settings", EditorStyles.boldLabel);

            normalColor = EditorGUILayout.ColorField("Normal Color", normalColor);
            normalLength = EditorGUILayout.Slider("Normal Length", normalLength, 0.05f, 1.5f);
            normalLineThickness = EditorGUILayout.Slider("Shaft Thickness", normalLineThickness, 0.002f, 0.08f);
            normalHeadLength = EditorGUILayout.Slider("Head Length", normalHeadLength, 0.01f, 0.35f);
            normalHeadWidthMultiplier = EditorGUILayout.Slider("Head Width Multiplier", normalHeadWidthMultiplier, 1f, 8f);

            EditorGUILayout.Space(5);
            normalStep = EditorGUILayout.IntSlider("Draw Every Nth Normal", normalStep, 1, 50);
            normalDepthFade = EditorGUILayout.Toggle("Depth Fade", normalDepthFade);
            normalBackfaceCull = EditorGUILayout.Toggle("Backface Cull Normals", normalBackfaceCull);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Reset All Visualizations"))
            {
                showVertices = true;
                showTriangles = true;
                showNormals = false;
                showUVs = false;

                normalDepthFade = true;
                normalBackfaceCull = false;
                normalStep = 1;

                vertexSize = 0.06f;
                normalLength = 0.25f;
                normalLineThickness = 0.015f;
                normalHeadLength = 0.08f;
                normalHeadWidthMultiplier = 3f;

                vertexColor = Color.yellow;
                wireColor = Color.green;
                normalColor = Color.cyan;
            }

            // Refresh all Scene views so setting changes are reflected immediately.
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Gets the mesh associated with a GameObject.
        /// </summary>
        /// <param name="go">The selected GameObject to inspect.</param>
        /// <returns>
        /// The shared mesh from a <see cref="MeshFilter"/> or <see cref="SkinnedMeshRenderer"/>,
        /// or <c>null</c> if the object has no supported mesh component.
        /// </returns>
        static Mesh GetMesh(GameObject go)
        {
            if (!go) return null;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) return mf.sharedMesh;

            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr && smr.sharedMesh) return smr.sharedMesh;

            return null;
        }

        /// <summary>
        /// Draws the active mesh visualization overlays in the Scene view.
        /// </summary>
        /// <param name="sceneView">The Scene view currently being rendered.</param>
        /// <remarks>
        /// Mesh vertices are transformed from local space into world space before drawing.
        /// Triangle edges are rendered as wireframe lines, vertices as handle spheres, normals as
        /// custom arrows, and UVs as labels positioned at each vertex.
        /// </remarks>
        static void DrawScene(SceneView sceneView)
        {
            GameObject go = Selection.activeGameObject;
            Mesh mesh = GetMesh(go);

            if (!go || !mesh) return;

            Transform t = go.transform;
            Camera camera = sceneView.camera;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            // Respect Scene view depth so hidden geometry is not drawn over foreground objects.
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            if (showTriangles)
            {
                Handles.color = wireColor;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = t.TransformPoint(vertices[triangles[i]]);
                    Vector3 b = t.TransformPoint(vertices[triangles[i + 1]]);
                    Vector3 c = t.TransformPoint(vertices[triangles[i + 2]]);

                    Handles.DrawLine(a, b);
                    Handles.DrawLine(b, c);
                    Handles.DrawLine(c, a);
                }
            }

            if (showVertices)
            {
                Handles.color = vertexColor;

                foreach (Vector3 v in vertices)
                {
                    Vector3 world = t.TransformPoint(v);
                    float size = HandleUtility.GetHandleSize(world) * vertexSize;

                    Handles.SphereHandleCap(
                        0,
                        world,
                        Quaternion.identity,
                        size,
                        EventType.Repaint
                    );
                }
            }

            if (showNormals && normals != null && normals.Length == vertices.Length)
            {
                DrawNormalArrows(t, vertices, normals, camera);
            }

            if (showUVs && mesh.uv != null && mesh.uv.Length == vertices.Length)
            {
                Handles.color = Color.magenta;

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 world = t.TransformPoint(vertices[i]);
                    Vector2 uv = mesh.uv[i];

                    Handles.Label(world, $"UV {uv.x:F2}, {uv.y:F2}");
                }
            }
        }

        /// <summary>
        /// Draws arrow-shaped normal indicators for the selected mesh.
        /// </summary>
        /// <param name="transform">The selected object's transform, used to convert mesh data to world space.</param>
        /// <param name="vertices">The mesh vertices in local space.</param>
        /// <param name="normals">The mesh normals in local space.</param>
        /// <param name="camera">The active Scene view camera.</param>
        /// <remarks>
        /// Normal arrows are scaled with <see cref="HandleUtility.GetHandleSize(Vector3)"/> so they
        /// remain visually consistent at different Scene view zoom levels.
        /// </remarks>
        static void DrawNormalArrows(
            Transform transform,
            Vector3[] vertices,
            Vector3[] normals,
            Camera camera
        )
        {
            if (!camera) return;

            Vector3 camForward = camera.transform.forward;

            for (int i = 0; i < vertices.Length; i += Mathf.Max(1, normalStep))
            {
                Vector3 world = transform.TransformPoint(vertices[i]);
                Vector3 normal = transform.TransformDirection(normals[i]).normalized;

                float facing = Vector3.Dot(normal, -camForward);

                if (normalBackfaceCull && facing <= 0f)
                    continue;

                float alpha = normalColor.a;

                if (normalDepthFade)
                    alpha *= Mathf.Lerp(0.2f, 1f, Mathf.Clamp01((facing + 1f) * 0.5f));

                Color c = normalColor;
                c.a = alpha;
                Handles.color = c;

                float scale = HandleUtility.GetHandleSize(world);

                float shaftThickness = scale * normalLineThickness;
                float arrowLength = scale * normalLength;
                float headLength = scale * normalHeadLength;
                float headWidth = shaftThickness * normalHeadWidthMultiplier;

                Vector3 end = world + normal * arrowLength;
                Vector3 headBase = end - normal * headLength;

                // Build a camera-facing basis so the arrow thickness remains visible from the Scene camera.
                Vector3 right = Vector3.Cross(normal, camForward);

                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(normal, camera.transform.up);

                right.Normalize();

                Vector3 up = Vector3.Cross(right, normal).normalized;

                DrawThickShaft(world, headBase, right, up, shaftThickness);
                DrawArrowHead(end, headBase, right, up, headWidth);
            }
        }

        /// <summary>
        /// Draws a four-sided anti-aliased shaft between two points.
        /// </summary>
        /// <param name="start">The world-space start position of the shaft.</param>
        /// <param name="end">The world-space end position of the shaft.</param>
        /// <param name="right">The right vector of the shaft's local drawing basis.</param>
        /// <param name="up">The up vector of the shaft's local drawing basis.</param>
        /// <param name="radius">The shaft radius in Scene view units.</param>
        static void DrawThickShaft(
            Vector3 start,
            Vector3 end,
            Vector3 right,
            Vector3 up,
            float radius
        )
        {
            Vector3 r = right * radius;
            Vector3 u = up * radius;

            Handles.DrawAAConvexPolygon(start + r, start + u, end + u, end + r);
            Handles.DrawAAConvexPolygon(start + u, start - r, end - r, end + u);
            Handles.DrawAAConvexPolygon(start - r, start - u, end - u, end - r);
            Handles.DrawAAConvexPolygon(start - u, start + r, end + r, end - u);
        }

        /// <summary>
        /// Draws a four-sided pyramid arrowhead.
        /// </summary>
        /// <param name="tip">The world-space arrow tip.</param>
        /// <param name="baseCenter">The world-space center of the arrowhead base.</param>
        /// <param name="right">The right vector of the arrowhead's local drawing basis.</param>
        /// <param name="up">The up vector of the arrowhead's local drawing basis.</param>
        /// <param name="width">The half-width of the arrowhead base.</param>
        static void DrawArrowHead(
            Vector3 tip,
            Vector3 baseCenter,
            Vector3 right,
            Vector3 up,
            float width
        )
        {
            Vector3 p1 = baseCenter + right * width;
            Vector3 p2 = baseCenter + up * width;
            Vector3 p3 = baseCenter - right * width;
            Vector3 p4 = baseCenter - up * width;

            Handles.DrawAAConvexPolygon(tip, p1, p2);
            Handles.DrawAAConvexPolygon(tip, p2, p3);
            Handles.DrawAAConvexPolygon(tip, p3, p4);
            Handles.DrawAAConvexPolygon(tip, p4, p1);

            Handles.DrawAAConvexPolygon(p1, p4, p3, p2);
        }
    }
}
