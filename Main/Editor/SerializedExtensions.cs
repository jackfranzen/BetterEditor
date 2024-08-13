// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    public static class SerializedExtensionMethods
    {
        
        // -------------------------------------
        //        Core GUI Methods
        // -------------------------------------
        
        public static GUIContent GetGUIContent(this SerializedProperty property)
        {
            return new GUIContent(property.displayName, property.tooltip);
        }
        
        public static bool DrawDefaultEditor_NoUpdates(this SerializedObject sObject)
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty iterator = sObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    EditorGUILayout.PropertyField(iterator, true);
            }
            return EditorGUI.EndChangeCheck();
        }
        
        // ---------------------------------------
        //      Core Serialized Object Methods
        // ----------------------------------------
        
        public static void ApplyIfModified(this SerializedObject sObject)
        {
            // -- One would assume this is the default behavior of ApplyModifiedProperties(), but it is not.
            // -- There are cases where ApplyModifiedProperties() will throw errors when attempting to apply changes
            //          to a SerializedObject containing recently deleted objects, even though no relevant changes
            //          actually occured, so this helps smooth over these cases. 
            
            if (sObject.hasModifiedProperties)
                sObject.ApplyModifiedProperties();
        }
        
        // --------------------------------------------
        //      Find Properties with Checks/Errors
        // --------------------------------------------
        
        public static SerializedProperty FindPropertyChecked(this SerializedObject sObject, in string name)
        {
            var prop = sObject.FindProperty(name);
            if (prop != null)
                return prop;
            
            if(sObject.targetObject)
                throw new ArgumentException($"Property {name} not found in {sObject.targetObject.GetType().Name}");
            
            throw new ArgumentException($"Property {name} not found in serialized object");
            return null;
        }
        
        public static SerializedProperty FindRelativeChecked(this SerializedProperty parent, in string name)
        {
            var prop = parent.FindPropertyRelative(name);
            if (prop != null)
                return prop;
            
            throw new ArgumentException($"Property {name} not found in {parent.displayName}");
            return null;
        }
        
        // -------------------------------------
        //      Serialized Prop: Number
        // -------------------------------------
        
        public static bool IsNumber(this SerializedProperty prop)
        {
            return prop.IsFloat() || prop.IsInt();
        }

        public static float GetNumberValue(this SerializedProperty prop)
        {
            if (prop.IsFloat())
                return prop.floatValue;
            if (prop.IsInt())
                return prop.intValue;
            throw new ArgumentException($"GetNumberValue() for {prop.name} failed, got {prop.propertyType} but must be a number");
        }

        public static bool IsFloat(this SerializedProperty prop)
        {
            return prop.propertyType == SerializedPropertyType.Float;
        }
        
        public static bool IsInt(this SerializedProperty prop)
        {
            return prop.propertyType == SerializedPropertyType.Integer;
        }
        
        // -- IMPORTANT: Like with any serializedProperty.value setter, this will cause a property
        //                  representing mixed values to collapse to a single value.
        public static bool SetNumberValue(this SerializedProperty prop, in float value)
        {
            if (prop.IsFloat())
            {
                var valueWas = prop.floatValue;
                prop.floatValue = value;
                return Mathf.Approximately(valueWas, prop.floatValue) == false;
            }
            if (prop.IsInt())
            {
                var valueWas = prop.intValue;
                prop.intValue = (int)value;
                return valueWas != prop.intValue;
            }
            throw new ArgumentException($"SetNumberValue() for {prop.name} failed, got {prop.propertyType} but must be a number");
            return false;
        }
        

        // -------------------------------------
        //      Serialized Prop: Boolean
        // -------------------------------------
        
        // -- Why use these methods?
        //        - boolValue only returns the value of the "primary" selected object (similar to all serialized properties)
        //        - This method checks if any of the selected objects under this serialized property have a true value
        //        - Used as a helper method for showing or hiding dependent UI, primarily. 
        public static bool AnyTrue(this SerializedProperty prop)
        {
            if (prop.propertyType != SerializedPropertyType.Boolean)
                throw new ArgumentException($"AnyTrue() for {prop.name} failed, got {prop.propertyType} but must be a boolean");
            
            return prop.boolValue || prop.hasMultipleDifferentValues;
        }
        public static bool AllTrue(this SerializedProperty prop)
        {
            if (prop.propertyType != SerializedPropertyType.Boolean)
                throw new ArgumentException($"AnyTrue() for {prop.name} failed, got {prop.propertyType} but must be a boolean");
            return prop.boolValue && !prop.hasMultipleDifferentValues;
        }
        public static bool AllFalse(this SerializedProperty prop)
        {
            return !prop.AnyTrue();
        }

        
        // ----------------------------------
        //     Enforce Range Methods
        // ----------------------------------
        
        
        // -- Enforce Min/Max/Clamp Methods 
        //       -  IMPORTANT: Like with any serializedProperty.value setter, this will cause a property
        //                     representing mixed values to collapse to a single value if a change is made.
        
        // -- SAFE REPLACEMENT FOR value = Mathf.Min()! (prevents mixed values from collapsing to a single value!)
        public static void EnforceMinimum(this SerializedProperty prop, float min)
        {
            if (prop.IsNumber() == false)
                throw new ArgumentException($"EnforceMinimum() for {prop.name} failed, got {prop.propertyType} but must be a number");
            
            if (prop.GetNumberValue() < min)
                prop.SetNumberValue(min);
        }
        
        // -- SAFE REPLACEMENT FOR = Mathf.Max()! (prevents mixed values from collapsing to a single value!)
        public static void EnforceMaximum(this SerializedProperty prop, float max)
        {
            if (prop.IsNumber() == false)
                throw new ArgumentException($"EnforceMaximum() for {prop.name} failed, got {prop.propertyType} but must be a number");
            if (prop.GetNumberValue() > max)
                prop.SetNumberValue(max);
        }
        
        // -- SAFE REPLACEMENT FOR = Mathf.Clamp()! (prevents mixed values from collapsing to a single value!)
        public static void EnforceClamp(this SerializedProperty prop, float min, float max)
        {
            if (prop.IsNumber() == false)
                throw new ArgumentException($"EnforceClamp() for {prop.name} failed, got {prop.propertyType} but must be a number");
            
            prop.EnforceMinimum(min);
            prop.EnforceMaximum(max);
        }
        
        
        
        
        // ----------------------------------------
        //          Was Updated Methods 
        //    (object getters and comparisons)
        // ----------------------------------------
        
        public static bool WasUpdated(this SerializedProperty prop, object previousValue, bool hasMixedValue)
        {
            // -- Note, this method only works for basic property types
            //          - lists, classes and structs are not supported
            
            // -- Why check hasMultipleDifferentValues?
            //          - a selection of booleans (for example) on a serialized Property can be updated from boolValue == true -> boolValue == true.
            //          - In this case, only hasMultipleDifferentValues is actually being changed, but it's a change nonetheless.
            
            if(prop == null)
                throw new ArgumentException("WasUpdated() failed, prop is null");
            
            if(hasMixedValue != prop.hasMultipleDifferentValues)
                return true;

            var newValue = prop.BetterObjectValue();
            var isDifferent = BetterCompareObjects(newValue, previousValue) == false;
            return isDifferent;
        }
        
        // -- Thank you Unity
        public static object BetterObjectValue(this SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue;
                case SerializedPropertyType.Enum:
                    return prop.enumValueFlag;
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value;
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value;
                case SerializedPropertyType.Vector3Int:
                    return prop.vector3IntValue;
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value;
                case SerializedPropertyType.Quaternion:
                    return prop.quaternionValue;
                case SerializedPropertyType.Rect:
                    return prop.rectValue;
                case SerializedPropertyType.ArraySize:
                    return prop.arraySize;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return prop.boundsValue;
                case SerializedPropertyType.Character:
                    // Assuming a char stored as an int (common in Unity's SerializedProperty)
                    return (char)prop.intValue;
                case SerializedPropertyType.Color:
                    return prop.colorValue;
                default:
                    throw new System.ArgumentException($"Unsupported property {prop.name} with type {prop.propertyType}");
            }
        }
        public static bool V2ApproxEqual(Vector2 v1, Vector2 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y);
        }
        
        public static bool V3ApproxEqual(Vector3 v1, Vector3 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y) && Mathf.Approximately(v1.z, v2.z);
        }
        
        public static bool V4ApproxEqual(Vector4 v1, Vector4 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y) && Mathf.Approximately(v1.z, v2.z) && Mathf.Approximately(v1.w, v2.w);
        }
        
        
        private static bool BetterCompareObjects(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null)
                return obj1 == obj2;
            
            if (obj1.GetType() != obj2.GetType())
                throw new ArgumentException("Objects are of different types and cannot be compared.");

            // -- Compare, with floating point tolerance for anything with float
            //      (Note Unity's Color and Quaternion .Equals() should be safe from floating point issues)
            switch (obj1)
            {
                case float asFloat:
                    return Mathf.Approximately(asFloat, (float)obj2);
                case Vector2 asVector2:
                    return V2ApproxEqual(asVector2, (Vector2)obj2);
                case Vector3 asVector3:
                    return V3ApproxEqual(asVector3, (Vector3)obj2);
                case Vector4 asVector4:
                    return V4ApproxEqual(asVector4, (Vector4)obj2);
            }
            return obj1.Equals(obj2);
        }
        
        
    }
}