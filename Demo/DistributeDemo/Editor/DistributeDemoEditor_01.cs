using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    // -- We can select multiple distribution objects at a time and modify them all
    [CanEditMultipleObjects]
    
    // -- Target DistributeDemoComponent_Vanilla component
    [CustomEditor(typeof(DistributeDemoComponent01))]
    public class DistributeDemoEditor_01 : Editor
    {
        
        // -- Track updates
        //      (because this boolean is private to this editor class, it will be obliterated when the selection is changed)
        private bool hasModifications = false;
        
        // -- Unity->OnEnable: Called whenever a new component is selected
        public void OnEnable()
        {
            hasModifications = false;
        }
        
        // -- Unity->OnInspectorGUI: Called whenever the inspector is drawn
        //      (which is every 10th of a second while mouse is within the inspector)
        public override void OnInspectorGUI()
        {
            
            // -- Information about this demo, and controls to swap
            DistributeDemoEditorCommon.DrawDemoInfoAndApplyRow(StageInfo, hasModifications, out var updatedStage, out var pressedApply);
            if(updatedStage) return;
            if(pressedApply)
            {
                DistributeDemoEditorCommon.Distribute(targets);
                hasModifications = false;
                return;
            }
            
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
        private static readonly DistributeDemo_StageInfo StageInfo = new()
        {
            stage = EDistributeDemoStages.BarebonesEditor,
            title = "Vanilla Inspector",
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
            fileName = "DistributeDemoEditor_01.cs",
        };
    }
}