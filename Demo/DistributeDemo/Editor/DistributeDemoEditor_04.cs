using System;
using System.Collections.Generic;
using BetterEditor;
using UnityEditor;
using UnityEngine;

namespace BetterEditorDemos
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DistributeDemoComponent04))]
    public class DistributeDemoEditor_04 : Editor
    {
        
        // -- Used for nameof()
        private static DistributeDemoComponent _demoComponent;
        private static SpheresDemo_ColorData COLORDATA;
        
        // -- Serialized Properties (Ones we don't track explicitly)
        private SerializedProperty previewColorProp;
        private SerializedProperty objectColorProp;
        private SerializedProperty hasModificationsProp;
        
        // -- BetterEditor: SerializedTrackers for all properties
        private Tracker enablePreviewTracker = new( nameof(_demoComponent.enablePreview) );
        private Tracker previewColorUseTracker = new( nameof(COLORDATA.use) ); // -- relative to previewColorProp
        private Tracker previewColorColorTracker = new( nameof(COLORDATA.color) ); // -- relative to previewColorProp
        
        private Tracker seedTracker = new( nameof(_demoComponent.seed) );
        private Tracker radiusTracker = new( nameof(_demoComponent.radius) );
        
        private Tracker totalToGenerateTracker = new( nameof(_demoComponent.totalToGenerate) );
        private Tracker objectColorUseTracker = new( nameof(COLORDATA.use) ); // -- relative to sphereColorProp
        private Tracker objectColorColorTracker = new( nameof(COLORDATA.color) ); // -- relative to sphereColorProp

        private ListTracker createdObjectsTracker = new(nameof(_demoComponent.createdObjects));
        
        // -- Collection of all Trackers, gather automatically below
        private TrackerGroup allTrackers = new();
        
        // -- Tracker Collections (So we can check which category was updated)
        private Tracker[] previewTrackers;
        private Tracker[] importantTrackers;
        
        public void OnEnable()
        {
            // -- Get ALL  ITrack objects via reflection (The 8 trackers)
            allTrackers.PopulateWithReflection(this);
            
            // -- Setup Tracker Groups
            previewTrackers = new Tracker[] {enablePreviewTracker, previewColorUseTracker, previewColorColorTracker};
            importantTrackers = new Tracker[] {seedTracker, radiusTracker, totalToGenerateTracker, objectColorUseTracker, objectColorColorTracker};

            // -- Get Serialized Properties
            previewColorProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.previewColor));
            objectColorProp = serializedObject.FindPropertyChecked(nameof(_demoComponent.objectColor));
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");

            // -- Track!
            RefreshTracking();
        }
        
        public void RefreshTracking()
        {
            // -- Preview Props
            enablePreviewTracker.Track(serializedObject.AsSource());
            
            // -- Preview Color (Relative to previewColorProp)
            previewColorUseTracker.Track(previewColorProp.AsSource());
            previewColorColorTracker.Track(previewColorProp.AsSource());
            
            // -- Important Props
            seedTracker.Track(serializedObject.AsSource());
            totalToGenerateTracker.Track(serializedObject.AsSource());
            radiusTracker.Track(serializedObject.AsSource());
            
            // -- Object Color (Relative to objectColorProp)
            objectColorUseTracker.Track(objectColorProp.AsSource());  
            objectColorColorTracker.Track(objectColorProp.AsSource());
            
            // -- Example: createdObjectsTracker
            createdObjectsTracker.Track(serializedObject.AsSource());
        }

        // -- Unity->OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = DistributeDemoEditorCommon.DrawDemoInfo(StageInfo);
            if(updatedStage) return;
            
            // -- Update Serialized Object
            serializedObject.Update();
            
            // -- Draw the modifications Row using the serialized property for hasModifications
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
            
            // -- Draw the default inspector (Nothing Fancy)
            //        - Using BetterEditor's DrawDefaultEditor_NoUpdates to avoid sObject Update() and Apply()
            //        - (It's generally better to apply in a controlled manner after making any additional serialized val changes)
            serializedObject.DrawDefaultEditor_NoUpdates();
            
            // -- draw our list of created objects
            using( new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(createdObjectsTracker.prop);
            
            // -- Clamp the number of spheres using BetterEditor's Enforce methods
            totalToGenerateTracker.prop.EnforceClamp(4, 100);
            seedTracker.prop.EnforceMinimum(0);
            
            // -- importantTrackers Modified?
            //       - Using IEnumerable<ITrack>.WasAnyUpdated(), use any structure you'd like!
            var modified_Important = importantTrackers.WasUpdated();
             
            // -- Check (and Log) if anything was updated using our group
            var anythingUpdated = allTrackers.WasUpdated( ETrackLog.LogIfUpdated);

            // -- Example: Tracking changes to a list, but not used
            var modifiedCreatedObjectsExample = createdObjectsTracker.WasUpdated();
            
            // -- Handle Enable-Preview Updated
            if (enablePreviewTracker.WasUpdated())
                HandlePreviewUpdated();
            
            // -- Track that we have modifications
            if (modified_Important)
                HandleDetectModifications();
            
            // -- Refresh Tracking.
            if(anythingUpdated)
                RefreshTracking();
            
            // -- (Regular Flow) Apply all changes made to our target components. 
            serializedObject.ApplyModifiedProperties();
        }
        
        private void HandlePreviewUpdated()
        {
            // -- Expand or close the preview Color struct automatically when updating previewEnabled
            previewColorProp.isExpanded = enablePreviewTracker.prop.AnyTrue();
            
            // -- Example: Set Color.use to false when preview is disabled
            if (enablePreviewTracker.prop.AllFalse())
                previewColorUseTracker.prop.boolValue = false;
        }
        
        private void HandleDetectModifications()
        {
            //  In This Demo: hasModification updates are not part of the undo chain, undo will not revert them to false.
            //      To accomplish this we apply any GUI updates first, regularly, then we push another silent update
            serializedObject.ApplyModifiedProperties();
            hasModificationsProp.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        
        
        // -- Info about this demo
        private static readonly DistributeDemo_StageInfo StageInfo = new()
        {
            stage = EDistributeDemoStages.BetterTrackers,
            title = "BetterEditor: Intro to Tracking",
            description = "Basic example of BetterEditor's Trackers & TrackerGroup to improve update-tracking!",
            greenTexts = new List<string>()
            {
                "Much less code and headache!",
                "Automatic logging options (see console)",
                "Comparisons in WasUpdated() are generic, SerializedTracker is Type-agnostic",
                "ListTracker for tracking lists",
            },
            redTexts = new List<string>()
            {
            },
            fileName = "DistributeDemoEditor_04.cs",
        };
    }
}