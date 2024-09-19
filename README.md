# BetterEditor for Unity
- Having trouble tracking changes to serialized Properties across multiple components?
- Can't track changes effectively across undo, revert, paste, and reset?
- Tired of Script Hot-reloads erasing your Lists?
- Do you just want to learn to make a custom Editor?

Intro Video: https://www.youtube.com/watch?v=b-ir8KaJxik

# Correctly track changes to SerializedProperty in Inspector

### Problems with tracking SerializedProperty updates:

I've run into some situations where developers are having trouble tracking changes to `SerializedProperties` and I present a working solution here, as well as an open-source library to help deal with these issues. 

The first thing developers are told to use is often Unity's `BeginChangeCheck()` and `EndChangeCheck()` methods. If any UI was interacted with between these two calls, then the second method will return true. However, this **fails to detect complicated updates made by the user, including paste, revert, reset, and undo operations.** It also erroneously returns true if a non-essential foldout is toggled, or if an int-slider is interacted with (even without reaching the next notch in the int slider)

The second option is to use `SerializedObject.hasModifiedProperties` to tell if data has been changed. Unfortunately, this **also fails to catch changes caused by paste, undo, revert, and reset.**


### Solution:

The only way I've found to correctly track updates to SerializedProperties is to individually track their previous values and directly compare for changes. This also requires that the original value of `Property.hasMultipleDifferentValues` is tracked and compared, because `.hMDV` can change from true to false (or vice versa) from user interaction. In these cases, the primary value for the SerializedProperty being updated can (and will often) remain the same.


So for a given SerializedProperty:
```csharp
private SerializedProperty enablePreviewProp;
```

We'll also need to define:
```csharp
private bool prev_enablePreview = false;
private bool prevMulti_enablePreview = false;
```

We track the previous values of enablePreviewProp:
```csharp
prev_enablePreview = enablePreviewProp.boolValue;
prevMulti_enablePreview = enablePreviewProp.hasMultipleDifferentValues;
```

And later, we do the comparison
```csharp
updated_previewEnabled |= prev_enablePreview != enablePreviewProp.boolValue;
updated_previewEnabled |= prevMulti_enablePreview != enablePreviewProp.hasMultipleDifferentValues;
```

If an update is detected, then the two "prev_" values need to be set to the new values so that we can check for more changes in the future.

If you run this comparison **AFTER drawing the UI** (and making any automatic changes to property values), then you can **effectively measure all changes to your SerializedProperties, including those made from Undo, Redo, Paste, Revert, and Reset.**

### Examples:

Examples are included in the BetterEditor library I've published, more on this below. 

The Demo scene contains 6 stages. 
 - Stage 1 has an example of EndChangeCheck() failing
 - Stage 2 has an example of hasModifiedProperties failing
 - Stage 3 shows this approach applied and working correctly. 

I've also made a video (with timestamps) that's more of a tutorial, but it explains these issues in detail alongside the demo scene. It also includes some more explanation on the subject for beginners, and a first-look at how to use the BetterEditor library to help with these issues.

If this stuff interests you at all, I hope you'll check out the video!
https://www.youtube.com/watch?v=b-ir8KaJxik

# What is BetterEditor?

BetterEditor primarily provides "Trackers" to help deal with our issue from before. These replace SerializedProperty as a wrapper for each Property. 

Instead of creating three variables for each SerializedProperty, we replace the property with a Tracker. (The target property name 
is given to the Tracker immediately)
```csharp
private Tracker enablePreviewTracker = new( nameof(_demoComponent.enablePreview) );
```

The tracker (and its internal SerializedProperty) are initialized via the Track Method:
```csharp
enablePreviewTracker.Track(serializedObject.AsSource());
```

Now we can quickly tell if the property was updated, with full logging support
```csharp
bool updated = Tracker.WasUpdated()
updated = Tracker.WasUpdated( ETrackLog.LogIfUpdated )
updated = Tracker.WasUpdated( ETrackLog.Log )
```

And to refresh Tracking, we can call either:
```csharp
Tracker.RefreshTracking()
```
```csharp
enablePreviewTracker.Track(serializedObject.AsSource()); (again)
```



# Advantages of Using Tracker

- Single variable per Property (instead of 3)
- Tracker provides a `.prop` value to get instant access to the original SerializedProperty.
- Tracker provides a `.content` `GUIContent` which is automatically generated from `[Tooltip]` and `[DisplayName]`
- Trackers are type-agnostic (you can change the type of the original property they refer to without any other code changes)
- Tracker comparisons are safe - floating point values are compared using the proper methods
- Trackers provide detailed logging quickly
- ListTrackers can be used to track updates to array-type SerializedProperties. 

