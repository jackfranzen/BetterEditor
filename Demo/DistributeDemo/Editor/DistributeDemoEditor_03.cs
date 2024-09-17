using System;
using System.Collections.Generic;
using System.Linq;
using BetterEditor;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DistributeDemoComponent03))]
    public class DistributeDemoEditor_03 : Editor
    {
        
        // -- Used for nameof()
        private static DistributeDemoComponent _demoComponent;
        
        // -- Serialized Properties from the target component
        private SerializedProperty enablePreviewProp;
        private SerializedProperty previewColorProp;
        private SerializedProperty previewColorUseProp;
        private SerializedProperty previewColorColorProp;
        
        private SerializedProperty seedProp;
        private SerializedProperty radiusProp;
        
        private SerializedProperty totalToGenerateProp;
        private SerializedProperty overrideObjectColorProps;
        private SerializedProperty overrideObjectColorUseProp;
        private SerializedProperty overrideObjectColorColorProp;
        
        private SerializedProperty objectPrefabsProp;
        
        // -- We'll store modifications on the actual components now that we're experts at serialized properties
        //      (This will also keep our hasModifications up to date with our data when we use Undo/Redo, see other comments)
        private SerializedProperty hasModificationsProp;
        
        private SerializedProperty createdObjectsProp;

        // -- Trackers
        private bool prev_enablePreview = false;
        private bool prevMulti_enablePreview = false;
        private bool prev_previewColorUse = false;
        private bool prevMulti_previewColorUse = false;
        private Color prev_previewColor = Color.cyan;
        private bool prevMulti_previewColor = false;
        
        private int prev_seed = 0;
        private bool prevMulti_seed = false;
        private float prev_radius = 1f;
        private bool prevMulti_radius = false;
        
        private int prev_totalToGen = 0;
        private bool prevMulti_totalToGen = false;
        private bool prev_colorDataUse = false;
        private bool prevMulti_colorDataUse = false;
        private Color prev_colorDataColor = Color.cyan;
        private bool prevMulti_colorDataColor = false;

        private object[] prev_objectPrefabs;
        private bool prevMulti_objectPrefabs = false;
        
        
        // -- Tracks whether our artificial foldout is expanded
        private bool foldoutExpanded = false;

        // -- Define GUIContent
        private GUIContent SeedsContent;
        private GUIContent ExampleFoldoutContent;
        public void OnEnable()
        {
            // -- Build GUI Content
            SeedsContent = new GUIContent("Seeds!!", "Give me seeds!! (Custom content override example)");
            ExampleFoldoutContent = new GUIContent("Objects to Distribute", "This foldout is an example, a regular foldout will not support copy/paste :(");
            
            // -- Preview Props
            enablePreviewProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.enablePreview));
            previewColorProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.previewColor));
            previewColorUseProp = previewColorProp.FindRelativeChecked(nameof(_demoComponent.previewColor.use)); // -- Relative
            previewColorColorProp = previewColorProp.FindRelativeChecked(nameof(_demoComponent.previewColor.color)); // -- Relative
            
            // -- Distribution Props
            seedProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.seed));
            totalToGenerateProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.totalToGenerate));
            radiusProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.radius));
            
            // -- Object Props
            overrideObjectColorProps = serializedObject.FindPropertyChecked(nameof(_demoComponent.objectColor));
            overrideObjectColorUseProp = overrideObjectColorProps.FindRelativeChecked(nameof(_demoComponent.objectColor.use)); // -- Relative
            overrideObjectColorColorProp = overrideObjectColorProps.FindRelativeChecked(nameof(_demoComponent.objectColor.color)); // -- Relative
            objectPrefabsProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.objectPrefabs));
            
            // -- Find the protected hasModifications property by name, using BetterEditor's FindPropertyChecked for safety
            //          (It's a good idea to always use this method in place of FindProperty)
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
            
            createdObjectsProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.createdObjects));
            
            // -- Track!
            RefreshTracking();
        }

        private void RefreshTracking()
        {
            // -- Track all properties which can update individually 
            //        - A boolValue can remain in its original true or false state, even when toggled, 
            //             in situations where hasMultipleDifferentValues is the only thing being updated.
            //        - Therefore, hasMultipleDifferentValues must be tracked.
            prev_enablePreview = enablePreviewProp.boolValue;
            prevMulti_enablePreview = enablePreviewProp.hasMultipleDifferentValues;
            prev_previewColorUse = previewColorUseProp.boolValue;
            prevMulti_previewColorUse = previewColorUseProp.hasMultipleDifferentValues;
            prev_previewColor = previewColorColorProp.colorValue;
            prevMulti_previewColor = previewColorColorProp.hasMultipleDifferentValues;
            
            prev_seed = seedProp.intValue;
            prevMulti_seed = seedProp.hasMultipleDifferentValues;
            prev_radius = radiusProp.floatValue;
            prevMulti_radius = radiusProp.hasMultipleDifferentValues;
            
            prev_totalToGen = totalToGenerateProp.intValue;
            prevMulti_totalToGen = totalToGenerateProp.hasMultipleDifferentValues;
            prev_colorDataUse = overrideObjectColorUseProp.boolValue;
            prevMulti_colorDataUse = overrideObjectColorUseProp.hasMultipleDifferentValues;
            prev_colorDataColor = overrideObjectColorColorProp.colorValue;
            prevMulti_colorDataColor = overrideObjectColorColorProp.hasMultipleDifferentValues;
            
            prev_objectPrefabs = new object[objectPrefabsProp.arraySize];
            for (int i = 0; i < objectPrefabsProp.arraySize; i++)
                prev_objectPrefabs[i] = objectPrefabsProp.GetArrayElementAtIndex(i).BetterObjectValue();
            prevMulti_objectPrefabs = objectPrefabsProp.hasMultipleDifferentValues;
        }
        

        // -- Unity->OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = DistributeDemoEditorCommon.DrawDemoInfo(StageInfo);
            if(updatedStage) return;
            
            // -- Update Serialized Object, as always
            serializedObject.Update();
            
            // -- Draw the modifications Row using the serialized property for hasModifications (after serializedObject.Update())
            var pressedApply = DistributeDemoEditorCommon.DrawApplyRowSerialized(hasModificationsProp);
            if (pressedApply)
            {
                // -- Do the actual logic to apply the changes
                DistributeDemoEditorCommon.Distribute(targets);
                
                // -- Because the above method silently modifies the "createdObjects" property, we need to update our serializedObject again,
                //      to ensure that the changes from other sources are respected and not overwritten or distorted by a follow-up apply
                //          (When this line is removed, you will see "createdObjects" be destroyed when pressing Apply)
                serializedObject.Update();
                
                // -- Set hasModifications to false, and silently apply it.
                //      (in this demo: hasModification updates are not part of the undo chain, undo will not revert them)
                hasModificationsProp.boolValue = false;
                serializedObject.ApplyModifiedPropertiesWithoutUndo(); 
            }
            
            // -- DRAW THE MAIN UI
            //      (Using all of our fancy new properties)
            DrawMainUI();
            
            // -- Clamp property limits using BetterEditor's Enforce methods
            totalToGenerateProp.EnforceClamp(4, 100);
            seedProp.EnforceMinimum(0);
            
            // -- Check for all Updates!
            CheckForUpdates();
            
            // -- (Regular Flow) Apply all changes made to the serialized Object's properties (since Update()) back to our target components
            //          (If we already applied changes from the block above then this will do nothing and that's okay)
            serializedObject.ApplyModifiedProperties();
        }


        private void DrawMainUI()
        {
            // -- Primary Props
            EditorGUILayout.LabelField("Primary Props", EditorStyles.boldLabel);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.HelpBox(DistributeDemoEditorCommon.GizmosInfo, MessageType.Info);
            EditorGUILayout.PropertyField(enablePreviewProp);
            
            // -- Alternate to GUI.enabled from Unity
            using (new EditorGUI.DisabledScope(enablePreviewProp.AllFalse()))
                EditorGUILayout.PropertyField(previewColorProp, true);
            EditorGUI.indentLevel -= 1;
            
            EditorGUILayout.LabelField("Distribution Props:", EditorStyles.boldLabel);
            // -- Alternate to EditorGUI.IndentLevel += 1, from Unity
            using( new EditorGUI.IndentLevelScope() )
            {
                EditorGUILayout.PropertyField(seedProp, SeedsContent);
                EditorGUILayout.PropertyField(totalToGenerateProp);
                EditorGUILayout.PropertyField(radiusProp);
            }
            
            foldoutExpanded = EditorGUILayout.Foldout(foldoutExpanded, ExampleFoldoutContent, true, EditorStyles.foldout);
            if (foldoutExpanded)
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(overrideObjectColorProps, true);
                    EditorGUILayout.PropertyField(objectPrefabsProp, true);
                }
            
            // -- draw our list of created objects
            using( new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(createdObjectsProp); 
        }


        private void CheckForUpdates()
        {
             var updated_previewEnabled = false;
            updated_previewEnabled |= prev_enablePreview != enablePreviewProp.boolValue;
            updated_previewEnabled |= prevMulti_enablePreview != enablePreviewProp.hasMultipleDifferentValues;
            
            var updated_preview = updated_previewEnabled;
            updated_preview |= prev_previewColorUse != previewColorUseProp.boolValue;
            updated_preview |= prevMulti_previewColorUse != previewColorUseProp.hasMultipleDifferentValues;
            updated_preview |= prev_previewColor != previewColorColorProp.colorValue;
            updated_preview |= prevMulti_previewColor != previewColorColorProp.hasMultipleDifferentValues;
            if(updated_preview)
                Debug.Log($"Better Editor Demo: EnablePreview updated to {enablePreviewProp.AnyTrue()} ");
                
            // -- Updated Distribution Props?
            var updated_distribution = false;
            updated_distribution |= prev_seed != seedProp.intValue;
            updated_distribution |= prevMulti_seed != seedProp.hasMultipleDifferentValues;
            updated_distribution |= prev_totalToGen != totalToGenerateProp.intValue;
            updated_distribution |= prevMulti_totalToGen != totalToGenerateProp.hasMultipleDifferentValues;
            updated_distribution |= (Mathf.Approximately(prev_radius, radiusProp.floatValue) == false);
            updated_distribution |= prevMulti_radius != radiusProp.hasMultipleDifferentValues;
            
            // -- Updated Object Props?
            var updated_objects = false;
            updated_objects |= prev_colorDataUse != overrideObjectColorUseProp.boolValue;
            updated_objects |= prevMulti_colorDataUse != overrideObjectColorUseProp.hasMultipleDifferentValues;
            updated_objects |= prev_colorDataColor != overrideObjectColorColorProp.colorValue;
            updated_objects |= prevMulti_colorDataColor != overrideObjectColorColorProp.hasMultipleDifferentValues;
            
            // -- Check the list against the previous list
            //         (You can do any sort of array comparison, but fundamentally this is what BetterTrackers use internally, when we get to them)
            var currentValues = new object[objectPrefabsProp.arraySize];
            for (int i = 0; i < objectPrefabsProp.arraySize; i++)
                currentValues[i] = objectPrefabsProp.GetArrayElementAtIndex(i).BetterObjectValue();
            updated_objects |= (prev_objectPrefabs.SequenceEqual(currentValues) == false);
            updated_objects |= prevMulti_objectPrefabs != objectPrefabsProp.hasMultipleDifferentValues;
            
            
            // -- Were Modifications made to important Properties?
            var modified_Important = updated_distribution || updated_objects;
            
            // -- Was Anything Updated?
            var updated_Any = updated_preview || updated_distribution || updated_objects;

            // -- Handle Enable-Preview Updated
            if (updated_previewEnabled)
                HandlePreviewUpdated();
            
            // -- Track that we have modifications
            if (modified_Important)
                HandleDetectModifications();
            
            // -- Refresh Tracking.
            //      - We've had a chance to respond to updates, now we update them to current
            //      - Calls to WasUpdated will now return false, until further updates are made
            //      - We can do this before or after applying, no biggie. 
            if (updated_Any)
                RefreshTracking();
        }
        
        private void HandlePreviewUpdated()
        {
            // -- Why use AnyTrue() here?
            //       - Even though user updates always collapse hasMultipleDifferentValues to false,
            //              updates from undo could set it back to true.
            //       - In later demos, we'll be able to have different reactions to undo vs user changes.  
            Debug.Log($"Better Editor Demo: EnablePreview updated to {enablePreviewProp.AnyTrue()} ");
            
            // -- Expand or close the preview Color struct automatically when updating previewEnabled
            previewColorProp.isExpanded = enablePreviewProp.AnyTrue();
            
            // -- Example: Set Color.use to false when preview is disabled
            if (enablePreviewProp.AllFalse())
                previewColorUseProp.boolValue = false;
        }
        
        
        private void HandleDetectModifications()
        {
            Debug.Log($"Better Editor Demo: Detected Modifications!");
            
            //  In This Demo: hasModification updates are not part of the undo chain, undo will not revert them to false.
            //      To accomplish this we apply any GUI updates first, regularly, then we push another silent update
            serializedObject.ApplyModifiedProperties();
            hasModificationsProp.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        
        
        // -- Info about this demo
        private static readonly DistributeDemo_StageInfo StageInfo = new()
        {
            stage = EDistributeDemoStages.FullSerialized,
            title = "Drawing and Tracking each Property",
            description = "Draws the component by gathering all properties which we want to draw and drawing a fully customized UI.\n\n" +
                          "Checks for changes by storing and comparing previous values against current values, without BetterEditor trackers.\n\n"+
                          "Demonstrates how to correctly track changes to groups of serialized properties across all operations. \n\n",
            greenTexts = new List<string>()
            {
                "Fully customized UI with foldouts, labels, and custom rules",
                "Properly track updates to individual properties",
                "Handle Undo, Revert, Reset, and paste correctly!",
                "Vanilla Unity code"
            },
            redTexts = new List<string>()
            {
                "Verbose, improved in next step",
                "Large code-changes required when updating component data structure",
                "No logging",
            },
            fileName = "DistributeDemoEditor_03.cs",
        };
    }
}