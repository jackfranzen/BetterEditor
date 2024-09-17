using System;
using System.Collections.Generic;
using BetterRuntime;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BetterEditorDemos
{

    
    // -- A Boolean / Color Pair, indicating an optional color override
    [Serializable]
    public class SpheresDemo_ColorData
    {
        public bool use = false;
        public Color color = Color.cyan;
    }
    
    // -- The main component class used in the demo
    public class DistributeDemoComponent : MonoBehaviour
    {
        // -- PREVIEW PROPERTIES
        public bool enablePreview = true;
        public SpheresDemo_ColorData previewColor;
        
        // -- ZONE PROPERTIES
        public int seed = 0;
        public int totalToGenerate = 10;
        [Range(0.5f, 15)] public float radius = 2f;  // (Range is permanently forced like this forever)
        
        // -- OBJECT PROPERTIES
        public List<GameObject> objectPrefabs = new();
        public SpheresDemo_ColorData objectColor;
        
        // -- Tracking Properties (stuff the user isn't changing)
        [HideInInspector][SerializeField] 
        protected bool hasModifications = false;
        [HideInInspector][SerializeField] 
        public List<GameObject> createdObjects = new();
        
        // -----------------
        //      Gizmos
        // -----------------
        private Color defaultPreviewColor = new Color(1, 1, 1, 0.7f);
        void OnDrawGizmosSelected()
        {
            // -- This method is called to draw gizmos when the object is selected
            
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
        
        
        // --------------------------------------
        //      Primary Distribution Logic
        // --------------------------------------
        
        public void Distribute(bool directSetHasModifications = false)
        {
            
// -- Undo is only available in the editor, so in order to allow Distribute() to run at runtime as well, we need
//      to wrap any Undo calls (Editor only) in a conditional block.
//             - This is more complicated with calls such as 'Undo.DestroyObjectImmediate' vs `Object.DestroyImmediate()`
//             - BetterEditor provides BetterUndo, which is safe to use in runtime, and handle the conditionals internally
#if UNITY_EDITOR
            
            // Undo.SetCurrentGroupName("Distribute Objects");
            // Undo.RegisterCompleteObjectUndo(this, default);
#endif
            
            // -- All operations done in a single response from the editor are automatically grouped,
            //         this line just renames the group (See "Undo History" window in Unity) 
            BetterUndo.SetCurrentGroupName("Distribute Objects");
            
            // -- Register a complete object undo, to preserve the state of the createdObjects List.
            //        - Using Undo.RecordObject() (or not recording at all) will cause the list to not revert on undo,
            //             filling it with empty references
            BetterUndo.RegisterCompleteObjectUndo(this);
            
            // -- Get Valid Objects
            var validObjects = objectPrefabs.FindAll(o => o != null);
            var hasValidObjects = validObjects.Count > 0;
            Material primitiveMaterial = null;
            List<Material> objectCustomMaterials = new();
            
            // -- Init Seed
            var usingSeed = seed;
            if (usingSeed == 0)
                usingSeed = Random.Range(1, 1000000);
            Random.InitState(usingSeed);
            
            // -- Delete Old Objects with BetterUndo
            foreach (var obj in createdObjects)
                if (obj != null)
                    BetterUndo.DestroyObjectImmediate(obj, true);
            createdObjects.Clear();
            
            // -- Create any new materials we need for each object
            if (objectColor.use && hasValidObjects)
            {
                foreach (var prefab in validObjects)
                {
                    var eachRender = prefab.GetComponent<Renderer>();
                    if (eachRender == null)
                        throw new Exception($"Given object for distribution {prefab.name} does not have a Renderer component");
                    var sharedMaterial = eachRender.sharedMaterial;
                    if (sharedMaterial == null)
                        continue;
                    var newMaterial = new Material(sharedMaterial);
                    newMaterial.color = objectColor.color;
                    objectCustomMaterials.Add(newMaterial);
                }
            }
            
            // -- Run Distribution
            for(var ii = 0; ii < totalToGenerate; ii++)
            {
                // -- Basic Props
                var position = transform.position + Random.insideUnitSphere * radius;
                var rotation = Random.rotation;
                
                // -- Create Object
                GameObject newObject;
                if (!hasValidObjects)
                {
                    // -- Create a sphere if we have no targets
                    newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    newObject.transform.parent = transform;
                    newObject.transform.position = position;
                    newObject.transform.rotation = rotation;
                    
                    // -- Set Material with custom Color
                    if (objectColor.use)
                    {
                        // -- Create shared material if we haven't
                        if (primitiveMaterial == null)
                        {
                            primitiveMaterial = new Material(newObject.GetComponent<Renderer>().sharedMaterial);
                            primitiveMaterial.color = objectColor.color;
                        }
                        newObject.GetComponent<Renderer>().sharedMaterial = primitiveMaterial;
                    }
                }
                else
                {
                    // -- Create a random object from the list
                    var objectIndex = Random.Range(0, validObjects.Count);
                    var prefab = validObjects[objectIndex];
                    newObject = Instantiate(prefab, position, rotation, transform);
                    
                    if (objectColor.use)
                        newObject.GetComponent<Renderer>().sharedMaterial = objectCustomMaterials[objectIndex];
                }
                
                // -- Register Undo and rename new object
                BetterUndo.RegisterCreatedObjectUndo(newObject);
                newObject.name = $"{name}-{ii}";
                
                // -- Set Color
                if (objectColor.use)
                {
                    var newRenderer = newObject.GetComponent<Renderer>();
                    if (newRenderer != null)
                        newRenderer.sharedMaterial.color = objectColor.color;
                }
                
                // -- Directly set hasModifications to false if flagged (we show two different ways to do this in the demos)
                if(directSetHasModifications)
                    hasModifications = false;
                
                // -- Track
                createdObjects.Add(newObject);
            }
            
            
        }
    }

    
}