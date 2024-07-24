using System;
using System.Collections.Generic;
using BetterEditor;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpheresDemo_04))]
    public class SpheresDemoEditor_04 : Editor
    {
        
        // -- Used for nameof()
        private static SpheresDemo DEMO;
        
        // -- BetterEditor: SerializedTrackers for all properties
        //          (optionally, with property names predefined)
        private SerializedTracker enablePreviewTracker = new( nameof(DEMO.enablePreview) );
        private SerializedTracker seedTracker = new( nameof(DEMO.seed) );
        private SerializedTracker numSpheresTracker = new( nameof(DEMO.numSpheres) );
        private SerializedTracker colorDataUseTracker = new( nameof(DEMO.colorData.use) ); // -- relative to colorDataProp
        private SerializedTracker colorDataColorTracker = new( nameof(DEMO.colorData.Color) ); // -- relative to colorDataProp
        
        // -- Collections for Trackers
        private BasicTrackerCollection allCollection = new();
        private BasicTrackerCollection colorPropCollection = new();
        private BasicTrackerCollection otherPropCollection = new();
        
        // -- Property for the colorData class
        private SerializedProperty colorDataProp;
        
        // -- Storing hasModification on the target component
        private SerializedProperty hasModificationsProp;
        
        public void OnEnable()
        {
            // -- Setup Collections for convenience
            allCollection.Set(enablePreviewTracker, seedTracker, numSpheresTracker, colorDataUseTracker,
                colorDataColorTracker);
            colorPropCollection.Set(colorDataUseTracker, colorDataColorTracker);
            otherPropCollection.Set(enablePreviewTracker, seedTracker, numSpheresTracker);

            // -- Get the colorData property
            colorDataProp = serializedObject.FindProperty(nameof(DEMO.colorData));

            // -- Get the hasModifications property
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");

            // -- Track!
            TrackFromCurrentValues();
        }
        
        public void TrackFromCurrentValues()
        {
            // -- 3 Primary Trackers from SerializedObject
            enablePreviewTracker.Track(serializedObject);
            seedTracker.Track(serializedObject);
            numSpheresTracker.Track(serializedObject);
            
            // -- 2 Relative Trackers from colorData SerializedProperty
            colorDataUseTracker.TrackRelative(colorDataProp);
            colorDataColorTracker.TrackRelative(colorDataProp);
        }

        // -- Unity->OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcher(Info);
            if(updatedStage) return;
            
            // -- Update Serialized Object
            serializedObject.Update();
            
            // -- Draw the modifications Row using the serialized property for hasModifications
            //      (in this demo: hasModification updates are not part of the undo chain, undo will not revert them)
            var pressedApply = SpheresDemoEditors.DrawModifyWarningRowSerialized(hasModificationsProp);
            if (pressedApply)
            {
                hasModificationsProp.boolValue = false;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            // -- Draw the default inspector (Nothing Fancy)
            //        - Using BetterEditor's DrawDefaultEditor_NoUpdates to avoid sObject Update() and Apply()
            //        - (It's generally better to apply in a controlled manner after making any additional serialized val changes)
            this.DrawDefaultEditor_NoUpdates();
            
            
            // -- Clamp the number of spheres using BetterEditor's Enforce methods
            numSpheresTracker.prop.EnforceClamp(4, 100);
            seedTracker.prop.EnforceMinimum(0);
            //  - Do NOT use .intValue = Mathf.Clamp()!!!
            //          - It would cause the value to immediately collapse to a single value when selecting multiple
            //            components with mixed Values! 
            
            // -- Example: Set seed to 0 when preview is disabled
            if(enablePreviewTracker.WasUpdated())
                if (enablePreviewTracker.prop.AnyTrue() == false)
                    seedTracker.prop.intValue = 0;
            
            // -- Check (and Log) if anything was updated!
            var updated_Any = allCollection.AnyWasUpdated(SerializedTrackerLogging.LogIfUpdated);
            if (updated_Any)
            {
                // -- Apply any GUI and all serializedProperty value changes back to our target components
                serializedObject.ApplyModifiedProperties();
                
                // -- Track that we have modifications
                //      (in this demo: hasModification updates are not part of the undo chain, undo will not revert to false)
                hasModificationsProp.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                
                // -- Reset Trackers to current state, to detect changes from this point on
                //      (Can re-track at any time, relative to applying changes)
                TrackFromCurrentValues();
            }
            
            
            // -- Backup, Apply any GUI changes to properties that might not have been tracked
            //     (Note, it's okay to do this twice, it won't do anything if there aren't changes)
            serializedObject.ApplyModifiedProperties();
        }
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.BetterTrackers,
            title = "BetterEditor: Intro to Tracking",
            description = "Basic example of BetterEditor's SerializedTrackers & TrackerCollections to improve update-tracking!",
            greenTexts = new List<string>()
            {
                "Much less headache!",
                "Automatic logging options (see inspector)",
                "Comparisons in WasUpdated() are generic, SerializedTracker is Type-agnostic",
                "Reacts to updates from all sources",
            },
            redTexts = new List<string>()
            {
            },
            fileName = "SpheresDemoEditor_04.cs",
        };
    }
}