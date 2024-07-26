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
        private static SpheresDemo_ColorData COLORDATA;
        
        // -- Serialized Properties (Ones we don't track explicitly)
        private SerializedProperty previewColorProp;
        private SerializedProperty sphereColorProp;
        private SerializedProperty hasModificationsProp;
        
        // -- BetterEditor: SerializedTrackers for all properties
        //          (optionally, with property names predefined)
        private Tracker enablePreviewTracker = new( nameof(DEMO.enablePreview) );
        private Tracker previewColorUseTracker = new( nameof(COLORDATA.use) ); // -- relative to previewColorProp
        private Tracker previewColorColorTracker = new( nameof(COLORDATA.color) ); // -- relative to previewColorProp
        
        private Tracker seedTracker = new( nameof(DEMO.seed) );
        private Tracker radiusTracker = new( nameof(DEMO.radius) );
        
        private Tracker numSpheresTracker = new( nameof(DEMO.numResults) );
        private Tracker sphereColorUseTracker = new( nameof(COLORDATA.use) ); // -- relative to sphereColorProp
        private Tracker sphereColorColorTracker = new( nameof(COLORDATA.color) ); // -- relative to sphereColorProp
        
        // -- Collections for Trackers
        private TrackerCollectionFull allCollectionFull = new();
        private TrackerCollectionFull previewPropCollectionFull = new();
        private TrackerCollectionFull importantPropCollectionFull = new();
        
        public void OnEnable()
        {
            // --  Get all Trackers 
            //      (Technically gets more than that, see next step...)
            allCollectionFull.PopulateWithReflection(this);
            
            // -- Setup Collections for convenience
            previewPropCollectionFull.Set(previewColorUseTracker, previewColorColorTracker);
            importantPropCollectionFull.Set(seedTracker, radiusTracker, numSpheresTracker, sphereColorUseTracker, sphereColorColorTracker);

            // -- Get Serialized Properties
            previewColorProp = serializedObject.FindPropertyChecked(nameof(DEMO.previewColor));
            sphereColorProp = serializedObject.FindPropertyChecked(nameof(DEMO.sphereColor));
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");

            // -- Track!
            RefreshTracking();
        }
        
        public void RefreshTracking()
        {
            // -- Preview Props
            enablePreviewTracker.Track(serializedObject.AsSource());
            previewColorUseTracker.Track(previewColorProp.AsSource());
            previewColorColorTracker.Track(previewColorProp.AsSource());
            
            // -- Important Props
            seedTracker.Track(serializedObject.AsSource());
            numSpheresTracker.Track(serializedObject.AsSource());
            radiusTracker.Track(serializedObject.AsSource());
            sphereColorUseTracker.Track(sphereColorProp.AsSource()); 
            sphereColorColorTracker.Track(sphereColorProp.AsSource());
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
            serializedObject.DrawDefaultEditor_NoUpdates();
            
            // -- Clamp the number of spheres using BetterEditor's Enforce methods
            //  - Do NOT use .intValue = Mathf.Clamp()!!!
            //          - It would cause the value to immediately collapse to a single value when selecting multiple
            //            components with mixed Values! 
            numSpheresTracker.prop.EnforceClamp(4, 100);
            seedTracker.prop.EnforceMinimum(0);
            
            // -- Example: Set seed to 0 when preview is disabled
            if(enablePreviewTracker.WasUpdated())
                if (enablePreviewTracker.prop.AnyTrue() == false)
                    seedTracker.prop.intValue = 0;
            
            // -- Check (and Log) if anything was updated!
            var updated_Any = allCollectionFull.WasUpdated(TrackLogging.LogIfUpdated);
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
                RefreshTracking();
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