// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    
    
    // -- All Tracker Types are given an unchanging property name to find a serialized Property by later. 
    //        - A Tracker keeps a reference to the fully realized SerializedProperty "prop" (once Track() has been called)
    //        - A Tracker keeps a Reference to GUIContent (label + tooltip) for convenience. 
    //              (will auto-fill from [DisplayName()] and [Tooltip()] if provided)
    public abstract class TrackerAbstract : ITrack, IDisposable
    {
        // -- Props
        protected readonly string propName;
        public GUIContent content;
        public SerializedProperty prop {get; private set;}
        public bool tracking { get; private set; } = false;
        
        // -- Constructors
        public TrackerAbstract( in string targetPropName, in GUIContent contentIn) : this(targetPropName)
        {
            content = contentIn;
        }
        public TrackerAbstract( in string targetPropName)
        {
            propName = targetPropName;
        }
        
        // -- [ITrack] Methods
        public void Track(TrackSource source)
        {
            // -- The end point for all tracking operations, we find our property from source 
            var foundProp = source.FindProperty(propName);
            if (foundProp == null)
                throw new Exception($"{GetType()}->Track() given invalid property");
            if (ValidFor(foundProp) == false)
                throw new Exception(InvalidMessage());
            
            // -- Tracking Success
            prop = foundProp;
            tracking = true;
            content ??= prop.GetGUIContent();
            RefreshTracking();
            
        }
        
        public bool WasUpdated(ETrackLog log = ETrackLog.None)
        {
            if (!tracking)
                return false;
            return WasUpdatedInternal(log);
        }
        
        public abstract void RefreshTracking();
        
        public void StopTracking()
        {
            prop?.Dispose();
            prop = null;
            tracking = false;
        }
        
        // -- Abstract Methods
        public abstract bool WasUpdatedInternal(ETrackLog log = ETrackLog.None);
        protected abstract bool ValidFor(SerializedProperty sPropIn);
        protected abstract string InvalidMessage();
        
        // -- [IDisposable] Methods
        public void Dispose()
        {
            if (tracking)
                StopTracking();
        }

    }
    
    public class Tracker : TrackerAbstract
    {
        // -- Type
        private readonly bool hasExpectedType = false;
        private readonly SerializedPropertyType expectedSerializedType;
        
        // -- Tracking props (public get)
        public object previousValue { get; private set; }
        public bool previouslyHadMultipleDifferentValues { get; private set; } = false;

        
        // -----------------
        //    Construct
        // -----------------
        public Tracker(in string targetPropName, SerializedPropertyType expected) : base(targetPropName)
        {
            expectedSerializedType = expected;
            hasExpectedType = true;
        }
        public Tracker(in string targetPropName) : base(targetPropName) { }
        
        
        // -----------------------
        //    ITrack Methods
        // -----------------------
        public override bool WasUpdatedInternal(ETrackLog log = ETrackLog.None)
        {
            var wasUpdated = SerializedExtensionMethods.WasUpdated(prop, previousValue, previouslyHadMultipleDifferentValues);
            if (log == ETrackLog.None) 
                return wasUpdated;
            
            if (wasUpdated)
                Debug.Log($"tracker '{prop.displayName}' was updated: {previousValue} -> {prop.BetterObjectValue()}");
            else if (log == ETrackLog.Log)
                Debug.Log($"tracker '{prop.displayName}' was not updated");
            return wasUpdated;
        }
        public override void RefreshTracking()
        {
            if (!tracking) // [todo] this return feels out of place, should be higher up?
                return;
            previousValue = prop.BetterObjectValue();
            previouslyHadMultipleDifferentValues = prop.hasMultipleDifferentValues;
        }
        
        
        // -----------------------
        //    Other Abstract
        // -----------------------
        protected override bool ValidFor(SerializedProperty sPropIn)
        {
            if (!hasExpectedType)
                return true;
            return sPropIn.propertyType == expectedSerializedType;
        }
        protected override string InvalidMessage()
        {
            return $"{GetType()}-> expected {expectedSerializedType} but got {prop.propertyType}";
        }
    }

}