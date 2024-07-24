using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BetterBookmark))]
    public class BetterBookmarkEditor : Editor
    {
        private const string longTextDescription =
            "Folder bookmarks like this one can be placed around a project and found easily by editor scripts." +
            "Each bookmark should have a unique tag. " +
            "Folder bookmarks are Editor only objects!";
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Better Bookmark", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(longTextDescription, EditorStyles.wordWrappedLabel);
            
            GUILayout.Space(2);
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tag"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}