using System.Collections.Generic;
using System.Text.RegularExpressions;
using BetterEditor;
using UnityEditor;
using UnityEngine;



namespace BetterEditorDemos
{
    
    
    // -- Separating out a "Mini-Editor" for 'SpheresDemo_ColorData' properties
    
    //       - Ideally, this would be in its own file, but for the demo it's here.
    //       - Most importantly, uses a "TrackerGroup"
    //          - Trackers are created for each property in `SpheresDemo_ColorData`
    //          - The group is created as a "RelativeTracker" and all Trackers are added to it
    //          - Trackers are ideally added via reflection, to save typing when adding/removing/updating properties 
    //              in the target file.
    //       - Uses ITrack (an optional interface)
    //           - ITrack provides cleaner access to the group's core ITrack methods
    //                (Track, WasUpdated, RefreshTracking)
    //           - ITrack interface also allows this class to be gathered using reflection (show below)
    
    public class Color_TrackAndDraw_05 : ITrack, IDrawUI // [Alternatively: ITrackAndDraw] 
    {
        // -- Target Info
        private static readonly SpheresDemo_ColorData COLOR;
        public TrackerGroup group = new (typeof(SpheresDemo_ColorData));
        private GUIContent content;

        // -- Trackers (Gathered via Reflection)
        public Tracker use = new(nameof(COLOR.use), SerializedPropertyType.Boolean);
        public Tracker color = new(nameof(COLOR.color));
        
        // -- Constructor (Prepare the group)
        public Color_TrackAndDraw_05 (string propName, GUIContent content = null)
        {
            group.SetAsRelativeTracker(propName);
            group.PopulateWithReflection(this);
            this.content = content;
        }
        
        // -- Draw Single Row, a custom method.
        public void DrawSingleRow()
        {
            // -- Throw error if group has not begun tracking
            group.CheckTracking();
            
            // -- Get Content from the property if it hasn't been gathered or set yet (This could also be done in Track method)
            content ??= group.prop.GetGUIContent();
            
            // -- Using some of the BetterEditorGUI functions, to compose a custom row. 
            //      - The serialized property representing the entire SpheresDemo_ColorData (group.prop) is used
            //         as the right-click target for the entire row, for revert
            BetterEditorGUI.DrawCustomRow(group.prop, (builder) =>
            {
                BetterEditorGUI.LabelInRow(builder, content);
                BetterEditorGUI.ToggleInRow(builder, use.prop);
                if (!use.prop.AnyTrue())
                    return;
                using (new EditorGUI.DisabledScope(!use.prop.AnyTrue()))
                    BetterEditorGUI.ColorInRow(builder, color.prop, -1);
            });
        }
        
        // -- [IDraw] Example of drawing a struct/class with a default copy-paste foldout header, but with custom contents
        public void Draw()
        {
            // -- Throw error if group has not begun tracking
            group.CheckTracking();
            
            // -- Get Content from the property if it hasn't been gathered or set yet (This could also be done in Track method)
            content ??= group.prop.GetGUIContent();
            
            // -- Draws a copy+paste enabled foldout header, then the inner "DrawNoHeader()" method, indented.
            EditorGUILayout.PropertyField(group.prop, content, false);
            
            // -- Draw custom inner, if expanded
            if (group.prop.isExpanded)
                using (new EditorGUI.IndentLevelScope())
                    DrawNoHeader();
        }

        // -- [IDraw] Draws the custom contents of this struct/class, without the header. 
        public void DrawNoHeader()
        {
            // -- Throw error if group has not begun tracking
            group.CheckTracking();
            
            // -- Draw "Use" and "Color" as two different lines
            EditorGUILayout.PropertyField(use.prop);
            using (new EditorGUI.DisabledScope(use.prop.AllFalse())) // -- disable "color" if "use" is all false.
                EditorGUILayout.PropertyField(color.prop);
        }

