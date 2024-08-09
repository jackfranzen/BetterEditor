using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BetterEditorDemos
{

    [Serializable]
    public class SpheresDemo_ColorData
    {
        public bool use = false;
        [FormerlySerializedAs("Color")] public Color color = Color.cyan;
    }
    
    public class SpheresDemo : MonoBehaviour
    {
        // -- Public Properties, used by editor
        // [Header("Preview")] (Drawn along with PropertyField(enablePreview), which is undesirable when doing a full custom inspector)
        public bool enablePreview = true;
        public SpheresDemo_ColorData previewColor;
        
        public int seed = 0;
        [Range(0.5f, 15)]  // (Forced, forever)
        public float radius = 2f; 
        
        public int numResults = 10;
        public SpheresDemo_ColorData sphereColor;
        
        // -- Tracking Properties
        [HideInInspector][SerializeField] 
        protected bool hasModifications = false;
        [HideInInspector][SerializeField] 
        protected List<GameObject> createdObjects = new();
        
        
        // This method is called to draw gizmos when the object is selected
        private Color defaultPreviewColor = new Color(1, 1, 1, 0.7f);
        void OnDrawGizmosSelected()
        {
            // -- Preview Disabled
            if(!enablePreview)
                return;
            
            // -- Draw the spheres
            if(previewColor.use)
            {
                var color = previewColor.color;
                color.a *= 0.4f;
                Gizmos.color = color;
                Gizmos.DrawSphere(transform.position, radius);
                Gizmos.color = previewColor.color;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
            else
            {
                Gizmos.color = defaultPreviewColor;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }

    
}