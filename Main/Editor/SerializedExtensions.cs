using System;
using UnityEditor;
using UnityEngine;

namespace BetterEditorSerialzied
{
    public static class SerializedExtensionMethods
    {
        
        // -- Enforce Min/Max/Clamp Methods 
        //       -  IMPORTANT: Like with any serializedProperty.value setter, this will cause a property
        //                     representing mixed values to collapse to a single value if a change is made.
        public static void EnforceMinimum(this SerializedProperty prop, float min)
        {
            if (prop.IsNumber() == false)
                throw new ArgumentException($"EnforceMinimum() for {prop.name} failed, got {prop.propertyType} but must be a number");
            
            if (prop.GetNumberValue() < min)
                prop.SetNumberValue(min);
        }
        
        public static void EnforceMaximum(this SerializedProperty prop, float max)
        {
            if (prop.IsNumber() == false)
                throw new ArgumentException($"EnforceMaximum() for {prop.name} failed, got {prop.propertyType} but must be a number");
            if (prop.GetNumberValue() > max)
                prop.SetNumberValue(max);
        }
        
        public static void EnforceClamp(this SerializedProperty prop, float min, float max)
        {
            if (prop.IsNumber() == false)
                throw new ArgumentException($"EnforceClamp() for {prop.name} failed, got {prop.propertyType} but must be a number");
            
            prop.EnforceMinimum(min);
            prop.EnforceMaximum(max);
        }
        
        
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

            var newValue = prop.GetPropertyObjectValue();
            var isDifferent = CompareObjects(newValue, previousValue) == false;
            return isDifferent;
        }

        
        public static bool AnyTrue(this SerializedProperty prop)
        {
            // -- Why use this method?
            //        - boolValue only returns the value of the "primary" selected object (similar to all serialized properties)
            //        - This method checks if any of the selected objects under this serialized property have a true value
            //        - Used as a helper method for showing or hiding dependent UI, primarily. 
            
            if (prop.propertyType != SerializedPropertyType.Boolean)
                throw new ArgumentException($"AnyTrue() for {prop.name} failed, got {prop.propertyType} but must be a boolean");
            
            return prop.boolValue || prop.hasMultipleDifferentValues;
        }

        
        // -- Thank you Unity
        public static object GetPropertyObjectValue(this SerializedProperty prop)
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
        
        
        public static bool CompareObjects(object obj1, object obj2)
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
        
        public static void CheckType(this SerializedProperty sProp, System.Type type)
        {
            // -- Check Type
            var isPrimaryPropertyOfTargetClassType = (sProp.type == type.Name);
            if (!isPrimaryPropertyOfTargetClassType)
                throw new System.Exception($"Property {sProp.displayName} is not of type {type.Name} in serializedObject");
        }
        
        
        public static bool DrawHeaderFoldout(this SerializedProperty sProp, GUIContent headerContent)
        {
            if (sProp == null)
                throw new System.Exception($"DrawHeaderFoldout() failed, SerializedProperty is null");
            return EditorGUILayout.PropertyField(sProp, headerContent, false);
        }
    }
}