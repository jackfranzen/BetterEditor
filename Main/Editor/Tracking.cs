// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    
    // -- Logging Level
    //      (Any logging causes iteration to continue after hitting true results in WasUpdated)
    public enum TrackLogging
    {
        None = 0,
        Log = 1,
        LogIfUpdated = 2,
    }
    
    
    // -- TrackSource
    //     - Attempts to ease headaches between SerializedObject's.FindProperty() and
    //       SerializedProperty's.FindRelativeProperty().
    public struct TrackSource
    {
        public SerializedObject sObject;
        public SerializedProperty sParentProp;
        public bool sourceIsObject;
        
        
        // -- Track from Object
        //      @isTopLevelIn: If true, we are sending an Object into tracking
        public TrackSource(SerializedObject sObjectIn)
        {
            if (sObjectIn == null)
                throw new Exception("SerializedObject provided to TrackSource was null");
            sourceIsObject = true;
            sObject = sObjectIn;
            sParentProp = null;
        }
        public TrackSource(SerializedProperty sParentPropIn)
        {
            if (sParentPropIn == null)
                throw new Exception("SerializedProperty provided to TrackSource was null");
            sourceIsObject = false;
            sObject = null;
            sParentProp = sParentPropIn;
        }

        public string GetLogStuff()
        {
            if (sourceIsObject)
                return $"(TrackSource: object {sObject.targetObject.GetType().Name})";
            return $"(TrackSource: property {sParentProp.displayName})";
        }

        public SerializedProperty FindProperty(in string propName)
        {
            if (string.IsNullOrEmpty(propName))
                throw new Exception("Cannot find a property without a name");
            
            return sourceIsObject ? sObject.FindPropertyChecked(propName) : sParentProp.FindRelativeChecked(propName);
        }
    }

    public static class TrackingHelperExtensions
    {
        
        // -- SERIALIZED-PROPERTY: Get as TrackSource quickly
        public static TrackSource AsSource(this SerializedProperty sProp)
        {
            return new TrackSource(sProp);
        }
        
        // -- SERIALIZED-OBJECT: Get as TrackSource quickly
        public static TrackSource AsSource(this SerializedObject sObject)
        {
            return new TrackSource(sObject);
        }

        // -- IENUMERABLE<ITRACK>: WasUpdated
        public static bool WasAnyUpdated(this IEnumerable<ITrack> trackersToCheck, TrackLogging log = TrackLogging.None)
        {
            if (!trackersToCheck.Any())
                throw new Exception($"WasUpdated() called on an empty set!");

            var wasUpdated = false;
            foreach (var tracker in trackersToCheck)
            {
                // -- Skip if not updated
                if (!tracker.WasUpdated(log))
                    continue;
                
                // -- Immediately return true (if not logging)
                if (log == TrackLogging.None)
                    return true;
                
                // -- Otherwise, return at the end. 
                wasUpdated = true;
            }
            return wasUpdated;
        }

    }
    
    // -- ITrack allows for multiple layers of tracking, eventually arriving at a Tracker. 
    //         - [User Collections] -> [User ITrackAndDraw] -> [Collection] -> [Tracker]
    public interface ITrack
    {
        void Track(TrackSource source);  // -- trackers receive a source serialized Object or Property
        bool WasUpdated(TrackLogging log = TrackLogging.None);
        void RefreshTracking(); // -- Reset state, WasUpdated is now false!
    }
    
    // -- Draw a complete collection's UI, with or without the header. 
    public interface IDrawUI
    {
        void Draw(GUIContent content = null); // null generally uses the default, while GUIContent.None is empty. 
        void DrawNoHeader();
    }

    public interface ITrackAndDraw : ITrack, IDrawUI
    {
    }

    
    // -- All Tracker Types are given an unchanging property name to find a serialized Property by later. 
    //        - A Tracker keeps a reference to the fully realized SerializedProperty "prop" (once Track() has been called)
    //        - A Tracker keeps a Reference to GUIContent (label + tooltip) for convenience. 
    //              (will auto-fill from [DisplayName()] and [Tooltip()] if provided)
    public abstract class TrackerAbstract : ITrack
    {
        // -- Props
        protected readonly string propName;
        public GUIContent content;
        public SerializedProperty prop {get; private set;}
        
        // -- Constructors
        public TrackerAbstract( in string targetPropName, in GUIContent contentIn) : this(targetPropName)
        {
            content = contentIn;
        }
        public TrackerAbstract( in string targetPropName)
        {
            propName = targetPropName;
        }
        
        // -- ITrack Methods
        public void Track(TrackSource source)
        {
            // -- The end point for all tracking operations, we find our property from source 
            TrackDirect( source.FindProperty(propName) );
        }
        public abstract bool WasUpdated(TrackLogging log = TrackLogging.None);
        public abstract void RefreshTracking();
        
        
        // -- Internal Tracking
        public void TrackDirect(SerializedProperty sProp)
        {
            prop = sProp;
            if (prop == null)
                throw new Exception($"{GetType()}->Track() given invalid property");
            if (ValidFor(prop) == false)
                throw new Exception(InvalidMessage());
            content ??= prop.GetGUIContent();
            RefreshTracking();
        }

        protected abstract bool ValidFor(SerializedProperty sPropIn);
        protected abstract string InvalidMessage();
    }
    
    public class Tracker : TrackerAbstract
    {
        // -- Type
        private readonly bool checkType = false;
        private readonly SerializedPropertyType expectedSerializedType;
        
        // -- Tracking props (public get)
        public object previousValue { get; private set; }
        public bool previouslyMixed { get; private set; } = false;

        
        // -----------------
        //    Construct
        // -----------------
        public Tracker(in string targetPropName, SerializedPropertyType expected) : base(targetPropName)
        {
            expectedSerializedType = expected;
            checkType = true;
        }
        public Tracker(in string targetPropName) : base(targetPropName) { }
        
        
        // -----------------------
        //    ITrack Methods
        // -----------------------
        public override bool WasUpdated(TrackLogging log = TrackLogging.None)
        {
            var wasUpdated = SerializedExtensionMethods.WasUpdated(prop, previousValue, previouslyMixed);
            if (log == TrackLogging.None) 
                return wasUpdated;
            
            if (wasUpdated)
                Debug.Log($"tracker {prop.displayName} was updated: {previousValue} -> {prop.BetterObjectValue()}");
            else if (log == TrackLogging.Log)
                Debug.Log($"tracker {prop.displayName} was not updated");
            return wasUpdated;
        }
        public override void RefreshTracking()
        {
            
            previousValue = prop.BetterObjectValue();
            previouslyMixed = prop.hasMultipleDifferentValues;
        }
        
        
        // -----------------------
        //    Other Abstract
        // -----------------------
        protected override bool ValidFor(SerializedProperty sPropIn)
        {
            if (!checkType)
                return true;
            return sPropIn.propertyType == expectedSerializedType;
        }
        protected override string InvalidMessage()
        {
            return $"{GetType()}-> expected {expectedSerializedType} but got {prop.propertyType}";
        }
    }
    
    
    public class ListTracker : TrackerAbstract
    {
        public int previousCount { get; private set; }
        private List<object> previousValues = new ();
        private List<object> currentValues = new ();
        public List<object> PreviousValues => previousValues;
        public ListTracker(in string targetPropName) : base(targetPropName) { }
        
        // -----------------------
        //    ITrack Methods
        // -----------------------
        public override bool WasUpdated(TrackLogging log = TrackLogging.None)
        {
            
            // -- This is unfortunate amount of work every tick! Too bad!
            GetCurrentList(ref currentValues);
            
            // -- Check if updated
            var wasUpdated = (previousValues.SequenceEqual(currentValues) == false);
            if (log == TrackLogging.None)
                return wasUpdated;
            
            // -- Log
            if (wasUpdated)
                Debug.Log($"list tracker {propName} was updated from {previousCount} to {prop.arraySize} elements");
            else if (log == TrackLogging.Log)
                Debug.Log($"list tracker {propName} was not updated, {prop.arraySize} elements");
            return wasUpdated;
        }
        
        public override void RefreshTracking()
        {
            GetCurrentList(ref previousValues);
            previousCount = prop.arraySize;
        }
        
        
        private void GetCurrentList(ref List<object> list)
        {
            list.Clear();
            for (int i = 0; i < prop.arraySize; i++)
                list.Add(prop.GetArrayElementAtIndex(i).BetterObjectValue());
        }
        
        // -----------------------
        //    Other Abstract
        // -----------------------
        
        // -- this isn't enough validation, but other errors downstream will be descriptive enough...
        protected override bool ValidFor(SerializedProperty sPropIn) => sPropIn.isArray;
        protected override string InvalidMessage() => $"{GetType()}-> expected array but got {prop.propertyType}";
        
    }


}