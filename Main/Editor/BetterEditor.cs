// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)


using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;

namespace BetterEditor
{
    
    public enum EUpdateSource
    {
        GUI,
        UndoRedo,
    }
    
    public class BetterEditor <TargetClass> : IDisposable where TargetClass : UnityEngine.Object
    {
        // -- Required Param: The editor whose targets we will parse
        public Editor editor;
        
        // -- Required Delegate: Build all Trackers and Serialized Properties from serializedObject (or any other optional sources.) 
        public BuildTrackersDelegate BuildTrackers;
        
        // -- Required Delegate: Perform any updates in reaction to updated trackers
        //      - Argument 1: UpdateSource, the source of the update (GUI or UndoRedo)
        public PropertiesUpdatedDelegate OnPropsUpdated;
        
        // -- [BetterEditor] OnTargetsUpdated: Called often.
        //          - BEFORE THIS IS CALLED, BetterEditor.Targets is updated 
        //          - AFTER THIS IS CALLED, BetterEditor.GroupFull.RefreshTrackers() is performed
        //          - [Cause 1] There was an update to any property tracked in betterEditor.GroupFull
        //          - [Cause 2] .refreshOnHierarchyUpdate was set to true and the hierarchy changed
        //          - [Cause 3] .refreshOnUndoRedo was set to true and an Undo/Redo operation occurred
        //          - [Cause 4] A Script HotReload has destroyed all lists and data needs to be regathered
        //          - [Cause 5] .SetNeedsNewTargets() was called manually
        public TargetsUpdatedDelegate OnTargetsUpdated;
            
        // -- Customizable
        public bool logImportantWarnings = true;
        public bool logAllTrackerUpdates = false;
        public bool logImporantFunctions = false;
        public bool logAllFunctions = false;
        public bool refreshOnHierarchyUpdate = true;
        public bool refreshOnUndoRedo = true;
        
        // -- Internal
        private bool hasNonstandardOp = false;
        private EUpdateSource nonstandardOpSource;
        private bool needsNewTargets = true;
        private TargetClass target;
        private List<TargetClass> targets = new ();
        public TrackerGroup GroupFull { get; private set; } = new ();

        // -- Internal Getters
        public TargetClass Target => target;
        public List<TargetClass> Targets => targets;

        // -- Delegates
        public delegate void UpdateSerializedObjectsDelegate();
        public delegate void TargetsUpdatedDelegate();
        public delegate void BuildTrackersDelegate();
        public delegate void PropertiesUpdatedDelegate(EUpdateSource source);
        
        // -----------------------------------
        //      Primary Usage Methods
        // -----------------------------------
        
        // -- Enable the BetterEditor, and start listening for Unity events
        public void Enable()
        {
            Undo.undoRedoPerformed += HandleUndoRedo;
            EditorApplication.hierarchyChanged += HandleHierarchyUpdate;
        }
        
        // -- Disable the BetterEditor, and stop listening for Unity events
        public void Disable(bool permanent = true)
        {
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.hierarchyChanged -= HandleHierarchyUpdate;
            
            if(permanent)
            {
                editor = null;
                OnTargetsUpdated = null;
                OnPropsUpdated = null;
                BuildTrackers = null;
            }
        }
        public void Dispose() => Disable();
        
        // -- Update: Call this at the beginning of OnInspectorGUI() to keep things running
        public void Update()
        {
            // -- Check if Editor was Hot-Reloaded from script changes
            //        - This causes loss of some properties such as lists (it's convenient we have one!)
            var detectedEditorReload = !needsNewTargets && (targets.Count == 0);
            if (detectedEditorReload)
            {
                if (logImporantFunctions)
                    Debug.LogWarning("[BetterEditor] Detected Editor Reload!");
                SetNeedsNewTargets();
            }

            // -- Regather Target and ValidTargets from Editor (if `needsNewTargets`) and invoke delegates
            UpdateTargetsIfRequired();
        }

        // -- Force a full refresh of all targets next Update()
        public void SetNeedsNewTargets() => needsNewTargets = true;
        
