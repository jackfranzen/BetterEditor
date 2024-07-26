// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    
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
        public override bool WasUpdated(ETrackLog log = ETrackLog.None)
        {
            
            // -- This is unfortunate amount of work every tick! Too bad!
            GetCurrentList(ref currentValues);
            
            // -- Check if updated
            var wasUpdated = (previousValues.SequenceEqual(currentValues) == false);
            if (log == ETrackLog.None)
                return wasUpdated;
            
            // -- Log
            if (wasUpdated)
                Debug.Log($"list tracker {propName} was updated from {previousCount} to {prop.arraySize} elements");
            else if (log == ETrackLog.Log)
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