using System;
using System.Collections.Generic;
using System.Reflection;
using BetterEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace BetterEditorDemos
{
    
    
    public enum EDistributeDemoStages
    {
        BarebonesEditor = 1,
        BasicSerialized = 2,
        FullSerialized = 3,
        BetterTrackers = 4,
        UsingGroups = 5,
        UsingEverything = 6,
    }

    
    public struct DistributeDemo_StageInfo
    {
        public EDistributeDemoStages stage;
        public string title;
        public string description;
        public List<string> greenTexts;
        public List<string> redTexts;
        public string fileName;
    }
    
    public struct DistributeDemo_Stage
    {
        public EDistributeDemoStages stage;
        public System.Type component;
    }
    
    

    
    
    public static class DistributeDemoEditorCommon
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
        
        
        
        public static DistributeDemo_Stage Demo_01 = new ()
        {
            stage = EDistributeDemoStages.BarebonesEditor,
            component = typeof(DistributeDemoComponent01),
        };

        public static DistributeDemo_Stage Demo_02 = new ()
        {
            stage = EDistributeDemoStages.BasicSerialized,
            component = typeof(DistributeDemoComponent02),
        };
        
        public static DistributeDemo_Stage Demo_03 = new ()
        {
            stage = EDistributeDemoStages.FullSerialized,
            component = typeof(DistributeDemoComponent03),
        };
        
        public static DistributeDemo_Stage Demo_04 = new ()
        {
            stage = EDistributeDemoStages.BetterTrackers,
            component = typeof(DistributeDemoComponent04),
        };
        
        public static DistributeDemo_Stage Demo_05 = new ()
        {
            stage = EDistributeDemoStages.UsingGroups,
            component = typeof(DistributeDemoComponent05),
        };
        
        public static DistributeDemo_Stage Demo_06 = new ()
        {
            stage = EDistributeDemoStages.UsingEverything,
            component = typeof(DistributeDemoComponent06),
        };
        
        public static DistributeDemo_Stage DemoByEnum(EDistributeDemoStages stage)
        {
            switch (stage)
            {
                case EDistributeDemoStages.BarebonesEditor:
                    return Demo_01;
                case EDistributeDemoStages.BasicSerialized:
                    return Demo_02;
                case EDistributeDemoStages.FullSerialized:
                    return Demo_03;
                case EDistributeDemoStages.BetterTrackers:
                    return Demo_04;
                case EDistributeDemoStages.UsingGroups:
                    return Demo_05;
                case EDistributeDemoStages.UsingEverything:
                    return Demo_06;
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
        
        //public static EDistributeDemoStages currentSelection = EDistributeDemoStages.UnityChangeChecking;
        
        
        public static bool DrawDemoInfo(in DistributeDemo_StageInfo demoStageInfo)//, Editor editor)
        {
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            var previousStage = demoStageInfo.stage;
            var newStage = previousStage;
            
            var fullPath = EditorsPath + "/" + demoStageInfo.fileName;
            currentEditorScript = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label( "Demo: ", DemoPrefixHeaderStyle, GUILayout.ExpandWidth(false));
            GUILayout.Label(demoStageInfo.title, DemoHeaderStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(4);

            // -- Horizontal Row with Controls
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(25)))
            {
                // -- Enum Popup (centered vertically using BetterEditorGUI scope)
                var width1 = GUILayout.Width(180);
                using (new CenterVerticalScope(width1))
                    newStage = (EDistributeDemoStages)EditorGUILayout.EnumPopup(GUIContent.none, previousStage, width1);

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
            EditorGUILayout.LabelField( demoStageInfo.description, EditorStyles.wordWrappedLabel );
            
            // -- Draw Green Text
            DrawBulletPointsText(demoStageInfo.greenTexts, GreenWrappedTextStyle());
            
            // -- Draw Red Text
            DrawBulletPointsText(demoStageInfo.redTexts, RedWrappedTextStyle());
            
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

        public static void DrawDemoInfoAndApplyRow(in DistributeDemo_StageInfo demoStageInfo, bool hasModifications, out bool updatedStage, out bool pressedApply)
        {
            // -- Information about this demo, and controls to swap
            updatedStage = DrawDemoInfo(demoStageInfo);

            // -- Draw a custom row with a button
            EditorGUILayout.BeginHorizontal();
            if (hasModifications)
                EditorGUILayout.LabelField($"Modifications Detected", ModificationsStyle, GUILayout.ExpandWidth(false));
            else
                EditorGUILayout.LabelField($"No Modifications Detected", DemoHeaderStyle, GUILayout.ExpandWidth(false));
            pressedApply = GUILayout.Button(new GUIContent("Apply"));
            EditorGUILayout.EndHorizontal();
            
            // -- Divider
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            // -- Un-focus on Apply
            if (pressedApply)
                GUI.FocusControl(null);
            
        }

        public static bool DrawApplyRowSerialized(SerializedProperty hasModifications)
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
            var pressedApply = false;
            EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            EditorGUILayout.LabelField(hasModificationsText, style, GUILayout.ExpandWidth(false));
            pressedApply = GUILayout.Button(new GUIContent("Apply"));
            EditorGUILayout.EndHorizontal();

            // -- Divider
            BetterEditorGUI.DrawBoxWithColor(Color.grey, dividerStyle);
            
            if(pressedApply)
                GUI.FocusControl(null);
            
            // -- Clear Modifications on object
            return pressedApply;
            
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
        
        // -- Helper method to quickly call Distribute on multiple components
        public static void Distribute(IEnumerable<Object> objects, bool directSetHasModifications = false)
        {
            foreach (var obj in objects)
                if (obj is DistributeDemoComponent demo)
                    demo.Distribute(directSetHasModifications);
        }
        
        


    }
}