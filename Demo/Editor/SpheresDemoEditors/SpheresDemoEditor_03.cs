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
        private SerializedProperty previewColorProp;
        private SerializedProperty previewColorUseProp;
        private SerializedProperty previewColorColorProp;
        
        private SerializedProperty seedProp;
        private SerializedProperty radiusProp;
        
        private SerializedProperty numSpheresProp;
        private SerializedProperty sphereColorProp;
        private SerializedProperty sphereColorUseProp;
        private SerializedProperty sphereColorColorProp;
        
        // -- We'll store modifications on the actual components now that we're experts at serialized properties
        //      (This will also keep our hasModifications up to date with our data when we use Undo/Redo, see other comments)
        private SerializedProperty hasModificationsProp;

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
        
        private int prev_numSpheres = 0;
        private bool prevMulti_numSpheres = false;
        private bool prev_colorDataUse = false;
        private bool prevMulti_colorDataUse = false;
        private Color prev_colorDataColor = Color.cyan;
        private bool prevMulti_colorDataColor = false;
        
        
        // -- Tracks whether our artificial foldout is expanded
        private bool foldoutExpanded = false;

        // -- Define GUIContent
        private GUIContent SeedsContent;
        private GUIContent ExampleFoldoutContent;
        public void OnEnable()
        {
            // -- Build GUI Content
            SeedsContent = new GUIContent("Seeds!!!", "Give me seeds!!");
            ExampleFoldoutContent = new GUIContent("Example Foldout", "this is annoying, and also doesn't support right click copy/paste :(");
            
            // -- Preview Props
            enablePreviewProp = serializedObject.FindPropertyChecked(nameof(DEMO.enablePreview));
            previewColorProp = serializedObject.FindPropertyChecked(nameof(DEMO.previewColor));
            previewColorUseProp = previewColorProp.FindRelativeChecked(nameof(DEMO.previewColor.use)); // -- Relative
            previewColorColorProp = previewColorProp.FindRelativeChecked(nameof(DEMO.previewColor.color)); // -- Relative
            
            // -- Distribution Props
            seedProp = serializedObject.FindPropertyChecked(nameof(DEMO.seed));
            radiusProp = serializedObject.FindPropertyChecked(nameof(DEMO.radius));
            
            // -- Spheres Props
            numSpheresProp = serializedObject.FindPropertyChecked(nameof(DEMO.numResults));
            sphereColorProp = serializedObject.FindPropertyChecked(nameof(DEMO.sphereColor));
            sphereColorUseProp = sphereColorProp.FindRelativeChecked(nameof(DEMO.sphereColor.use)); // -- Relative
            sphereColorColorProp = sphereColorProp.FindRelativeChecked(nameof(DEMO.sphereColor.color)); // -- Relative
            
            // -- Find the protected hasModifications property by name, using BetterEditor's FindPropertyChecked for safety
            //          (It's a good idea to always use this method in place of FindProperty)
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
            
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
            
            prev_numSpheres = numSpheresProp.intValue;
            prevMulti_numSpheres = numSpheresProp.hasMultipleDifferentValues;
            prev_colorDataUse = sphereColorUseProp.boolValue;
            prevMulti_colorDataUse = sphereColorUseProp.hasMultipleDifferentValues;
            prev_colorDataColor = sphereColorColorProp.colorValue;
            prevMulti_colorDataColor = sphereColorColorProp.hasMultipleDifferentValues;
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
                EditorGUILayout.LabelField("Preview Props:", EditorStyles.boldLabel);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox(SpheresDemoEditors.GizmosInfo, MessageType.Info);
                EditorGUILayout.PropertyField(enablePreviewProp);
                // -- Alternate to GUI.enabled from Unity
                using (new EditorGUI.DisabledScope(enablePreviewProp.AllFalse()))
                {
                    EditorGUILayout.PropertyField(previewColorProp, true);
                }
                EditorGUI.indentLevel -= 1;
                
                EditorGUILayout.LabelField("Distribution Props:", EditorStyles.boldLabel);
                // -- Alternate to EditorGUI.IndentLevel += 1, from Unity
                using( new EditorGUI.IndentLevelScope() )
                {
                    EditorGUILayout.PropertyField(seedProp, SeedsContent);
                    EditorGUILayout.PropertyField(radiusProp);
                }
                
                EditorGUILayout.LabelField("Spheres Props:", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    foldoutExpanded = EditorGUILayout.Foldout(foldoutExpanded, ExampleFoldoutContent, true, EditorStyles.foldout);
                    if (foldoutExpanded)
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(numSpheresProp);
                            EditorGUILayout.PropertyField(sphereColorProp, true);
                        }
                }
                
            }
            
            // -- Clamp property limits using BetterEditor's Enforce methods
            numSpheresProp.EnforceClamp(4, 100);
            seedProp.EnforceMinimum(0);
            //  - Do NOT use .intValue = Mathf.Clamp()!!!
            //          - It would cause the value to immediately collapse to a single value when selecting multiple
            //            components with mixed Values! 
            
            
            // -- Updated Preview Enabled?
            //      (Note: These properties don't trigger HasModified, as they are automatically being drawn as a gizmo and don't indicate a change in data)
            var updated_previewEnabled = false;
            updated_previewEnabled |= prev_enablePreview != enablePreviewProp.boolValue;
            updated_previewEnabled |= prevMulti_enablePreview != enablePreviewProp.hasMultipleDifferentValues;
            updated_previewEnabled |= prev_previewColorUse != previewColorUseProp.boolValue;
            updated_previewEnabled |= prevMulti_previewColorUse != previewColorUseProp.hasMultipleDifferentValues;
            updated_previewEnabled |= prev_previewColor != previewColorColorProp.colorValue;
            updated_previewEnabled |= prevMulti_previewColor != previewColorColorProp.hasMultipleDifferentValues;
            if(updated_previewEnabled)
                Debug.Log($"Better Editor Demo: EnablePreview updated to {enablePreviewProp.AnyTrue()} ");
                
            // -- Updated Distribution Props?
            var updated_distribution = false;
            updated_distribution |= prev_seed != seedProp.intValue;
            updated_distribution |= prevMulti_seed != seedProp.hasMultipleDifferentValues;
            updated_distribution |= prev_radius != radiusProp.floatValue;
            updated_distribution |= prevMulti_radius != radiusProp.hasMultipleDifferentValues;
            
            // -- Updated Spheres Props?
            var updated_spheres = false;
            updated_spheres |= prev_numSpheres != numSpheresProp.intValue;
            updated_spheres |= prevMulti_numSpheres != numSpheresProp.hasMultipleDifferentValues;
            updated_spheres |= prev_colorDataUse != sphereColorUseProp.boolValue;
            updated_spheres |= prevMulti_colorDataUse != sphereColorUseProp.hasMultipleDifferentValues;
            updated_spheres |= prev_colorDataColor != sphereColorColorProp.colorValue;
            updated_spheres |= prevMulti_colorDataColor != sphereColorColorProp.hasMultipleDifferentValues;
            
            // -- Was PreviewEnabled Updated?
            if (updated_previewEnabled)
            {
                // -- Why use AnyTrue() here?
                //       - Even though user updates always collapse hasMultipleDifferentValues to false,
                //              updates from undo could set it back to true.
                //       - In later demos, we'll be able to have different reactions to undo vs user changes.  
                
                // -- Expand or close the preview Color struct automatically when updating previewEnabled
                previewColorProp.isExpanded = enablePreviewProp.AnyTrue();
                
                Debug.Log($"Better Editor Demo: EnablePreview updated to {enablePreviewProp.AnyTrue()} ");
                
                // -- Example: Set Color.use to false when preview is disabled (bad UX, but for demo)
                if (enablePreviewProp.AllFalse())
                    previewColorUseProp.boolValue = false;
            }
            
            
            // -- Were important modifications (distribute or spheres) updated?
            if (updated_distribution || updated_spheres)
            {
                
                // -- Reset Trackers to current state, to detect changes from this point on
                //      (Can re-track at any time, relative to applying changes)
                RefreshTracking();
                
                // -- Apply any GUI and all serializedProperty value changes back to our target components
                serializedObject.ApplyModifiedProperties();

                // -- Track that we have modifications
                //      (in this demo: hasModification updates are not part of the undo chain, undo will not revert to false)
                hasModificationsProp.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            
            // -- Backup, Apply any GUI changes to properties that might not have been tracked
            //      - For example, for any other or new properties on the component which aren't tracked
            //      - It's okay to do this twice, it won't do anything if there aren't changes
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