// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Collections.Generic;
using UnityEditor;

namespace BetterEditor
{

    public class BasicTrackerCollection
    {
        private List<ISerializedTracker> trackers = new List<ISerializedTracker>();
        public List<ISerializedTracker> Trackers => trackers;
        
        public void Add(ISerializedTracker tracker)
        {
            trackers.Add(tracker);
        }
        public void Add(IEnumerable<ISerializedTracker> trackersIn)
        {
            trackers.AddRange(trackersIn);
        }
        public void Add(BasicTrackerCollection tracker)
        {
            Add(tracker.Trackers);
        }
        public void Add(IHasRelativeCollection relativeCollectionObject)
        {
            Add(relativeCollectionObject.GetCollection().Trackers);
        }
        
        public void Clear()
        {
            trackers.Clear();
        }
        
        public int Count => trackers.Count;
        public bool Empty => trackers.Count == 0;
        
        public void PopulateWithReflectionIfEmpty(object obj)
        {
            if (trackers.Count == 0)
                PopulateWithReflection(obj);
        }
        
        // -- Builds the tracker list from object reflection
        public void PopulateWithReflection(object obj)
        {
            var fields = obj.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(SerializedTracker))
                    continue;
                var tracker = (SerializedTracker)field.GetValue(obj);
                Add(tracker);
            }
            
            if(fields.Length == 0)
                throw new Exception($"TrackerCollection.PopulateWithReflection() found no fields in {obj.GetType().Name}");
        }        
        
        
        // -- Check if any tracker in the collection 
        public bool AnyWasUpdated(SerializedTrackerLogging log = SerializedTrackerLogging.None)
        {
            ThrowIfEmpty();
            
            var wasUpdated = false;
            foreach (var tracker in trackers)
                if (tracker.WasUpdated(log))
                {
                    // -- Break immediately if not logging
                    if(log == SerializedTrackerLogging.None)
                        return true;
                    // -- Otherwise, return at the end. 
                    wasUpdated |= true;
                }
            return wasUpdated;
        }
        
        // -- Throws if no trackers
        public void ThrowIfEmpty()
        {
            if(Empty)
                throw new System.Exception("Trackers not initialized. Populate the trackers array first using any of the methods");
        }
    }

    public class RelativeTrackerCollection
    {
        private SerializedProperty prop;
        private System.Type targetClass;
        private BasicTrackerCollection collection = new BasicTrackerCollection();
        public SerializedProperty Prop => prop;
        public System.Type TargetClass => targetClass;
        public BasicTrackerCollection Collection => collection;
        public List<ISerializedTracker> Trackers => collection.Trackers;
        
        public RelativeTrackerCollection(System.Type targetClass)
        {
            this.targetClass = targetClass;
        }
        
        public RelativeTrackerCollection(object reflectionGatherTarget, System.Type expectedTrackingClass)
        {
            Init(reflectionGatherTarget, expectedTrackingClass);
        }
        
        public void Init(object reflectionGatherTarget, System.Type expectedTrackingClass)
        {
            PopulateWithReflection(reflectionGatherTarget);
            targetClass = expectedTrackingClass;
        }
        
        public void Add(ISerializedTracker tracker)
        {
            if(!tracker.HasPropName())
                throw new Exception("Cannot add tracker without property name");
            collection.Add(tracker);
        }
        
        public void AddRange(IEnumerable<ISerializedTracker> trackersIn)
        {
            foreach (var tracker in trackersIn)
                if(!tracker.HasPropName())
                    throw new Exception("Cannot add tracker without property name");
            collection.Add(trackersIn);
        }

        public void PopulateWithReflectionIfEmpty(object obj)
        {
            collection.PopulateWithReflectionIfEmpty(obj);
        }
        
        public void PopulateWithReflection(object obj)
        {
            collection.PopulateWithReflection(obj);
        }        
        
        // -- Helper Method to track from a serialized object and a property name
        public void TrackFrom(SerializedObject sObject, in string nameIn)
        {
            var foundProp = sObject.FindPropertyChecked(nameIn);
            TrackFrom(foundProp);
        }

        // -- Set the primary property for this collection and track from it
        //         (each tracker gets a serialized property relative to the provided prop)
        //         (trackers must be initialized with a property name)
        public void TrackFrom(SerializedProperty sProp)
        {
            if (sProp == null)
                throw new Exception("Cannot set primary property to null");
            if (targetClass == null)
                throw new Exception("Target class not set, call constructor with type first");
            sProp.ThrowIfBadType(targetClass);
            collection.ThrowIfEmpty();
            prop = sProp;
            foreach (var tracker in collection.Trackers)
                tracker.TrackRelative(prop);
        }
        
        // -- Check if any tracker in the collection 
        public bool AnyWasUpdated(SerializedTrackerLogging log = SerializedTrackerLogging.None)
        {
            return collection.AnyWasUpdated(log);
        }
        
        
        // -- Helper method which throws if primary property is not set
        public void CheckTracking()
        {
            if (prop == null)
                throw new Exception("Primary property not set, call TrackFrom() first");
        }
    }
    
    public interface IHasRelativeCollection
    {
        RelativeTrackerCollection GetCollection();
    }
    
    public interface IRelativeCollectionEditor : IHasRelativeCollection
    {
        void DrawUI();
        void DrawUIInner();
    }
}