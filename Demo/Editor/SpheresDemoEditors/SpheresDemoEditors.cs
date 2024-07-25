using System;
using System.Collections.Generic;
using System.Reflection;
using BetterEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterEditorDemos
{
    
    
    public enum ESpheresDemoStages
    {
        BarebonesEditor = 1,
        BasicSerialized = 2,
        FullSerialized = 3,
        BetterTrackers = 4,
        TrackAndDrawCollections = 5,
    }

    
    public struct SpheresDemoInfo
    {
        public ESpheresDemoStages stage;
        public string title;
        public string description;
        public List<string> greenTexts;
        public List<string> redTexts;
        public string fileName;
    }
    
    public struct SphereDemoInfo
    {
        public ESpheresDemoStages stage;
        public System.Type component;
    }
    
    

    
    
    public static class SpheresDemoEditors
    {

        public static Color mutedRed = new Color(255 / 255f, 79 / 255f, 79 / 255f);
        public static Color brightGrey = new Color(0.8f, 0.8f, 1f);
        
        private static GUIStyle demoHeaderStyle;
        public static GUIStyle DemoHeaderStyle => BetterEditorGUI.GetLabelStyle(ref demoHeaderStyle, brightGrey, FontStyle.Bold, 16);
        
        private static GUIStyle modificationsStyle;
        public static GUIStyle ModificationsStyle => BetterEditorGUI.GetLabelStyle(ref modificationsStyle, mutedRed, FontStyle.Bold, 16);
        
        private static GUIStyle demoPrefixHeaderStyle;
        public static GUIStyle DemoPrefixHeaderStyle => BetterEditorGUI.GetLabelStyle(ref demoPrefixHeaderStyle, EditorStyles.label.normal.textColor, FontStyle.Bold, 16);

        private static GUIStyle redWrappedTextStyle = null;

        public static GUIStyle RedWrappedTextStyle()
        {
            if (redWrappedTextStyle != null)
                return redWrappedTextStyle;
            redWrappedTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            redWrappedTextStyle.fontStyle = FontStyle.Bold;
            redWrappedTextStyle.normal.textColor = mutedRed;
            redWrappedTextStyle.hover.textColor = mutedRed;
            return redWrappedTextStyle;
        }
        
        private static GUIStyle greenWrappedTextStyle = null;
        public static GUIStyle GreenWrappedTextStyle()
        {
            if (greenWrappedTextStyle != null)
                return greenWrappedTextStyle;
            greenWrappedTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            greenWrappedTextStyle.fontStyle = FontStyle.Bold;
            greenWrappedTextStyle.normal.textColor = new Color(79 / 255f, 201 / 255f, 79 / 255f);
            greenWrappedTextStyle.hover.textColor = new Color(79 / 255f, 201 / 255f, 79 / 255f);
            return greenWrappedTextStyle;
        }
        
        
        
        public static SphereDemoInfo Demo_01 = new ()
        {
            stage = ESpheresDemoStages.BarebonesEditor,
            component = typeof(SpheresDemo_01),
        };

        public static SphereDemoInfo Demo_02 = new ()
        {
            stage = ESpheresDemoStages.BasicSerialized,
            component = typeof(SpheresDemo_02),
        };
        
        public static SphereDemoInfo Demo_03 = new ()
        {
            stage = ESpheresDemoStages.FullSerialized,
            component = typeof(SpheresDemo_03),
        };
        
        public static SphereDemoInfo Demo_04 = new ()
        {
            stage = ESpheresDemoStages.BetterTrackers,
            component = typeof(SpheresDemo_04),
        };
        
        public static SphereDemoInfo Demo_05 = new ()
        {
            stage = ESpheresDemoStages.TrackAndDrawCollections,
            component = typeof(SpheresDemo_05),
        };
        
        public static SphereDemoInfo DemoByEnum(ESpheresDemoStages stage)
        {
            switch (stage)
            {
                case ESpheresDemoStages.BarebonesEditor:
                    return Demo_01;
                case ESpheresDemoStages.BasicSerialized:
                    return Demo_02;
                case ESpheresDemoStages.FullSerialized:
                    return Demo_03;
                case ESpheresDemoStages.BetterTrackers:
                    return Demo_04;
                case ESpheresDemoStages.TrackAndDrawCollections:
                    return Demo_05;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
        }
        public static string EditorsPath => BetterBookmarkLocator.GetBookmark("SpheresEditors").path;


        public static MonoScript currentEditorScript;
        public static bool HasEditorScript()
        {
            return currentEditorScript != null;
        }
        
        public static void OpenEditorScript()
        {
            if (HasEditorScript())
                AssetDatabase.OpenAsset(currentEditorScript);
        }

        public static GUIStyle dividerStyle = BetterEditorGUI.CreateThinBox(2, 8, 8);

        public const string GizmosInfo =
            "Enables a gizmo preview of the sphere distribution, these props don't requires an update and aren't tracked";
        
        //public static ESpheresDemoStages currentSelection = ESpheresDemoStages.UnityChangeChecking;
        
        
        public static bool DrawInfoAndSwitcher(in SpheresDemoInfo demoInfo)//, Editor editor)
        {
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            var previousStage = demoInfo.stage;
            var newStage = previousStage;
            
            var fullPath = EditorsPath + "/" + demoInfo.fileName;
            currentEditorScript = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label( "Demo: ", DemoPrefixHeaderStyle, GUILayout.ExpandWidth(false));
            GUILayout.Label(demoInfo.title, DemoHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(4);

            // -- Horizontal Row with Controls
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(25)))
            {
                // -- Enum Popup (centered vertically using BetterEditorGUI scope)
                var width1 = GUILayout.Width(180);
                using (new CenterVerticalScope(width1))
                    newStage = (ESpheresDemoStages)EditorGUILayout.EnumPopup(GUIContent.none, previousStage, width1);

                // -- Linker to script
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(currentEditorScript, typeof(MonoScript), false,
                        GUILayout.ExpandHeight(true));

                // -- Open Script Magic
                using (new EditorGUI.DisabledScope(HasEditorScript() == false))
                    if (BetterEditorGUI.Button(new GUIContent("Open"), Color.green, true, GUILayout.Width(60)))
                        OpenEditorScript();
            }
            
            // -- Vertical Space
            GUILayout.Space(2);
            
            // -- Draw Description
            EditorGUILayout.LabelField( demoInfo.description, EditorStyles.wordWrappedLabel );
            
            // -- Draw Green Text
            DrawBulletPointsText(demoInfo.greenTexts, GreenWrappedTextStyle());
            
            // -- Draw Red Text
            DrawBulletPointsText(demoInfo.redTexts, RedWrappedTextStyle());
            
            // -- Divider
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            // -- Handle Change
            if (previousStage != newStage)
            {
                var previousComponent = DemoByEnum(previousStage).component;
                var currentComponent = DemoByEnum(newStage).component;
                var newName = $"Demo {(int)newStage}";
                ReplaceAllComponentsInScene(previousComponent, currentComponent, newName);
                Debug.Log($"Switching from {previousStage} to {newStage}");
                return true;
            }

            return false;
        }

        public static void DrawBulletPointsText(List<string> texts, GUIStyle style)
        {
            if(texts == null || texts.Count == 0)
                return;
            
            var allText = "";
            for(var i = 0; i < texts.Count; i++)
                allText += $"• {texts[i]}" + (i < texts.Count - 1 ? "\n" : "");
            
            EditorGUI.indentLevel += 1;
            EditorGUILayout.LabelField(allText, style);
            EditorGUI.indentLevel -= 1;
            
            GUILayout.Space(2);
        }

        public static bool DrawInfoAndSwitcherWithModifyWarning(in SpheresDemoInfo demoInfo, ref bool hasModifications)
        {
            // -- Information about this demo, and controls to swap
            var updatedStage = DrawInfoAndSwitcher(demoInfo);
            
            // -- Draw Modify Warning
            if (!hasModifications)
                return updatedStage;
            
            // -- Draw a custom row with a button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Modifications Detected", ModificationsStyle, GUILayout.ExpandWidth(false));
            var pressedApply = GUILayout.Button(new GUIContent("Apply"));
            EditorGUILayout.EndHorizontal();

            // -- Divider
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            if (pressedApply)
                hasModifications = false;

            return updatedStage;
        }

        public static bool DrawModifyWarningRowSerialized(SerializedProperty hasModifications)
        {
            // -- Not a boolean!
            if(hasModifications.propertyType != SerializedPropertyType.Boolean)
                throw new Exception("Property must be a boolean!");
            
            // -- Build Text
            var numObjects = hasModifications.serializedObject.targetObjects.Length;
            var all = numObjects > 2 ? "All " : "";
            var s = numObjects == 1 ? " " : "s";
            string hasModificationsText = default;
            if(hasModifications.AnyTrue())
                hasModificationsText = hasModifications.AllTrue() ? $"({numObjects}) Component{s} Modified" : $"(~/{numObjects}) Component{s} Modified";
            else
                hasModificationsText = $"({numObjects}) Component{s}";
            
            
            var style = hasModifications.AnyTrue() ? ModificationsStyle : DemoHeaderStyle;
            
            // -- Draw a custom row with a button
            var pressedClear = false;
            EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            EditorGUILayout.LabelField(hasModificationsText, style, GUILayout.ExpandWidth(false));
            GUI.enabled = hasModifications.AnyTrue();
            pressedClear = GUILayout.Button(new GUIContent("Apply"));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // -- Divider
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            // -- Clear Modifications on object
            return pressedClear;
            
        }

        public static void ReplaceAllComponentsInScene(Type oldType, Type newType, in string newName)
        {
            // Check if the types are valid components
            if (!typeof(Component).IsAssignableFrom(oldType) || !typeof(Component).IsAssignableFrom(newType))
            {
                Debug.LogError("Provided types must be derived from Component.");
                return;
            }

            // Get all components of the old type in the current scene
            Component[] oldComponents = (Component[])GameObject.FindObjectsOfType(oldType);

            foreach (Component oldComponent in oldComponents)
            {
                // Get the GameObject to which the old component is attached
                GameObject go = oldComponent.gameObject;

                // Add the new component to the GameObject
                Component newComponent = go.AddComponent(newType);

                // Copy fields from the old component to the new component if they share the same field names and types
                CopyComponentReflection(oldComponent, newComponent);
                
                // -- Update GameObject names for clarity
                go.name = newName;

                // Remove the old component
                Object.DestroyImmediate(oldComponent);
            }
        }
        
        private static void CopyComponentReflection(Component source, Component destination)
        {
            // Get the type of both components
            Type typeSource = source.GetType();
            Type typeDest = destination.GetType();

            // Get fields from both components
            FieldInfo[] sourceFields = typeSource.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in sourceFields)
            {
                // Check if the destination has this field and if it can be written to
                FieldInfo destField = typeDest.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (destField != null && destField.IsPublic || destField.GetCustomAttribute(typeof(SerializeField)) != null)
                {
                    // Copy the value from the source to the destination
                    destField.SetValue(destination, field.GetValue(source));
                }
            }
        }


    }
}