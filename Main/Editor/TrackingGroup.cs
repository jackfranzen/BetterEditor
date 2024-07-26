// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace BetterEditor
{

    public class TrackingGroup : HashSet<ITrack>, ITrack
    {
        
        // -- Constructor
        //      - @ExpectedTypeIn: Optionally set an expected type for Collections that call .Track(), only used for
        //              non-top-level collections that call .SetAsRelative()
        public TrackingGroup(System.Type expectedTypeIn)
        {
            SetExpectedType(expectedTypeIn);
        }
        public TrackingGroup() {}
        
        // -- Expected Type
        protected bool hasExpectedType = false;
        protected System.Type expectedType = null;
        
        // -- Top Level, do we represent an entire OBJECT from the Editor?
        //          (Alternatively as "Relative", we need a propName to find our property in the given OBJECT or PROPERTY provided)
        protected bool isTopLevel = true; // -- if SetAsRelativeTracker() is never called, we assume top-level. 
        protected string propName = null;
        
        // -- Tracking Props
        public SerializedProperty prop { get; protected set; }
        public bool isTracking { get; protected set; } = false;
        
        
        
        // -------------------------------
        //     General Tracking Methods
        // -------------------------------
        public void SetExpectedType(System.Type expectedTypeIn)
        {
            expectedType = expectedTypeIn;
            hasExpectedType = true;
        }
        
        public void SetAsRelativeTracker(in string propNameIn, System.Type expectedTypeIn = null)
        {
            propName = propNameIn;
            isTopLevel = false;
            
            if (expectedTypeIn != null)
                SetExpectedType(expectedTypeIn);
            
            if (!hasExpectedType)
                throw new Exception($"{GetType()}.SetAsRelativeTracker() called without expectedType! (You can also set this in the constructor!)");
        }
        
        public bool IsTracking => isTracking;
        public void CheckTracking()
        {
            if (IsTracking == false)
                throw new Exception($"{GetType()}.CheckTracking() failed! Tracking has not begun, there is no data.");
        }

        public string GetLogStuff()
        {
            return isTopLevel ? "(Top Level)" : $"(Relative, {propName})";
        }
        
        
        // ------------------------
        //     ITrack Methods
        // ------------------------
        public bool WasUpdated(TrackLogging log = TrackLogging.None)
        {
            // -- Check has Trackers
            if (this.Any() == false)
                throw new Exception($"{GetType()}.WasUpdated() called on empty collection! {GetLogStuff()}");
            
            return this.WasAnyUpdated(log);
        }

        // -- Propagate "Refresh" to all subTrackers
        public void RefreshTracking()
        {
            
            // -- Check has Trackers
            if (this.Any() == false)
                throw new Exception($"{GetType()}.RefreshTracking() called on empty collection! {GetLogStuff()}");
            
            foreach (var tracker in this)
                tracker.RefreshTracking();
        }
        
        public void Track(TrackSource source)
        {
            // -- Check has Trackers
            if (this.Any() == false)
                throw new Exception($"{GetType()}.Track() called on empty collection! {GetLogStuff()} {source.GetLogStuff()}");
            
            // -- There are actually 3 layers of hierarchy (or input) that can occur here.
            //     -  [TOP-LEVEL] (An iTrack Editor representing an entire Serialized Object.)
            //              - RECEIVES: SerializedObject
            //              - TARGET: does nothing (always a Collection)
            //              - SENDS: SerializedObject,
            //     -  [FIRST-RELATIVE]
            //              - RECEIVE: OBJECT
            //              - TARGET: Gets "our" PROPERTY relative to this OBJECT
            //              - SENDS: "our" PROPERTY (Collection only)
            //     -  [FULL-RELATIVE]
            //              - RECEIVE: PROPERTY
            //              - TARGET: Gets "our" PROPERTY relative to this PROPERTY
            //              - SENDS: "our" PROPERTY (Collection only)
            
            // -- In Other words:
            //         - Tracking for this collection is configured as either (A) Top-level or (B) Relative with property name
            //         - Input for this collection is (C) SerializedObject or (D) SerializedProperty
            //              Top-Level: (A) + (C)
            //              First-Relative: (B) + (C)
            //              Full-Relative: (B) + (D)
            
            
            // -- (A) Top-Level:
            if (isTopLevel)
            {
                // -- Case (A) + (D) is invalid
                if (source.sourceIsObject == false)
                    throw new Exception($"{GetType()}.Track() Top-Level collection received a property, did you forget SetRelativePropName()?");
                
                // -- Do nothing, all of our children simply receive the source.
                foreach (var tracker in this)
                    tracker.Track( source );
                isTracking = true;
                return;
            }
            
            // -- (B) Relative:
            else
            {
                // -- Requires that we were given a property. 
                if ( string.IsNullOrEmpty(propName) )
                    throw new Exception($"{GetType()}.Track() called with no propName set");
                
                // -- Find & Check "our" PROPERTY
                //        (which our children trackers will need to be tracked relative to)
                var foundProp = source.FindProperty(propName);
                if(foundProp == null)
                    throw new Exception($"{GetType()}.Track() could not find property {propName} from {source.GetLogStuff()}");
                
                if(hasExpectedType && !foundProp.type.Contains(expectedType.Name))
                    throw new Exception($"{GetType()}.Track() found property {propName} with unexpected type {foundProp.type} instead of {expectedType} on {source.GetLogStuff()}");
                
                // -- Propagate "Track" to all subTrackers, using the found property (struct or class) as the relative Source
                prop = foundProp;
                foreach (var tracker in this)
                    tracker.Track( prop.AsSource() );
                isTracking = true;
            }
        }
        
        
        
        // ------------------------------------------------
        //       Populate by Reflection Methods
        // ------------------------------------------------
        
        // -- Builds the tracker list from object reflection
        //      - It's only intended that one collection run this operation per object, as a definitive source of all sub-trackers defined in the object.
        //      - @includeCollections: If true, will also include any TrackerCollections found in the object, which is a terrible idea.
        //              (Note, an iTrack wrapper with an internal collection, like a mini-editor, will always be gathered.)
        public void PopulateWithReflection(object obj, bool includeCollections = false)
        {
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                
                // -- Skip self and Collections
                var isCollection = field.FieldType == typeof(TrackingGroup);
                if(isCollection && field.GetValue(obj) == this)
                    continue;
                if(isCollection && !includeCollections)
                    continue;
                
                // -- check implements ITrack
                if (!typeof(ITrack).IsAssignableFrom(field.FieldType))
                    continue;
                var tracker = (ITrack)field.GetValue(obj);
                Add(tracker);
            }
            
            if(fields.Length == 0)
                throw new Exception($"{GetType()}.PopulateWithReflection() found no fields in {obj.GetType().Name}");
        }    
        
        public void PopulateWithReflectionIfEmpty(object obj)
        {
            if (this.Any() == false)
                PopulateWithReflection(obj);
        }    
    }
}