// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace BetterEditor
{
    
    public class TrackerCollection : ITrack
    {
        
        private HashSet<ITrack> subTrackers = new ();
        
        // -- Expected Type
        private bool hasExpectedType = false;
        private string propName = null;
        private bool hasPropName = false;
        public SerializedProperty prop { get; private set; }
        private bool isTracking = false;
        private System.Type expectedType = null;
        
        // -- Constructor
        //      - @ExpectedTypeIn: Optionally set an expected type for Collections that call .Track()
        public TrackerCollection(System.Type expectedTypeIn)
        {
            SetExpectedType(expectedTypeIn);
        }
        public TrackerCollection() {}
        public void SetExpectedType(System.Type expectedTypeIn)
        {
            expectedType = expectedTypeIn;
            hasExpectedType = true;
        }
        
        // ------------------------
        //     ITrack Methods
        // ------------------------
        
        public bool IsTracking => isTracking;
        public void CheckTracking()
        {
            if (IsTracking == false)
                throw new Exception($"{GetType()}.CheckTracking() failed! Tracking has not begun, there is no data.");
        }
        
        public void SetPropName(string targetPropertyName)
        {
            propName = targetPropertyName;
            hasPropName = true;
        }

        public void Track(TrackSource source, in string targetPropertyName)
        {
            SetPropName(targetPropertyName);
            Track(source);
        }
        
        public void Track(TrackSource source)
        {
            
            
            // -- Check has Trackers
            if (IsEmpty)
                throw new Exception($"{GetType()}.Track() for {propName} called on empty collection");

            // -- Check if we are tracking a relative source
            if (source.isRelative == false)
            {
                foreach (var tracker in subTrackers)
                    tracker.Track( source );
                isTracking = true;
            }
            
            // -- More Checks
            if (hasPropName == false)
                throw new Exception($"{GetType()}.Track() called without a property name from {source.LogName}");
            
            // -- Find the property
            var foundProp = source.FindProperty(propName);
            
            // -- More Checks
            if(foundProp == null)
                throw new Exception($"{GetType()}.Track() could not find property {propName} from {source.LogName}");
            if(hasExpectedType && source.GetType() != expectedType)
                throw new Exception($"{GetType()}.Track() found property {propName} with unexpected type {foundProp.propertyType} instead of {expectedType} on {source.LogName}");
            
            // -- Propagate "Track" to all subTrackers, using the found property (struct or class) as the relative Source
            prop = foundProp;
            foreach (var tracker in subTrackers)
                tracker.Track( prop.AsRelativeSource() );
            isTracking = true;
        }
        
        // -- Propagate "Refresh" to all subTrackers
        public void RefreshTracking()
        {
            foreach (var tracker in subTrackers)
                tracker.RefreshTracking();
        }
        
        // -- Check if any subtrackers in the collection was updated
        public bool WasUpdated(TrackLogging log = TrackLogging.None)
        {
            if (IsEmpty)
                throw new Exception($"{GetType()}.WasUpdated() called on empty collection");
            
            var wasUpdated = false;
            foreach (var tracker in subTrackers)
            {
                // -- Skip if not updated
                if (!tracker.WasUpdated(log)) 
                    continue;
                // -- Immediately return true (if not logging)
                if (log == TrackLogging.None)
                    return true;
                // -- Otherwise, return at the end. 
                wasUpdated |= true;
            }
            return wasUpdated;
        }
        
        
        // ------------------------------------------------
        //       Child Management Methods
        // ------------------------------------------------
        
        public void Clear() => subTrackers.Clear();
        public int Count => subTrackers.Count;
        public bool IsEmpty => subTrackers.Count == 0;
        
        public void Add(ITrack tracker)
        {
            subTrackers.Add(tracker);
        }
        public void Add(IEnumerable<ITrack> trackersIn)
        {
            subTrackers.UnionWith(trackersIn);
        }
        public void Add(params ITrack[] trackersIn)
        {
            subTrackers.UnionWith(trackersIn);
        }
        public void Add(params IEnumerable<ITrack>[] trackersIn)
        {
            foreach (var trackers in trackersIn)
                subTrackers.UnionWith(trackers);
        }
        public void Set(IEnumerable<ITrack> trackersIn)
        {
            Clear();
            Add(trackersIn);
        }
        public void Set(params ITrack[] trackersIn)
        {
            Clear();
            Add(trackersIn);
        }
        public void Set(params IEnumerable<ITrack>[] trackersIn)
        {
            Clear();
            Add(trackersIn);
        }
        
        // ------------------------------------------------
        //       Populate by Reflection Methods
        // ------------------------------------------------
        
        // -- Builds the tracker list from object reflection
        public void PopulateWithReflection(object obj)
        {
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                // -- Skip TrackerCollection to prevent recursion on self and others.
                //      - Only one TrackerCollection per class should run this op anyway,
                //      - Something like an ITrackAndDraw user class that contains a collection is perfectly fine and will be collected
                if (field.FieldType == typeof(TrackerCollection))
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
            if (IsEmpty)
                PopulateWithReflection(obj);
        }    
        
    }
}