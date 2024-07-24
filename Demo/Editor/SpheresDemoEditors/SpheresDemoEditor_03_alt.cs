// using System;
// using System.Collections.Generic;
// using BetterEditor;
// using UnityEditor;
// using UnityEngine;
//
// namespace BetterEditorDemos
// {
//     [CanEditMultipleObjects]
//     [CustomEditor(typeof(SpheresDemo_03))]
//     public class SpheresDemoEditor_03 : Editor
//     {
//         
//         // -- Used for nameof()
//         private static SpheresDemo DEMO;
//         
//         // -- Serialized Properties from the target component
//         private SerializedProperty enablePreviewProp;
//         private SerializedProperty seedProp;
//         private SerializedProperty numSpheresProp;
//         private SerializedProperty colorDataProp;
//         private SerializedProperty colorDataUseProp;
//         private SerializedProperty colorDataColorProp;
//         
//         // -- We'll store modifications on the actual object now that we're good with serialized properties
//         //      (This will also keep our hasModifications up to date with our data when we use Undo/Redo)
//         private SerializedProperty hasModificationsProp;
//         
//         // -- Tracks whether our artificial foldout is expanded
//         private bool foldoutExpanded = false;
//
//         // -- Define GUIContent
//         private GUIContent SeedsContent;
//         private GUIContent OtherPreviewPropsContent;
//         public void OnEnable()
//         {
//             Debug.Log("Better Editor Demo: SpheresDemoEditor_03 Enabled");
//             
//             // -- Build GUI Content
//             SeedsContent = new GUIContent("Seeds!!!", "Give me seeds!!");
//             OtherPreviewPropsContent = new GUIContent("Other Preview Props", "These are other preview props");
//             
//             // -- Build serialized properties
//             RebuildSerializedProps();
//         }
//
//         public void RebuildSerializedProps()
//         {
//             // -- 3 Basic Props
//             enablePreviewProp = serializedObject.FindProperty(nameof(DEMO.enablePreview));
//             seedProp = serializedObject.FindProperty(nameof(DEMO.seed));
//             numSpheresProp = serializedObject.FindProperty(nameof(DEMO.numSpheres));
//             
//             // -- Find the protected hasModifications property by name, using BetterEditor's FindPropertyChecked for safety
//             //          (It's a good idea to always use this method in place of FindProperty)
//             hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
//             
//             // -- Get the class, then get the next two properties relative to it
//             colorDataProp = serializedObject.FindProperty(nameof(DEMO.colorData));
//             colorDataUseProp = colorDataProp.FindPropertyRelative(nameof(DEMO.colorData.use));
//             colorDataColorProp = colorDataProp.FindPropertyRelative(nameof(DEMO.colorData.Color));
//             
//         }
//
//         // -- Unity->OnInspectorGUI
//         public override void OnInspectorGUI()
//         {
//             // -- Information about this demo, and controls to swap
//             var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcher(Info);
//             if(updatedStage) return;
//             
//             // -- Update Serialized Object, as always
//             serializedObject.Update();
//             
//             // -- Draw the modifications Row using the serialized property for hasModifications (after serializedObject.Update())
//             SpheresDemoEditors.DrawModifyWarningRowSerialized(hasModificationsProp);
//             
//             // -- Track all properties which can update individually 
//             //        - A .boolValue can change from true -> true or false to false, when in the background hasMultipleDifferentValues is true,
//             //             so we must also track each hasMultipleDifferentValues
//             var prev_enablePreview = enablePreviewProp.boolValue;
//             var prevMulti_enablePreview = enablePreviewProp.hasMultipleDifferentValues;
//             var prev_seed = seedProp.intValue;
//             var prevMulti_seed = seedProp.hasMultipleDifferentValues;
//             var prev_numSpheres = numSpheresProp.intValue;
//             var prevMulti_numSpheres = numSpheresProp.hasMultipleDifferentValues;
//             var prev_colorDataUse = colorDataUseProp.boolValue;
//             var prevMulti_colorDataUse = colorDataUseProp.hasMultipleDifferentValues;
//             var prev_colorDataColor = colorDataColorProp.colorValue;
//             var prevMulti_colorDataColor = colorDataColorProp.hasMultipleDifferentValues;
//             
//             // -- DRAW THE MAIN UI
//             {
//                 // -- Draw 3 Fields
//                 EditorGUILayout.LabelField("Preview Props:", EditorStyles.boldLabel);
//                 EditorGUI.indentLevel += 1;
//                 
//                 EditorGUILayout.PropertyField(enablePreviewProp);
//                 
//                 // -- Color Data (disabled if no preview)
//                 GUI.enabled = enablePreviewProp.AnyTrue();
//                 EditorGUILayout.PropertyField(colorDataProp, true);
//                 GUI.enabled = true;
//
//                 // -- Draw Other Preview Props in a foldout (with examples for changing content and tooltips)
//                 if (enablePreviewProp.AnyTrue())
//                 {
//                     foldoutExpanded = EditorGUILayout.Foldout(foldoutExpanded, OtherPreviewPropsContent, true, EditorStyles.foldout);
//                     if (foldoutExpanded)
//                     {
//                         EditorGUI.indentLevel += 1;
//                         EditorGUILayout.PropertyField(seedProp, SeedsContent);
//                         EditorGUILayout.PropertyField(numSpheresProp);
//                         EditorGUI.indentLevel -= 1;
//                     }
//                 }
//                 
//                 EditorGUI.indentLevel -= 1;
//             }
//             
//             // -- [Note] It doesn't really matter when you check for updates
//             //       (SerializedObject.Update() and ApplyModifiedProperties() will not cause these to output differently)
//             
//             // -- Updated Preview Enabled?
//             var updated_previewEnabled = false;
//             updated_previewEnabled |= prev_enablePreview != enablePreviewProp.boolValue;
//             updated_previewEnabled |= prevMulti_enablePreview != enablePreviewProp.hasMultipleDifferentValues;
//             
//             // -- Updated Color Data?
//             var updated_colorData = false;
//             updated_colorData |= prev_colorDataUse != colorDataUseProp.boolValue;
//             updated_colorData |= prevMulti_colorDataUse != colorDataUseProp.hasMultipleDifferentValues;
//             updated_colorData |= prev_colorDataColor != colorDataColorProp.colorValue;
//             updated_colorData |= prevMulti_colorDataColor != colorDataColorProp.hasMultipleDifferentValues;
//             
//             // -- Updated Other Props?
//             var updated_other = false;
//             updated_other |= prev_seed != seedProp.intValue;
//             updated_other |= prevMulti_seed != seedProp.hasMultipleDifferentValues;
//             updated_other |= prev_numSpheres != numSpheresProp.intValue;
//             updated_other |= prevMulti_numSpheres != numSpheresProp.hasMultipleDifferentValues;
//
//             
//             // -- Respond to specific changes
//             if (updated_previewEnabled)
//             {
//                 
//                 // -- Expand or close foldouts (including the foldout powered by the serialized property)
//                 foldoutExpanded = enablePreviewProp.AnyTrue();
//                 colorDataProp.isExpanded = enablePreviewProp.AnyTrue();
//                 
//                 Debug.Log($"Better Editor Demo: EnablePreview is {enablePreviewProp.AnyTrue()} ");
//                 
//                 // -- Example: Set Seed to 0 when preview is disabled
//                 if (!enablePreviewProp.AnyTrue())
//                     seedProp.intValue = 0;
//             }
//             
//             // -- Respond to other changes in data
//             if(updated_colorData)
//                 Debug.Log("Better Editor Demo: Color Data Changed");
//             if(updated_other)
//                 Debug.Log("Better Editor Demo: Other Props Changed");
//             
//             // -- Set hasModifications on Update
//             //       - (this will be stored on the undo chain as well, allowing us to track through undo and redo correctly)
//             var updated_Any = updated_previewEnabled || updated_colorData || updated_other;
//             if(updated_Any)
//                 hasModificationsProp.boolValue = true;
//             
//             // -- Apply any GUI and all serializedProperty value changes back to our target components
//             serializedObject.ApplyModifiedProperties();
//         }
//         
//         
//         
//         
//         // -- Info about this demo
//         private static readonly SpheresDemoInfo Info = new()
//         {
//             stage = ESpheresDemoStages.FullSerialized,
//             title = "Using Serialized (full)",
//             description = "Draws the component by gathering all properties which we want to draw and drawing them in a completely custom ruleset.\n\n" +
//                           "Checks for un-applied change by storing and comparing previous values against current manually (improved by BetterEditor in the next step).\n\n"+
//                           "Track HasModifications permanently (and across Undo/Redo) with Serialized Properties.",
//             greenTexts = new List<string>()
//             {
//                 "Vanilla Unity code",
//                 "UI structure is clear; Easier to build complicated UI",
//                 "Respond to changes in specific data",
//                 "Undo/Redo keeps HasModifications in sync (using a trick)"
//             },
//             redTexts = new List<string>()
//             {
//                 "Verbose, especially comparisons, improved in next step",
//                 "Large code-changes required when updating component data structure",
//                 "Pasting/Reset/Revert do not trigger updates!",
//             },
//             fileName = "SpheresDemoEditor_03.cs",
//         };
//     }
// }