using System.Collections.Generic;
using BetterEditor;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    // -- We can select multiple distribution objects at a time and modify them all
    [CanEditMultipleObjects]
    
    // -- Target DistributeDemoComponent_Vanilla component
    [CustomEditor(typeof(SpheresDemo_02))]
    public class SpheresDemoEditor_02 : Editor
    {
        
        // -- Track updates
        //      (We're only tracking in the editor, so clear this state if we change selection)
        private bool hasModifications = false;
        public void OnEnable()
        {
            hasModifications = false;
        }

        
        // -- Used for nameof()
        private static SpheresDemo_02 DEMO_02;

        // -- Top-Level Properties to hide, used below
        private List<string> hiddenProperties = new List<string>()
        {
            "m_Script", // -- hide the default linker to the Unity script
        };

        // -- Unity->OnInspectorGUI: Called whenever the inspector is drawn
        //      (which is every 10th of a second while mouse is within the inspector)
        public override void OnInspectorGUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcherWithModifyWarning(Info, ref hasModifications);
            if(updatedStage) return;
            
            // -- Update Serialized Object
            //       - serializedObject is an Editor property. It's a collection of all properties
            //              on all user selected components (of the target type) whose data is shown in this editor.
            //       - this is normal flow for Editor, always use this or .UpdateIfRequiredOrScript())
            serializedObject.Update();
            
            // -- Get the "enablePreview" SerializedProperty from that object
            var propertyName = nameof(DEMO_02.enablePreview); // -- Gives "enablePreview"
            var enablePreviewProp = serializedObject.FindProperty(propertyName);
            
            // -- Check if the property is true for ALL selected components
            //        (SerializedProperties can represent data for multiple components!)
            var anyPreviewsEnabled = enablePreviewProp.AnyTrue();
            
            // -- Draw default inspector by iterating properties
            SerializedProperty serializedPropIterator = serializedObject.GetIterator();
            for (bool enterChildren = true; serializedPropIterator.NextVisible(enterChildren); enterChildren = false)
            {
                // -- Unfortunately, we don't get any control over individual child properties within object properties,
                //       we can only operate on the top level properties in the serializedObject...
                
                var propPath = serializedPropIterator.propertyPath;

                // -- Skip hidden properties (using an array)
                if (hiddenProperties.Contains(propPath))
                    continue;

                // -- All other properties are disabled if preview is disabled
                bool propEnabled = true; 
                
                // -- preview color is only enabled if preview is enabled
                if(propPath == nameof(DEMO_02.previewColor))
                    propEnabled = anyPreviewsEnabled;
                
                // -- Unfortunately, we can't disable individual child properties within our color classes...

                // -- Universal Property Drawer
                //      (draws a fully functional editor row, with right click support, tooltip support, all propertyDrawers, etcs)
                GUI.enabled = propEnabled;
                EditorGUILayout.PropertyField(serializedPropIterator, true);
                GUI.enabled = true;
            }
            
            // -- Check if any changed we made to the serializedObject's properties by the User
            hasModifications |= serializedObject.hasModifiedProperties;
           
            // -- Apply all of those changes back to our target components
            serializedObject.ApplyModifiedProperties();
        }
        
        
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.BasicSerialized,
            title = "Using Serialized (iterate)",
            description = "Draws the component by iterating all properties, similar to Unity's DrawDefaultInspector().\n\n" +
                          "Checks for change using Unity's serializedObject.hasModifiedProperties.\n\n"+
                          "Shows how to hide and disable controls in the most basic form, with basic SerializedProperty usage.",
            greenTexts = new List<string>()
            {
                "Vanilla Unity code, adapts well to changes in code",
            },
            redTexts = new List<string>()
            {
                "Iterating props provides poor support for modifying child props of objects",
                "Pasting/Undo/Redo/Reset/Revert do not trigger updates!",
            },
            fileName = "SpheresDemoEditor_02.cs",
        };
    }
}