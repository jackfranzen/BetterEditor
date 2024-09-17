# BetterEditor
- Having trouble tracking changes to serialized Properties across multiple components?
- Can't track changes effectively across undo, revert, paste, and reset?
- Tired of Script Hot-reloads erasing your Lists?
- Do you just want to learn to make a custom Editor?

## I have the solution, and it's in video format! (crowd boos)
https://www.youtube.com/watch?v=b-ir8KaJxik

# What is BetterEditor?

### Feature-Complete Demo demonstrating Custom Inspector
- Demonstrates multiple feature complete inspectors of various complexity for a component
- All expected Prefab and multi-select functionality included
- The Component is marked dirty when critical data is changed, pending another distribution
- Respond to data changes across complicated Undo/Redo/Revert/Reset operations.
- All Undo points make sense, and the chain is fully respected and described

### Easy Serialized Property Update Tracking
- Replace your SerializedProperties with BetterEditor's "Tracker"
- Easily check for changes to data across groups of Trackers
- Adaptive design, trackers are typeless and gathered via reflection easily.
- Lots of Logging options to ensure changes are being tracked effectively

### Quality of Life Extensions for SerializedProperty
- Number and Boolean methods.
- Safer Clamps/Min/Max via Enforce methods.
- Automatic GUIContent generation (including [Tooltip] grabber).
- ... and More.
  
### The BetterEditor Object
- A complete framework for an Editor, calls methods at the correct times.
- Provides a more reliable and adaptable ".targets" list.
- Triggers a full refresh when the UnityEditor is hot-reloaded from script changes.
- Informs whether updates are made from GUI interaction or undo/redo operations.
- Additional options for Logging / Triggering full refreshes.

### BetterGUI
- Provides missing "Serialized" GUI elements.
- Provides a RowBuilder framework to compact multiple properties to a single row.
- ... and More.

### BetterUndo
- Provides all of Unity's Undo methods with a `#if UNITY_EDITOR` preprocessor check.
- BetterUndo.DestroyImmediate will safely call the runtime version of Object.DestroyImmediate instead.

### BetterBookmark
- A tagged "ScriptableObject" which can be located along with its folder path.
- Useful for locating and loading assets (for editor use) without the use of /resource/ folders.
- Useful for publishing addons like this one, and used to power the demos.
