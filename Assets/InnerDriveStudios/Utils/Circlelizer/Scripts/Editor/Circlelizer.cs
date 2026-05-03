using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnerDriveStudios.Util
{
    /// <summary>
    /// EditorWindow that arranges the currently selected scene objects in a circle or spiral.
    ///
    /// The tool uses an explicit preview session:
    /// - Activate captures the selected transforms and their original state.
    /// - While active, UI and SceneView handles update the captured transforms as a live preview.
    /// - Apply commits the previewed transforms through Unity's Undo system.
    /// - Revert, selection changes, or closing/discarding the window restores the captured original state.
    ///
    /// Settings are saved whenever the session deactivates, regardless of whether the preview was applied or reverted.
    /// </summary>
    public class Circlelizer : EditorWindow
    {
        /// STATIC FIELDS & METHODS

        // This ID is used to load & save all the settings for this dialog upon activating & deactivating

        private static readonly string saveDataIdentifier = MethodBase.GetCurrentMethod().DeclaringType.Name;

        // Method to actually open the Circlelizer window

        [MenuItem(Settings.MENU_PATH + "Circlelizer")]
        private static void Init()
        {
            Circlelizer window = GetWindow<Circlelizer>("Circlelizer");
            window.minSize = new Vector2(350, 200);
            window.Show();
        }

        /// ADDITIONAL DATA TYPES

        /// <summary>
        /// Describes a default facing with a label and a direction (ie normal)
        /// </summary>
        private readonly struct Facing
        {
            public readonly string label;
            public readonly Vector3 normal;

            public Facing(string pLabel, Vector3 pNormal)
            {
                label = pLabel;
                normal = pNormal;
            }
        }

        /// <summary>
        /// A set of default facings
        /// </summary>
        private static readonly Facing[] facings = {
                    new ("-X", Vector3.left),
                    new ("X", Vector3.right),
                    new ("-Y", Vector3.down),
                    new ("Y", Vector3.up),
                    new ("-Z", Vector3.back),
                    new ("Z", Vector3.forward)
                };

        /// <summary>
        /// Captured transform state for one object at the start of the current preview session.
        /// </summary>
        private readonly struct TransformState
        {
            public readonly Vector3 position;
            public readonly Quaternion rotation;

            public TransformState(Transform pTransform)
            {
                position = pTransform.position;
                rotation = pTransform.rotation;
            }

            public void ResetPositionAndRotation(Transform pTransform)
            {
                pTransform.position = position;
                pTransform.rotation = rotation;
            }

            public void ResetRotation(Transform pTransform)
            {
                pTransform.rotation = rotation;
            }
        }

        // Serialized preview settings. These are persisted to EditorPrefs when the tool deactivates.

        [SerializeField] private Vector3 centerPoint = Vector3.zero;
        [SerializeField] private float radius = 5f;
        [SerializeField] private float startRotation = 0f;
        [SerializeField] private bool spiral = false;
        [SerializeField] private float rotations = 1f;
        [SerializeField] private bool lookAtCenter = false;
        [SerializeField] private Quaternion facing = Quaternion.identity;

        // Returned by GetSelectionStatus. Controls whether the tool can activate.
        private enum SelectionStatus { None, Invalid, NeedMoreThanOne, Ok }
        private SelectionStatus currentSelectionStatus;

        // True only while a preview session is active.
        private bool isActivated = false;

        // Preserve Unity's global Tools.hidden value so the tool does not unhide tools that were already hidden.
        private bool previousToolsHidden = false;

        // Original transform state captured at activation. Used to revert previews and create a clean Undo record on apply.
        private readonly Dictionary<Transform, TransformState> originalStates = new();

        // Stable snapshot of the activated selection. Do not rely on live Selection while previewing.
        private readonly List<Transform> cachedTransforms = new List<Transform>();

        /// <summary>
        /// When the selection status changes we need to update our UI so we:
        /// - store the current selection status
        /// - refresh the UI
        /// - make sure we revert and deactivate if a "session" was in progress
        /// </summary>
        private void OnSelectionChange()
        {
            currentSelectionStatus = GetSelectionStatus();
            Repaint();

            if (isActivated) RevertAllChanges();
        }

        /// <summary>
        /// Validates the current Unity selection for use by the tool.
        /// Only scene objects are supported.
        /// </summary>
        private SelectionStatus GetSelectionStatus()
        {
            int count = Selection.gameObjects.Length;

            // No objects selected...
            if (count == 0)
            {
                return SelectionStatus.None;
            }

            // Objects other than gameobjects selected ...
            if (Selection.objects.Length != count)
            {
                return SelectionStatus.Invalid;
            }

            // Objects selected that are not scene objects (eg prefabs)...
            for (int i = 0; i < count; i++)
            {
                if (!Selection.gameObjects[i].scene.IsValid())
                {
                    return SelectionStatus.Invalid;
                }
            }

            return (count == 1) ? SelectionStatus.NeedMoreThanOne : SelectionStatus.Ok;
        }

        private void OnGUI()
        {
            if (!isActivated)   RenderNonActivatedUI();
            else                RenderActivatedUI();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!isActivated) return;

            EditorGUI.BeginChangeCheck();

            // Show the circle facing and a rotationhandle to update the facing if needed
            facing = Handles.RotationHandle(facing, centerPoint);
            facing.Normalize();

            // Show/override the standard position handle for the selection
            centerPoint = Handles.PositionHandle(
                centerPoint,
                Tools.pivotRotation == PivotRotation.Local ? facing : Quaternion.identity
            );

            Vector3 up = facing * Vector3.up;
            Vector3 right = facing * Vector3.right;
            Vector3 forward = facing * Vector3.forward;

            Handles.color = new Color(0, 1, 0, 0.1f);

            // Draw a disc to show our facing direction and where we consider the rotation to start
            Handles.DrawSolidArc(centerPoint, forward, right, 359.6f, radius);

            Handles.color = new Color(0, 1, 0);
            Vector3 currentPos = centerPoint + radius * right;
            Vector3 newPos = Handles.Slider(
                currentPos,
                right,
                HandleUtility.GetHandleSize(currentPos) * 0.2f,
                Handles.SphereHandleCap,
                0f
            );

            if (EditorGUI.EndChangeCheck())
            {
                // Update the radius by projecting the new position onto the right vector
                radius = Vector3.Dot(newPos - centerPoint, right);

                // Force the editor view to update which will call OnGUI.
                Repaint();

                UpdateAllTransforms();
            }
        }

        /// <summary>
        /// Applies the current preview settings to the captured activation snapshot.
        /// </summary>
        private void UpdateAllTransforms()
        {
            int count = cachedTransforms.Count;
            if (count == 0) return;

            float angleStep = rotations * 2 * Mathf.PI / count;
            Vector3 right = facing * Vector3.right;
            Vector3 up = facing * Vector3.up;
            Vector3 forward = facing * Vector3.forward;

            for (int i = 0; i < count; i++)
            {
                Transform t = cachedTransforms[i];
                if (t == null) continue;

                float angle = i * angleStep + Mathf.Deg2Rad * startRotation;
                float radius = spiral ? (i * this.radius / count) : this.radius;
                // Apply the 2D circle formula in the chosen facing plane.
                t.position = centerPoint + Mathf.Cos(angle) * right * radius + Mathf.Sin(angle) * up * radius;

                if (lookAtCenter)
                {
                    // Use the facing plane normal as the LookAt up vector.
                    t.LookAt(centerPoint, forward);
                }
            }
        }

        /////////////////////////// NON ACTIVATED UI

        private void RenderNonActivatedUI()
        {
            ShowInfoForCurrentSelectionStatus();

            bool oldEnabled = GUI.enabled;
            GUI.enabled = currentSelectionStatus == SelectionStatus.Ok;

            if (GUILayout.Button("Activate"))
            {
                SelectionStatus selectionStatus = GetSelectionStatus();

                if (selectionStatus == SelectionStatus.Ok) Activate();
            }

            GUI.enabled = oldEnabled;
        }

        private void ShowInfoForCurrentSelectionStatus()
        {
            switch (currentSelectionStatus)
            {
                case SelectionStatus.None:
                    EditorGUILayout.HelpBox(
                        "Select some objects in the scene to get started!",
                        MessageType.Info
                    );
                    break;

                case SelectionStatus.Invalid:
                    EditorGUILayout.HelpBox(
                        "Circlelizer only works on scene objects, make sure you don't accidentally " +
                        "have assets selected in the ProjectWindow as well.",
                        MessageType.Warning);
                    break;

                case SelectionStatus.NeedMoreThanOne:
                    EditorGUILayout.HelpBox(
                        "Select more than one object.",
                        MessageType.Warning);
                    break;

                case SelectionStatus.Ok:
                    EditorGUILayout.HelpBox(
                        "Press 'Activate' to get started!",
                        MessageType.Info
                    );
                    break;
            }
        }

        /////////////////////////// ACTIVATED UI

        private void RenderActivatedUI()
        {
            bool editorUIChangesMadeThisFrame = ShowSettingsUI();
            
            EditorGUILayout.Space();

            ShowApplyRevertOptions();

            if (editorUIChangesMadeThisFrame)
            {
                // Force the scene view to update which will trigger SceneView.duringSceneGui 
                // which will call OnSceneGUI. In other words: Editor UI changes trigger a Scene UI update.
                SceneView.RepaintAll();

                UpdateAllTransforms();
            }
        }

        private bool ShowSettingsUI()
        {
            bool prev = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            // Only make the UI editable when the selection status is Ok
            EditorGUI.BeginDisabledGroup(currentSelectionStatus != SelectionStatus.Ok);

            // Track changes in the UI
            EditorGUI.BeginChangeCheck();

            // Vector3 Position field
            centerPoint = EditorGUILayout.Vector3Field("Position", centerPoint);

            // Float start rotation field
            startRotation = EditorGUILayout.FloatField("Start rotation", startRotation);
            radius = EditorGUILayout.FloatField("Radius", radius);

            bool lookAtCenterOld = lookAtCenter;

            lookAtCenter = EditorGUILayout.Toggle("Look at center?", lookAtCenter);

            if (lookAtCenterOld && !lookAtCenter) RevertRotationChanges();

            // Reset facing button menu
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Reset facing:");
            foreach (Facing presetFacing in facings)
            {
                if (GUILayout.Button(presetFacing.label))
                {
                    facing = Quaternion.FromToRotation(Vector3.forward, presetFacing.normal);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Left over settings...
            spiral = EditorGUILayout.Toggle("Spiral?", spiral);
            rotations = EditorGUILayout.FloatField("Rotations?", rotations);

            // Track if there were changes made this frame and overall
            bool changesMadeThisFrame = EditorGUI.EndChangeCheck();

            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.wideMode = prev;

            return changesMadeThisFrame;
        }

        private void ShowApplyRevertOptions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply"))
            {
                ApplyAllChanges();
            }

            if (GUILayout.Button("Revert"))
            {
                RevertAllChanges();
            }

            EditorGUILayout.EndHorizontal();
        }

        /////////////////////////// ACTIVATION & DEACTIVATION CODE

        private void Activate()
        {
            RecordCurrentSelectionTransformStates();
            LoadDialogSettings();
            isActivated = true;
            previousToolsHidden = Tools.hidden;
            Tools.hidden = true;

            centerPoint = GetStartingCenterPosition();

            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            // Create the initial preview immediately, then mark the window dirty so closing prompts Apply/Discard.
            UpdateAllTransforms();
            UpdateWindowDirtyState();
            Repaint();
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Ends the active preview session. Safe to call multiple times.
        /// </summary>
        private void Deactivate()
        {
            if (!isActivated) return;

            SaveDialogSettings();
            hasUnsavedChanges = false;
            isActivated = false;
            Tools.hidden = previousToolsHidden;

            SceneView.duringSceneGui -= OnSceneGUI;

            Repaint();
            SceneView.RepaintAll();
        }

        /////////////////////////// LOAD & SAVE

        private void LoadDialogSettings()
        {
            string data = EditorPrefs.GetString(saveDataIdentifier, JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);
        }

        private void SaveDialogSettings()
        {
            string data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(saveDataIdentifier, data);
        }

        /////////////////////////// WINDOW CLOSE HANDLING

        /// <summary>
        /// Synchronizes the EditorWindow unsaved-changes prompt with the preview session state.
        /// </summary>
        private void UpdateWindowDirtyState()
        {
            hasUnsavedChanges = true;
            saveChangesMessage = "You have unapplied Circlelizer changes. Apply to keep the circular layout, or discard to restore the original transforms.";
        }

        public override void SaveChanges()
        {
            ApplyAllChanges();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            RevertAllChanges();
            base.DiscardChanges();
        }

        /////////////////////////// APPLY & REVERT HANDLING

        /// <summary>
        /// Commits the current preview state and creates a single Undo operation.
        /// </summary>
        private void ApplyAllChanges()
        {
            // Deleted objects can leave null Transform references in the captured state, so filter them out first.
            Transform[] validTransforms = originalStates.Where(x => x.Key != null).Select(x => x.Key).ToArray();

            if (validTransforms.Length > 0)
            {
                // Store the previewed state, reset to the original state, then record Undo from original -> preview.
                Dictionary<Transform, TransformState> currentState = new();
                foreach (Transform validTransform in validTransforms)
                {
                    currentState[validTransform] = new TransformState(validTransform);
                    originalStates[validTransform].ResetPositionAndRotation(validTransform);
                }

                // Start a new undo group
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Apply Circlelize Transform Changes");

                Undo.RecordObjects(validTransforms, "Apply Transform Changes");

                // Mark every involved scene dirty once.
                HashSet<Scene> dirtyScenes = new();

                foreach (Transform validTransform in validTransforms)
                {
                    currentState[validTransform].ResetPositionAndRotation(validTransform);

                    Scene scene = validTransform.gameObject.scene;
                    if (scene.IsValid() && scene.isLoaded) dirtyScenes.Add(scene);
                }

                foreach (Scene scene in dirtyScenes) EditorSceneManager.MarkSceneDirty(scene);

                Undo.CollapseUndoOperations(undoGroup);
            }

            Deactivate();
        }

        /// <summary>
        /// Restores the full captured transform state and ends the current preview session.
        /// </summary>
        private void RevertAllChanges()
        {
            foreach (var kv in originalStates)
            {
                if (kv.Key == null) continue;
                kv.Value.ResetPositionAndRotation(kv.Key);
            }

            Deactivate();
        }

        /// <summary>
        /// Restores only rotations while keeping the position preview active.
        /// Used when disabling Look At Center during an active session.
        /// </summary>
        private void RevertRotationChanges()
        {
            foreach (var kv in originalStates)
            {
                if (kv.Key == null) continue;
                kv.Value.ResetRotation(kv.Key);
            }

            // Do not deactivate; this is an in-session adjustment for the Look At Center toggle.
        }

        /////////////////////////// UTILITY METHODS

        private Vector3 GetStartingCenterPosition()
        {
            if (cachedTransforms.Count == 0) return Vector3.zero;

            Vector3 totalPositions = Vector3.zero;

            foreach (Transform selection in cachedTransforms)
            {
                totalPositions += selection.position;
            }

            return totalPositions / cachedTransforms.Count;
        }

        private void RecordCurrentSelectionTransformStates()
        {
            originalStates.Clear();
            cachedTransforms.Clear();

            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null) continue;

                originalStates[go.transform] = new TransformState(go.transform);
                cachedTransforms.Add(go.transform);
            }
        }

        private void OnEnable()
        {
            OnSelectionChange();
        }

        private void OnDisable()
        {
            if (isActivated && hasUnsavedChanges)
            {
                // Closing/discarding an active preview restores the captured transforms.
                RevertAllChanges();
            }
            else
            {
                // No preview changes are pending; just tear down subscriptions/state.
                Deactivate();
            }
        }

    }
}

