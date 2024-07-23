// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
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
            var sProp = sObject.FindPropertyChecked(tracker.GetPropName());
            tracker.Track(sProp);
        }
        
        public static void TrackRelative(this ISerializedTracker tracker, SerializedProperty parentProperty, in string nameIn)
        {
            if (parentProperty == null)
                throw new Exception("Cannot TrackRelative, parent property is null");
            tracker.SetPropName(nameIn);
            tracker.TrackRelative(parentProperty);
        }
        
        public static void TrackRelative(this ISerializedTracker tracker, SerializedProperty parentProperty)
        {
            if (tracker.HasPropName() == false)
                throw new Exception("Cannot track, no property name given");
            if (parentProperty == null)
                throw new Exception("Cannot TrackRelative, parent property is null");
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
            var wasUpdated = SerializedExtensionMethods.WasUpdated(prop, value, hasMixed);
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


}