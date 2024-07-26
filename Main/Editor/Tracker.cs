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
        public abstract bool WasUpdated(ETrackLog log = ETrackLog.None);
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
        public override bool WasUpdated(ETrackLog log = ETrackLog.None)
        {
            var wasUpdated = SerializedExtensionMethods.WasUpdated(prop, previousValue, previouslyMixed);
            if (log == ETrackLog.None) 
                return wasUpdated;
            
            if (wasUpdated)
                Debug.Log($"tracker {prop.displayName} was updated: {previousValue} -> {prop.BetterObjectValue()}");
            else if (log == ETrackLog.Log)
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

}