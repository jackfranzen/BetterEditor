﻿// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace BetterEditor
{
    
    // -------------------
    //   Scope Helpers
    // -------------------
        
    public class IndentEditorLabelFieldScope : IDisposable
    {
        private bool m_Disposed;

        public IndentEditorLabelFieldScope (GUIContent content, GUIStyle style = null, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(content, style ?? EditorStyles.boldLabel, options);
            EditorGUI.indentLevel += 1;
        }
            
        public IndentEditorLabelFieldScope (string content, GUIStyle style = null, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(content, style ?? EditorStyles.boldLabel, options);
            EditorGUI.indentLevel += 1;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            EditorGUI.indentLevel -= 1;
        }
    }
        
    // -- Center content in scope within a EditorGUILayout.BeginVertical() using GUILayout.FlexibleSpace()
    public class CenterVerticalScope : IDisposable
    {
        private bool m_Disposed;

        public CenterVerticalScope(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            GUILayout.FlexibleSpace();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
        
            m_Disposed = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
    }

    public static class BetterEditorGUI
    {
        
        // ---------------------------------------------------
        //   Property Foldout Methods
        //       - for structs / classes, supports copy/paste
        // ---------------------------------------------------
        
        // public static bool PropertyFoldout(SerializedProperty sProp, GUIContent headerContent)
        // {
        //     if (sProp == null)
        //         throw new System.Exception($"DrawHeaderFoldout() failed, SerializedProperty is null");
        //     return EditorGUILayout.PropertyField(sProp, headerContent, false);
        // }
        //
        // public delegate void DrawPropertyFoldoutInner();
        // public static bool PropertyFoldout(SerializedProperty sProp, GUIContent headerContent, DrawPropertyFoldoutInner drawInnerFunc)
        // {
        //     if (sProp == null)
        //         throw new System.Exception($"DrawHeaderFoldout() failed, SerializedProperty is null");
        //     
        //     // -- Check expanded and return if not (using includeChildrenFalse)
        //     var expanded = EditorGUILayout.PropertyField(sProp, headerContent, false);
        //     if (!expanded) 
        //         return false;
        //     
        //     // -- Draw inner and return true
        //     using(new EditorGUI.IndentLevelScope())
        //         drawInnerFunc();
        //     return true;
        // }
        
        
        // ------------------------------
        //   Box Styles and Textures
        // ------------------------------
        
        public static GUIStyle MakeCustomBoxStyle(int width, int height, Color backgroundColor, Color borderColor, int borderWidth = 1)
        {
            var boxStyle = new GUIStyle();
            boxStyle.normal.background = MakeCustomBoxTexture(width, height, backgroundColor, borderColor, borderWidth);
            boxStyle.border = new RectOffset(borderWidth, borderWidth, borderWidth, borderWidth);
            return boxStyle;
        }
        public static Texture2D MakeCustomBoxTexture(int width, int height, Color backgroundColor, Color borderColor, int borderWidth = 1)
        {
            Texture2D texture = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x < borderWidth || y < borderWidth || x >= width - borderWidth || y >= height - borderWidth)
                        texture.SetPixel(x, y, borderColor);
                    else
                        texture.SetPixel(x, y, backgroundColor);
                }
            }
            texture.Apply();
            return texture;
        }
        
        // -------------------
        //   Text Style
        // -------------------
        
        
        public static GUIStyle GetLabelStyle(ref GUIStyle style, in Color textColor, in FontStyle fontStyle = FontStyle.Normal, int fontSize = 12)
        {
            if (style != null)
                return style;
            style = new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = textColor
                },
                hover =
                {
                    textColor = textColor
                },
                fontStyle = fontStyle,
                fontSize = fontSize
            };
            return style;
        }
        
        // -------------------
        //   Button Methods
        // -------------------

        
        public static bool Button(GUIContent content, bool useColor, in Color color, bool expandHeight = true, params GUILayoutOption[] layoutOptions)
        {
            if (useColor)
                return Button(content, color, expandHeight, layoutOptions);
            return Button(content, expandHeight, layoutOptions);
        }
        
        public static bool Button(GUIContent content, in Color color, bool expandHeight = true, params GUILayoutOption[] layoutOptions)
        {
            var savedColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            var pressed = Button(content, expandHeight, layoutOptions);
            GUI.backgroundColor = savedColor;
            return pressed;
        }

        public static bool Button(GUIContent content, bool expandHeight = true, params GUILayoutOption[] layoutOptions)
        {
            if (expandHeight)
                layoutOptions = layoutOptions.Append(GUILayout.ExpandHeight(true)).ToArray();
            return GUILayout.Button(content, layoutOptions);
        }
        
        // --------------------
        //  Boxes and Dividers
        // --------------------
        

        public static GUIStyle CreateThinBox(int height, int topPad, int bottomPad)
        {
            var thinBoxStyle = new GUIStyle();
            thinBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
            thinBoxStyle.margin = new RectOffset(0, 0, topPad, bottomPad);
            thinBoxStyle.fixedHeight = height;
            return thinBoxStyle;
        }
        
        public static void DrawBoxWithColor(in Color color, GUIStyle boxStyle)
        {
            var savedColor = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, boxStyle);
            GUI.color = savedColor;
        }
        
        // -------------------------
        //    List Property Field
        // -------------------------

        public static void ListPropertyField(SerializedProperty sProp, GUIContent content = null, FontStyle fontStyle = FontStyle.Normal, bool forceExpand = false)
        {
            var prevStyle = EditorStyles.foldout.fontStyle;
            EditorStyles.foldoutHeader.fontStyle = fontStyle;
            if(forceExpand)
                sProp.isExpanded = true;
            EditorGUILayout.PropertyField(sProp, content);
            EditorStyles.foldoutHeader.fontStyle = prevStyle;
        }

        
        // -------------------
        //   Header Foldout
        // -------------------

        public static void Foldout(ref bool foldoutProp, GUIContent content, in GUIStyle style = null)
        {
            foldoutProp = EditorGUILayout.Foldout(foldoutProp, content, true, style ?? EditorStyles.foldout);
        }
        public static void Foldout(ref bool foldoutProp, in string content, in GUIStyle style = null)
        {
            foldoutProp = EditorGUILayout.Foldout(foldoutProp, content, true, style ?? EditorStyles.foldout);
        }
        
        public delegate void DrawFoldoutInner();
        
        public static void FoldoutWrapped(ref bool foldoutProp, GUIContent content, DrawFoldoutInner drawFunc, GUIStyle style = null)
        {
            Foldout(ref foldoutProp, content, style);
            FoldoutWrappedInner(foldoutProp, drawFunc, 2);
        }
        
        public static void FoldoutWrapped(ref bool foldoutProp, in string content, DrawFoldoutInner drawFunc, GUIStyle style = null)
        {
            Foldout(ref foldoutProp, content, style);
            FoldoutWrappedInner(foldoutProp, drawFunc, 2);
        }

        public static void FoldoutWrappedInner(bool foldoutProp, DrawFoldoutInner drawFunc, in float space = 6)
        {
            // -- If closed, don't draw content
            if (foldoutProp == false)
            {
                GUILayout.Space(space);
                return;
            }
            
            // -- Draw Indented Content and space
            EditorGUI.indentLevel += 1;
            drawFunc();
            EditorGUI.indentLevel -= 1;
            GUILayout.Space(space);
        }


        // -------------------
        //   Sliders
        // -------------------
        
        
        // -- Float Slider for a serialized Property, no label, limits optional
        public static bool FloatSliderNoLabel(SerializedProperty property, in float min, in float max, params GUILayoutOption[] layoutOptions)
        {

            EditorGUI.BeginChangeCheck();
            var newValue = GUILayout.HorizontalSlider(property.floatValue, min, max, layoutOptions);
            var updated = EditorGUI.EndChangeCheck();
            if (updated)
                property.floatValue = newValue;
            return updated;
        }

        // -- Property(), makes it easier to swap from Slider<->Property
        public static void Property(SerializedProperty property, GUIContent content, params GUILayoutOption[] layoutOptions)
        {
            // -- mark input fields as NOPOPUP so RFLongOperationPopup doesn't steal focus from them.
            if(property.IsNumber())
                GUI.SetNextControlName(property.propertyPath+"-NOPOPUP");
            
            // -- just draw a property field
            EditorGUILayout.PropertyField(property, content, layoutOptions);
        }

        public static bool Slider(SerializedProperty property, GUIContent content, in float min, in float max, bool enforceLimits = false)
        {
            var updated = SliderNoEnforce(property, min, max, content);

            // -- Enforce Limits
            if (enforceLimits)
                property.EnforceClamp(min, max);
            
            return updated;
        }
        
        // -- SliderNoEnforce()
        //     - Exactly Like EditorGuiLayout.SliderField() or EditorGuiLayout.PropertyField(float)
        //          - BUT the included input field does not enforce the range limitations! yipeee!
        //          - NOTE This will fail to draw an input field if Range() is defined for your serialized property,
        //              you should use EditorGuiLayout.PropertyField(float) in that case.
        public static bool SliderNoEnforce(SerializedProperty property, in float min, in float max, GUIContent content, bool drawFieldFirst = false)
        {
            if (!property.IsNumber())
                throw new ArgumentException($"Property {property.displayName} must be a float or integer type to use Slider()");
            
            // -- Begin Horizontal Row
            EditorGUILayout.BeginHorizontal();
            
            // -- Begin Property
            //      (This allows the label to have right-click context like a regular PropertyField)
            Rect rect = EditorGUILayout.GetControlRect();
            
            // -- Draw Prefix Label
            EditorGUI.BeginProperty(rect, content, property);
            var prefixRect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), content);
            EditorGUI.EndProperty();
            
            // -- Determine Widths (80 matches Unity's PropertyField Size)
            var remainingWidth = rect.width - prefixRect.x + rect.x;
            var inputFieldWidth = 65;
            var sliderWidth = remainingWidth - inputFieldWidth - 5;
            
            // -- Determine X Positions
            var inputFieldX = (drawFieldFirst) ? prefixRect.x : prefixRect.x + sliderWidth + 5;
            var sliderX = (drawFieldFirst) ? prefixRect.x + inputFieldWidth + 5 : prefixRect.x;
            
            // -- Build Rects
            var inputFieldRect = new Rect(inputFieldX, prefixRect.y, inputFieldWidth, prefixRect.height);
            var sliderRect = new Rect(sliderX, prefixRect.y, sliderWidth, prefixRect.height);
            
            // -- Draw Basic Slider
            var updated = RectSlider(sliderRect, property, min, max);
            
            // -- Input Field
            updated |= RectNumberField(inputFieldRect, property);
            

            // -- Finish
            EditorGUILayout.EndHorizontal();
            return updated;
        }
        
        public static bool RectSlider(Rect rect, SerializedProperty property, in float min, in float max)
        {
            // -- Mark the slider with a focus name, so we can focus the slider when it's used
            //      - If we don't do this, the previous control will remain focused, and it looks ugly :( ...
            string controlName = "Slider_" + property.propertyPath;
            GUI.SetNextControlName(controlName);
            
            // -- Multi-Value? Draw a Slider with no handle, and watch for any input.
            if (property.hasMultipleDifferentValues)
            {
                EditorGUI.BeginChangeCheck();
                var newVal2 = GUI.HorizontalSlider(rect, int.MaxValue, min, max);
                if(EditorGUI.EndChangeCheck())
                {
                    property.SetNumberValue(newVal2);
                    return true;
                }

                return false;
            }
            
            // -- Regular Slider
            var newValue = GUI.HorizontalSlider(rect, property.GetNumberValue(), min, max);
            var updated = Mathf.Approximately(newValue, property.GetNumberValue()) == false;
            if(updated)
            {
                property.SetNumberValue(newValue);
                GUI.FocusControl(controlName);
            }
            return updated;
        }
        
        public static bool RectNumberField(Rect rect, SerializedProperty property)
        {
            // -- set the focus name for this input field, but more importantly tag it with NOPOPUP so RFLongOperationPopup doesn't steal focus from it. 
            GUI.SetNextControlName(property.propertyPath+"-NOPOPUP");
            
            // -- Use EditorGUI.PropertyField to handle multi-editing best, but without a label.
            //      we also need to remove indenting to get it to render correctly in any context.
            //      NOTE: This will draw a slider instead if a Range() is defined,
            //          in that case you probably want to use unity's default PropertyField (slider) for the entire row.
            var previousIndent = EditorGUI.indentLevel;
            var numValues = property.GetNumberValue();
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, property, GUIContent.none);
            EditorGUI.indentLevel = previousIndent;
            return Mathf.Approximately(numValues, property.GetNumberValue()) == false;
        }
        
        
        
        // -------------------
        //   Toggle Methods
        // -------------------
        
        
        // -- ToggleRow():
        //          - Very Similar to Unity's EditorGUILayout.PropertyField(bool) but with the toggle on the left side.
        //          - Allows for longer names, easier access, better UX, and stuffing more data to the right of the label (see interactive mode toggle)
        //          - [TODO] bring some of the logic from UI_Interactive here, and allow for a content function which takes the currentX.
        
        public static void ToggleRow(SerializedProperty sProp, GUIContent content, bool setTrueOnMixed = true, bool forceFalse = false, int width = -1)
        {
            ToggleRow(sProp, sProp, content, setTrueOnMixed, forceFalse, width);
        }
        public static void ToggleRow(SerializedProperty toggleProp, SerializedProperty rowProp, GUIContent content, bool setTrueOnMixed = true, bool forceFalse = false, int width = -1)
        {

            // -- Start
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            if (width > 0)
                rect.width = width;
            
            // EditorGUI.DrawRect(rect, Color.cyan);

            // -- Property and Horizontal
            using (new EditorGUI.PropertyScope(rect, content, rowProp))
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(rect.width)))
            {
                // -- Draw the Toggle
                int indentOffset = indentLevel * 15; // -- Estimated Indent width
                Rect toggleRect = new Rect(rect.x + indentOffset, rect.y, 15, rect.height);
                //Rect toggleRect = new Rect(rect.x, rect.y, 15, rect.height);
            
                // -- If Property is null, draw a toggle forced to false
                if (forceFalse) 
                    EditorGUI.Toggle(toggleRect, false);
                else
                    RectToggle(toggleRect, toggleProp, setTrueOnMixed);
            
    
                // -- Draw Content in Label
                Rect labelRect = new Rect(rect.x + 20 + indentOffset, rect.y, rect.width - 20, rect.height);
                EditorGUI.LabelField(labelRect, content);
            }

    
            // -- Finish
            EditorGUI.indentLevel = indentLevel;
        }
        
        
        

        public static void RectToggle(Rect toggleRect, SerializedProperty sProp, bool setTrueOnMixed)
        {
            EditorGUI.showMixedValue = sProp.hasMultipleDifferentValues;
            var inVal = sProp.boolValue || sProp.hasMultipleDifferentValues;
            var newVal = EditorGUI.Toggle(toggleRect, inVal);
            EditorGUI.showMixedValue = false;
            var updated = newVal != inVal;
                
            // -- Update Serialized when toggled
            if (updated)
            {
                if(sProp.hasMultipleDifferentValues)
                    sProp.boolValue = setTrueOnMixed;
                else
                    sProp.boolValue = !sProp.boolValue;
            }
        }

    }
}