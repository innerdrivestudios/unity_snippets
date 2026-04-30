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
    /// Editor tool that arranges the current scene selection in a circular layout with live preview.
    /// </summary>
    public class Circlelizer : EditorWindow
    {
        // Used to store all the settings for this dialog
        
        private static readonly string saveDataIdentifier = MethodBase.GetCurrentMethod().DeclaringType.Name;

        // All the settings we can change for the UI/Selection

        [SerializeField] private Vector3 centerPoint = Vector3.zero;
        [SerializeField] private float radius = 5f;
        [SerializeField] private float startRotation = 0f;
        [SerializeField] private bool spiral = false;
        [SerializeField] private float rotations = 1f;
        [SerializeField] private bool lookAtCenter = false;
        [SerializeField] private Quaternion facing = Quaternion.identity;

        // SelectionStatus is updated by calling UpdateSelectionStatus on selectionChanged
        private enum SelectionStatus { None, Invalid, Ok }
        private SelectionStatus selectionStatus = SelectionStatus.None;
        private bool activated = false;

        // Define possible default circle facings (eg the direction the circle normal should point)
        private readonly struct Facing
        {
            public readonly string label;
            public readonly Vector3 normal;

            public Facing (string pLabel, Vector3 pNormal)
            {
                label = pLabel;
                normal = pNormal;
            }
        }

        private static readonly Facing[] facings = {
                    new ("-X", Vector3.left),
                    new ("X", Vector3.right),
                    new ("-Y", Vector3.down), 
                    new ("Y", Vector3.up),
                    new ("-Z", Vector3.back), 
                    new ("Z", Vector3.forward)
                };

        // Whenever the selection changes, we need to apply/revert any changes, 
        // and store the memento's for the selected transforms...

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

            public void ResetPositionAndRotation (Transform pTransform)
            {
                pTransform.position = position;
                pTransform.rotation = rotation;
            }

            public void ResetRotation(Transform pTransform)
            {
                pTransform.rotation = rotation;
            }
        }

        // Original transform state captured whenever the window opens or the selection changes.
        private readonly Dictionary<Transform, TransformState> originalStates = new();

        // True while the current preview differs from the snapshotted baseline.
        private bool selectionTransformsHaveChanged = false;

        // Guards against recursive session refreshes while applying or cancelling. Is this needed?
        // private bool isApplyingOrCancelling = false;

        [MenuItem(Settings.MENU_PATH + "Circlelizer")]
        private static void Init()
        {
            Circlelizer window = GetWindow<Circlelizer>("Circlelizer");
            window.minSize = new Vector2(350, 200);
            window.Show();
        }

        private void OnEnable()
        {
            Tools.hidden = true;

            activated = false;

            LoadDialogSettings();

            SceneView.duringSceneGui += OnSceneGUI;

            // Register for selection changes, and force a selectionchange update...
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Tools.hidden = false;

            SaveDialogSettings();

            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

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

        private void OnSelectionChanged ()
        {
            if (selectionTransformsHaveChanged) HandlePreviousSelectionChanges();

            UpdateSelectionStatus();

            if (selectionStatus == SelectionStatus.Ok) RecordCurrentSelectionTransformStates();

            Repaint();
        }

        private void HandlePreviousSelectionChanges()
        {
            if (!selectionTransformsHaveChanged) return;
           
            bool applyChanges = EditorUtility.DisplayDialog(
                    "Apply / Revert",
                    "Do you wish to Apply or Revert your modified transforms?",
                    "Apply",
                    "Revert"
                );

            if (applyChanges)   ApplyAllChanges();
            else                RevertAllChanges();
        }

        private void ApplyAllChanges()
        {
            // find all not deleted transforms first... 
            Transform[] validTransforms = originalStates.Where (x => x.Key != null).Select (x => x.Key).ToArray();

            // record their current state and reset them so we can record the change in an undo object
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
            HashSet<Scene> dirtyScenes = new ();

            foreach (Transform validTransform in validTransforms)
            {
                //Reset 
                currentState[validTransform].ResetPositionAndRotation(validTransform);
                
                //Get scene
                Scene scene = validTransform.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded) dirtyScenes.Add(scene);
            }

            foreach (Scene scene in dirtyScenes) EditorSceneManager.MarkSceneDirty(scene);

            Undo.CollapseUndoOperations(undoGroup);

            SaveDialogSettings();

            selectionTransformsHaveChanged = false;
        }

        private void RevertAllChanges()
        {
            foreach (var kv in originalStates)
            {
                kv.Value.ResetPositionAndRotation(kv.Key);
            }

            LoadDialogSettings();

            selectionTransformsHaveChanged = false;
            hasUnsavedChanges = false;
        }

        /// <summary>
        /// Validates the current Unity selection for use by the tool.
        /// Only scene objects are supported.
        /// </summary>
        private void UpdateSelectionStatus()
        {
            int count = Selection.gameObjects.Length;

            // No objects selected...
            if (count == 0)
            {
                selectionStatus = SelectionStatus.None;
                return;
            }

            // Objects other than gameobjects selected ...
            if (Selection.objects.Length != count)
            {
                selectionStatus = SelectionStatus.Invalid;
                return;
            }

            // Objects selected that are not scene objects (eg prefabs)...
            for (int i = 0; i < count; i++)
            {
                if (!Selection.gameObjects[i].scene.IsValid())
                {
                    selectionStatus = SelectionStatus.Invalid;
                    return;
                }
            }

            selectionStatus = SelectionStatus.Ok;
        }

        private void RecordCurrentSelectionTransformStates ()
        {
            originalStates.Clear();

            foreach (GameObject go in Selection.gameObjects)
            {
                originalStates[go.transform] = new TransformState(go.transform);
            }
        }

        private void OnGUI()
        {
            activated = GUILayout.Toggle(activated, activated ? "Deactivate" : "Activate", GUI.skin.button);

            bool active = false;
            GUI.enabled = activated;


            bool editorUIChangesMadeThisFrame = ShowSettingsUI();
            selectionTransformsHaveChanged |= editorUIChangesMadeThisFrame;

            EditorGUILayout.Space();

            ShowApplyRevertOptions();

            ShowSelectionStatusHelpBox();

            if (editorUIChangesMadeThisFrame)
            {
                // Force the scene view to update which will trigger SceneView.duringSceneGui 
                // which will call OnSceneGUI. In other words: Editor UI changes trigger a Scene UI update.
                SceneView.RepaintAll();

                // Mark the window state as changed (show asterisk and save message)
                UpdateWindowDirtyState();
            }

            GUI.enabled = active;
        }

        private bool ShowSettingsUI()
        {
            bool prev = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            // Only make the UI editable when the selection status is Ok
            EditorGUI.BeginDisabledGroup(selectionStatus != SelectionStatus.Ok);

                // Track changes in the UI
                EditorGUI.BeginChangeCheck();

                    // Vector3 Position field
                    centerPoint = EditorGUILayout.Vector3Field("Position", centerPoint);

                    // Float start rotation field
                    startRotation = EditorGUILayout.FloatField("Start rotation", startRotation);
                    radius = EditorGUILayout.FloatField("Radius", radius);
                    lookAtCenter = EditorGUILayout.Toggle("Look at center?", lookAtCenter);

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
            using (new EditorGUI.DisabledScope(!selectionTransformsHaveChanged))
            {
                if (GUILayout.Button("Apply"))
                {
                    ApplyAllChanges();
                }

                if (GUILayout.Button("Cancel"))
                {
                    RevertAllChanges();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowSelectionStatusHelpBox()
        {
            switch (selectionStatus)
            {
                case SelectionStatus.None:
                    EditorGUILayout.HelpBox("Select some objects in the scene to get started!", MessageType.Info);
                    break;
                case SelectionStatus.Invalid:
                    EditorGUILayout.HelpBox("Circlelizer only works on scene objects!", MessageType.Warning);
                    break;
                case SelectionStatus.Ok:
                    //    if (!hasPreviewChanges)
                    //    {
                    //        EditorGUILayout.HelpBox("The current selection is previewed immediately. Use Apply to commit or Cancel to restore the snapshotted transforms.", MessageType.None);
                    //    }
                    break;
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!activated) return;

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
            
            if (selectionStatus == SelectionStatus.None) Handles.color = new Color(1, 0.8f, 0, 0.1f);
            if (selectionStatus == SelectionStatus.Invalid) Handles.color = new Color(1, 0, 0, 0.1f);
            
            // Draw a disc to show our facing direction and where we consider the rotation to start
            Handles.DrawSolidArc (centerPoint, forward, right, 359.6f, radius);

            Handles.color = new Color(0, 1, 0);
            Vector3 currentPos = centerPoint + radius * right;
            Vector3 newPos = Handles.Slider (
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

                // Mark the window state as changed (show asterisk and save message when we close)
                UpdateWindowDirtyState();
            }
        }

        /// <summary>
        /// Synchronizes the EditorWindow unsaved-changes prompt with the preview session state.
        /// </summary>
        private void UpdateWindowDirtyState()
        {
            if (hasUnsavedChanges) return;

            hasUnsavedChanges = selectionTransformsHaveChanged;
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

    }
}


//TODO:

/// Workflow:
/// - When the window opens, the current selection is snapshotted immediately.
/// - When the selection changes, the previous preview is restored, the new selection is snapshotted,
///   and the preview is applied immediately with the current settings.
/// - Any parameter or handle change updates the same preview live.
/// - Apply commits the current preview as a single explicit Undo step.
/// - Cancel restores the selection to the snapshotted transform state.
/// - Closing the window with unapplied preview changes triggers Unity's unsaved-changes prompt.
/// 
/// Notes:
/// - Preview changes are intentionally non-destructive and are not recorded in Undo.
/// - Undo is recorded only when Apply is pressed, and all affected objects are grouped into one
///   deterministic undo operation.
