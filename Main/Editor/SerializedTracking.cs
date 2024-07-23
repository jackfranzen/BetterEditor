using System;
using System.Collections.Generic;
using System.Linq;
using BetterEditorSerialzied;
using UnityEditor;
using UnityEngine;

namespace BetterEditorTrackers
{
    public enum SerializedTrackerLogging
    {
        None,
        Log,
        LogIfUpdated
    }

    public interface ISerializedTracker
    {
        void Track(SerializedProperty sProperty);
        
        void SetPropName(in string propName);
        bool HasPropName();
        string GetPropName();
        bool WasUpdated(SerializedTrackerLogging log = SerializedTrackerLogging.None);
    }

    public static class SerializedTrackerMethods
    {
        
        public static void Track(this ISerializedTracker tracker, SerializedObject sObject, in string nameIn)
        {
            tracker.SetPropName(nameIn);
            tracker.Track(sObject);
        }
        
        public static void Track(this ISerializedTracker tracker, SerializedObject sObject)
        {
            if (tracker.HasPropName() == false)
                throw new Exception("Cannot track, no property name given");
            var sProp = sObject.FindProperty(tracker.GetPropName());
            if(sProp == null)
                throw new Exception($"Property {tracker.GetPropName()} not found in object {sObject.targetObject.name}");
            tracker.Track(sProp);
        }
        
        public static void TrackRelative(this ISerializedTracker tracker, SerializedProperty parentProperty, in string nameIn)
        {
            tracker.SetPropName(nameIn);
            tracker.TrackRelative(parentProperty);
        }
        
        public static void TrackRelative(this ISerializedTracker tracker, SerializedProperty parentProperty)
        {
            if (tracker.HasPropName() == false)
                throw new Exception("Cannot track, no property name given");
            var sProp = parentProperty.FindPropertyRelative(tracker.GetPropName());
            if(sProp == null)
                throw new Exception($"Property {tracker.GetPropName()} not found relative to {parentProperty.name}");
            tracker.Track(sProp);
        }
    }

    
    public class SerializedTracker : ISerializedTracker
    {
        public SerializedProperty prop;
        public string propName;
        private bool hasPropName;
        
        public SerializedTracker(in string propName)
        {
            SetPropName(propName);
        }
        
        public SerializedTracker(){}
        
        public void SetPropName(in string propName)
        {
            this.propName = propName;
            hasPropName = true;
        }
        public string GetPropName() { return propName; }
        public bool HasPropName(){ return hasPropName; }

        
        public object value;
        public bool hasMixed = false;
        
        public void Track(SerializedProperty sProp)
        {
            prop = sProp;
            if(prop == null)
                throw new Exception($"RFSerializedTracker->Track given invalid property");
            value = prop.GetPropertyObjectValue();
            hasMixed = prop.hasMultipleDifferentValues;
        }
        
        public bool WasUpdated(SerializedTrackerLogging log = SerializedTrackerLogging.None)
        {
            var wasUpdated = prop.WasUpdated(value, hasMixed);
            if (log == SerializedTrackerLogging.None) 
                return wasUpdated;
            
            if (wasUpdated)
                Debug.Log($"tracker {prop.displayName} was updated: {value} -> {prop.GetPropertyObjectValue()}");
            else if (log == SerializedTrackerLogging.Log)
                Debug.Log($"tracker {prop.displayName} was not updated");
            return wasUpdated;
        }
    }
    
    
    public class SerializedListTracker : ISerializedTracker
    {
        public SerializedProperty prop;
        public string propName;
        private bool hasPropName;
        
        public SerializedListTracker(in string propNameIn)
        {
            SetPropName(propNameIn);
        }
        
        public SerializedListTracker(){}
        
        public void SetPropName(in string propName)
        {
            this.propName = propName;
            hasPropName = true;
        }
        public string GetPropName() { return propName; }
        public bool HasPropName(){ return hasPropName; }
        
        
        
        public int previousCount = 0;
        List<object> previousValues = new List<object>();
        List<object> currentValues = new List<object>();

        public void Track(SerializedProperty sProp)
        {
            prop = sProp;
            if(prop == null)
                throw new Exception($"RFSerializedListTracker->Track given invalid property");
            GetCurrentList(ref previousValues);
            previousCount = prop.arraySize;
        }
        
        private void GetCurrentList(ref List<object> list)
        {
            list ??= new List<object>();
            list.Clear();
            for (int i = 0; i < prop.arraySize; i++)
                list.Add(prop.GetArrayElementAtIndex(i).GetPropertyObjectValue());
        }
        
        public bool WasUpdated(SerializedTrackerLogging log = SerializedTrackerLogging.None)
        {
            
            // -- This is unfortunate amount of work every tick! Too bad!
            GetCurrentList(ref currentValues);
            var wasUpdated = (previousValues.SequenceEqual(currentValues) == false);
            
            if (log != SerializedTrackerLogging.None)
            {
                if (wasUpdated)
                    Debug.Log($"list tracker {propName} was updated from {previousCount} to {prop.arraySize} elements");
                else if (log == SerializedTrackerLogging.Log)
                    Debug.Log($"list tracker {propName} was not updated, {prop.arraySize} elements");
            }
            
            if (wasUpdated)
            {
                previousValues = currentValues;
                currentValues = new List<object>();
                previousCount = prop.arraySize;
            }
            
            return wasUpdated;
        }
    }
    
    
    public class BulkSerializedTracker
    {
        private SerializedProperty property;
        private List<ISerializedTracker> trackers = new List<ISerializedTracker>();
        public SerializedProperty Property => property;
        public List<ISerializedTracker> Trackers => trackers;
        
        public void Add(ISerializedTracker tracker)
        {
            trackers.Add(tracker);
        }
        public void AddRange(IEnumerable<ISerializedTracker> trackersIn)
        {
            trackers.AddRange(trackersIn);
        }
        
        // -- Builds the tracker list from object reflection
        public void PopulateWithReflection(object obj)
        {
            var fields = obj.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(SerializedTracker))
                {
                    var tracker = (SerializedTracker)field.GetValue(obj);
                    Add(tracker);
                }
            }
            
            if(fields.Length == 0)
                Debug.LogWarning($"TrackerList.PopulateWithReflection() found no fields in {obj.GetType().Name}");
        }
        
        public bool AnyWasUpdated(SerializedTrackerLogging log = SerializedTrackerLogging.None)
        {
            ExceptionOnNoTrackers();
            
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
        
        private void ExceptionOnNoTrackers()
        {
            if(trackers.Count == 0)
                throw new System.Exception("Trackers not initialized. Call Start() first.");
        }
        
        public void SetPrimaryProperty(SerializedProperty sProp)
        {
            if (sProp == null)
                throw new Exception("Cannot set primary property to null");
            ExceptionOnNoTrackers();
            property = sProp;
            foreach (var tracker in trackers)
                tracker.TrackRelative(property);
        }

        public void CheckProperty()
        {
            if (property == null)
                throw new Exception("Primary property not set, call SetPrimaryProperty() first");
        }
    }
    
    public interface IHasBulkTracker
    {
        BulkSerializedTracker GetTracker();
    }
}