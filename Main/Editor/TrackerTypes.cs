// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    
    // -- ETrackLog: Tracker Logging Level
    //      (Any logging causes iteration to continue after hitting true results in WasUpdated)
    public enum ETrackLog
    {
        None = 0,
        Log = 1,
        LogIfUpdated = 2,
    }
    
    // -----------------------------------------------------
    //                TrackSource
    //      (SerializedObject/Property Wrapper)
    // -----------------------------------------------------
    
    // -- TrackSource
    //     - Attempts to ease headaches between SerializedObject's.FindProperty() and
    //       SerializedProperty's.FindRelativeProperty().
    public struct TrackSource
    {
        public SerializedObject sObject;
        public SerializedProperty sParentProp;
        public bool sourceIsObject;
        
        // -- Track directly from Object
        public TrackSource(SerializedObject sObjectIn)
        {
            if (sObjectIn == null)
                throw new Exception("SerializedObject provided to TrackSource was null");
            sourceIsObject = true;
            sObject = sObjectIn;
            sParentProp = null;
        }
        
        // -- Track relative to another Property
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
    
    // -----------------------------------------------------
    //              Tracking Interfaces
    // -----------------------------------------------------
    
    // -- ITrack allows for multiple layers of tracking, eventually arriving at a Tracker. 
    public interface ITrack
    {
        void Track(TrackSource source);  // -- trackers receive a source serialized Object or Property
        bool WasUpdated(ETrackLog log = ETrackLog.None);
        void RefreshTracking(); // -- Reset state, WasUpdated is now false!
    }
    
    // -- Draw a complete collection's UI, with or without the header. 
    public interface IDrawUI
    {
        void Draw(); 
        void DrawNoHeader();
    }

    public interface ITrackAndDraw : ITrack, IDrawUI
    {
    }

    // -----------------------------------------------------
    //              Tracking Extensions
    //      (Static Extensions for classes in this file)
    // -----------------------------------------------------
    public static class TrackingExtensions
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
        //         - Returns true if any of the trackers were updated, but continues to iterate if logging is enabled. 
        public static bool WasUpdated(this IEnumerable<ITrack> trackersToCheck, ETrackLog log = ETrackLog.None)
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
                if (log == ETrackLog.None)
                    return true;
                
                // -- Otherwise, return at the end. 
                wasUpdated = true;
            }
            return wasUpdated;
        }
    }
}