# TrackerGroups:

TrackerGroups `HashSet<ITrack>` can have multiple Trackers added to them, such that `TrackerGroup.WasUpdated()` will return true
if any member of the group was updated. Groups can be populated via reflection easily, and `TrackerGroup.Track(SerializedObject.AsSource())` will cause all child Trackers to begin tracking from the provided SerializedObject. 

TrackerGroup can be made relative using `SetAsRelativeTracker(string PropName)` such that its children Trackers are populated relative to a given property. This is shown in the 5th step of the demo, and used to make a "mini-Editor" for a commonly reused sub-class  

# Full Example

```csharp
private static readonly DistributeDemoComponent06 TARGET;

public Tracker enablePreviewTracker = new(nameof(TARGET.enablePreview));
private Tracker seedTracker = new( nameof(TARGET.seed) );
private Tracker radiusTracker = new( nameof(TARGET.radius) );
private Tracker totalToGenerateTracker = new( nameof(TARGET.totalToGenerate) );
private ListTracker objectPrefabsTracker = new( nameof(TARGET.objectPrefabs) );

// -- Tracker Collections (So we can check which category was updated)
public TrackerGroup allComponentTrackers = new (typeof(DistributeDemoComponent06) );
public TrackerGroup previewTrackers = new();
public TrackerGroup importantTrackers = new();

public void OnEnable()
{
    // -- Setup Different collections to track changes to different sets of data
    allComponentTrackers.PopulateWithReflection(this);
    previewTrackers = new TrackerGroup { enablePreviewTracker };
    importantTrackers = new TrackerGroup { seedTracker, radiusTracker, totalToGenerateTracker, objectPrefabsTracker };
    
    // -- Other Serialized
    hasModificationsProp = serializedObject.FindPropertyChecked("hasModifications");
    createdObjectsProp = serializedObject.FindPropertyChecked(nameof(TARGET.createdObjects));
    
    // -- Start our component trackers, like before
    allComponentTrackers.Track(serializedObject.AsSource());
}

public void OnInspectorGUI()
{
    serializedObject.Update();
    
    // -- Throw error if group has not been given a started Tracking yet
    allComponentTrackers.CheckTracking();
    
    // -- Draw the UI
    DrawMainUI();

    // -- Log all Updates:
    allComponentTrackers.WasUpdated( ETrackLog:LogIfUpdated );

    // -- Check for important updates 
    var imporantUpdateDetected = importantTrackers.WasUpdated();
    if (updatedDetected)
    {
        Debug.Log("Updates Detected!");
        allComponentTrackers.RefreshTracking();
    }

    serializedObject.ApplyModifiedProperties();
}


```


# Check out the full list of BetterEditor Features

## Feature-Complete Demo demonstrating Custom Inspector
- Demonstrates multiple feature complete inspectors of various complexity for a component
- All expected Prefab and multi-select functionality included
- The Component is marked dirty when critical data is changed, pending another distribution
- Respond to data changes across complicated Undo/Redo/Revert/Reset operations.
- All Undo points make sense, and the chain is fully respected and described

## Easy Serialized Property Update Tracking
- Replace your SerializedProperties with BetterEditor's "Tracker"
- Easily check for changes to data across groups of Trackers
- Adaptive design, trackers are typeless and gathered via reflection easily.
- Lots of Logging options to ensure changes are being tracked effectively

## Quality of Life Extensions for SerializedProperty
- Number and Boolean methods.
- Safer Clamps/Min/Max via Enforce methods.
- Automatic GUIContent generation (including [Tooltip] grabber).
- ... and More.
  
## The BetterEditor Object
- A complete framework for an Editor, calls methods at the correct times.
- Provides a more reliable and adaptable ".targets" list.
- Triggers a full refresh when the UnityEditor is hot-reloaded from script changes.
- Informs whether updates are made from GUI interaction or undo/redo operations.
- Additional options for Logging / Triggering full refreshes.

## BetterGUI
- Provides missing "Serialized" GUI elements.
- Provides a RowBuilder framework to compact multiple properties to a single row.
- ... and More.

## BetterUndo
- Provides all of Unity's Undo methods with a `#if UNITY_EDITOR` preprocessor check.
- BetterUndo.DestroyImmediate will safely call the runtime version of Object.DestroyImmediate instead.

## BetterBookmark
- A tagged "ScriptableObject" which can be located along with its folder path.
- Useful for locating and loading assets (for editor use) without the use of /resource/ folders.
- Useful for publishing addons like this one, and used to power the demos.
