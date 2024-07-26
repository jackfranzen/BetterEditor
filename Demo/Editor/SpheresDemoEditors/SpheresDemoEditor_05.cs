using System;
using System.Collections.Generic;
using BetterEditor;
using BetterEditorDemos;
using UnityEditor;
using UnityEngine;



namespace BetterEditorDemos
{
    
    
    public class Color_TrackAndDraw_05 : ITrack
    {
        // -- Target Info
        private static readonly SpheresDemo_ColorData COLOR;
        public TrackingGroup group = new (typeof(SpheresDemo_ColorData));

        // -- Trackers (Gathered via Reflection)
        public Tracker use = new(nameof(COLOR.use), SerializedPropertyType.Boolean);
        public Tracker color = new(nameof(COLOR.color));
        
        // -- Constructor (Prepare the group)
        public Color_TrackAndDraw_05 (string propName)
        {
            group.SetAsRelativeTracker(propName);
            group.PopulateWithReflection(this);
        }
        
        // -- Draw Single Row (Alternate GUI Draw, good for 2 or 3 property classes)
        public void DrawSingleRow(GUIContent content = null)
        {
            // EditorGUILayout.PropertyField(color.prop);
            // EditorGUILayout.ColorField(color.prop.GetGUIContent(), color.prop.colorValue, true, false, true);
            // EditorGUI.ColorField(color);
            
            group.CheckTracking();
            using (new EditorGUILayout.HorizontalScope())
            {
                // -- Use BetterEditorGUI.ToggleRow to draw a row with a toggle on the left
                //      ("use" powers the toggle, but color is the copy-paste target)
                BetterEditorGUI.ToggleRow(use.prop, color.prop, content ?? use.content, true, false, 150);
                
                // -- Inline "Color" prop (disabled if "Use" is false)
                using (new EditorGUI.DisabledScope(use.prop.AllFalse()))
                using (new EditorGUI.IndentLevelScope())
                // using(new EditorGUILayout.HorizontalScope(GUILayout.Width(80))) // -- Wrap it one more time, because color-field likes to float right...
                    BetterEditorGUI.Property(color.prop, GUIContent.none);
            }
        }

        // -- [ITrack] Interface (just use the group)
        public void Track(TrackSource source) { group.Track(source); }
        public bool WasUpdated(TrackLogging log = TrackLogging.None) { return group.WasUpdated(log); }
        public void RefreshTracking() { group.RefreshTracking(); }
    }

    public class SpheresDemo_TrackAndDraw_05 : ITrackAndDraw
    {
        // -- Target Info
        private static readonly SpheresDemo COMPONENT;
        public TrackingGroup group = new ();
        
        // -- Trackers and Sub-Editors
        public Tracker enablePreviewTracker = new(nameof(COMPONENT.enablePreview));
        private Color_TrackAndDraw_05 previewColor = new( nameof(COMPONENT.previewColor) );
        private Tracker seedTracker = new( nameof(COMPONENT.seed) );
        private Tracker radiusTracker = new( nameof(COMPONENT.radius) );
        private Tracker numSpheresTracker = new( nameof(COMPONENT.numResults) );
        private Color_TrackAndDraw_05 sphereColor = new( nameof(COMPONENT.sphereColor) );
        
        // -- Tracker Collections (So we can check which category was updated)
        public ITrack[] previewTrackers;
        public ITrack[] importantTrackers;
        
        // -- Constructor
        public SpheresDemo_TrackAndDraw_05 ()
        {
            // -- Get all (Non-Group) ITrack objects via reflection (The 6 trackers and editors)
            group.PopulateWithReflection(this);

            // -- Setup Collections for convenience
            previewTrackers = new ITrack[] { enablePreviewTracker, previewColor };
            importantTrackers = new ITrack[] { seedTracker, radiusTracker, numSpheresTracker, sphereColor };
        }
        
        // -- [IDraw] Draw the UI
        public void Draw(GUIContent mainContent)
        {
            // - Draws a copy+paste enabled foldout header, then DrawNoHeader() indented.
            EditorGUILayout.PropertyField(group.prop, mainContent);
            using (new EditorGUI.IndentLevelScope())
                DrawNoHeader();
        }

        // -- [IDraw] Draw the UI without the header
        public void DrawNoHeader()
        {
            // -- Throw error if group has not been given a tracking Property yet (using .TrackRelative())
            group.CheckTracking();
            
            // -- Draw Preview Props
            using(new IndentEditorLabelFieldScope("Preview Props:"))
            {
                BetterEditorGUI.ToggleRow(enablePreviewTracker.prop, new GUIContent("Enable Preview"));
                using (new EditorGUI.DisabledScope(enablePreviewTracker.prop.AllFalse()))
                    previewColor.DrawSingleRow();
            }
                
            // -- Draw Distribution Props
            using(new IndentEditorLabelFieldScope("Distribution Props:"))
            {
                EditorGUILayout.PropertyField(seedTracker.prop);
                EditorGUILayout.PropertyField(radiusTracker.prop);
            }

            // -- Draw Spheres Props
            using(new IndentEditorLabelFieldScope("Spheres Props:"))
            {
                EditorGUILayout.PropertyField(numSpheresTracker.prop);
                sphereColor.DrawSingleRow();
            }
            
            // -- Enforce Ranges
            EnforceRanges();
            
            // -- Handle Preview Updated
            //     (We can handle updates externally and internally (pretty much anywhere), as long as the element we're checking has been drawn)
            if(enablePreviewTracker.WasUpdated())
                HandlePreviewUpdated();
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
            numSpheresTracker.prop.EnforceClamp(4, 100);
            seedTracker.prop.EnforceMinimum(0);
        }
        
        // -- [ITrack] Interface (just uses the group)
        public void Track(TrackSource source) { group.Track(source); }
        public bool WasUpdated(TrackLogging log = TrackLogging.None) { return group.WasUpdated(log); }
        public void RefreshTracking() { group.RefreshTracking(); }
    }
    



    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpheresDemo_05))]
    public class SpheresDemoEditor_05 : Editor
    {
        
        private SerializedProperty hasModificationsProp;
        private SpheresDemo_TrackAndDraw_05 componentEditor = new();
        
        public void OnEnable()
        {
            // -- Get Serialized Properties
            hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");

            // -- Track!
            componentEditor.Track(serializedObject.AsSource());
        }
        
        public void RefreshTracking()
        {
            componentEditor.RefreshTracking();
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
                hasModificationsProp.boolValue = false;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            // -- Draw the Editor
            componentEditor.DrawNoHeader();
            
            var updatedImportant = componentEditor.importantTrackers.WasAnyUpdated(TrackLogging.LogIfUpdated);
            if (updatedImportant)
                HandleDetectModifications();
            
            // -- Check (and Log) if anything was updated!
            var updated_Any = componentEditor.WasUpdated(TrackLogging.LogIfUpdated);
            if (updated_Any)
            {
                RefreshTracking();
            }
            
            // -- Apply!
            serializedObject.ApplyModifiedProperties();
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
            description = "Uses ITrack interface and TrackerGroups to create reusable 'Mini-Editors'\n\n"+
                          "In this example, an ITrack class is created for ColorData and used twice.\n"+
                          "An ITrackAndDraw class is created for the component, which isn't necessary but can "+
                          "be compact and convenient, especially for future refactors",
            greenTexts = new List<string>()
            {
                "Define a single tracking layout for a given class or struct, with methods to draw it, forming a complete mini-editor!",
            },
            redTexts = new List<string>()
            {
            },
            fileName = "SpheresDemoEditor_05.cs",
        };
    }
}