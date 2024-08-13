# What is BetterEditor?

### Demos for Editor Development
- Shows how to create a feature complete, fully customized Editor for a component.
- Responds to changes across complicated Undo/Redo/Revert/Reset operations.

### SerializedTrackers
- Provides full information of exactly which properties have been updated and how.
- Trackers completely replace SerializedProperties.
- Adapts easily to changes in source component data-structure.
- Trackers can be gathered via reflection, improving adaptability.

### The BetterEditor Object
- A complete framework for an Editor, calls methods at the correct times.
- Provides a more reliable and adaptable ".targets" list.
- Triggers a full refresh when the UnityEditor is hot-reloaded from script changes.
- Informs whether updates are made from GUI, or undo/redo operations.
- Additional options for Logging / Triggering full refreshes.

### Quality of Life Extensions for SerializedProperty
- Number and Boolean methods.
- Safer Clamps/Min/Max via Enforce methods.
- Automatic GUIContent generation (including [Tooltip] grabber).
- ... and More.

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