        // -- Single Target Mode: Returns true if only one target is selected
        public bool SingleTargetMode() => Targets.Count == 1;
        public bool MultiTargetMode() => Targets.Count > 1;
        
        // -- CheckForUpdates: Call this after drawing the UI to check for updates. Calls
        public void CheckForUpdates()
        {
            // -- check WasUpdated all Trackers 
            var logMode = logAllTrackerUpdates ? ETrackLog.LogIfUpdated : ETrackLog.None;
            var trackersUpdated = GroupFull.WasUpdated(logMode);
            
            // -- Discontinue if no updates
            //      - SerializedObject.hasModifiedProperties check is a backup for adaptability
            if (!trackersUpdated)
                if (!editor.serializedObject.hasModifiedProperties)
                    return;
                else if(logImporantFunctions)
                    Debug.LogWarning("[BetterEditor] Detected Serialized Object Update, but the property was not tracked.");
            
            // -- Alternative, shorter logging if we didn't log all tracker updates
            if (!logAllTrackerUpdates && logImporantFunctions)
                Debug.Log("[BetterEditor] Detected Tracker Update");
            
            // -- Invoke OnPropsUpdated
            InvokeOnPropsUpdated();
        }
        
        // ----------------------------
        //      Internal
        // ----------------------------
        
        // -- Invoke OnUpdate and set needsNewTargets
        private void InvokeOnPropsUpdated()
        {
            // -- Determine Source
            var source = hasNonstandardOp ? nonstandardOpSource : EUpdateSource.GUI;
            
            // -- Log
            if (logImporantFunctions)
                Debug.Log($"[BetterEditor] OnPropsUpdated being called from source: {source}");
            
            // -- Invoke OnPropsUpdated (with warning if it's not bound)
            if (OnPropsUpdated == null && logImportantWarnings)
                Debug.LogWarning("[BetterEditor] OnPropsUpdated is not bound");
            OnPropsUpdated?.Invoke(source);
            
            // -- Set Needs New Targets
            SetNeedsNewTargets();
            
            // -- No longer have a nonstandard operation
            hasNonstandardOp = false;
        }
        
        private void UpdateTargetsIfRequired()
        {
            
            // -- Not Needed
            if(needsNewTargets == false)
                return;
            
            // -- Log
            if (logImporantFunctions)
                Debug.Log("[BetterEditor] Updating Targets...");
            
            // -- Gather valid Targets as TargetClass
            target = editor.target as TargetClass;
            targets.Clear();
            foreach (var itrTarget in editor.targets)
            {
                // -- Impossible!?
                var eachTarget = (TargetClass)itrTarget;
                if (!eachTarget)
                    continue;

                // -- Add to valid targets
                targets.Add(eachTarget);
            }
            
            // -- Invoke the delegate, notifying user of targets update
            OnTargetsUpdated?.Invoke();
            
            // -- State
            needsNewTargets = false;
            
            // -- Refresh all Tracking
            GroupFull.RefreshTracking();
        }

        // ----------------------------
        //      Handle Unity Events
        // ----------------------------
        
        private void HandleUndoRedo()
        {
            // -- Data can be incorrect in this method / thread, so process it next frame...
            if(logAllFunctions)
                Debug.Log("[BetterEditor] Detected Undo/Redo Operation!");
            
            hasNonstandardOp = true;
            nonstandardOpSource = EUpdateSource.UndoRedo;
            
            if (refreshOnUndoRedo)
                SetNeedsNewTargets();
        }
        
        private void HandleHierarchyUpdate()
        {
            // -- This calls SetNeedsNewTargets when hierarchy changes
            //         - Can cause SetNeedsNewTargets() to run twice in a row, when adding a component to your target
            //         - Can cause SetNeedsNewTargets() to run twice in a row, when the scene is marked dirty or saved
            //         - Can help trigger refreshes when things are added or removed from the scene from scripts, which
            //               is convenient for some use-cases.
            if (needsNewTargets)
                return;
            if(!refreshOnHierarchyUpdate)
                return;
            if(logAllFunctions)
                Debug.Log("[BetterEditor] Hierarchy Changed, Refreshing Targets...");
            SetNeedsNewTargets();
        }
        
        
    }
}