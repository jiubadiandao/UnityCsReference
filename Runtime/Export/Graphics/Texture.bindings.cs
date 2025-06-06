// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections.LowLevel.Unsafe;

using TextureDimension = UnityEngine.Rendering.TextureDimension;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/Texture.h")]
    [NativeHeader("Runtime/Streaming/TextureStreamingManager.h")]
    [UsedByNativeCode]
    public partial class Texture : Object
    {
        protected Texture() {}

        [Obsolete("masterTextureLimit has been deprecated. Use globalMipmapLimit instead (UnityUpgradable) -> globalMipmapLimit", false)]
        [NativeProperty("ActiveGlobalMipmapLimit")] extern public static int masterTextureLimit { get; set; }
        [Obsolete("globalMipmapLimit is not supported. Use QualitySettings.globalTextureMipmapLimit or Mipmap Limit Groups instead.", false)]
        [NativeProperty("ActiveGlobalMipmapLimit")] extern public static int globalMipmapLimit { get; set; }

        extern public int mipmapCount { [NativeName("GetMipmapCount")] get; }

        [NativeProperty("AnisoLimit")] extern public static AnisotropicFiltering anisotropicFiltering { get; set; }
        [NativeName("SetGlobalAnisoLimits")] extern public static void SetGlobalAnisotropicFilteringLimits(int forcedMin, int globalMax);

        public virtual GraphicsFormat graphicsFormat
        {
            get { return GraphicsFormatUtility.GetFormat(this); }
        }

        [ThreadSafe]
        extern private int GetDataWidth();

        [ThreadSafe]
        extern private int GetDataHeight();

        [ThreadSafe]
        extern private TextureDimension GetDimension();

        // Note: not implemented setters in base class since some classes do need to actually implement them (e.g. RenderTexture)
        virtual public int width { get { return GetDataWidth(); } set { throw new NotImplementedException(); } }
        virtual public int height { get { return GetDataHeight(); } set { throw new NotImplementedException(); } }
        virtual public TextureDimension dimension { get { return GetDimension(); } set { throw new NotImplementedException(); } }

        extern internal bool isNativeTexture { [NativeName("IsNativeTexture")] get; }

        extern virtual public bool isReadable { get; }

        extern internal bool allowReadingInEditor { get; set; }

        // Note: getter for "wrapMode" returns the U mode on purpose
        extern public TextureWrapMode wrapMode { [NativeName("GetWrapModeU")] get; set; }

        extern public TextureWrapMode wrapModeU { get; set; }
        extern public TextureWrapMode wrapModeV { get; set; }
        extern public TextureWrapMode wrapModeW { get; set; }
        extern public FilterMode filterMode { get; set; }
        extern public int anisoLevel { get; set; }
        extern public float mipMapBias { get; set; }
        extern public Vector2 texelSize { [NativeName("GetTexelSize")] get; }
        extern public IntPtr GetNativeTexturePtr();
        [Obsolete("Use GetNativeTexturePtr instead.", false)]
        public int GetNativeTextureID() { return (int)GetNativeTexturePtr(); }

        extern public uint updateCount { get; }
        extern public void IncrementUpdateCount();

        [NativeMethod("GetActiveTextureColorSpace")]
        extern private int Internal_GetActiveTextureColorSpace();

        internal ColorSpace activeTextureColorSpace
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements")]
            get { return Internal_GetActiveTextureColorSpace() == 0 ? ColorSpace.Linear : ColorSpace.Gamma; }
        }

        [NativeMethod("GetStoredColorSpace")]
        extern private TextureColorSpace Internal_GetStoredColorSpace();

        public bool isDataSRGB
        {
            get { return Internal_GetStoredColorSpace() == TextureColorSpace.sRGB; }
        }

        extern public Hash128 imageContentsHash { get; set; }

        extern public static ulong totalTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetTotalTextureMemory")]
            get;
        }

        extern public static ulong desiredTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetDesiredTextureMemory")]
            get;
        }

        extern public static ulong targetTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetTargetTextureMemory")]
            get;
        }

        extern public static ulong currentTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetCurrentTextureMemory")]
            get;
        }

        extern public static ulong nonStreamingTextureMemory
        {
            [FreeFunction("GetTextureStreamingManager().GetNonStreamingTextureMemory")]
            get;
        }

        extern public static ulong streamingMipmapUploadCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingMipmapUploadCount")]
            get;
        }

        extern public static ulong streamingRendererCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingRendererCount")]
            get;
        }

        extern public static ulong streamingTextureCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingTextureCount")]
            get;
        }

        extern public static ulong nonStreamingTextureCount
        {
            [FreeFunction("GetTextureStreamingManager().GetNonStreamingTextureCount")]
            get;
        }

        extern public static ulong streamingTexturePendingLoadCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingTexturePendingLoadCount")]
            get;
        }

        extern public static ulong streamingTextureLoadingCount
        {
            [FreeFunction("GetTextureStreamingManager().GetStreamingTextureLoadingCount")]
            get;
        }

        [FreeFunction("GetTextureStreamingManager().SetStreamingTextureMaterialDebugProperties")]
        extern public static void SetStreamingTextureMaterialDebugProperties();

        [FreeFunction("GetTextureStreamingManager().SetStreamingTextureMaterialDebugPropertiesWithSlot")]
        extern private static void SetStreamingTextureMaterialDebugPropertiesWithSlot(int materialTextureSlot);
        public static void SetStreamingTextureMaterialDebugProperties(int materialTextureSlot)
        {
            SetStreamingTextureMaterialDebugPropertiesWithSlot(materialTextureSlot);
        }

        extern public static bool streamingTextureForceLoadAll
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetForceLoadAll")]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetForceLoadAll")]
            set;
        }
        extern public static bool streamingTextureDiscardUnusedMips
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetDiscardUnusedMips")]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetDiscardUnusedMips")]
            set;
        }
        extern public static bool allowThreadedTextureCreation
        {
            [FreeFunction(Name = "Texture2DScripting::IsCreateTextureThreadedEnabled")]
            get;
            [FreeFunction(Name = "Texture2DScripting::EnableCreateTextureThreaded")]
            set;
        }

        extern internal ulong GetPixelDataSize(int mipLevel, int element = 0);
        extern internal ulong GetPixelDataOffset(int mipLevel, int element = 0);

        extern public Rendering.GraphicsTexture graphicsTexture
        {
            [FreeFunction(Name = "Texture2DScripting::GetCurrentGraphicsTexture", HasExplicitThis = true)]
            get;
        }
    }

    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    [NativeHeader("Runtime/Graphics/GeneratedTextures.h")]
    [HelpURL("texture-type-default")] // 2D texture is considering the 'default' texture, so it hasn't been given it own dedicated 'class-Texture2D' manual page
    [UsedByNativeCode]
    [ExcludeFromPreset]
    public sealed partial class Texture2D : Texture
    {
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern private bool IgnoreMipmapLimit();
        extern private void SetIgnoreMipmapLimitAndReload(bool value);

        extern public string mipmapLimitGroup
        {
            [NativeName("GetMipmapLimitGroupName")] get;
        }

        extern public int activeMipmapLimit
        {
            [NativeName("GetMipmapLimit")] get;
        }

        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D whiteTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D blackTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D redTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D grayTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D linearGrayTexture { get; }
        [StaticAccessor("builtintex", StaticAccessorType.DoubleColon)] extern public static Texture2D normalTexture { get; }

        extern public void Compress(bool highQuality);

        [FreeFunction("Texture2DScripting::CreateEmpty")]
        extern private static bool Internal_CreateEmptyImpl([Writable] Texture2D mono);
        [FreeFunction("Texture2DScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex, bool ignoreMipmapLimit, string mipmapLimitGroupName);
        private static void Internal_Create([Writable] Texture2D mono, int w, int h, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex, bool ignoreMipmapLimit, string mipmapLimitGroupName)
        {
            if (!Internal_CreateImpl(mono, w, h, mipCount, format, colorSpace, flags, nativeTex, ignoreMipmapLimit, mipmapLimitGroupName))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        extern override public bool isReadable { get; }
        [NativeConditional("ENABLE_VIRTUALTEXTURING && UNITY_EDITOR")][NativeName("VTOnly")] extern public bool vtOnly { get; }
        [NativeName("Apply")] extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);
        [NativeName("Reinitialize")] extern private bool ReinitializeImpl(int width, int height);
        [NativeName("SetPixel")] extern private void SetPixelImpl(int image, int mip, int x, int y, Color color);
        [NativeName("GetPixel")] extern private Color GetPixelImpl(int image, int mip, int x, int y);
        [NativeName("GetPixelBilinear")] extern private Color GetPixelBilinearImpl(int image, int mip, float u, float v);

        [FreeFunction(Name = "Texture2DScripting::ReinitializeWithFormat", HasExplicitThis = true)]
        extern private bool ReinitializeWithFormatImpl(int width, int height, GraphicsFormat format, bool hasMipMap);

        [FreeFunction(Name = "Texture2DScripting::ReinitializeWithTextureFormat", HasExplicitThis = true)]
        extern private bool ReinitializeWithTextureFormatImpl(int width, int height, TextureFormat textureFormat, bool hasMipMap);

        [FreeFunction(Name = "Texture2DScripting::ReadPixels", HasExplicitThis = true)]
        extern private void ReadPixelsImpl(Rect source, int destX, int destY, bool recalculateMipMaps);


        [FreeFunction(Name = "Texture2DScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetPixelsImpl(int x, int y, int w, int h, Color[] pixel, int miplevel, int frame);

        [FreeFunction(Name = "Texture2DScripting::LoadRawData", HasExplicitThis = true)]
        extern private bool LoadRawTextureDataImpl(IntPtr data, ulong size);

        [FreeFunction(Name = "Texture2DScripting::LoadRawData", HasExplicitThis = true)]
        extern private bool LoadRawTextureDataImplArray(byte[] data);

        [FreeFunction(Name = "Texture2DScripting::SetPixelDataArray", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplArray(System.Array data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture2DScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        extern private IntPtr GetWritableImageData(int frame);
        extern private ulong GetImageDataSize();

        extern private static AtomicSafetyHandle GetSafetyHandle(Texture2D tex);
        extern private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel);

        [FreeFunction("Texture2DScripting::GenerateAtlas")]
        extern private static void GenerateAtlasImpl(Vector2[] sizes, int padding, int atlasSize, [Out] Rect[] rect);
        extern internal bool isPreProcessed { get; }

        // Must be kept in sync with their C++ counterparts. See Texture2D::s_StreamingMipmapsPriority[Min|Max].
        internal const int streamingMipmapsPriorityMin = SByte.MinValue;
        internal const int streamingMipmapsPriorityMax = SByte.MaxValue;

        extern public bool streamingMipmaps { get; }
        extern public int streamingMipmapsPriority { get; }

        extern public int requestedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetRequestedMipmapLevel", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetRequestedMipmapLevel", HasExplicitThis = true)]
            set;
        }
        extern public int minimumMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetMinimumMipmapLevel", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetMinimumMipmapLevel", HasExplicitThis = true)]
            set;
        }

        extern internal bool loadAllMips
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadAllMips", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetLoadAllMips", HasExplicitThis = true)]
            set;
        }

        extern public int calculatedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetCalculatedMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int desiredMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetDesiredMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadingMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadingMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadedMipmapLevel", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GetTextureStreamingManager().ClearRequestedMipmapLevel", HasExplicitThis = true)]
        extern public void ClearRequestedMipmapLevel();

        [FreeFunction(Name = "GetTextureStreamingManager().IsRequestedMipmapLevelLoaded", HasExplicitThis = true)]
        extern public bool IsRequestedMipmapLevelLoaded();

        [FreeFunction(Name = "GetTextureStreamingManager().ClearMinimumMipmapLevel", HasExplicitThis = true)]
        extern public void ClearMinimumMipmapLevel();

        [FreeFunction("Texture2DScripting::UpdateExternalTexture", HasExplicitThis = true)]
        extern public void UpdateExternalTexture(IntPtr nativeTex);

        [FreeFunction("Texture2DScripting::SetAllPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetAllPixels32(Color32[] colors, int miplevel);

        [FreeFunction("Texture2DScripting::SetBlockOfPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetBlockOfPixels32(int x, int y, int blockWidth, int blockHeight, Color32[] colors, int miplevel);

        [FreeFunction("Texture2DScripting::GetRawTextureData", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public byte[] GetRawTextureData();

        [FreeFunction("Texture2DScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return: Unmarshalled]
        extern public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight, [uei.DefaultValue("0")] int miplevel);

        [uei.ExcludeFromDocs]
        public Color[] GetPixels(int x, int y, int blockWidth, int blockHeight)
        {
            return GetPixels(x, y, blockWidth, blockHeight, 0);
        }

        [FreeFunction("Texture2DScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color32[] GetPixels32([uei.DefaultValue("0")] int miplevel);

        [uei.ExcludeFromDocs]
        public Color32[] GetPixels32()
        {
            return GetPixels32(0);
        }

        [FreeFunction("Texture2DScripting::PackTextures", HasExplicitThis = true)]
        [return:Unmarshalled]
        extern public Rect[] PackTextures(Texture2D[] textures, int padding, int maximumAtlasSize, bool makeNoLongerReadable);

        public Rect[] PackTextures(Texture2D[] textures, int padding, int maximumAtlasSize)
        {
            return PackTextures(textures, padding, maximumAtlasSize, false);
        }

        public Rect[] PackTextures(Texture2D[] textures, int padding)
        {
            return PackTextures(textures, padding, 2048);
        }

        [FreeFunction(Name = "Texture2DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "Texture2DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstMip);

        [FreeFunction(Name = "Texture2DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstMip, int dstX, int dstY);

        extern public bool alphaIsTransparency { get; set; }

        [VisibleToOtherModules("UnityEngine.UIElementsModule", "Unity.UIElements")]
        extern internal float pixelsPerPoint { get; set; }
    }

    [NativeHeader("Runtime/Graphics/CubemapTexture.h")]
    [ExcludeFromPreset]
    public sealed partial class Cubemap : Texture
    {
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        [FreeFunction("CubemapScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Cubemap mono, int ext, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex);
        private static void Internal_Create([Writable] Cubemap mono, int ext, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex)
        {
            if (!Internal_CreateImpl(mono, ext, mipCount, format, colorSpace, flags, nativeTex))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [FreeFunction(Name = "CubemapScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction("CubemapScripting::UpdateExternalTexture", HasExplicitThis = true)]
        extern public void UpdateExternalTexture(IntPtr nativeTexture);

        extern override public bool isReadable { get; }
        [NativeName("SetPixel")] extern private void SetPixelImpl(int image, int mip, int x, int y, Color color);
        [NativeName("GetPixel")] extern private Color GetPixelImpl(int image, int mip, int x, int y);

        [NativeName("FixupEdges")] extern public void SmoothEdges([uei.DefaultValue("1")] int smoothRegionWidthInPixels);
        public void SmoothEdges() { SmoothEdges(1); }

        [FreeFunction(Name = "CubemapScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color[] GetPixels(CubemapFace face, int miplevel);

        public Color[] GetPixels(CubemapFace face)
        {
            return GetPixels(face, 0);
        }

        [FreeFunction(Name = "CubemapScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, CubemapFace face, int miplevel);

        [FreeFunction(Name = "CubemapScripting::SetPixelDataArray", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplArray(System.Array data, int mipLevel, int face, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "CubemapScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int face, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        public void SetPixels(Color[] colors, CubemapFace face)
        {
            SetPixels(colors, face, 0);
        }

        [FreeFunction(Name = "CubemapScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "CubemapScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstFace, int dstMip);

        [FreeFunction(Name = "CubemapScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstFace, int dstMip, int dstX, int dstY);

        extern private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel, int face);
        extern private IntPtr GetWritableImageData(int frame);

        extern internal bool isPreProcessed { get; }

        extern public bool streamingMipmaps { get; }
        extern public int streamingMipmapsPriority { get; }

        extern public int requestedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetRequestedMipmapLevel", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetRequestedMipmapLevel", HasExplicitThis = true)]
            set;
        }

        extern internal bool loadAllMips
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadAllMips", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "GetTextureStreamingManager().SetLoadAllMips", HasExplicitThis = true)]
            set;
        }

        extern public int desiredMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetDesiredMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadingMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadingMipmapLevel", HasExplicitThis = true)]
            get;
        }

        extern public int loadedMipmapLevel
        {
            [FreeFunction(Name = "GetTextureStreamingManager().GetLoadedMipmapLevel", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GetTextureStreamingManager().ClearRequestedMipmapLevel", HasExplicitThis = true)]
        extern public void ClearRequestedMipmapLevel();

        [FreeFunction(Name = "GetTextureStreamingManager().IsRequestedMipmapLevelLoaded", HasExplicitThis = true)]
        extern public bool IsRequestedMipmapLevelLoaded();

    }

    [NativeHeader("Runtime/Graphics/Texture3D.h")]
    [ExcludeFromPreset]
    public sealed partial class Texture3D : Texture
    {
        extern public int depth { [NativeName("GetTextureLayerCount")] get; }
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern override public bool isReadable { get; }
        [NativeName("SetPixel")] extern private void SetPixelImpl(int mip, int x, int y, int z, Color color);
        [NativeName("GetPixel")] extern private Color GetPixelImpl(int mip, int x, int y, int z);
        [NativeName("GetPixelBilinear")] extern private Color GetPixelBilinearImpl(int mip, float u, float v, float w);

        [FreeFunction("Texture3DScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture3D mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex);
        private static void Internal_Create([Writable] Texture3D mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, IntPtr nativeTex)
        {
            if (!Internal_CreateImpl(mono, w, h, d, mipCount, format, colorSpace, flags, nativeTex))
                throw new UnityException("Failed to create texture because of invalid parameters.");
        }

        [FreeFunction("Texture3DScripting::UpdateExternalTexture", HasExplicitThis = true)]
        extern public void UpdateExternalTexture(IntPtr nativeTex);
        [FreeFunction(Name = "Texture3DScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction(Name = "Texture3DScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color[] GetPixels(int miplevel);

        public Color[] GetPixels()
        {
            return GetPixels(0);
        }

        [FreeFunction(Name = "Texture3DScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color32[] GetPixels32(int miplevel);

        public Color32[] GetPixels32()
        {
            return GetPixels32(0);
        }

        [FreeFunction(Name = "Texture3DScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, int miplevel);

        public void SetPixels(Color[] colors)
        {
            SetPixels(colors, 0);
        }

        [FreeFunction(Name = "Texture3DScripting::SetPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels32(Color32[] colors, int miplevel);

        public void SetPixels32(Color32[] colors)
        {
            SetPixels32(colors, 0);
        }

        [FreeFunction(Name = "Texture3DScripting::SetPixelDataArray", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplArray(System.Array data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture3DScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture3DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "Texture3DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstElement, int dstMip);

        [FreeFunction(Name = "Texture3DScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY);

        extern private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel);
        extern private IntPtr GetImageData();
    }

    [NativeHeader("Runtime/Graphics/Texture2DArray.h")]
    [ExcludeFromPreset]
    public sealed partial class Texture2DArray : Texture
    {
        extern static public int allSlices { [NativeName("GetAllTextureLayersIdentifier")] get; }
        extern public int depth { [NativeName("GetTextureLayerCount")] get; }
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern private bool IgnoreMipmapLimit();
        extern private void SetIgnoreMipmapLimitAndReload(bool value);

        extern public string mipmapLimitGroup
        {
            [NativeName("GetMipmapLimitGroupName")] get;
        }

        extern public int activeMipmapLimit
        {
            [NativeName("GetMipmapLimit")] get;
        }

        extern override public bool isReadable { get; }

        [FreeFunction("Texture2DArrayScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] Texture2DArray mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, bool ignoreMipmapLimit, string mipmapLimitGroupName);
        private static void Internal_Create([Writable] Texture2DArray mono, int w, int h, int d, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags, bool ignoreMipmapLimit, string mipmapLimitGroupName)
        {
            if (!Internal_CreateImpl(mono, w, h, d, mipCount, format, colorSpace, flags, ignoreMipmapLimit, mipmapLimitGroupName))
                throw new UnityException("Failed to create 2D array texture because of invalid parameters.");
        }

        [FreeFunction(Name = "Texture2DArrayScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction(Name = "Texture2DArrayScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color[] GetPixels(int arrayElement, int miplevel);

        public Color[] GetPixels(int arrayElement)
        {
            return GetPixels(arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixelDataArray", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplArray(System.Array data, int mipLevel, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "Texture2DArrayScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color32[] GetPixels32(int arrayElement, int miplevel);

        public Color32[] GetPixels32(int arrayElement)
        {
            return GetPixels32(arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, int arrayElement, int miplevel);

        public void SetPixels(Color[] colors, int arrayElement)
        {
            SetPixels(colors, arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::SetPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels32(Color32[] colors, int arrayElement, int miplevel);

        public void SetPixels32(Color32[] colors, int arrayElement)
        {
            SetPixels32(colors, arrayElement, 0);
        }

        [FreeFunction(Name = "Texture2DArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "Texture2DArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstElement, int dstMip);

        [FreeFunction(Name = "Texture2DArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY);

        extern private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel, int element);
        extern private IntPtr GetImageData();
    }

    [NativeHeader("Runtime/Graphics/CubemapArrayTexture.h")]
    [ExcludeFromPreset]
    public sealed partial class CubemapArray : Texture
    {
        extern public int cubemapCount { get; }
        extern public TextureFormat format { [NativeName("GetTextureFormat")] get; }

        extern override public bool isReadable { get; }

        [FreeFunction("CubemapArrayScripting::Create")]
        extern private static bool Internal_CreateImpl([Writable] CubemapArray mono, int ext, int count, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags);
        private static void Internal_Create([Writable] CubemapArray mono, int ext, int count, int mipCount, GraphicsFormat format, TextureColorSpace colorSpace, TextureCreationFlags flags)
        {
            if (!Internal_CreateImpl(mono, ext, count, mipCount, format, colorSpace, flags))
                throw new UnityException("Failed to create cubemap array texture because of invalid parameters.");
        }

        [FreeFunction(Name = "CubemapArrayScripting::Apply", HasExplicitThis = true)]
        extern private void ApplyImpl(bool updateMipmaps, bool makeNoLongerReadable);

        [FreeFunction(Name = "CubemapArrayScripting::GetPixels", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color[] GetPixels(CubemapFace face, int arrayElement, int miplevel);

        public Color[] GetPixels(CubemapFace face, int arrayElement)
        {
            return GetPixels(face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::GetPixels32", HasExplicitThis = true, ThrowsException = true)]
        [return:Unmarshalled]
        extern public Color32[] GetPixels32(CubemapFace face, int arrayElement, int miplevel);

        public Color32[] GetPixels32(CubemapFace face, int arrayElement)
        {
            return GetPixels32(face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::SetPixels", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels(Color[] colors, CubemapFace face, int arrayElement, int miplevel);

        public void SetPixels(Color[] colors, CubemapFace face, int arrayElement)
        {
            SetPixels(colors, face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::SetPixels32", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetPixels32(Color32[] colors, CubemapFace face, int arrayElement, int miplevel);

        public void SetPixels32(Color32[] colors, CubemapFace face, int arrayElement)
        {
            SetPixels32(colors, face, arrayElement, 0);
        }

        [FreeFunction(Name = "CubemapArrayScripting::SetPixelDataArray", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImplArray(System.Array data, int mipLevel, int face, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "CubemapArrayScripting::SetPixelData", HasExplicitThis = true, ThrowsException = true)]
        extern private bool SetPixelDataImpl(IntPtr data, int mipLevel, int face, int element, int elementSize, int dataArraySize, int sourceDataStartIndex = 0);

        [FreeFunction(Name = "CubemapArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Full(Texture src);

        [FreeFunction(Name = "CubemapArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Slice(Texture src, int srcElement, int srcMip, int dstElement, int dstMip);

        [FreeFunction(Name = "CubemapArrayScripting::CopyPixels", HasExplicitThis = true, ThrowsException = true)]
        extern private void CopyPixels_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, int dstElement, int dstMip, int dstX, int dstY);

        extern private AtomicSafetyHandle GetSafetyHandleForSlice(int mipLevel, int face, int element);
        extern private IntPtr GetImageData();
    }

    [NativeHeader("Runtime/Graphics/SparseTexture.h")]
    public sealed partial class SparseTexture : Texture
    {
        extern public int tileWidth { get; }
        extern public int tileHeight { get; }
        extern public bool isCreated { [NativeName("IsInitialized")] get; }

        [FreeFunction(Name = "SparseTextureScripting::Create", ThrowsException = true)]
        extern private static void Internal_Create([Writable] SparseTexture mono, int width, int height, GraphicsFormat format, TextureColorSpace colorSpace, int mipCount);

        [FreeFunction(Name = "SparseTextureScripting::UpdateTile", HasExplicitThis = true)]
        extern public void UpdateTile(int tileX, int tileY, int miplevel, Color32[] data);

        [FreeFunction(Name = "SparseTextureScripting::UpdateTileRaw", HasExplicitThis = true)]
        extern public void UpdateTileRaw(int tileX, int tileY, int miplevel, byte[] data);

        public void UnloadTile(int tileX, int tileY, int miplevel)
        {
            UpdateTileRaw(tileX, tileY, miplevel, null);
        }
    }

    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Graphics/RenderBufferManager.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [UsedByNativeCode]
    public partial class RenderTexture : Texture
    {
        override extern public int width { get; set; }
        override extern public int height { get; set; }

        override extern public TextureDimension dimension { get; set; }

        [NativeName("GetColorFormat")] extern private GraphicsFormat GetColorFormat(bool suppressWarnings);
        [NativeName("SetColorFormat")] extern private void SetColorFormat(GraphicsFormat format);
        public new GraphicsFormat graphicsFormat { get{ return GetColorFormat(true); } set { SetColorFormat(value); } } // Getter should not log warnings

        [NativeProperty("MipMap")]                  extern public bool useMipMap { get; set; }
        [NativeProperty("SRGBReadWrite")]           extern public bool sRGB { get; }
        [NativeProperty("VRUsage")]                 extern public VRTextureUsage vrUsage { get; set; }
        [NativeProperty("Memoryless")]              extern public RenderTextureMemoryless memorylessMode { get; set; }


        public RenderTextureFormat format
        {
            get
            {
                if (graphicsFormat != GraphicsFormat.None)
                {
                    return GraphicsFormatUtility.GetRenderTextureFormat(graphicsFormat);
                }
                else // If graphicsFormat is None, then the RT is a depth-only RT.
                {
                    return (GetDescriptor().shadowSamplingMode != ShadowSamplingMode.None) ? RenderTextureFormat.Shadowmap : RenderTextureFormat.Depth;
                }
            }
            // Setter can produce any of these following valid combinations, other combos are invalid. ('depthStencilFormat' untouched unless RTFormat infers a depth-only RT)
            // graphicsFormat: None                                     depthStencilFormat: a depth-stencil format (D16_UNorm, ...)     <- depth-only RT.
            // graphicsFormat: a color format (R8G8B8A8_SRGB, ...)      depthStencilFormat: a depth-stencil format (D16_UNorm, ...)     <- color + depth RT.
            // graphicsFormat: a color format (R8G8B8A8_SRGB, ...)      depthStencilFormat: None                                        <- color-only RT.
            set
            {
                if (value == RenderTextureFormat.Depth || value == RenderTextureFormat.Shadowmap)
                {
                    if (depthStencilFormat == GraphicsFormat.None)
                    {
                        WarnAboutFallbackTo16BitsDepth(value);
                        depthStencilFormat = GraphicsFormat.D16_UNorm;
                    }
                    if (value == RenderTextureFormat.Shadowmap)
                    {
                        SetShadowSamplingMode(ShadowSamplingMode.CompareDepths);
                    }
                }
                graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(value, sRGB);
            }
        }

        extern public GraphicsFormat stencilFormat { get; set; }

        extern public GraphicsFormat depthStencilFormat { get; set; }

        extern public bool autoGenerateMips { get; set; }
        extern public int volumeDepth { get; set; }
        extern public int antiAliasing { get; set; }
        extern public bool bindTextureMS { get; set; }
        extern public bool enableRandomWrite { get; set; }
        extern public bool useDynamicScale { get; set; }
        extern public bool useDynamicScaleExplicit { get; set; }
        extern public bool enableShadingRate { get; set; }

        extern public void ApplyDynamicScale();

        // for some reason we are providing isPowerOfTwo setter which is empty (i dont know what the intent is/was)
        extern private bool GetIsPowerOfTwo();
        public bool isPowerOfTwo { get { return GetIsPowerOfTwo(); } set {} }


        [FreeFunction("RenderTexture::GetActiveAsRenderTexture")] extern private static RenderTexture GetActive();
        [FreeFunction("RenderTextureScripting::SetActive")] extern private static void SetActive(RenderTexture rt);
        public static RenderTexture active { get { return GetActive(); } set { SetActive(value); } }

        [FreeFunction(Name = "RenderTextureScripting::GetColorBuffer", HasExplicitThis = true)]
        extern private RenderBuffer GetColorBuffer();
        [FreeFunction(Name = "RenderTextureScripting::GetDepthBuffer", HasExplicitThis = true)]
        extern private RenderBuffer GetDepthBuffer();

        extern private void SetMipMapCount(int count);

        extern internal void SetShadowSamplingMode(Rendering.ShadowSamplingMode samplingMode);

        public RenderBuffer colorBuffer { get { return GetColorBuffer(); } }
        public RenderBuffer depthBuffer { get { return GetDepthBuffer(); } }

        extern public IntPtr GetNativeDepthBufferPtr();


        extern public void DiscardContents(bool discardColor, bool discardDepth);
        [Obsolete("This function has no effect.", false)]
        extern public void MarkRestoreExpected();
        public void DiscardContents() { DiscardContents(true, true); }


        [NativeName("ResolveAntiAliasedSurface")] extern private void ResolveAA();
        [NativeName("ResolveAntiAliasedSurface")] extern private void ResolveAATo(RenderTexture rt);

        public void ResolveAntiAliasedSurface() { ResolveAA(); }
        public void ResolveAntiAliasedSurface(RenderTexture target) { ResolveAATo(target); }


        [FreeFunction(Name = "RenderTextureScripting::SetGlobalShaderProperty", HasExplicitThis = true)]
        extern public void SetGlobalShaderProperty(string propertyName);


        extern public bool Create();
        extern public void Release();
        extern public bool IsCreated();
        extern public void GenerateMips();
        [NativeThrows]
        extern public void ConvertToEquirect(RenderTexture equirect, Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono);

        extern internal void SetSRGBReadWrite(bool srgb);

        [FreeFunction("RenderTextureScripting::Create")] extern private static void Internal_Create([Writable] RenderTexture rt);

        [FreeFunction("RenderTextureSupportsStencil")] extern public static bool SupportsStencil(RenderTexture rt);

        [NativeName("SetRenderTextureDescFromScript")]
        extern private void SetRenderTextureDescriptor(RenderTextureDescriptor desc);

        [NativeName("GetRenderTextureDesc")]
        extern private RenderTextureDescriptor GetDescriptor();

        [FreeFunction("GetRenderBufferManager().GetTextures().GetTempBuffer")]
        extern private static RenderTexture GetTemporary_Internal(RenderTextureDescriptor desc);


        [FreeFunction("GetRenderBufferManager().GetTextures().ReleaseTempBuffer")]
        extern public static void ReleaseTemporary(RenderTexture temp);

        extern public int depth
        {
            [FreeFunction("RenderTextureScripting::GetDepth", HasExplicitThis = true)]
            get;
            [FreeFunction("RenderTextureScripting::SetDepth", HasExplicitThis = true)]
            set;
        }
    }

    [System.Serializable]
    [UsedByNativeCode]
    public struct CustomRenderTextureUpdateZone
    {
        public Vector3 updateZoneCenter;
        public Vector3 updateZoneSize;
        public float rotation;
        public int passIndex;
        public bool needSwap;
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/CustomRenderTexture.h")]
    public sealed partial class CustomRenderTexture : RenderTexture
    {
        [FreeFunction(Name = "CustomRenderTextureScripting::Create")]
        extern private static void Internal_CreateCustomRenderTexture([Writable] CustomRenderTexture rt);

        [NativeName("TriggerUpdate")]
        extern void TriggerUpdate(int count);

        public void Update(int count)
        {
            CustomRenderTextureManager.InvokeTriggerUpdate(this, count);
            TriggerUpdate(count);
        }

        public void Update()
        {
            Update(1);
        }

        [NativeName("TriggerInitialization")]
        extern void TriggerInitialization();

        public void Initialize()
        {
            TriggerInitialization();
            CustomRenderTextureManager.InvokeTriggerInitialize(this);
        }

        extern public void ClearUpdateZones();

        extern public Material material { get; set; }

        extern public Material initializationMaterial { get; set; }

        extern public Texture initializationTexture { get; set; }

        [FreeFunction(Name = "CustomRenderTextureScripting::GetUpdateZonesInternal", HasExplicitThis = true)]
        extern internal void GetUpdateZonesInternal([NotNull] object updateZones);

        public void GetUpdateZones(List<CustomRenderTextureUpdateZone> updateZones)
        {
            GetUpdateZonesInternal(updateZones);
        }

        [FreeFunction(Name = "CustomRenderTextureScripting::SetUpdateZonesInternal", HasExplicitThis = true)]
        extern private void SetUpdateZonesInternal(CustomRenderTextureUpdateZone[] updateZones);

        [FreeFunction(Name = "CustomRenderTextureScripting::GetDoubleBufferRenderTexture", HasExplicitThis = true)]
        extern public RenderTexture GetDoubleBufferRenderTexture();

        extern public void EnsureDoubleBufferConsistency();

        public void SetUpdateZones(CustomRenderTextureUpdateZone[] updateZones)
        {
            if (updateZones == null)
                throw new ArgumentNullException("updateZones");

            SetUpdateZonesInternal(updateZones);
        }

        extern public CustomRenderTextureInitializationSource initializationSource { get; set; }
        extern public Color initializationColor { get; set; }
        extern public CustomRenderTextureUpdateMode updateMode { get; set; }
        extern public CustomRenderTextureUpdateMode initializationMode { get; set; }
        extern public CustomRenderTextureUpdateZoneSpace updateZoneSpace { get; set; }
        extern public int shaderPass { get; set; }
        extern public uint cubemapFaceMask { get; set; }
        extern public bool doubleBuffered { get; set; }
        extern public bool wrapUpdateZones { get; set; }
        extern public float updatePeriod { get; set; }
    }
}
