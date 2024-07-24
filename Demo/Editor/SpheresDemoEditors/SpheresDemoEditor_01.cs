using System.Collections.Generic;
using BetterEditor;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    // -- We can select multiple distribution objects at a time and modify them all
    [CanEditMultipleObjects]
    
    // -- Target DistributeDemoComponent_Vanilla component
    [CustomEditor(typeof(SpheresDemo_01))]
    public class SpheresDemoEditor_01 : Editor
    {
        
        // -- Track updates
        //      (We're only tracking in the editor, so clear this state if we change selection)
        private bool hasModifications = false;
        private void ClearModifications() => hasModifications = false;
        public void OnEnable()
        {
            ClearModifications();
        }
        
        
        // -- Unity->OnInspectorGUI: Called whenever the inspector is drawn
        //      (which is every 10th of a second while mouse is within the inspector)
        public override void OnInspectorGUI()
        {
            
            // -- Information about this demo, and controls to swap
            var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcherWithModifyWarning(Info, ref hasModifications);
            if(updatedStage) return;
            
            // -- Begin a change check
            //       - This watches for ALL GUI interaction including
            //            - Int sliding (without reaching the next float notch)
            //            - Toggling non-essential foldouts
            //            - Anything...
            EditorGUI.BeginChangeCheck();
            
            // -- Draw default inspector
            //       - Draws all of our properties as though we were using the default inspector
            //       - Does the same thing as base.OnInspectorGUI()
            DrawDefaultInspector();
            
            // -- Finish the change check
            hasModifications |= EditorGUI.EndChangeCheck();
        }
        
        
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.BarebonesEditor,
            title = "Barebones Editor",
            description = "Draws the component using Unity's DrawDefaultInspector().\n" +
                          "Attempts to track any updates to modified data using BeginChangeCheck()",
            greenTexts = new List<string>()
            {
                "Very easy to setup using vanilla Unity code",
            },
            redTexts = new List<string>()
            {
                "Pasting Data does not trigger an update",
                "Pasting/Undo/Redo/Reset/Revert do not trigger updates!",
                "Slightly adjusting Int sliders will trigger a false update!",
                "Toggling non-essential foldouts will trigger a false update!",
            },
            fileName = "SpheresDemoEditor_01.cs",
        };
    }
}