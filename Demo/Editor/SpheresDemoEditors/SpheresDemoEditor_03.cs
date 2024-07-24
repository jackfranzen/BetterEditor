using System;
using System.Collections.Generic;
using BetterEditor;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpheresDemo_03))]
    public class SpheresDemoEditor_03 : Editor
    {
        
        // -- Used for nameof()
        private static SpheresDemo DEMO;
        
        // -- Serialized Properties from the target component
        private SerializedProperty enablePreviewProp;
        private SerializedProperty seedProp;
        private SerializedProperty numSpheresProp;
        private SerializedProperty colorDataProp;
        private SerializedProperty colorDataUseProp;
        private SerializedProperty colorDataColorProp;
        private SerializedProperty radiusProp;

        // -- Tracked data for the properties
        bool prev_enablePreview = false;
        bool prevMulti_enablePreview = false;
        int prev_seed = 0;
        bool prevMulti_seed = false;
        int prev_numSpheres = 0;
        bool prevMulti_numSpheres = false;
        bool prev_colorDataUse = false;
        bool prevMulti_colorDataUse = false;
        Color prev_colorDataColor = Color.cyan;
        bool prevMulti_colorDataColor = false;
        float prev_radius = 1f;
        bool prevMulti_radius = false;
        
        // -- We'll store modifications on the actual components now that we're experts at serialized properties
        //      (This will also keep our hasModifications up to date with our data when we use Undo/Redo, see other comments)
        private SerializedProperty hasModificationsProp;
        
        // -- Tracks whether our artificial foldout is expanded
        private bool foldoutExpanded = false;

        // -- Define GUIContent
        private GUIContent SeedsContent;
        private GUIContent OtherPreviewPropsContent;
        public void OnEnable()
        {
            // -- Build GUI Content
            SeedsContent = new GUIContent("Seeds!!!", "Give me seeds!!");
            OtherPreviewPropsContent = new GUIContent("Other Preview Props", "These are other preview props");
            
            // -- 3 Basic Props
            enablePreviewProp = serializedObject.FindProperty(nameof(DEMO.enablePreview));
            seedProp = serializedObject.FindProperty(nameof(DEMO.seed));
            numSpheresProp = serializedObject.FindProperty(nameof(DEMO.numSpheres));
            radiusProp = serializedObject.FindProperty(nameof(DEMO.radius));
            
            // -- Find the protected hasModifications property by name, using BetterEditor's FindPropertyChecked for safety
            //          (It's a good idea to always use this method in place of FindProperty)
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
            
            // -- Get the class, then get the next two properties relative to it
            colorDataProp = serializedObject.FindProperty(nameof(DEMO.colorData));
            colorDataUseProp = colorDataProp.FindPropertyRelative(nameof(DEMO.colorData.use));
            colorDataColorProp = colorDataProp.FindPropertyRelative(nameof(DEMO.colorData.Color));
            
            // -- Track!
            TrackFromCurrentValues();
        }

        private void TrackFromCurrentValues()
        {
            // -- Track all properties which can update individually 
            //        - A boolValue can remain in its original true or false state, even when toggled, 
            //             in situations where hasMultipleDifferentValues is the only thing being updated.
            //        - Therefore, hasMultipleDifferentValues must be tracked.
            prev_enablePreview = enablePreviewProp.boolValue;
            prevMulti_enablePreview = enablePreviewProp.hasMultipleDifferentValues;
            prev_seed = seedProp.intValue;
            prevMulti_seed = seedProp.hasMultipleDifferentValues;
            prev_numSpheres = numSpheresProp.intValue;
            prevMulti_numSpheres = numSpheresProp.hasMultipleDifferentValues;
            prev_colorDataUse = colorDataUseProp.boolValue;
            prevMulti_colorDataUse = colorDataUseProp.hasMultipleDifferentValues;
            prev_colorDataColor = colorDataColorProp.colorValue;
            prevMulti_colorDataColor = colorDataColorProp.hasMultipleDifferentValues;
            prev_radius = radiusProp.floatValue;
            prevMulti_radius = radiusProp.hasMultipleDifferentValues;
        }
        

        // -- Unity->OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcher(Info);
            if(updatedStage) return;
            
            // -- Update Serialized Object, as always
            serializedObject.Update();
            
            // -- Draw the modifications Row using the serialized property for hasModifications (after serializedObject.Update())
            var pressedApply = SpheresDemoEditors.DrawModifyWarningRowSerialized(hasModificationsProp);
            if (pressedApply)
            {
                hasModificationsProp.boolValue = false;
                serializedObject.ApplyModifiedPropertiesWithoutUndo(); 
            }
            
            
            // -- DRAW THE MAIN UI
            {
                // -- Draw 3 Fields
                EditorGUILayout.LabelField("Preview Props:", EditorStyles.boldLabel);
                EditorGUI.indentLevel += 1;
                
                EditorGUILayout.PropertyField(enablePreviewProp);
                
                // -- Color Data (disabled if no preview)
                GUI.enabled = enablePreviewProp.AnyTrue();
                EditorGUILayout.PropertyField(colorDataProp, true);
                GUI.enabled = true;

                // -- Draw Other Preview Props in a foldout (with examples for changing content and tooltips)
                if (enablePreviewProp.AnyTrue())
                {
                    foldoutExpanded = EditorGUILayout.Foldout(foldoutExpanded, OtherPreviewPropsContent, true, EditorStyles.foldout);
                    if (foldoutExpanded)
                    {
                        EditorGUI.indentLevel += 1;
                        EditorGUILayout.PropertyField(seedProp, SeedsContent);
                        EditorGUILayout.PropertyField(numSpheresProp);
                        EditorGUI.indentLevel -= 1;
                    }
                }
                
                EditorGUI.indentLevel -= 1;
            }
            
            // -- Clamp the number of spheres using BetterEditor's Enforce methods
            numSpheresProp.EnforceClamp(4, 100);
            seedProp.EnforceMinimum(0);
            //  - Do NOT use .intValue = Mathf.Clamp()!!!
            //          - It would cause the value to immediately collapse to a single value when selecting multiple
            //            components with mixed Values! 
            
            
            // -- Updated Preview Enabled?
            var updated_previewEnabled = false;
            updated_previewEnabled |= prev_enablePreview != enablePreviewProp.boolValue;
            updated_previewEnabled |= prevMulti_enablePreview != enablePreviewProp.hasMultipleDifferentValues;
            
            // -- Updated Color Data?
            var updated_colorData = false;
            updated_colorData |= prev_colorDataUse != colorDataUseProp.boolValue;
            updated_colorData |= prevMulti_colorDataUse != colorDataUseProp.hasMultipleDifferentValues;
            updated_colorData |= prev_colorDataColor != colorDataColorProp.colorValue;
            updated_colorData |= prevMulti_colorDataColor != colorDataColorProp.hasMultipleDifferentValues;
            
            // -- Updated Other Props?
            var updated_other = false;
            updated_other |= prev_seed != seedProp.intValue;
            updated_other |= prevMulti_seed != seedProp.hasMultipleDifferentValues;
            updated_other |= prev_numSpheres != numSpheresProp.intValue;
            updated_other |= prevMulti_numSpheres != numSpheresProp.hasMultipleDifferentValues;
            updated_other |= prev_radius != radiusProp.floatValue;
            

            
            // -- Respond to specific changes
            if (updated_previewEnabled)
            {
                // -- Expand or close foldouts (including the foldout powered by the serialized property)
                foldoutExpanded = enablePreviewProp.AnyTrue();
                colorDataProp.isExpanded = enablePreviewProp.AnyTrue();
                
                Debug.Log($"Better Editor Demo: EnablePreview updated to {enablePreviewProp.AnyTrue()} ");
                
                // -- Example: Set Seed to 0 when preview is disabled
                if (!enablePreviewProp.AnyTrue())
                    seedProp.intValue = 0;
            }
            
            // -- Respond to other changes in data
            if(updated_colorData)
                Debug.Log("Better Editor Demo: Color Data Changed");
            if(updated_other)
                Debug.Log("Better Editor Demo: Other Props Changed");
            
            // -- Check if anything was Updated
            var updated_Any = updated_previewEnabled || updated_colorData || updated_other;
            if (updated_Any)
            {
                
                // -- Reset Trackers to current state, to detect changes from this point on
                //      (Can re-track at any time, relative to applying changes)
                TrackFromCurrentValues();
                
                // -- Apply any GUI and all serializedProperty value changes back to our target components
                serializedObject.ApplyModifiedProperties();

                // -- Track that we have modifications
                //      (in this demo: hasModification updates are not part of the undo chain, undo will not revert to false)
                hasModificationsProp.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            
            // -- Backup, Apply any GUI changes to properties that might not have been tracked
            //     (Note, it's okay to do this twice, it won't do anything if there aren't changes)
            serializedObject.ApplyModifiedProperties();
        }
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.FullSerialized,
            title = "Custom Editor & Tracking each Property",
            description = "Draws the component by gathering all properties which we want to draw and drawing them in a completely custom ruleset.\n\n" +
                          "Checks for changes by storing and comparing previous values against current values, without BetterEditor trackers.\n\n"+
                          "Tracking HasModifications directly on the serializedObject, it won't be forgotten\n\n",
            greenTexts = new List<string>()
            {
                "Vanilla Unity code",
                "UI structure is clear; Easier to build complicated UI",
                "Reacts to updates from all sources",
                "Undo/Redo keeps HasModifications in sync (using a trick)"
            },
            redTexts = new List<string>()
            {
                "Verbose, improved in next step",
                "Large code-changes required when updating component data structure",
                "No logging",
            },
            fileName = "SpheresDemoEditor_03.cs",
        };
    }
}