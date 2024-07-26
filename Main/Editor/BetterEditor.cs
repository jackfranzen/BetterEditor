// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)


using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace BetterEditor
{
    
    public enum UpdateSource
    {
        User,
        AutoDetect,
        UndoRedo,
    }
    
    public class BetterEditor <TargetClass> where TargetClass : UnityEngine.Object
    {
        // -- Required Param: The editor whose targets we will parse
        public Editor editor;
        
        // -- Required Delegate: Build all Trackers and Serialized Properties from serializedObject (or any other optional sources.) 
        public BuildTrackersDelegate BuildTrackers;
        
        // -- Required Delegate: Perform any updates in reaction to updated trackers
        //      - Argument 1: UpdateSource, the source of the update (User, AutoDetect, UndoRedo)
        public PropertiesUpdatedDelegate HandlePropertiesUpdated;
        
        // -- Almost a required Delegate: Called when Target and ValidTargets are updated
        public TargetsUpdatedDelegate HandleTargetsUpdated;
            
        // -- Customizable
        public bool logImportantWarnings = true;
        public bool logAllTrackerUpdates = false;
        public bool logImporantFunctions = false;
        public bool logAllFunctions = false;
        public bool refreshOnHierarchyUpdate = true;
        public bool refreshOnSelectionChange = true;
        
        // -- Internal
        private bool hasNonstandardOp = false;
        private UpdateSource nonstandardOpSource;
        private bool needsFullRefresh = true;
        private TargetClass target;
        private List<TargetClass> validTargets = new ();
        public SerializedObject serializedObject { get; private set; }
        public TrackerCollectionFull CollectionFull { get; private set; } = new ();
        public HashSet<SerializedObject> allSerializedObjects  { get; private set; } = new ();

        // -- Internal Getters
        public TargetClass Target => target;
        public List<TargetClass> ValidTargets => validTargets;

        // -- Delegates
        public delegate void UpdateSerializedObjectsDelegate();
        public delegate void TargetsUpdatedDelegate();
        public delegate void BuildTrackersDelegate();
        public delegate void PropertiesUpdatedDelegate(UpdateSource source);
        
        // -----------------------------------
        //      Primary Usage Methods
        // -----------------------------------
        
        // -- Enable the BetterEditor, and start listening for Unity events
        public void Enable()
        {
            Selection.selectionChanged += HandleSelectionChanged;
            Undo.undoRedoPerformed += HandleUndoRedo;
            EditorApplication.hierarchyChanged += HandleHierarchyUpdate;
        }
        
        // -- Disable the BetterEditor, and stop listening for Unity events
        public void Disable()
        {
            serializedObject?.Dispose();
            
            Selection.selectionChanged -= HandleSelectionChanged;
            Undo.undoRedoPerformed -= HandleUndoRedo;
            EditorApplication.hierarchyChanged -= HandleHierarchyUpdate;
        }
        
        // -- Update: Call this at the beginning of OnInspectorGUI() to keep things running
        public void Update()
        {

            // -- Check if Editor was Hot-Reloaded from script changes
            //        - This causes loss of some properties such as lists (it's convenient we have one!)
            var detectedEditorReload = !needsFullRefresh && (validTargets.Count == 0);
            if (detectedEditorReload)
            {
                if (logImporantFunctions)
                    Debug.LogWarning("Detected Editor Reload!");
                SetNeedsFullRefresh();
            }

            // -- Do we need a full Refresh?
            if (needsFullRefresh)
            {
                // -- Regather Target and ValidTargets from Editor, and invoke delegates
                FullRefresh();
                needsFullRefresh = false;
            }
            // -- If we didn't do a full refresh, just update serialized objects.
            else
                UpdateSerializedObjects();
            
            // -- Auto-Detect updates from paste or revert
            //      - We have to check everything, every frame, to detect this...
            //      - At least it makes for convenient logging...
            if (!hasNonstandardOp)
            {
                // -- Find all changes to tracked serialized Properties, On tick!
                var logMode = logAllTrackerUpdates ? TrackLogging.LogIfUpdated : TrackLogging.None;
                var trackersUpdated = CollectionFull.WasUpdated(logMode);
                if (trackersUpdated && !logAllTrackerUpdates && logImporantFunctions)
                    Debug.Log("Better Editor: Detected Tracker Update");

                // -- If we detect tracker updates but not object updates, this is a paste operation. (Or a revert operation!)
                //      There is no better way to detect this, there is no hook, we must check all trackers every frame. Thanks Unity!
                //      If there is a change, it will be treated similar to undo / redo 
                var autoDetectChange = trackersUpdated; //&& areObjectsUpdated == false; [CHECK]
                if (autoDetectChange)
                {
                    if(logImporantFunctions)
                        Debug.LogWarning("BetterEditor: Detected Automatic changes (could be paste or revert)!");
                    hasNonstandardOp = true;
                    nonstandardOpSource = UpdateSource.AutoDetect;
                
                    // -- Pasted info won't update inside a selected box, so we must manually deselect.
                    GUI.FocusControl(null);
                }
            }

            // -- Apply a Nonstandard op if we have one
            //      - This is for Undo/Redo, Paste, or Revert operations.
            if (hasNonstandardOp)
            {
                // if(logAllTrackerUpdates)
                //     AnyTrackersUpdated(SerializedTrackerLogging.LogIfUpdated);
                HandlePropertiesUpdatedAndDoFullRefresh(nonstandardOpSource);
                hasNonstandardOp = false;
                return;
            }
        }

        public void ApplyModifiedProperties(bool forceGroupUndo = false, in string undoGroupName = default)
        {
            // -- Detect Changes to the serialized Object. 
            int numModified = NumSerializedObjectsModified();
            if (numModified <= 0)
                return;
            
            // -- Automatically group the undo when multiple serialized objects are modified
            var modifiedMultipleSerializedObjects = numModified > 1;
            if(modifiedMultipleSerializedObjects && logImporantFunctions)
                Debug.LogWarning($"BetterEditor: Modified {numModified} Serialized Objects, using group Undo.");
                    
            // -- Begin Group Undo
            var undoGroup = 0;
            var useUndoGroup = forceGroupUndo || modifiedMultipleSerializedObjects;
            if (useUndoGroup)
            {
                var hasGroupName = !string.IsNullOrEmpty(undoGroupName);
                var groupName = hasGroupName ? undoGroupName : $"{typeof(TargetClass).Name} group update";
                Undo.SetCurrentGroupName(groupName);
                Undo.GetCurrentGroup();
            }

            // -- ApplyModifiedProperties to all Serialized Objects
            foreach (var eachSerialized in allSerializedObjects)
                eachSerialized.ApplyModifiedProperties();
            
            // -- Log full Tracker Update
            if(logAllTrackerUpdates)
                CollectionFull.WasUpdated(TrackLogging.LogIfUpdated);
            
            // -- Perform User Update
            HandlePropertiesUpdatedAndDoFullRefresh(UpdateSource.User);
            
            // -- Finish Group Undo
            if(useUndoGroup)
            {
                Undo.CollapseUndoOperations(undoGroup);
                Undo.IncrementCurrentGroup();
            }
        }
        
        // -- Force a full refresh of all targets next Update()
        public void SetNeedsFullRefresh() => needsFullRefresh = true;
        
        // -- Single Target Mode: Returns true if only one target is selected
        public bool SingleTargetMode() => ValidTargets.Count == 1;
        
        // -- Update Serialized Objects: Call this to update all serialized objects, though you shouldn't need to. 
        public void UpdateSerializedObjects()
        {
            foreach (var eachSerialized in allSerializedObjects)
                eachSerialized.Update();
        }
        
        public int NumSerializedObjectsModified()
        {
            var numModified = allSerializedObjects.Count(eachSerialized => eachSerialized.hasModifiedProperties);
            return numModified;
        }
        
        public bool hasModifiedProperties => NumSerializedObjectsModified() > 0;

        
        // ----------------------------
        //      Internal
        // ----------------------------
        
        
        
        private void HandlePropertiesUpdatedAndDoFullRefresh(UpdateSource source)
        {
            if(HandlePropertiesUpdated == null)
            {
                if (logImportantWarnings)
                    Debug.LogWarning("BetterEditor: PerformUpdate Delegate is null");
                return;
            };
            
            if (logImporantFunctions)
                Debug.LogWarning($"BetterEditor: Properties Updated from {source}");
            
            HandlePropertiesUpdated.Invoke(source);
            
            SetNeedsFullRefresh();
        }
        
        private void FullRefresh()
        {
            // -- Log
            if (logImporantFunctions)
                Debug.Log("Doing Full Refresh....");
            
            // -- Gather valid Targets as TargetClass
            target = editor.target as TargetClass;
            validTargets.Clear();
            foreach (var itrTarget in editor.targets)
            {
                // -- Impossible!?
                var eachTarget = (TargetClass)itrTarget;
                if (!eachTarget)
                    continue;

                // -- Add to valid targets
                validTargets.Add(eachTarget);
            }
            
            // -- Invoke the delegate, notifying user of targets update
            HandleTargetsUpdated?.Invoke();
            
            // -- Create Fresh Serialized Object (Direct replacement for Editor.serializedObject)
            serializedObject?.Dispose();
            serializedObject = new SerializedObject(validTargets.ToArray());
            
            // -- Build Trackers via the Delegate
            if(BuildTrackers != null)
            {
                // -- Reset Tracker Arrays
                allSerializedObjects.Clear();
                allSerializedObjects.Add(serializedObject);
                CollectionFull.Clear();
                
                BuildTrackers.Invoke();
                if (logImportantWarnings && CollectionFull.IsEmpty)
                    Debug.LogWarning("BetterEditor: BuildTrackers did not repopulate collection, can't detect automatically...");
            }
            else if (logImportantWarnings)
                Debug.LogWarning("BetterEditor: BuildTrackers Delegate is null");
            
        }
        
        // ----------------------------
        //      Handle Unity Events
        // ----------------------------
        private void HandleSelectionChanged()
        {
            if(!refreshOnSelectionChange)
                return;
            if(logAllFunctions)
                Debug.Log("BetterEditor: Selection Changed, Refreshing Targets...");
            SetNeedsFullRefresh();
        }
        
        private void HandleUndoRedo()
        {
            // -- React to Undo on next tick, in main thread...
            if(logImporantFunctions)
                Debug.LogWarning("BetterEditor: Detected Undo/Redo Operation!");
            hasNonstandardOp = true;
            nonstandardOpSource = UpdateSource.UndoRedo;
        }
        
        private void HandleHierarchyUpdate()
        {
            // -- Note: this could be component change, like adding or deleting a renderer somewhere.
            if(!refreshOnHierarchyUpdate)
                return;
            if(logAllFunctions)
                Debug.Log("BetterEditor: Hierarchy Changed, Refreshing Targets...");
            SetNeedsFullRefresh();
        }
        
        
    }
}