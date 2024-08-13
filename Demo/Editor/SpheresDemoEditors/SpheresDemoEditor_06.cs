using System;
using System.Collections.Generic;
using BetterEditor;
using UnityEditor;
using UnityEngine;


namespace BetterEditorDemos
{
    
    
    
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpheresDemo_06))]
    public class SpheresDemoEditor_06 : Editor
    {
        
        private SerializedProperty hasModificationsProp;
        
        // -- Using Editor from demo-stage-5. 
        private SpheresDemo_TrackAndDraw_05 componentEditor = new();

        // -- [New Feature] Track enabled renderers from created objects
        //      - (Note: 'm_Enabled' is the internal Unity property for enabling a component, such as Renderers in this demo)
        private SerializedObject objectRenderersObject;
        private Tracker showObjectRenderersTracker = new("m_Enabled"); // 
        private GUIContent showObjectRenderersContent = new("Object Renderers Enabled", "Enable/Disable all renderers in all created objects");
        
        // -- The Better Editor
        public BetterEditor<SpheresDemo_06> betterEditor;
        
        // -- [Unity] OnEnable
        public void OnEnable()
        {
            
            // -- Start BetterEditor
            betterEditor = new BetterEditor<SpheresDemo_06>()
            {
                editor = this,
                OnTargetsUpdated = HandleTargetsUpdated,
                OnPropsUpdated = HandlePropertiesUpdated,
                logAllTrackerUpdates = true,
                logImporantFunctions = true,
                logAllFunctions = true,
                refreshOnHierarchyUpdate = false,
                refreshOnUndoRedo = true,
            };
            betterEditor.Enable();
            
            // -- Make sure BetterEditor is aware of all our trackers
            betterEditor.GroupFull.UnionWith(componentEditor.group);
            betterEditor.GroupFull.Add(showObjectRenderersTracker);
            
            // -- Get Serialized Properties
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
            
            // -- Start the component editor, like before
            componentEditor.Track(serializedObject.AsSource());
        }

        // -- [Unity] OnDisable
        public void OnDisable()
        {
            betterEditor.Disable();
            objectRenderersObject?.Dispose();
        }
        
        // -- [Unity] OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // -- Update Better Editor
            betterEditor.Update();
            
            // -- Update Serialized Objects
            serializedObject.Update();
            objectRenderersObject?.Update();
            
            // -- Draw the UI
            DrawUI();
           
            // -- Check for Updates after drawing the UI. 
            betterEditor.CheckForUpdates();
        }

        // -- [BetterEditor] OnTargetsUpdated: Called often.
        //          - BEFORE THIS IS CALLED, BetterEditor.Targets is updated 
        //          - AFTER THIS IS CALLED, BetterEditor.GroupFull.RefreshTrackers() is performed
        //          - [Cause 1] There was an update to any property tracked in betterEditor.GroupFull
        //          - [Cause 2] .refreshOnHierarchyUpdate was set to true and the hierarchy changed
        //          - [Cause 3] .refreshOnUndoRedo was set to true and an Undo/Redo operation occurred
        //          - [Cause 4] A Script HotReload was detected and all data needs to be regathered
        //          - [Cause 5] .SetNeedsNewTargets() was called manually
        private void HandleTargetsUpdated()
        {
            // -- Better Editor will call this OnUpdate delegate after gathering a valid list
            //         of selected targets (from given Editor's (this) targets as SpheresDemo_06) 
            //Debug.Log($"Has New Targets! {betterEditor.Targets.Count}");
            
            // -- Strip all Invalid Created Objects from all targets
            //      (If a tracked created object is deleted from the scene, also delete it from the list)
            foreach (SpheresDemo_06 target in betterEditor.Targets)
                target.createdObjects.RemoveAll(item => item == null);
            
            // -- Build a full list of renderers from all createdObjects in all targets
            List<Renderer> allRenderers = new();
            foreach (SpheresDemo_06 target in betterEditor.Targets)
                foreach (var createdObject in target.createdObjects)
                    allRenderers.AddRange(createdObject.GetComponentsInChildren<Renderer>());
            
            // -- Build a new custom SerializedObject and property to track visibility across all createdObject renderers
            objectRenderersObject?.Dispose();
            objectRenderersObject = null;
            showObjectRenderersTracker.StopTracking();
            if (allRenderers.Count > 0)
            {
                objectRenderersObject = new SerializedObject(allRenderers.ToArray());
                showObjectRenderersTracker.Track(objectRenderersObject.AsSource());
            }
        }
        
        // -- [BetterEditor] HandlePropertiesUpdated
        //      - Called when any tracker registered to the BetterEditor is updated
        //      - EUpdateSource allows different responses to Undo/Redo Operations
        private void HandlePropertiesUpdated(EUpdateSource source)
        {
            // -- In this demo, hasModifications is part of the undo chain, which means it shouldn't be set to true when undoing/redoing
            //          to a state where hasModifications was previously set to false, as the generated result is still in line with the data and 
            //          doesn't require a regeneration.
            //              - (Note: This is only possible if we track Undo via Unity's delegates, which BetterEditor handles and converts into an UpdateSource.)
            if(source != EUpdateSource.UndoRedo)
            {
                if (componentEditor.importantTrackers.WasAnyUpdated())
                    hasModificationsProp.boolValue = true;
            }
            
            // -- Apply changes to serialized Objects
            serializedObject.ApplyModifiedProperties();
            objectRenderersObject?.ApplyIfModified(); 
        }
        
        // -- Draw the UI, using the UI from componentEditor defined in step 5 and adding to it. 
        private void DrawUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcher(Info);
            if(updatedStage) return;
            
            // -- Draw the modifications Row
            var pressedApply = SpheresDemoEditors.DrawModifyWarningRowSerialized(hasModificationsProp);
            
            // -- Draw the Editor
            componentEditor.DrawNoHeader();
            
            // -- Draw the new feature: "showObjectRenderersTracker"
            if (showObjectRenderersTracker.tracking)
                EditorGUILayout.PropertyField(showObjectRenderersTracker.prop, showObjectRenderersContent);
            // -- Backup: Fake Disabled Toggle
            else using (new EditorGUI.DisabledScope(true)) 
                EditorGUILayout.Toggle(showObjectRenderersContent, false);
            
            // -- Respond to Apply button
            if (pressedApply)
            {
                // -- Do the actual logic to apply the changes
                //       - In this demo, we're setting hasModifiedProperties to false directly in the objects code, it's easier
                //          and we're done teaching how serialized properties work.
                SpheresDemoEditors.Distribute(targets, true);
                
                // -- Regather targets immediately, to prevent error spam from the objectRenderersObject based on previous (but now-deleted) objects
                //   
                betterEditor.SetNeedsNewTargets();
            }
        }
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.UsingEverything,
            title = "BetterEditor Framework object",
            description = "The final demo, using the BetterEditor framework object to provide a better Targets List and info about the source of updates. " +
                          "HasModifiedProperties is now part of the Undo-Chain",
            greenTexts = new List<string>()
            {
                "Uses the full BetterEditor framework object",
                "Build a second SerializedObject to disable all renderers in all created objects",
                "Uses BetterEditor's .targets list",
                "HasModifications boolean can now be part of the undo chain"
            },
            redTexts = new List<string>()
            {
            },
            fileName = "SpheresDemoEditor_06.cs",
        };
    }
}