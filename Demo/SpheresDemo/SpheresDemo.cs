using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BetterEditorDemos
{

    [Serializable]
    public class SpheresDemo_ColorData
    {
        public bool use = true;
        public Color Color = Color.cyan;
    }
    
    public class SpheresDemo : MonoBehaviour
    {
        public bool enablePreview = true;
        public int seed = 0;
        public int numSpheres = 10;
        [Range(0.02f, 1)] 
        public float radius = 1f; 
        public SpheresDemo_ColorData colorData;
        
        // -- Tracking Properties
        [FormerlySerializedAs("needsRebuild")] [HideInInspector][SerializeField] protected bool hasModifications = false;
        [HideInInspector][SerializeField] protected List<GameObject> createdObjects = new();
    }

    // [Serializable]
    // public enum E_BDemo_3D_Shape
    // {
    //     Cube,
    //     Sphere,
    // }
    //
    // [Serializable]
    // public struct BDemo_MaterialAndColors : IEquatable<BDemo_MaterialAndColors>
    // {
    //     public List<Material> material;
    //     public int materialToColorIndex;
    //     public List<Color> randomColors;
    //
    //     public bool Equals(BDemo_MaterialAndColors other)
    //     {
    //         if (materialToColorIndex != other.materialToColorIndex)
    //             return false;
    //         if (!material.SequenceEqual(other.material))
    //             return false;
    //         if (!randomColors.SequenceEqual(other.randomColors))
    //             return false;
    //         return true;
    //     }
    // }
    //
    // [Serializable]
    // public class BDemo_Location
    // {
    //     public bool zoneOriginUseVector = true;
    //     public Vector3 zoneOriginVector = Vector3.zero;
    //     public Transform zoneOriginTransform = null;
    // }
    //
    //
    // [Serializable]
    // public class BDemo_DistributeZone : IEquatable<BDemo_DistributeZone>
    // {
    //     public bool preview = false;
    //
    //     public E_BDemo_3D_Shape zoneShape = E_BDemo_3D_Shape.Cube;
    //     public float zoneRadius = 1;
    //     public Vector3 zoneCubeSize = Vector3.one;
    //
    //     public bool zoneOriginUseVector = true;
    //     public Vector3 zoneOriginVector = Vector3.zero;
    //     public Transform zoneOriginTransform = null;
    //
    //     public bool Equals(BDemo_DistributeZone other)
    //     {
    //         if (this == other)
    //             return true;
    //         if (other == null)
    //             return false;
    //         if (preview != other.preview)
    //             return false;
    //         if (zoneShape != other.zoneShape)
    //             return false;
    //         if (!Mathf.Approximately(zoneRadius, other.zoneRadius))
    //             return false;
    //         if (zoneCubeSize != other.zoneCubeSize)
    //             return false;
    //         if (zoneOriginUseVector != other.zoneOriginUseVector)
    //             return false;
    //         if (zoneOriginVector != other.zoneOriginVector)
    //             return false;
    //         if (zoneOriginTransform != other.zoneOriginTransform)
    //             return false;
    //
    //         return true;
    //     }
    // }
    //
    //
    // public class DistributeDemo : MonoBehaviour
    // {
    //     // -- Preview Properties
    //     public bool preview = false;
    //     public Color previewZoneColor = Color.red;
    //
    //     // -- Zone Properties
    //     public BDemo_DistributeZone zone;
    //
    //     // -- Distribute Properties
    //     public GameObject distributeObject = null;
    //     public Transform distributeTarget = null;
    //     public int distributeNum = 0;
    //     public bool distributeRandomSeed = false;
    //     [Range(-25,25)]
    //     public int distributeSeed = 0;
    //     public List<Material> distributeMats = new();
    //
    //     // -- Tracking Properties
    //     [HideInInspector][SerializeField] protected bool needsRebuild = false;
    //     [HideInInspector][SerializeField] protected List<GameObject> createdObjects = new();
    // }
    
}