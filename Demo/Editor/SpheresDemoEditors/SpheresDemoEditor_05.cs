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
        public TrackerCollection collection = new (typeof(SpheresDemo_ColorData));

        // -- Trackers (Gathered via Reflection)
        public Tracker use = new(nameof(COLOR.use), SerializedPropertyType.Boolean);
        public Tracker color = new(nameof(COLOR.color));
        
        // -- Constructor (Prepare the collection)
        public Color_TrackAndDraw_05(in string propNameIn)
        {
            collection.PopulateWithReflection(this);
            collection.SetPropName(propNameIn);
        }

        // -- [ITrack] Interface (just use the collection)
        public void Track(TrackSource source) { collection.Track(source); }
        public bool WasUpdated(TrackLogging log = TrackLogging.None) { return collection.WasUpdated(log); }
        public void RefreshTracking() { collection.RefreshTracking(); }
        
        // -- Draw Single Row (Alternate GUI Draw, good for 2 or 3 property classes)
        public void DrawSingleRow(GUIContent content = null)
        {  
            collection.CheckTracking();
            using (new EditorGUILayout.HorizontalScope())
            {
                // -- Use BetterEditorGUI.ToggleRow to draw a row with a toggle on the left
                //      ("use" powers the toggle, but color is the copy-paste target)
                BetterEditorGUI.ToggleRow(use.prop, color.prop, content ?? use.content, true, false, 150);
                
                // -- Inline "Color" prop (disabled if "Use" is false)
                using (new EditorGUI.DisabledScope(use.prop.AllFalse()))
                using (new EditorGUI.IndentLevelScope())
                    BetterEditorGUI.Property(color.prop, GUIContent.none);
            }
            
        }
    }

    public class SpheresDemo_TrackAndDraw_05 : ITrackAndDraw
    {
        // -- Target Info
        private static readonly SpheresDemo COMPONENT;
        public TrackerCollection collectionAll = new ( typeof(SpheresDemo) );
        
        // -- Trackers and Sub-Editors
        public Tracker enablePreviewTracker = new(nameof(COMPONENT.enablePreview));
        private Color_TrackAndDraw_05 previewColor = new(nameof(COMPONENT.previewColor));
        private Tracker seedTracker = new( nameof(COMPONENT.seed) );
        private Tracker radiusTracker = new( nameof(COMPONENT.radius) );
        private Tracker numSpheresTracker = new( nameof(COMPONENT.numResults) );
        private Color_TrackAndDraw_05 sphereColor = new( nameof(COMPONENT.sphereColor) );
        
        // -- Extra Collections
        public TrackerCollection previewTrackers = new ();
        public TrackerCollection trackersWhichCauseModifications = new ();
        
        // -- Constructor
        public SpheresDemo_TrackAndDraw_05()
        {
            collectionAll.PopulateWithReflection(this);
            
            previewTrackers.Set(enablePreviewTracker, previewColor);

            trackersWhichCauseModifications.Clear();
            trackersWhichCauseModifications.Add(seedTracker);
            trackersWhichCauseModifications.Add(radiusTracker);
            trackersWhichCauseModifications.Add(numSpheresTracker);
            trackersWhichCauseModifications.Add(sphereColor);
        }
        
        // -- [ITrack] Interface (just use the collection)
        public void Track(TrackSource source) { collectionAll.Track(source); }
        public bool WasUpdated(TrackLogging log = TrackLogging.None) { return collectionAll.WasUpdated(log); }
        public void RefreshTracking() { collectionAll.RefreshTracking(); }
        
        // -- [IDraw] Draw the UI
        public void Draw(GUIContent mainContent)
        {
            // - Draws a copy+paste enabled foldout header, then DrawNoHeader() indented.
            EditorGUILayout.PropertyField(collectionAll.prop, mainContent);
            using (new EditorGUI.IndentLevelScope())
                DrawNoHeader();
        }

        // -- [IDraw] Draw the UI without the header
        public void DrawNoHeader()
        {
            // -- Throw error if collection has not been given a tracking Property yet (using .TrackRelative())
            collectionAll.CheckTracking();
            
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
        }

        // -- Enforce Ranges
        private void EnforceRanges()
        {
            numSpheresTracker.prop.EnforceClamp(4, 100);
            seedTracker.prop.EnforceMinimum(0);
        }
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
            
            // -- Draw the modifications Row using the serialized property for hasModifications
            //      (in this demo: hasModification updates are not part of the undo chain, undo will not revert them)
            var pressedApply = SpheresDemoEditors.DrawModifyWarningRowSerialized(hasModificationsProp);
            if (pressedApply)
            {
                hasModificationsProp.boolValue = false;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            // -- Draw the Editor twice, because we can!
            componentEditor.DrawNoHeader();
            
            // -- Check (and Log) if anything was updated!
            var updated_Any = componentEditor.trackersWhichCauseModifications.WasUpdated(TrackLogging.LogIfUpdated);
            if (updated_Any)
            {
                RefreshTracking();
                
                // - in this demo: hasModification updates are not part of the undo chain, undo will not revert to false)
                serializedObject.ApplyModifiedProperties();
                hasModificationsProp.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            
            // -- Apply!
            serializedObject.ApplyModifiedProperties();
        }
        
        
        // -- Info about this demo
        private static readonly SpheresDemoInfo Info = new()
        {
            stage = ESpheresDemoStages.TrackAndDrawCollections,
            title = "Reusable Editors: Collections and ITrackAndDraw",
            description = "Uses ITrack interface and TrackerCollection to create reusable 'Mini-Editors'\n\n"+
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