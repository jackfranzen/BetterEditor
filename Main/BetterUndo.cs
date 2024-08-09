
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BetterRuntime
{
    public static class BetterUndo
    {
        public static void DestroyObjectImmediate(UnityEngine.Object obj, bool immediateAtRunTime = true)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(obj);
#else
            if (immediateAtRunTime) 
                UnityEngine.Object.DestroyImmediate(obj);
            else
                UnityEngine.Object.Destroy(obj);
#endif
        }
        
        public static void SetCurrentGroupName(in string undoName = default)
        {
#if UNITY_EDITOR
            Undo.SetCurrentGroupName(undoName);
#endif
        }

        public static void RecordObject(UnityEngine.Object obj, in string undoName = default)
        {
#if UNITY_EDITOR
            Undo.RecordObject(obj, undoName);
#endif
        }

        public static void RegisterCompleteObjectUndo(UnityEngine.Object obj, in string undoName = default)
        {
#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(obj, undoName);
#endif
        }

        public static void RegisterCreatedObjectUndo(UnityEngine.Object obj, in string undoName = default)
        {
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(obj, undoName);
#endif
        }

        public static void RegisterSceneUndo(in string undoName = default)
        {
#if UNITY_EDITOR
            Undo.RegisterSceneUndo(undoName);
#endif
        }

        public static void PerformUndo()
        {
#if UNITY_EDITOR
            Undo.PerformUndo();
#endif
        }

        public static void PerformRedo()
        {
#if UNITY_EDITOR
            Undo.PerformRedo();
#endif
        }

        public static void ClearUndo(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            Undo.ClearUndo(obj);
#endif
        }

    }
    
    // There are probably more methods we're missing here, I just let the LLM go wild, feel free to add more...
}