        // -- [ITrack] Interface methods, they just forward to the group. 
        public void Track(TrackSource source) { group.Track(source); }
        public bool WasUpdated(ETrackLog log = ETrackLog.None) { return group.WasUpdated(log); }
        public void RefreshTracking() { group.RefreshTracking(); }
    }
    
    

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpheresDemo_05))]
    public class SpheresDemoEditor_05 : Editor
    {
        // -- Target
        private static readonly SpheresDemo_05 TARGET;
        
        // -- An extra content for the color override
        private static GUIContent objectColorContent = new GUIContent("Override Color", "Override the first material's color with a custom color");
        
        // -- Trackers and Sub-Editors
        private Tracker enablePreviewTracker = new(nameof(TARGET.enablePreview));
        private Color_TrackAndDraw_05 previewColor = new( nameof(TARGET.previewColor) ); // (defined above)
        private Tracker seedTracker = new( nameof(TARGET.seed) );
        private Tracker radiusTracker = new( nameof(TARGET.radius) );
        private Tracker totalToGenerateTracker = new( nameof(TARGET.totalToGenerate) );
        private ListTracker objectPrefabsTracker = new( nameof(TARGET.objectPrefabs) );
        private Color_TrackAndDraw_05 objectColorTracker = new( nameof(TARGET.objectColor), objectColorContent); // (defined above)
        
        // -- Extra Props
        private SerializedProperty createdObjectsProp;
        private SerializedProperty hasModificationsProp;
        
        // -- Main Group
        private TrackerGroup allTrackers = new(typeof(SpheresDemo_05));
        
        // -- Tracker Collections (So we can check which category was updated)
        private ITrack[] previewTrackers;
        private ITrack[] importantTrackers;
        
        public void OnEnable()
        {
            // -- Get all (Non-Group) ITrack objects via reflection (The 4 trackers and 2 Color_TrackAndDraw_05 ITrack objects)
            allTrackers.PopulateWithReflection(this);

            // -- Setup Different collections to track changes to different sets of data
            previewTrackers = new ITrack[] { enablePreviewTracker, previewColor };
            importantTrackers = new ITrack[] { seedTracker, radiusTracker, totalToGenerateTracker, objectPrefabsTracker, objectColorTracker };

            // -- Other Serialized
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
            createdObjectsProp = serializedObject.FindPropertyChecked(nameof(TARGET.createdObjects));

            // -- Begin Tracking
            RefreshTracking();
        }
        
        public void RefreshTracking()
        {
            allTrackers.Track(serializedObject.AsSource());
        }

        // -- Unity->OnInspectorGUI
        public override void OnInspectorGUI()
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = SpheresDemoEditors.DrawInfoAndSwitcher(Info);
            if(updatedStage) return;
            
            // -- Update Serialized Object
            serializedObject.Update();
            
            // -- Draw the modifications Row
            var pressedApply = SpheresDemoEditors.DrawModifyWarningRowSerialized(hasModificationsProp);
            if (pressedApply)
            {
                // -- Do the actual logic to apply the changes
                //       - In this demo, we're setting hasModifiedProperties to false directly in the objects code, it's easier
                //          and we're done teaching how serialized properties work.
                SpheresDemoEditors.Distribute(targets, true); 
            }
            
            // -- Throw error if group has not been given a started Tracking yet
            allTrackers.CheckTracking();
            
            // -- Draw the Editor
            DrawMainUI();
            
            // -- Enforce Ranges
            EnforceRanges();
            
            // -- Handle Preview Updated
            //     (We can handle updates externally and internally (pretty much anywhere), as long as the element we're checking has been drawn)
            if(enablePreviewTracker.WasUpdated())
                HandlePreviewUpdated();

            // -- Were important properties updated? (those that should trigger a "modifications detected" me ssage)
            var updatedImportant = importantTrackers.WasUpdated();
            if (updatedImportant)
                HandleDetectModifications();
            
            // -- Check (and Log) if anything was updated!
            var updated_Any = allTrackers.WasUpdated(ETrackLog.LogIfUpdated);
            if (updated_Any)
                RefreshTracking();
            
            // -- Apply!
            serializedObject.ApplyModifiedProperties();
        }
        
        protected virtual void DrawMainUI()
        {
            
            // -- Draw Preview Props
            EditorGUILayout.PropertyField(enablePreviewTracker.prop, enablePreviewTracker.content);
            if(enablePreviewTracker.prop.AnyTrue())
                using(new EditorGUI.IndentLevelScope())
                    previewColor.DrawSingleRow();
                
            // -- Draw Zone Props
            using(new IndentEditorLabelFieldScope("Primary Props"))
            {
                EditorGUILayout.PropertyField(seedTracker.prop);
                EditorGUILayout.PropertyField(totalToGenerateTracker.prop);
                EditorGUILayout.PropertyField(radiusTracker.prop);
            }

            // -- Draw Objects Props
            using(new IndentEditorLabelFieldScope("Objects to Distribute"))
            {
                objectColorTracker.Draw();
                BetterEditorGUI.ListPropertyField(objectPrefabsTracker.prop, objectPrefabsTracker.content, FontStyle.Normal, true);
            }
            
            // -- Draw list of all created objects
            using( new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(createdObjectsProp);
        }
        
        private void HandlePreviewUpdated()
        {
            Debug.Log($"Better Editor Demo: EnablePreview updated to {enablePreviewTracker.prop.AnyTrue()} ");
            
            // -- Example: Set Color.use to false when preview is disabled
            if (enablePreviewTracker.prop.AllFalse())
                previewColor.use.prop.boolValue = false;
        }

        // -- Enforce Ranges
        private void EnforceRanges()
        {
            totalToGenerateTracker.prop.EnforceClamp(4, 100);
            seedTracker.prop.EnforceMinimum(0);
        }
        
        private void HandleDetectModifications()
        {
            Debug.Log($"Better Editor Demo: Detected Modifications!");
            
            //  In This Demo: hasModification updates are not part of the undo chain, undo will not revert them to false.
            //      To accomplish this we apply any GUI updates first, regularly, then we push another silent update
            serializedObject.ApplyModifiedProperties();
            hasModificationsProp.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.UsingGroups,
            title = "Reusable Editors: Groups and ITrackAndDraw",
            description = "Uses TrackerGroups and separate 'Mini-Editors'\n\n"+
                          "In this example, a separate class is created to track and draw ColorData, then used twice.\n"+
                          "Similarly, a separate class is created to track/draw the full component, as an example",
            greenTexts = new List<string>()
            {
                "Define a single tracking layout for a given class or struct, with methods to draw it, forming a complete mini-editor.",
                "Use some of BetterEditor's GUI methods to compose a custom row",
            },
            redTexts = new List<string>()
            {
            },
            fileName = "SpheresDemoEditor_05.cs",
        };
    }
}