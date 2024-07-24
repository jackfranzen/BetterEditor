// Derived from BetterEditor (https://github.com/jackfranzen/BetterEditor)
//  (See BetterEditor/LICENSE.txt for details)

using System;
using UnityEditor;
using UnityEngine;

namespace BetterEditor
{
    public static class EditorClassExtensions
    {
        public static bool DrawDefaultEditor_NoUpdates(this Editor editor)
        {
            EditorGUI.BeginChangeCheck();
            //editor.serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = editor.serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                    EditorGUILayout.PropertyField(iterator, true);
            }
            return EditorGUI.EndChangeCheck();
        }
        
    }
}