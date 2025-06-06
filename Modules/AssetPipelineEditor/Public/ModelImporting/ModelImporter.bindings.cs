// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEditor
{
    // Mesh data that can be optimized to improve rendering quality
    [Flags]
    public enum MeshOptimizationFlags
    {
        PolygonOrder = 1 << 0,
        VertexOrder = 1 << 1,

        Everything = ~0
    }

    public enum ClipAnimationMaskType
    {
        CreateFromThisModel = 0,

        CopyFromOther = 1,
        None = 3
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Modules/Animation/AvatarMask.h", IntermediateScriptingStructName = "MonoTransformMaskElement")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.bindings.h")]
    internal partial struct TransformMaskElement
    {
        public string path;
        public float weight;
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions = CodegenOptions.Custom, IntermediateScriptingStructName = "MonoClipAnimationInfoCurve")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ClipAnimationInfoCurve
    {
        public string name;
        public AnimationCurve curve;
    }

    [UsedByNativeCode]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom, IntermediateScriptingStructName = "MonoClipAnimationInfo")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.bindings.h")]
    public sealed partial class ModelImporterClipAnimation
    {
        string m_TakeName;
        string m_Name;
        float m_FirstFrame;
        float m_LastFrame;
        WrapMode m_WrapMode;
        int m_Loop;

        float m_OrientationOffsetY;
        float m_Level;
        float m_CycleOffset;
        float m_AdditiveReferencePoseFrame;

        int m_HasAdditiveReferencePose;
        int m_LoopTime;
        int m_LoopBlend;
        int m_LoopBlendOrientation;
        int m_LoopBlendPositionY;
        int m_LoopBlendPositionXZ;
        int m_KeepOriginalOrientation;
        int m_KeepOriginalPositionY;
        int m_KeepOriginalPositionXZ;
        int m_HeightFromFeet;
        int m_Mirror;
        int m_MaskType = 3;
        AvatarMask m_MaskSource;

        int[] m_BodyMask;
        AnimationEventBlittable[] m_AnimationEventsBlittable;
        ClipAnimationInfoCurve[] m_AdditionnalCurves;
        TransformMaskElement[] m_TransformMask;

        bool m_MaskNeedsUpdating;

        long internalID;

        public string takeName { get { return m_TakeName; } set { m_TakeName = value; } }
        public string name { get { return m_Name; } set { m_Name = value; } }
        public float firstFrame { get { return m_FirstFrame; } set { m_FirstFrame = value; } }
        public float lastFrame { get { return m_LastFrame; } set { m_LastFrame = value; } }

        public WrapMode wrapMode { get { return m_WrapMode; } set { m_WrapMode = value; } }

        public bool loop { get { return m_Loop != 0; } set { m_Loop = value ? 1 : 0; } }

        public float rotationOffset { get { return m_OrientationOffsetY; } set { m_OrientationOffsetY = value; } }

        public float heightOffset { get { return m_Level; } set { m_Level = value; } }

        public float cycleOffset { get { return m_CycleOffset; } set { m_CycleOffset = value; } }

        public bool loopTime { get { return m_LoopTime != 0; } set { m_LoopTime = value ? 1 : 0; } }

        public bool loopPose { get { return m_LoopBlend != 0; } set { m_LoopBlend = value ? 1 : 0; } }

        public bool lockRootRotation { get { return m_LoopBlendOrientation != 0; } set { m_LoopBlendOrientation = value ? 1 : 0; } }

        public bool lockRootHeightY { get { return m_LoopBlendPositionY != 0; } set { m_LoopBlendPositionY = value ? 1 : 0; } }

        public bool lockRootPositionXZ { get { return m_LoopBlendPositionXZ != 0; } set { m_LoopBlendPositionXZ = value ? 1 : 0; } }

        public bool keepOriginalOrientation { get { return m_KeepOriginalOrientation != 0; } set { m_KeepOriginalOrientation = value ? 1 : 0; } }

        public bool keepOriginalPositionY { get { return m_KeepOriginalPositionY != 0; } set { m_KeepOriginalPositionY = value ? 1 : 0; } }

        public bool keepOriginalPositionXZ { get { return m_KeepOriginalPositionXZ != 0; } set { m_KeepOriginalPositionXZ = value ? 1 : 0; } }

        public bool heightFromFeet { get { return m_HeightFromFeet != 0; } set { m_HeightFromFeet = value ? 1 : 0; } }

        public bool mirror { get { return m_Mirror != 0; } set { m_Mirror = value ? 1 : 0; } }

        public ClipAnimationMaskType maskType { get { return (ClipAnimationMaskType)m_MaskType; } set { m_MaskType = (int)value; } }

        public AvatarMask maskSource { get { return m_MaskSource; } set { m_MaskSource = value; } }

        public AnimationEvent[] events { get { return m_AnimationEventsBlittable.Select(AnimationEventBlittable.ToAnimationEvent).ToArray(); } set { m_AnimationEventsBlittable = value.Select(AnimationEventBlittable.FromAnimationEvent).ToArray(); } }
        public ClipAnimationInfoCurve[] curves { get { return m_AdditionnalCurves; } set { m_AdditionnalCurves = value; } }

        public bool maskNeedsUpdating { get { return m_MaskNeedsUpdating; } }

        public float additiveReferencePoseFrame { get { return m_AdditiveReferencePoseFrame; } set { m_AdditiveReferencePoseFrame = value; } }
        public bool hasAdditiveReferencePose { get { return m_HasAdditiveReferencePose != 0; } set { m_HasAdditiveReferencePose = value ? 1 : 0; } }

        public void ConfigureMaskFromClip(ref AvatarMask mask)
        {
            mask.transformCount = this.m_TransformMask.Length;
            for (int i = 0; i < mask.transformCount; i++)
            {
                mask.SetTransformPath(i, this.m_TransformMask[i].path);
                mask.SetTransformActive(i, this.m_TransformMask[i].weight > 0f);
            }
            for (int i = 0; i < this.m_BodyMask.Length; i++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, m_BodyMask[i] != 0);
            }
        }

        public void ConfigureClipFromMask(AvatarMask mask)
        {
            this.m_TransformMask = new TransformMaskElement[mask.transformCount];
            for (int i = 0; i < mask.transformCount; i++)
            {
                m_TransformMask[i].path = mask.GetTransformPath(i);
                m_TransformMask[i].weight = mask.GetTransformActive(i) ? 1f : 0f;
            }
            m_BodyMask = new int[(int)AvatarMaskBodyPart.LastBodyPart];

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                m_BodyMask[i] = mask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i) ? 1 : 0;
            }
        }

        public override bool Equals(object o)
        {
            ModelImporterClipAnimation other = o as ModelImporterClipAnimation;
            return other != null && takeName == other.takeName && name == other.name && firstFrame == other.firstFrame && lastFrame == other.lastFrame && m_WrapMode == other.m_WrapMode && m_Loop == other.m_Loop &&
                loopPose == other.loopPose && lockRootRotation == other.lockRootRotation && lockRootHeightY == other.lockRootHeightY && lockRootPositionXZ == other.lockRootPositionXZ &&
                mirror == other.mirror && maskType == other.maskType && maskSource == other.maskSource && additiveReferencePoseFrame == other.additiveReferencePoseFrame && hasAdditiveReferencePose == other.hasAdditiveReferencePose;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    [System.Obsolete("Use ModelImporterMaterialName, ModelImporter.materialName and ModelImporter.materialImportMode instead", true)]
    public enum ModelImporterGenerateMaterials
    {
        [System.Obsolete("Use ModelImporter.materialImportMode=None instead", true)]
        None = 0,
        [System.Obsolete("Use ModelImporter.materialImportMode=Import and ModelImporter.materialName=ModelImporterMaterialName.BasedOnTextureName instead", true)]
        PerTexture = 1,
        [System.Obsolete("Use ModelImporter.materialImportMode=Import and ModelImporter.materialName=ModelImporterMaterialName.BasedOnModelNameAndMaterialName instead", true)]
        PerSourceMaterial = 2,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialName
    {
        BasedOnTextureName = 0,

        BasedOnMaterialName = 1,

        BasedOnModelNameAndMaterialName = 2,

        [System.Obsolete("You should use ModelImporterMaterialName.BasedOnTextureName instead, because it it less complicated and behaves in more consistent way.")]
        BasedOnTextureName_Or_ModelNameAndMaterialName = 3,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialSearch
    {
        Local = 0,

        RecursiveUp = 1,

        Everywhere = 2,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialLocation
    {
        [InspectorName("Use External Materials (Legacy)")]
        [Tooltip("Use external materials if found in the project.")]
        External = 0,
        [InspectorName("Use Embedded Materials")]
        [Tooltip("Embed the material inside the imported asset.")]
        InPrefab = 1
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialImportMode
    {
        [Tooltip("Do not import materials")]
        None = 0,
        [InspectorName("Standard (Legacy)")]
        [Tooltip("Use the standard Material import method.")]
        ImportStandard = 1,
        [InspectorName("Import via MaterialDescription")]
        [Tooltip("Use AssetPostprocessor.OnPreprocessMaterialDescription.")]
        ImportViaMaterialDescription = 2,

        [System.Obsolete("Use ImportStandard (UnityUpgradable) -> ImportStandard")]
        LegacyImport = 1,
        [System.Obsolete("Use ImportViaMaterialDescription (UnityUpgradable) -> ImportViaMaterialDescription")]
        Import = 2
    }

    public enum ModelImporterTangentSpaceMode
    {
        [System.Obsolete("Use ModelImporterNormals.Import instead")]
        Import = 0,
        [System.Obsolete("Use ModelImporterNormals.Calculate instead")]
        Calculate = 1,
        [System.Obsolete("Use ModelImporterNormals.None instead")]
        None = 2,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ImportMesh.h")]
    public enum ModelImporterNormals
    {
        Import = 0,

        Calculate = 1,

        None = 2,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ImportMesh.h")]
    public enum ModelImporterNormalCalculationMode
    {
        [InspectorName("Unweighted (Legacy)")]
        Unweighted_Legacy,

        Unweighted,
        AreaWeighted,
        AngleWeighted,
        AreaAndAngleWeighted
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ImportMesh.h")]
    public enum ModelImporterNormalSmoothingSource
    {
        PreferSmoothingGroups = 0,
        FromSmoothingGroups = 1,
        FromAngle = 2,
        None = 3
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ImportMesh.h")]
    public enum ModelImporterTangents
    {
        Import = 0,

        CalculateLegacy = 1,

        CalculateLegacyWithSplitTangents = 4,
        [InspectorName("Calculate Mikktspace")]
        CalculateMikk = 3,

        None = 2,
    }

    public enum ModelImporterMeshCompression
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }

    public enum ModelImporterIndexFormat
    {
        Auto = 0,
        [InspectorName("16 bits")]
        UInt16 = 1,
        [InspectorName("32 bits")]
        UInt32 = 2,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterAnimationCompression
    {
        Off = 0,

        KeyframeReduction = 1,

        KeyframeReductionAndCompression = 2,

        Optimal = 3
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterGenerateAnimations
    {
        None = 0,

        GenerateAnimations = 4,

        InRoot = 3,

        InOriginalRoots = 1,

        InNodes = 2
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterAnimationType
    {
        None = 0,

        Legacy = 1,

        Generic = 2,

        Human = 3
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterHumanoidOversampling
    {
        X1 = 1,

        X2 = 2,

        X4 = 4,

        X8 = 8
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterSecondaryUVMarginMethod
    {
        Manual = 0,

        Calculate = 1,
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    public enum ModelImporterAvatarSetup
    {
        NoAvatar = 0,
        [Tooltip("Create an Avatar based on the model from this file.")]
        CreateFromThisModel = 1,
        [Tooltip("Copy an Avatar from another file to import muscle clip. No avatar will be created.")]
        [InspectorName("Copy From Other Avatar")]
        CopyFromOther = 2,
    }

    public enum ModelImporterSkinWeights
    {
        Standard = 0,
        Custom = 1,
    }

    [UsedByNativeCode]
    [NativeType(Header = "Editor/Src/Animation/HumanTemplate.h")]
    public sealed partial class HumanTemplate : Object
    {
        public HumanTemplate()
        {
            Internal_Create(this);
        }

        extern private static void Internal_Create([Writable] HumanTemplate self);

        extern public void Insert(string name, string templateName);

        extern public string Find(string name);

        extern public void ClearTemplate();
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public partial struct TakeInfo
    {
        public string name;
        public string defaultClipName;
        public float startTime;
        public float stopTime;
        public float bakeStartTime;
        public float bakeStopTime;
        public float sampleRate;
    }

    [NativeType(Header = "Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.h")]
    [NativeHeader("Modules/AssetPipelineEditor/Public/ModelImporting/ModelImporter.bindings.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/AvatarBuilder.bindings.h")]
    public partial class ModelImporter : AssetImporter
    {
        const string obsoleteGenerateMaterials = "generateMaterials has been  removed. Use materialImportMode, materialName and materialSearch instead.";
        [System.Obsolete(obsoleteGenerateMaterials, true)]
        public ModelImporterGenerateMaterials generateMaterials => throw new NotSupportedException(obsoleteGenerateMaterials);

        const string obsoleteImportMaterials = "importMaterials has been  removed. Use materialImportMode instead.";
        [System.Obsolete(obsoleteImportMaterials, true)]
        public bool importMaterials => throw new NotSupportedException(obsoleteImportMaterials);

        public extern ModelImporterMaterialName materialName
        {
            get;
            set;
        }

        public extern ModelImporterMaterialSearch materialSearch
        {
            get;
            set;
        }

        public extern ModelImporterMaterialLocation materialLocation { get; set; }

        internal extern SourceAssetIdentifier[] sourceMaterials
        {
            [FreeFunction(Name = "ModelImporterBindings::GetSourceMaterials", HasExplicitThis = true)]
            get;
        }

        public extern float globalScale
        {
            get;
            set;
        }

        public extern bool isUseFileUnitsSupported
        {
            [NativeMethod("IsUseFileUnitsSupported")]
            get;
        }

        public extern bool importVisibility
        {
            get;
            set;
        }

        public extern bool useFileUnits
        {
            get;
            set;
        }

        public extern float fileScale
        {
            get;
        }

        public extern bool useFileScale
        {
            get;
            set;
        }

        [System.Obsolete("Use useFileScale instead")]
        public bool isFileScaleUsed
        {
            get { return useFileScale; }
        }

        public extern bool importBlendShapes
        {
            get;
            set;
        }

        public extern bool importBlendShapeDeformPercent
        {
            get;
            set;
        }

        public extern bool importCameras
        {
            get;
            set;
        }

        public extern bool importLights
        {
            get;
            set;
        }

        public extern bool addCollider
        {
            [NativeMethod("GetAddColliders")]
            get;
            [NativeMethod("SetAddColliders")]
            set;
        }

        public extern float normalSmoothingAngle
        {
            get;
            set;
        }

        [System.Obsolete("Please use tangentImportMode instead")]
        public bool splitTangentsAcrossSeams
        {
            get
            {
                return importTangents == ModelImporterTangents.CalculateLegacyWithSplitTangents;
            }
            set
            {
                if (importTangents == ModelImporterTangents.CalculateLegacyWithSplitTangents && !value)
                    importTangents = ModelImporterTangents.CalculateLegacy;
                else if (importTangents == ModelImporterTangents.CalculateLegacy && value)
                    importTangents = ModelImporterTangents.CalculateLegacyWithSplitTangents;
            }
        }

        public extern bool swapUVChannels
        {
            get;
            set;
        }

        public extern bool weldVertices
        {
            get;
            set;
        }

        public extern bool bakeAxisConversion
        {
            get;
            set;
        }

        public extern bool optimizeBones
        {
            get;
            set;
        }

        public extern bool keepQuads
        {
            get;
            set;
        }

        public extern ModelImporterIndexFormat indexFormat
        {
            get;
            set;
        }

        public extern bool preserveHierarchy
        {
            get;
            set;
        }

        public extern bool generateSecondaryUV
        {
            get;
            set;
        }

        public extern float secondaryUVAngleDistortion
        {
            get;
            set;
        }

        public extern float secondaryUVAreaDistortion
        {
            get;
            set;
        }

        public extern float secondaryUVHardAngle
        {
            get;
            set;
        }

        public extern ModelImporterSecondaryUVMarginMethod secondaryUVMarginMethod
        {
            get;
            set;
        }

        public extern float secondaryUVPackMargin
        {
            get;
            set;
        }

        public extern float secondaryUVMinLightmapResolution
        {
            get;
            set;
        }

        public extern float secondaryUVMinObjectScale
        {
            get;
            set;
        }

        public extern bool generateMeshLods
        {
            get;
            set;
        }

        public extern MeshLodUtility.LodGenerationFlags meshLodGenerationFlags
        {
            get;
            set;
        }


        public extern int maximumMeshLod
        {
            get;
            set;
        }

        public extern ModelImporterGenerateAnimations generateAnimations
        {
            [NativeMethod("GetLegacyGenerateAnimations")]
            get;
            [NativeMethod("SetLegacyGenerateAnimations")]
            set;
        }

        public extern TakeInfo[] importedTakeInfos
        {
            get;
        }

        public extern string[] transformPaths
        {
            get;
        }

        public string[] referencedClips
        {
            get { return INTERNAL_GetReferencedClips(this); }
        }
        [FreeFunction("ModelImporterBindings::GetReferencedClips")]
        private extern static string[] INTERNAL_GetReferencedClips(ModelImporter self);

        public static string[] GetReferencedClipsForModelPath(string modelPath)
        {
            return INTERNAL_GetReferencedClipsForModelPath(modelPath);
        }
        [FreeFunction("ModelImporterBindings::GetReferencedClipsForModelPath")]
        private extern static string[] INTERNAL_GetReferencedClipsForModelPath(string modelPath);

        public extern bool isReadable
        {
            get;
            set;
        }

        public extern MeshOptimizationFlags meshOptimizationFlags
        {
            get;
            set;
        }

        public bool optimizeMeshPolygons
        {
            get
            {
                return (meshOptimizationFlags & MeshOptimizationFlags.PolygonOrder) != 0;
            }
            set
            {
                if (value)
                    meshOptimizationFlags |= MeshOptimizationFlags.PolygonOrder;
                else
                    meshOptimizationFlags &= ~MeshOptimizationFlags.PolygonOrder;
            }
        }

        public bool optimizeMeshVertices
        {
            get
            {
                return (meshOptimizationFlags & MeshOptimizationFlags.VertexOrder) != 0;
            }
            set
            {
                if (value)
                    meshOptimizationFlags |= MeshOptimizationFlags.VertexOrder;
                else
                    meshOptimizationFlags &= ~MeshOptimizationFlags.VertexOrder;
            }
        }

        [System.Obsolete("optimizeMesh is deprecated. Use optimizeMeshPolygons and/or optimizeMeshVertices instead.  Note that optimizeMesh false equates to optimizeMeshPolygons true and optimizeMeshVertices false while optimizeMesh true equates to both true")]
        public bool optimizeMesh
        {
            // Legacy property that has been replaced with 'optimizeMeshPolygons' and 'optimizeMeshVertices' to provide more granular mesh optimization control
            get { return meshOptimizationFlags != 0; }
            set
            {
                if (value)
                {
                    // Original single flag 'optimizeMesh' caused both polygons and vertices to be optimized when true so emulate that behaviour
                    meshOptimizationFlags = MeshOptimizationFlags.Everything;
                }
                else
                {
                    // Original single flag 'optimizeMesh' caused polygons but not vertices to be optimized when false so emulate that behaviour
                    optimizeMeshPolygons = true;
                    optimizeMeshVertices = false;
                }
            }
        }

        public extern ModelImporterSkinWeights skinWeights
        {
            [NativeMethod("GetSkinWeightsMode")]
            get;
            [NativeMethod("SetSkinWeightsMode")]
            set;
        }

        public int maxBonesPerVertex
        {
            get { return GetMaxBonesPerVertex(); }

            set
            {
                if (value < 1 || value > 255)
                    throw new ArgumentOutOfRangeException(nameof(maxBonesPerVertex), value, "Value must be in the range 1 - 255.");
                if (skinWeights != ModelImporterSkinWeights.Custom)
                    Debug.LogWarning("ModelImporter.maxBonesPerVertex is ignored unless ModelImporter.skinWeights is set to ModelImporterSkinWeights.Custom.");
                SetMaxBonesPerVertex(value);
            }
        }
        private extern int GetMaxBonesPerVertex();
        private extern void SetMaxBonesPerVertex(int value);

        public float minBoneWeight
        {
            get { return GetMinBoneWeight(); }

            set
            {
                if (!(value >= 0.0f && value <= 1.0f))
                    throw new ArgumentOutOfRangeException(nameof(minBoneWeight), value, "Value must be in the range 0 - 1.");
                if (skinWeights != ModelImporterSkinWeights.Custom)
                    Debug.LogWarning("ModelImporter.minBoneWeight is ignored unless ModelImporter.skinWeights is set to ModelImporterSkinWeights.Custom.");
                SetMinBoneWeight(value);
            }
        }

        private extern float GetMinBoneWeight();
        private extern void SetMinBoneWeight(float value);

        [System.Obsolete("normalImportMode is deprecated. Use importNormals instead")]
        public ModelImporterTangentSpaceMode normalImportMode
        {
            get { return (ModelImporterTangentSpaceMode)importNormals; }
            set { importNormals = (ModelImporterNormals)value; }
        }

        [System.Obsolete("tangentImportMode is deprecated. Use importTangents instead")]
        public ModelImporterTangentSpaceMode tangentImportMode
        {
            get { return (ModelImporterTangentSpaceMode)importTangents; }
            set { importTangents = (ModelImporterTangents)value; }
        }

        public extern ModelImporterNormals importNormals
        {
            [NativeMethod("GetNormalImportMode")]
            get;
            [NativeMethod("SetNormalImportMode")]
            set;
        }

        internal extern bool legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes
        {
            get;
            set;
        }

        public extern ModelImporterNormalSmoothingSource normalSmoothingSource
        {
            get;
            set;
        }

        public extern ModelImporterNormals importBlendShapeNormals
        {
            [NativeMethod("GetBlendShapeNormalImportMode")]
            get;
            [NativeMethod("SetBlendShapeNormalImportMode")]
            set;
        }

        public extern ModelImporterNormalCalculationMode normalCalculationMode
        {
            get;
            set;
        }

        public extern ModelImporterTangents importTangents
        {
            [NativeMethod("GetTangentImportMode")]
            get;
            [NativeMethod("SetTangentImportMode")]
            set;
        }

        public extern bool bakeIK
        {
            get;
            set;
        }

        public extern bool isBakeIKSupported
        {
            [NativeMethod("IsBakeIKSupported")]
            get;
        }

        [System.Obsolete("use resampleCurves instead.")]
        public extern bool resampleRotations
        {
            [NativeMethod("GetResampleCurves")]
            get;
            [NativeMethod("SetResampleCurves")]
            set;
        }

        public extern bool resampleCurves
        {
            get;
            set;
        }

        public extern bool isTangentImportSupported
        {
            [NativeMethod("IsTangentImportSupported")]
            get;
        }

        public extern bool removeConstantScaleCurves
        {
            get;
            set;
        }

        public extern bool strictVertexDataChecks
        {
            get;
            set;
        }

        [System.Obsolete("Use animationCompression instead", true)]
        private bool reduceKeyframes { get { return false; } set {} }

        public ModelImporterMeshCompression meshCompression
        {
            get { return (ModelImporterMeshCompression)internal_meshCompression; }
            set { internal_meshCompression = (int)value; }
        }

        private extern int internal_meshCompression
        {
            [NativeMethod("GetMeshCompression")]
            get;
            [NativeMethod("SetMeshCompression")]
            set;
        }

        public extern bool importAnimation
        {
            get;
            set;
        }

        public extern bool optimizeGameObjects
        {
            get;
            set;
        }

        public string[] extraExposedTransformPaths
        {
            get { return GetExtraExposedTransformPaths(); }
            set { INTERNAL_set_extraExposedTransformPaths(this, value); }
        }

        private extern string[] GetExtraExposedTransformPaths();

        [FreeFunction("ModelImporterBindings::SetExtraExposedTransformPaths")]
        private extern static void INTERNAL_set_extraExposedTransformPaths([NotNull] ModelImporter self, string[] value);

        public string[] extraUserProperties
        {
            get { return GetExtraUserProperties(); }
            set { INTERNAL_set_extraUserProperties(this, value); }
        }

        private extern string[] GetExtraUserProperties();

        [FreeFunction("ModelImporterBindings::SetExtraUserProperties")]
        private extern static void INTERNAL_set_extraUserProperties([NotNull] ModelImporter self, string[] value);

        public extern ModelImporterAnimationCompression animationCompression
        {
            get;
            set;
        }

        public extern bool importAnimatedCustomProperties
        {
            get;
            set;
        }

        public extern bool importConstraints
        {
            get;
            set;
        }

        public extern float animationRotationError
        {
            get;
            set;
        }

        public extern float animationPositionError
        {
            get;
            set;
        }

        public extern float animationScaleError
        {
            get;
            set;
        }

        public extern WrapMode animationWrapMode
        {
            get;
            set;
        }

        public extern ModelImporterAnimationType animationType
        {
            get;
            set;
        }

        public extern ModelImporterHumanoidOversampling humanoidOversampling
        {
            get;
            set;
        }

        public extern string motionNodeName
        {
            get;
            set;
        }

        public extern ModelImporterAvatarSetup avatarSetup
        {
            get;
            set;
        }

        public Avatar sourceAvatar
        {
            get
            {
                return GetSourceAvatar();
            }
            set
            {
                if (value != null)
                    humanDescription = value.humanDescription;

                SetSourceAvatarInternal(this, value);
            }
        }

        extern public HumanDescription humanDescription
        {
            get;
            set;
        }

        private extern Avatar GetSourceAvatar();
        [FreeFunction("ModelImporterBindings::SetSourceAvatarInternal")]
        private extern static void SetSourceAvatarInternal(ModelImporter self, Avatar value);

        [System.Obsolete("splitAnimations has been deprecated please use clipAnimations instead.", true)]
        public bool splitAnimations
        {
            get { return clipAnimations.Length != 0; }
            set {}
        }

        public ModelImporterClipAnimation[] clipAnimations
        {
            get { return GetClipAnimations(this); }
            set { SetClipAnimations(this, value); }
        }
        [FreeFunction("ModelImporterBindings::GetClipAnimations")]
        private extern static ModelImporterClipAnimation[] GetClipAnimations(ModelImporter self);
        [FreeFunction("ModelImporterBindings::SetClipAnimations", ThrowsException = true)]
        private extern static void SetClipAnimations([NotNull] ModelImporter self, [Unmarshalled] ModelImporterClipAnimation[] value);

        public ModelImporterClipAnimation[] defaultClipAnimations
        {
            get { return GetDefaultClipAnimations(this); }
        }
        [FreeFunction("ModelImporterBindings::GetDefaultClipAnimations")]
        private extern static ModelImporterClipAnimation[] GetDefaultClipAnimations(ModelImporter self);

        internal extern bool isAssetOlderOr42
        {
            [NativeMethod("IsAssetOlderOr42")]
            get;
        }

        [FreeFunction("ModelImporterBindings::UpdateTransformMask")]
        extern internal static void UpdateTransformMask([NotNull] AvatarMask mask, SerializedProperty serializedProperty);

        [FreeFunction("ModelImporterBindings::UpdateSkeletonPose")]
        extern internal static void UpdateSkeletonPose([NotNull] SkeletonBone[] skeletonBones, [NotNull] SerializedProperty serializedProperty);
        extern internal AnimationClip GetPreviewAnimationClipForTake(string takeName);

        extern internal string CalculateBestFittingPreviewGameObject();

        public void CreateDefaultMaskForClip(ModelImporterClipAnimation clip)
        {
            if (this.defaultClipAnimations.Length > 0)
            {
                var mask = new AvatarMask();
                this.defaultClipAnimations[0].ConfigureMaskFromClip(ref mask);
                clip.ConfigureClipFromMask(mask);
                DestroyImmediate(mask);
            }
            else
                Debug.LogError("Cannot create default mask because the current importer doesn't have any animation information");
        }

        [NativeName("ExtractTextures")]
        private extern bool ExtractTexturesInternal(string folderPath);

        public bool ExtractTextures(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("The path cannot be empty", folderPath);

            return ExtractTexturesInternal(folderPath);
        }

        public extern bool SearchAndRemapMaterials(ModelImporterMaterialName nameOption, ModelImporterMaterialSearch searchOption);

        public extern bool useSRGBMaterialColor
        {
            get;
            set;
        }
        public extern bool sortHierarchyByName
        {
            get;
            set;
        }

        public extern ModelImporterMaterialImportMode materialImportMode
        {
            get;
            set;
        }

        public extern bool autoGenerateAvatarMappingIfUnspecified
        {
            get;
            set;
        }

        [StaticAccessor("ModelImporterBindings", StaticAccessorType.DoubleColon)]
        internal extern static int vertexCacheOptimizerPrimsPerJobCount { set; }

        [StaticAccessor("ModelImporterBindings", StaticAccessorType.DoubleColon)]
        internal extern static bool enableMikktspaceCallbacks { set; }
    }
}
