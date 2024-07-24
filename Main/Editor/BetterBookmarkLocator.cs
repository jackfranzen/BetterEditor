using System.Collections.Generic;
using UnityEditor;

namespace BetterEditor
{

    public struct BetterBookmarkInfo
    {
        public string tag;
        public BetterBookmark bookmark;
        public string path;
        public string pathFull;
        
        public void Ping()
        {
            EditorGUIUtility.PingObject(bookmark);
        }
    }

    public static class BetterBookmarkLocator
    {
        public static Dictionary<string, BetterBookmarkInfo> bookmarks = new Dictionary<string, BetterBookmarkInfo>();
    
        public static BetterBookmarkInfo GetBookmark(string tag)
        {
            if (bookmarks.TryGetValue(tag, out var bookmark))
                return bookmark;
            RefreshAll();
            if(bookmarks.TryGetValue(tag, out var bookmarkSecondTry))
                return bookmarkSecondTry;
            throw new System.Exception($"Failed to find the BetterBookmark with tag: {tag} from {bookmarks.Count} bookmarks.");
        }
        
        public static void RefreshAll()
        {
            var guids = AssetDatabase.FindAssets("t:BetterBookmark");
            if (guids.Length == 0)
            {
                throw new System.Exception("No BetterBookmark found in the project.");
                return;
            }
            
            bookmarks.Clear();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var bookmark = AssetDatabase.LoadAssetAtPath<BetterBookmark>(assetPath);
                if (bookmark == null)
                    throw new System.Exception("Failed to load the BetterBookmark at path: " + assetPath);
                
                var info = new BetterBookmarkInfo
                {
                    tag = bookmark.tag,
                    bookmark = bookmark,
                    
                    path = System.IO.Path.GetDirectoryName(assetPath),
                    pathFull = assetPath
                };
                
                if(bookmarks.ContainsKey(bookmark.tag))
                    throw new System.Exception("Duplicate tag found in BetterBookmark: " + bookmark.tag + " at path: " + assetPath);
                bookmarks[bookmark.tag] = info;
            }
        }
    }
}
