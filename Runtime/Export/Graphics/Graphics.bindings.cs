// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Jobs;
using System.Globalization;

namespace UnityEngine
{
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/RenderLoops/LightProbeContext.h")]
    [StaticAccessor("LightProbeContextWrapper", StaticAccessorType.DoubleColon)]
    public struct LightProbesQuery : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr m_LightProbeContextWrapper;

        internal Allocator m_AllocatorLabel;

        internal AtomicSafetyHandle m_Safety;

        public LightProbesQuery(Allocator allocator)
        {
            m_LightProbeContextWrapper = Create();

            m_AllocatorLabel = allocator;

            UnsafeUtility.LeakRecord(m_LightProbeContextWrapper, LeakCategory.LightProbesQuery, 0);
            AtomicSafetyHandle.CreateHandle(out m_Safety, allocator);
        }

        public void Dispose()
        {
            if (m_LightProbeContextWrapper == IntPtr.Zero)
            {
                throw new ObjectDisposedException("The LightProbesQuery is already disposed.");
            }

            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The LightProbesQuery can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                AtomicSafetyHandle.DisposeHandle(ref m_Safety);
                UnsafeUtility.LeakErase(m_LightProbeContextWrapper, LeakCategory.LightProbesQuery);
                Destroy(m_LightProbeContextWrapper);
                m_AllocatorLabel = Allocator.Invalid;
            }
            m_LightProbeContextWrapper = IntPtr.Zero;
        }

        [NativeContainer]
        internal unsafe struct LightProbesQueryDispose
        {
            [NativeDisableUnsafePtrRestriction]
            internal IntPtr    m_LightProbeContextWrapper;

            internal AtomicSafetyHandle m_Safety;

            public void Dispose()
            {
                UnsafeUtility.LeakErase(m_LightProbeContextWrapper, LeakCategory.LightProbesQuery);
                Destroy(m_LightProbeContextWrapper);
            }
        }

        internal struct LightProbesQueryDisposeJob : IJob
        {
            internal LightProbesQueryDispose Data;

            public void Execute()
            {
                Data.Dispose();
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The LightProbesQuery can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_LightProbeContextWrapper == IntPtr.Zero)
            {
                throw new InvalidOperationException("The LightProbesQuery is already disposed.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                // [DeallocateOnJobCompletion] is not supported on the m_LightProbeContextWrapper,
                // but we want the deallocation to happen on a thread.
                // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
                // will check that no jobs are writing to the container).

                var jobHandle = new LightProbesQueryDisposeJob { Data = new LightProbesQueryDispose { m_LightProbeContextWrapper = m_LightProbeContextWrapper, m_Safety = m_Safety } }.Schedule(inputDeps);

                AtomicSafetyHandle.Release(m_Safety);
                m_AllocatorLabel = Allocator.Invalid;
                m_LightProbeContextWrapper = IntPtr.Zero;
                return jobHandle;
            }

            m_LightProbeContextWrapper = IntPtr.Zero;
            return inputDeps;
        }

        public bool IsCreated
        {
            get { return m_LightProbeContextWrapper != IntPtr.Zero; }
        }

        static extern IntPtr Create();

        [ThreadSafe]
        static extern void Destroy(IntPtr lightProbeContextWrapper);

        public void CalculateInterpolatedLightAndOcclusionProbe(Vector3 position, ref int tetrahedronIndex, out SphericalHarmonicsL2 lightProbe, out Vector4 occlusionProbe)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            CalculateInterpolatedLightAndOcclusionProbe(m_LightProbeContextWrapper, position, ref tetrahedronIndex, out lightProbe, out occlusionProbe);
        }

        public void CalculateInterpolatedLightAndOcclusionProbes(NativeArray<Vector3> positions, NativeArray<int> tetrahedronIndices, NativeArray<SphericalHarmonicsL2> lightProbes, NativeArray<Vector4> occlusionProbes)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if (positions == null)
                throw new ArgumentException("positions", "Argument positions has not been specified");
            if (tetrahedronIndices == null || tetrahedronIndices.Length < positions.Length)
                throw new ArgumentException("tetrahedronIndices", "Argument tetrahedronIndices is null or has fewer elements than positions.");
            else if (lightProbes == null || lightProbes.Length < positions.Length)
                throw new ArgumentException("lightProbes", "Argument lightProbes is null or has fewer elements than positions.");
            else if (occlusionProbes == null || occlusionProbes.Length < positions.Length)
                throw new ArgumentException("occlusionProbes", "Argument occlusionProbes is null or has fewer elements than positions.");

            unsafe
            {
                CalculateInterpolatedLightAndOcclusionProbes(m_LightProbeContextWrapper, (IntPtr)positions.GetUnsafeReadOnlyPtr(), (IntPtr)tetrahedronIndices.GetUnsafeReadOnlyPtr(), (IntPtr)lightProbes.GetUnsafePtr(), (IntPtr)occlusionProbes.GetUnsafePtr(), positions.Length);
            }
        }

        [ThreadSafe]
        static extern void CalculateInterpolatedLightAndOcclusionProbe(IntPtr lightProbeContextWrapper, Vector3 position, ref int tetrahedronIndex, out SphericalHarmonicsL2 lightProbe, out Vector4 occlusionProbe);

        [ThreadSafe]
        static extern void CalculateInterpolatedLightAndOcclusionProbes(IntPtr lightProbeContextWrapper, IntPtr positions, IntPtr tetrahedronIndices, IntPtr lightProbes, IntPtr occlusionProbes, int count);
    }

    internal enum EnabledOrientation
    {
        kAutorotateToPortrait           = 1,
        kAutorotateToPortraitUpsideDown = 2,
        kAutorotateToLandscapeLeft      = 4,
        kAutorotateToLandscapeRight     = 8,
    }

    public enum FullScreenMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3,
    }

    [NativeType("Runtime/Graphics/RefreshRate.h")]
    public struct RefreshRate : IEquatable<RefreshRate>, IComparable<RefreshRate>
    {
        [RequiredMember]
        public uint numerator;
        [RequiredMember]
        public uint denominator;

        public double value
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => (double)numerator / (double)denominator;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(RefreshRate other)
        {
            if (denominator == 0)
                return other.denominator == 0;

            if (other.denominator == 0)
                return false;

            return (ulong)numerator * other.denominator == (ulong)denominator * other.numerator;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public int CompareTo(RefreshRate other)
        {
            if (denominator == 0)
                return other.denominator == 0 ? 0 : 1;

            if (other.denominator == 0)
                return -1;

            return ((ulong)numerator * other.denominator).CompareTo((ulong)denominator * other.numerator);
        }

        public override string ToString()
        {
            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }

    [UsedByNativeCode]
    [NativeType("Runtime/Graphics/DisplayInfo.h")]
    public struct DisplayInfo : IEquatable<DisplayInfo>
    {
        [RequiredMember]
        internal ulong handle;
        [RequiredMember]
        public int width;
        [RequiredMember]
        public int height;
        [RequiredMember]
        public RefreshRate refreshRate;
        [RequiredMember]
        public RectInt workArea;
        [RequiredMember]
        public string name;

        // Implement IEquatable<DisplayInfo> so that storing this struct
        // in a dictionary doesn't result in multiple boxing operations
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(DisplayInfo other)
        {
            return handle == other.handle &&
                width == other.width &&
                height == other.height &&
                refreshRate.Equals(other.refreshRate) &&
                workArea.Equals(other.workArea) &&
                name == other.name;
        }
    }

    public sealed partial class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }


    [NativeHeader("Runtime/Graphics/ScreenManager.h")]
    [NativeHeader("Runtime/Graphics/WindowLayout.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [StaticAccessor("GetScreenManager()", StaticAccessorType.Dot)]
    internal sealed class EditorScreen
    {
        extern public static int   width  {[NativeMethod(Name = "GetWidth",  IsThreadSafe = true)] get; }
        extern public static int   height {[NativeMethod(Name = "GetHeight", IsThreadSafe = true)] get; }
        extern public static float dpi    {[NativeName("GetDPI")] get; }

        extern private static void RequestOrientation(ScreenOrientation orient);
        extern private static ScreenOrientation GetScreenOrientation();

        public static ScreenOrientation orientation
        {
            get { return GetScreenOrientation(); }
            set
            {
            #pragma warning disable 618 // UnityEngine.ScreenOrientation.Unknown is obsolete
                if (value == ScreenOrientation.Unknown)
            #pragma warning restore 649
                {
                    Debug.Log("ScreenOrientation.Unknown is deprecated. Please use ScreenOrientation.AutoRotation");
                    value = ScreenOrientation.AutoRotation;
                }
                RequestOrientation(value);
            }
        }
        [NativeProperty("ScreenTimeout")] extern public static int sleepTimeout { get; set; }

        [NativeName("GetIsOrientationEnabled")] extern private static bool IsOrientationEnabled(EnabledOrientation orient);
        [NativeName("SetIsOrientationEnabled")] extern private static void SetOrientationEnabled(EnabledOrientation orient, bool enabled);

        public static bool autorotateToPortrait
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToPortrait); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToPortrait, value); }
        }
        public static bool autorotateToPortraitUpsideDown
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToPortraitUpsideDown); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToPortraitUpsideDown, value); }
        }
        public static bool autorotateToLandscapeLeft
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeLeft); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeLeft, value); }
        }
        public static bool autorotateToLandscapeRight
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeRight); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeRight, value); }
        }

        extern public static Resolution currentResolution { get; }
        extern public static bool fullScreen {[NativeName("IsFullscreen")] get; [NativeName("RequestSetFullscreenFromScript")] set; }
        extern public static FullScreenMode fullScreenMode {[NativeName("GetFullscreenMode")] get; [NativeName("RequestSetFullscreenModeFromScript")] set; }

        extern public static Rect safeArea { get; }
        extern public static Rect[] cutouts {[FreeFunction("ScreenScripting::GetCutouts")] get; }

        [NativeName("RequestResolution")]
        extern public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, RefreshRate preferredRefreshRate);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("SetResolution(int, int, FullScreenMode, int) is obsolete. Use SetResolution(int, int, FullScreenMode, RefreshRate) instead.")]
        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            if (preferredRefreshRate < 0)
                preferredRefreshRate = 0;

            SetResolution(width, height, fullscreenMode, new RefreshRate() { numerator = (uint)preferredRefreshRate, denominator = 1 });
        }

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode)
        {
            SetResolution(width, height, fullscreenMode, new RefreshRate() { numerator = 0, denominator = 1 });
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("SetResolution(int, int, bool, int) is obsolete. Use SetResolution(int, int, FullScreenMode, RefreshRate) instead.")]
        public static void SetResolution(int width, int height, bool fullscreen, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            if (preferredRefreshRate < 0)
                preferredRefreshRate = 0;

            SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, new RefreshRate() { numerator = (uint)preferredRefreshRate, denominator = 1 });
        }

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            SetResolution(width, height, fullscreen, 0);
        }

        [NativeName("SetRequestedMSAASamples")]
        extern public static void SetMSAASamples(int numSamples);

        [NativeName("GetRequestedMSAASamples")]
        extern private static int GetMSAASamples();

        public static int msaaSamples
        {
            get { return GetMSAASamples(); }
        }

        public static Vector2Int mainWindowPosition
        {
            get
            {
                return GetMainWindowPosition();
            }
        }

        public static DisplayInfo mainWindowDisplayInfo
        {
            get
            {
                return GetMainWindowDisplayInfo();
            }
        }

        public static void GetDisplayLayout(List<DisplayInfo> displayLayout)
        {
            if (displayLayout == null)
                throw new ArgumentNullException();

            GetDisplayLayoutImpl(displayLayout);
        }

        public static AsyncOperation MoveMainWindowTo(in DisplayInfo display, Vector2Int position)
        {
            return MoveMainWindowImpl(display, position);
        }

        [FreeFunction("GetMainWindowPosition")]
        extern static Vector2Int GetMainWindowPosition();

        [FreeFunction("GetMainWindowDisplayInfo")]
        extern static DisplayInfo GetMainWindowDisplayInfo();

        [FreeFunction("GetDisplayLayout")]
        extern static void GetDisplayLayoutImpl(List<DisplayInfo> displayLayout);

        [FreeFunction("MoveMainWindow")]
        extern static AsyncOperation MoveMainWindowImpl(in DisplayInfo display, Vector2Int position);

        extern public static Resolution[] resolutions {[FreeFunction("ScreenScripting::GetResolutions")] get; }

        extern public static float brightness { get; set; }
    }
}

namespace UnityEngine
{
    public sealed partial class Screen
    {
        public static int width => EditorScreen.width;
        public static int height => EditorScreen.height;
        public static float dpi => ShimManager.screenShim.dpi;
        public static Resolution currentResolution => ShimManager.screenShim.currentResolution;
        public static Resolution[] resolutions => ShimManager.screenShim.resolutions;

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, RefreshRate preferredRefreshRate)
        {
            ShimManager.screenShim.SetResolution(width, height, fullscreenMode, preferredRefreshRate);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("SetResolution(int, int, FullScreenMode, int) is obsolete. Use SetResolution(int, int, FullScreenMode, RefreshRate) instead.")]
        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            if (preferredRefreshRate < 0)
                preferredRefreshRate = 0;

            ShimManager.screenShim.SetResolution(width, height, fullscreenMode, new RefreshRate() { numerator = (uint)preferredRefreshRate, denominator = 1 });
        }

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode)
        {
            SetResolution(width, height, fullscreenMode, 0);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("SetResolution(int, int, bool, int) is obsolete. Use SetResolution(int, int, FullScreenMode, RefreshRate) instead.")]
        public static void SetResolution(int width, int height, bool fullscreen, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            if (preferredRefreshRate < 0)
                preferredRefreshRate = 0;

            SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, new RefreshRate() { numerator = (uint)preferredRefreshRate, denominator = 1 });
        }

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, new RefreshRate { numerator = 0, denominator = 1 });
        }

        public static void SetMSAASamples(int numSamples)
        {
            ShimManager.screenShim.SetMSAASamples(numSamples);
        }

        public static int msaaSamples
        {
            get { return ShimManager.screenShim.msaaSamples; }
        }

        public static bool fullScreen
        {
            get { return ShimManager.screenShim.fullScreen; }
            set { ShimManager.screenShim.fullScreen = value; }
        }

        public static FullScreenMode fullScreenMode
        {
            get { return ShimManager.screenShim.fullScreenMode; }
            set { ShimManager.screenShim.fullScreenMode = value; }
        }

        public static Rect safeArea => ShimManager.screenShim.safeArea;

        public static Rect[] cutouts => ShimManager.screenShim.cutouts;

        public static bool autorotateToPortrait
        {
            get { return ShimManager.screenShim.autorotateToPortrait; }
            set { ShimManager.screenShim.autorotateToPortrait = value; }
        }

        public static bool autorotateToPortraitUpsideDown
        {
            get { return ShimManager.screenShim.autorotateToPortraitUpsideDown; }
            set { ShimManager.screenShim.autorotateToPortraitUpsideDown = value; }
        }

        public static bool autorotateToLandscapeLeft
        {
            get { return ShimManager.screenShim.autorotateToLandscapeLeft; }
            set { ShimManager.screenShim.autorotateToLandscapeLeft = value; }
        }

        public static bool autorotateToLandscapeRight
        {
            get { return ShimManager.screenShim.autorotateToLandscapeRight; }
            set { ShimManager.screenShim.autorotateToLandscapeRight = value; }
        }

        public static ScreenOrientation orientation
        {
            get { return ShimManager.screenShim.orientation; }
            set { ShimManager.screenShim.orientation = value; }
        }

        public static int sleepTimeout
        {
            get { return ShimManager.screenShim.sleepTimeout; }
            set { ShimManager.screenShim.sleepTimeout = value; }
        }

        public static float brightness
        {
            get { return ShimManager.screenShim.brightness; }
            set { ShimManager.screenShim.brightness = value; }
        }

        public static Vector2Int mainWindowPosition => EditorScreen.mainWindowPosition;
        public static DisplayInfo mainWindowDisplayInfo => EditorScreen.mainWindowDisplayInfo;
        public static void GetDisplayLayout(List<DisplayInfo> displayLayout) => EditorScreen.GetDisplayLayout(displayLayout);
        public static AsyncOperation MoveMainWindowTo(in DisplayInfo display, Vector2Int position) => EditorScreen.MoveMainWindowTo(display, position);
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public partial struct RenderBuffer
    {
        [FreeFunction(Name = "RenderBufferScripting::SetLoadAction", HasExplicitThis = true)]
        extern internal void SetLoadAction(RenderBufferLoadAction action);
        [FreeFunction(Name = "RenderBufferScripting::SetStoreAction", HasExplicitThis = true)]
        extern internal void SetStoreAction(RenderBufferStoreAction action);

        [FreeFunction(Name = "RenderBufferScripting::GetLoadAction", HasExplicitThis = true)]
        extern internal RenderBufferLoadAction GetLoadAction();
        [FreeFunction(Name = "RenderBufferScripting::GetStoreAction", HasExplicitThis = true)]
        extern internal RenderBufferStoreAction GetStoreAction();

        [FreeFunction(Name = "RenderBufferScripting::GetNativeRenderBufferPtr", HasExplicitThis = true)]
        extern public IntPtr GetNativeRenderBufferPtr();
    }
}

namespace UnityEngineInternal
{
    public enum MemorylessMode
    {
        Unused,
        Forced,
        Automatic,
    }
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public class MemorylessManager
    {
        public static MemorylessMode depthMemorylessMode
        {
            get { return GetFramebufferDepthMemorylessMode(); }
            set { SetFramebufferDepthMemorylessMode(value); }
        }
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "GetFramebufferDepthMemorylessMode")]
        extern internal static MemorylessMode GetFramebufferDepthMemorylessMode();
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "SetFramebufferDepthMemorylessMode")]
        extern internal static void SetFramebufferDepthMemorylessMode(MemorylessMode mode);
    }
}

namespace UnityEngine
{
    [NativeType("Runtime/GfxDevice/GfxDeviceTypes.h")]
    public enum ComputeBufferMode
    {
        Immutable = 0,
        Dynamic,
        [Obsolete("ComputeBufferMode.Circular is deprecated (legacy mode)")]
        Circular,
        [Obsolete("ComputeBufferMode.StreamOut is deprecated (internal use only)")]
        StreamOut,
        SubUpdates,
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/LightProbeProxyVolume.h")]
    [NativeHeader("Runtime/Graphics/ColorGamut.h")]
    [NativeHeader("Runtime/Graphics/CopyTexture.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public partial class Graphics
    {
        [FreeFunction("GraphicsScripting::GetMaxDrawMeshInstanceCount", IsThreadSafe = true)] extern private static int Internal_GetMaxDrawMeshInstanceCount();
        internal static readonly int kMaxDrawMeshInstanceCount = Internal_GetMaxDrawMeshInstanceCount();

        [FreeFunction] extern private static ColorGamut GetActiveColorGamut();
        public static ColorGamut activeColorGamut { get { return GetActiveColorGamut(); } }

        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)] extern public static UnityEngine.Rendering.GraphicsTier activeTier { get; set; }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "GetPreserveFramebufferAlpha")]
        extern internal static bool GetPreserveFramebufferAlpha();
        public static bool preserveFramebufferAlpha { get { return GetPreserveFramebufferAlpha(); } }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "GetMinOpenGLESVersion")]
        extern internal static OpenGLESVersion GetMinOpenGLESVersion();
        public static OpenGLESVersion minOpenGLESVersion { get { return GetMinOpenGLESVersion(); } }


        [FreeFunction("GraphicsScripting::GetActiveColorBuffer")] extern private static RenderBuffer GetActiveColorBuffer();
        [FreeFunction("GraphicsScripting::GetActiveDepthBuffer")] extern private static RenderBuffer GetActiveDepthBuffer();

        [FreeFunction("GraphicsScripting::SetNullRT")] extern private static void Internal_SetNullRT();
        [NativeMethod(Name = "GraphicsScripting::SetGfxRT", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetGfxRT(GraphicsTexture gfxTex, int mip, CubemapFace face, int depthSlice);
        [NativeMethod(Name = "GraphicsScripting::SetRTSimple", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetRTSimple(RenderBuffer color, RenderBuffer depth, int mip, CubemapFace face, int depthSlice);
        [NativeMethod(Name = "GraphicsScripting::SetMRTSimple", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetMRTSimple([NotNull] RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face, int depthSlice);
        [NativeMethod(Name = "GraphicsScripting::SetMRTFull", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetMRTFullSetup(
            [NotNull] RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face, int depthSlice,
            [NotNull] RenderBufferLoadAction[] colorLA, [NotNull] RenderBufferStoreAction[] colorSA,
            RenderBufferLoadAction depthLA, RenderBufferStoreAction depthSA
        );

        [NativeMethod(Name = "GraphicsScripting::SetRandomWriteTargetRT", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetRandomWriteTargetRT(int index, RenderTexture uav);
        [FreeFunction("GraphicsScripting::SetRandomWriteTargetBuffer")]
        extern private static void Internal_SetRandomWriteTargetBuffer(int index, ComputeBuffer uav, bool preserveCounterValue);
        [FreeFunction("GraphicsScripting::SetRandomWriteTargetBuffer")]
        extern private static void Internal_SetRandomWriteTargetGraphicsBuffer(int index, GraphicsBuffer uav, bool preserveCounterValue);

        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)] extern public static void ClearRandomWriteTargets();

        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Full(Texture src, Texture dst);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice_AllMips(Texture src, int srcElement, Texture dst, int dstElement);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice(Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip);
        [FreeFunction("CopyTextureRegion")] extern private static void CopyTexture_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Full_Gfx(GraphicsTexture src, GraphicsTexture dst);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice_AllMips_Gfx(GraphicsTexture src, int srcElement, GraphicsTexture dst, int dstElement);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice_Gfx(GraphicsTexture src, int srcElement, int srcMip, GraphicsTexture dst, int dstElement, int dstMip);
        [FreeFunction("CopyTextureRegion")] extern private static void CopyTexture_Region_Gfx(GraphicsTexture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsTexture dst, int dstElement, int dstMip, int dstX, int dstY);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Full(Texture src, Texture dst);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Slice(Texture src, int srcElement, Texture dst, int dstElement);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Full_Gfx(GraphicsTexture src, GraphicsTexture dst);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Slice_Gfx(GraphicsTexture src, int srcElement, GraphicsTexture dst, int dstElement);

        [FreeFunction("GraphicsScripting::CopyBuffer", ThrowsException = true)] static extern void CopyBufferImpl([NotNull] GraphicsBuffer source, [NotNull] GraphicsBuffer dest);

        [FreeFunction("GraphicsScripting::DrawMeshNow")] extern private static void Internal_DrawMeshNow1([NotNull] Mesh mesh, int subsetIndex, Vector3 position, Quaternion rotation);
        [FreeFunction("GraphicsScripting::DrawMeshNow")] extern private static void Internal_DrawMeshNow2([NotNull] Mesh mesh, int subsetIndex, Matrix4x4 matrix);

        [FreeFunction("GraphicsScripting::DrawTexture")][VisibleToOtherModules("UnityEngine.IMGUIModule")]
        extern internal static void Internal_DrawTexture(ref Internal_DrawTextureArguments args);

        [FreeFunction("GraphicsScripting::RenderMesh")]
        extern unsafe private static void Internal_RenderMesh(RenderParams rparams, [NotNull] Mesh mesh, int submeshIndex, Matrix4x4 objectToWorld, Matrix4x4* prevObjectToWorld);

        [FreeFunction("GraphicsScripting::RenderMeshInstanced")]
        extern private static void Internal_RenderMeshInstanced(RenderParams rparams, [NotNull] Mesh mesh, int submeshIndex, IntPtr instanceData, RenderInstancedDataLayout layout, uint instanceCount);

        [FreeFunction("GraphicsScripting::RenderMeshIndirect")]
        extern private static void Internal_RenderMeshIndirect(RenderParams rparams, [NotNull] Mesh mesh, [NotNull] GraphicsBuffer argsBuffer, int commandCount, int startCommand);

        [FreeFunction("GraphicsScripting::RenderMeshPrimitives")]
        extern private static void Internal_RenderMeshPrimitives(RenderParams rparams, [NotNull] Mesh mesh, int submeshIndex, int instanceCount);

        [FreeFunction("GraphicsScripting::RenderPrimitives")]
        extern private static void Internal_RenderPrimitives(RenderParams rparams, MeshTopology topology, int vertexCount, int instanceCount);

        [FreeFunction("GraphicsScripting::RenderPrimitivesIndexed")]
        extern private static void Internal_RenderPrimitivesIndexed(RenderParams rparams, MeshTopology topology, [NotNull] GraphicsBuffer indexBuffer, int indexCount, int startIndex, int instanceCount);

        [FreeFunction("GraphicsScripting::RenderPrimitivesIndirect")]
        extern private static void Internal_RenderPrimitivesIndirect(RenderParams rparams, MeshTopology topology, [NotNull] GraphicsBuffer argsBuffer, int commandCount, int startCommand);

        [FreeFunction("GraphicsScripting::RenderPrimitivesIndexedIndirect")]
        extern private static void Internal_RenderPrimitivesIndexedIndirect(RenderParams rparams, MeshTopology topology, [NotNull] GraphicsBuffer indexBuffer, [NotNull] GraphicsBuffer commandBuffer, int commandCount, int startCommand);

        [FreeFunction("GraphicsScripting::DrawMesh")]
        extern private static void Internal_DrawMesh(Mesh mesh, int submeshIndex, Matrix4x4 matrix, Material material, int layer, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawMeshInstanced")]
        extern private static void Internal_DrawMeshInstanced([NotNull] Mesh mesh, int submeshIndex, [NotNull] Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawMeshInstancedProcedural")]
        extern private static void Internal_DrawMeshInstancedProcedural([NotNull] Mesh mesh, int submeshIndex, [NotNull] Material material, Bounds bounds, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawMeshInstancedIndirect")]
        extern private static void Internal_DrawMeshInstancedIndirect([NotNull] Mesh mesh, int submeshIndex, [NotNull] Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);
        [FreeFunction("GraphicsScripting::DrawMeshInstancedIndirect")]
        extern private static void Internal_DrawMeshInstancedIndirectGraphicsBuffer([NotNull] Mesh mesh, int submeshIndex, [NotNull]  Material material, Bounds bounds, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawProceduralNow")]
        extern private static void Internal_DrawProceduralNow(MeshTopology topology, int vertexCount, int instanceCount);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedNow")]
        extern private static void Internal_DrawProceduralIndexedNow(MeshTopology topology, GraphicsBuffer indexBuffer, int indexCount, int instanceCount);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirectNow")]
        extern private static void Internal_DrawProceduralIndirectNow(MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirectNow")]
        extern private static void Internal_DrawProceduralIndexedIndirectNow(MeshTopology topology, GraphicsBuffer indexBuffer, ComputeBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirectNow")]
        extern private static void Internal_DrawProceduralIndirectNowGraphicsBuffer(MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirectNow")]
        extern private static void Internal_DrawProceduralIndexedIndirectNowGraphicsBuffer(MeshTopology topology, GraphicsBuffer indexBuffer, GraphicsBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProcedural")]
        extern private static void Internal_DrawProcedural(Material material, Bounds bounds, MeshTopology topology, int vertexCount, int instanceCount, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexed")]
        extern private static void Internal_DrawProceduralIndexed(Material material, Bounds bounds, MeshTopology topology, GraphicsBuffer indexBuffer, int indexCount, int instanceCount, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirect")]
        extern private static void Internal_DrawProceduralIndirect(Material material, Bounds bounds, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirect")]
        extern private static void Internal_DrawProceduralIndirectGraphicsBuffer(Material material, Bounds bounds, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirect")]
        extern private static void Internal_DrawProceduralIndexedIndirect(Material material, Bounds bounds, MeshTopology topology, GraphicsBuffer indexBuffer, ComputeBuffer bufferWithArgs, int argsOffset, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirect")]
        extern private static void Internal_DrawProceduralIndexedIndirectGraphicsBuffer(Material material, Bounds bounds, MeshTopology topology, GraphicsBuffer indexBuffer, GraphicsBuffer bufferWithArgs, int argsOffset, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterial5(Texture source, RenderTexture dest, [NotNull] Material mat, int pass, bool setRT);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterial6(Texture source, RenderTexture dest, [NotNull] Material mat, int pass, bool setRT, int destDepthSlice);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTap4(Texture source, RenderTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTap5(Texture source, RenderTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets, int destDepthSlice);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit2(Texture source, RenderTexture dest);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit3(Texture source, RenderTexture dest, int sourceDepthSlice, int destDepthSlice);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit4(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit5(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset, int sourceDepthSlice, int destDepthSlice);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterialGfx5(Texture source, GraphicsTexture dest, [NotNull] Material mat, int pass, bool setRT);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterialGfx6(Texture source, GraphicsTexture dest, [NotNull] Material mat, int pass, bool setRT, int destDepthSlice);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTapGfx4(Texture source, GraphicsTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTapGfx5(Texture source, GraphicsTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets, int destDepthSlice);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void BlitGfx2(Texture source, GraphicsTexture dest);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void BlitGfx3(Texture source, GraphicsTexture dest, int sourceDepthSlice, int destDepthSlice);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void BlitGfx4(Texture source, GraphicsTexture dest, Vector2 scale, Vector2 offset);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void BlitGfx5(Texture source, GraphicsTexture dest, Vector2 scale, Vector2 offset, int sourceDepthSlice, int destDepthSlice);

        [NativeMethod(Name = "GraphicsScripting::CreateGPUFence", IsFreeFunction = true, ThrowsException = true)]
        extern private static IntPtr CreateGPUFenceImpl(GraphicsFenceType fenceType, SynchronisationStageFlags stage);

        [NativeMethod(Name = "GraphicsScripting::WaitOnGPUFence", IsFreeFunction = true, ThrowsException = true)]
        extern private static void WaitOnGPUFenceImpl(IntPtr fencePtr, SynchronisationStageFlags stage);

        [NativeMethod(Name = "GraphicsScripting::ExecuteCommandBuffer", IsFreeFunction = true, ThrowsException = true)]
        extern public static void ExecuteCommandBuffer([NotNull] CommandBuffer buffer);

        [NativeMethod(Name = "GraphicsScripting::ExecuteCommandBufferAsync", IsFreeFunction = true, ThrowsException = true)]
        extern public  static void ExecuteCommandBufferAsync([NotNull] CommandBuffer buffer, ComputeQueueType queueType);
    }
}

namespace UnityEngine
{
    public sealed partial class GL
    {
        public const int TRIANGLES      = 0x0004;
        public const int TRIANGLE_STRIP = 0x0005;
        public const int QUADS          = 0x0007;
        public const int LINES          = 0x0001;
        public const int LINE_STRIP     = 0x0002;
    }


    [NativeHeader("Runtime/GfxDevice/GfxDevice.h")]
    [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
    public sealed partial class GL
    {
        [NativeName("ImmediateVertex")] extern public static void Vertex3(float x, float y, float z);
        public static void Vertex(Vector3 v) { Vertex3(v.x, v.y, v.z); }

        [NativeName("ImmediateVertices")]
        extern internal static unsafe void Vertices(Vector3* v, Vector3* coords, Vector4* colors, int length);

        [NativeName("ImmediateTexCoordAll")] extern public static void TexCoord3(float x, float y, float z);
        public static void TexCoord(Vector3 v)          { TexCoord3(v.x, v.y, v.z); }
        public static void TexCoord2(float x, float y)  { TexCoord3(x, y, 0.0f); }

        [NativeName("ImmediateTexCoord")] extern public static void MultiTexCoord3(int unit, float x, float y, float z);
        public static void MultiTexCoord(int unit, Vector3 v)           { MultiTexCoord3(unit, v.x, v.y, v.z); }
        public static void MultiTexCoord2(int unit, float x, float y)   { MultiTexCoord3(unit, x, y, 0.0f); }

        [NativeName("ImmediateColor")] extern private static void ImmediateColor(float r, float g, float b, float a);
        public static void Color(Color c) { ImmediateColor(c.r, c.g, c.b, c.a); }

        extern public static bool wireframe     { get; set; }
        extern public static bool sRGBWrite     { get; set; }
        [NativeProperty("UserBackfaceMode")] extern public static bool invertCulling { get;  set; }

        extern public static void Flush();
        extern public static void RenderTargetBarrier();

        extern private static Matrix4x4 GetWorldViewMatrix();
        extern private static void SetViewMatrix(Matrix4x4 m);
        static public Matrix4x4 modelview { get { return GetWorldViewMatrix(); } set { SetViewMatrix(value); } }

        [NativeName("SetWorldMatrix")] extern public static void MultMatrix(Matrix4x4 m);

        [Obsolete("IssuePluginEvent(eventID) is deprecated. Use IssuePluginEvent(callback, eventID) instead.", false)]
        [NativeName("InsertCustomMarker")] extern public static void IssuePluginEvent(int eventID);

        [Obsolete("SetRevertBackfacing(revertBackFaces) is deprecated. Use invertCulling property instead. (UnityUpgradable) -> invertCulling", false)]
        [NativeName("SetUserBackfaceMode")] extern public static void SetRevertBackfacing(bool revertBackFaces);
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/CameraUtil.h")]
    public sealed partial class GL
    {
        [FreeFunction("GLPushMatrixScript")]            extern public static void PushMatrix();
        [FreeFunction("GLPopMatrixScript")]             extern public static void PopMatrix();
        [FreeFunction("GLLoadIdentityScript")]          extern public static void LoadIdentity();
        [FreeFunction("GLLoadOrthoScript")]             extern public static void LoadOrtho();
        [FreeFunction("GLLoadPixelMatrixScript")]       extern public static void LoadPixelMatrix();
        [FreeFunction("GLLoadProjectionMatrixScript")]  extern public static void LoadProjectionMatrix(Matrix4x4 mat);
        [FreeFunction("GLInvalidateState")]             extern public static void InvalidateState();
        [FreeFunction("GLGetGPUProjectionMatrix")]      extern public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 proj, bool renderIntoTexture);

        [FreeFunction] extern private static void GLLoadPixelMatrixScript(float left, float right, float bottom, float top);
        public static void LoadPixelMatrix(float left, float right, float bottom, float top)
        {
            GLLoadPixelMatrixScript(left, right, bottom, top);
        }

        [FreeFunction] extern private static void GLIssuePluginEvent(IntPtr callback, int eventID);
        public static void IssuePluginEvent(IntPtr callback, int eventID)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.", "callback");
            GLIssuePluginEvent(callback, eventID);
        }

        [FreeFunction("GLBegin", ThrowsException = true)] extern public static void Begin(int mode);
        [FreeFunction("GLEnd")]                           extern public static void End();

        [FreeFunction] extern private static void GLClear(bool clearDepth, bool clearColor, Color backgroundColor, float depth);
        static public void Clear(bool clearDepth, bool clearColor, Color backgroundColor, [uei.DefaultValue("1.0f")] float depth)
        {
            GLClear(clearDepth, clearColor, backgroundColor, depth);
        }

        static public void Clear(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            GLClear(clearDepth, clearColor, backgroundColor, 1.0f);
        }

        [FreeFunction("SetGLViewport")] extern public static void Viewport(Rect pixelRect);
        [FreeFunction("ClearWithSkybox")] extern public static void ClearWithSkybox(bool clearDepth, Camera camera);
    }
}

namespace UnityEngine
{
    // Scales render textures to support dynamic resolution.
    [NativeHeader("Runtime/GfxDevice/ScalableBufferManager.h")]
    [StaticAccessor("ScalableBufferManager::GetInstance()", StaticAccessorType.Dot)]
    static public class ScalableBufferManager
    {
        extern static public float widthScaleFactor { get; }
        extern static public float heightScaleFactor { get; }

        static extern public void ResizeBuffers(float widthScale, float heightScale);
    }

    [NativeHeader("Runtime/GfxDevice/FrameTiming.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameTiming
    {
        // Duration
        [NativeName("totalFrameTime")]            public double cpuFrameTime;
        [NativeName("mainThreadActiveTime")]      public double cpuMainThreadFrameTime;
        [NativeName("mainThreadPresentWaitTime")] public double cpuMainThreadPresentWaitTime;
        [NativeName("renderThreadActiveTime")]    public double cpuRenderThreadFrameTime;
        [NativeName("gpuFrameTime")]              public double gpuFrameTime;

        // Timestamps
        [NativeName("frameStartTimestamp")]      public UInt64 frameStartTimestamp;
        [NativeName("firstSubmitTimestamp")]     public UInt64 firstSubmitTimestamp;
        [NativeName("presentFrameTimestamp")]    public UInt64 cpuTimePresentCalled;
        [NativeName("frameCompleteTimestamp")]   public UInt64 cpuTimeFrameComplete;

        // Linked dynamic resolution data
        [NativeName("heightScale")]              public float heightScale;
        [NativeName("widthScale")]               public float widthScale;
        [NativeName("syncInterval")]             public UInt32 syncInterval;
    }

    [StaticAccessor("GetUncheckedRealGfxDevice().GetFrameTimingManager()", StaticAccessorType.Dot)]
    static public class FrameTimingManager
    {
        [StaticAccessor("FrameTimingManager", StaticAccessorType.DoubleColon)]
        static extern public bool IsFeatureEnabled();

        static extern public void CaptureFrameTimings();
        static extern public UInt32 GetLatestTimings(UInt32 numFrames, FrameTiming[] timings);

        static extern public float GetVSyncsPerSecond();
        static extern public UInt64 GetGpuTimerFrequency();
        static extern public UInt64 GetCpuTimerFrequency();
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [StaticAccessor("GeometryUtilityScripting", StaticAccessorType.DoubleColon)]
    public sealed partial class GeometryUtility
    {
        extern public static bool TestPlanesAABB(Plane[] planes, Bounds bounds);

        [NativeName("ExtractPlanes")]   extern private static void Internal_ExtractPlanes([Out] Plane[] planes, Matrix4x4 worldToProjectionMatrix);
        [NativeName("CalculateBounds")] extern private static Bounds Internal_CalculateBounds(Vector3[] positions, Matrix4x4 transform);
    }
}

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Graphics/LightmapData.h")]
    public sealed partial class LightmapData
    {
        internal Texture2D m_Light;
        internal Texture2D m_Dir;
        internal Texture2D m_ShadowMask;

        [System.Obsolete("Use lightmapColor property (UnityUpgradable) -> lightmapColor", false)]
        public Texture2D lightmapLight { get { return m_Light; }        set { m_Light = value; } }

        public Texture2D lightmapColor { get { return m_Light; }        set { m_Light = value; } }
        public Texture2D lightmapDir   { get { return m_Dir; }          set { m_Dir = value; } }
        public Texture2D shadowMask    { get { return m_ShadowMask; }   set { m_ShadowMask = value; } }
    }

    // Stores lightmaps of the scene.
    [NativeHeader("Runtime/Graphics/LightmapSettings.h")]
    [StaticAccessor("GetLightmapSettings()")]
    public sealed partial class LightmapSettings : Object
    {
        private LightmapSettings() {}

        // Lightmap array.
        public extern static LightmapData[] lightmaps {[FreeFunction][return: Unmarshalled] get; [FreeFunction(ThrowsException = true)][param: Unmarshalled] set; }

        public extern static LightmapsMode lightmapsMode { get; [FreeFunction(ThrowsException = true)] set; }

        // Holds all data needed by the light probes.
        public extern static LightProbes lightProbes
        {
            get;

            [FreeFunction]
            [NativeName("SetLightProbes")]
            set;
        }

        [NativeName("ResetAndAwakeFromLoad")]
        internal static extern void Reset();
    }
}

namespace UnityEngine
{
    // Stores light probes for the scene.
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Export/Graphics/Graphics.bindings.h")]
    public sealed partial class LightProbes : Object
    {
        private LightProbes() {}

        public static event Action lightProbesUpdated;
        [RequiredByNativeCode]
        private static void Internal_CallLightProbesUpdatedFunction()
        {
            if (lightProbesUpdated != null)
                lightProbesUpdated();
        }

        public static event Action tetrahedralizationCompleted;
        [RequiredByNativeCode]
        private static void Internal_CallTetrahedralizationCompletedFunction()
        {
            if (tetrahedralizationCompleted != null)
                tetrahedralizationCompleted();
        }

        public static event Action needsRetetrahedralization;
        [RequiredByNativeCode]
        private static void Internal_CallNeedsRetetrahedralizationFunction()
        {
            if (needsRetetrahedralization != null)
                needsRetetrahedralization();
        }

        [FreeFunction]
        public static extern void Tetrahedralize();

        [FreeFunction]
        public static extern void TetrahedralizeAsync();

        [FreeFunction]
        public extern static void GetInterpolatedProbe(Vector3 position, Renderer renderer, out UnityEngine.Rendering.SphericalHarmonicsL2 probe);

        [FreeFunction]
        internal static extern bool AreLightProbesAllowed(Renderer renderer);

        public static void CalculateInterpolatedLightAndOcclusionProbes(Vector3[] positions, SphericalHarmonicsL2[] lightProbes, Vector4[] occlusionProbes)
        {
            if (positions == null)
                throw new ArgumentNullException("positions");
            else if (lightProbes == null && occlusionProbes == null)
                throw new ArgumentException("Argument lightProbes and occlusionProbes cannot both be null.");
            else if (lightProbes != null && lightProbes.Length < positions.Length)
                throw new ArgumentException("lightProbes", "Argument lightProbes has less elements than positions");
            else if (occlusionProbes != null && occlusionProbes.Length < positions.Length)
                throw new ArgumentException("occlusionProbes", "Argument occlusionProbes has less elements than positions");

            CalculateInterpolatedLightAndOcclusionProbes_Internal(positions, positions.Length, lightProbes, occlusionProbes);
        }

        public static void CalculateInterpolatedLightAndOcclusionProbes(List<Vector3> positions, List<SphericalHarmonicsL2> lightProbes, List<Vector4> occlusionProbes)
        {
            if (positions == null)
                throw new ArgumentNullException("positions");
            else if (lightProbes == null && occlusionProbes == null)
                throw new ArgumentException("Argument lightProbes and occlusionProbes cannot both be null.");

            if (lightProbes != null)
                NoAllocHelpers.EnsureListElemCount(lightProbes, positions.Count);

            if (occlusionProbes != null)
                NoAllocHelpers.EnsureListElemCount(occlusionProbes, positions.Count);

            CalculateInterpolatedLightAndOcclusionProbes_Internal(NoAllocHelpers.ExtractArrayFromList(positions), positions.Count, NoAllocHelpers.ExtractArrayFromList(lightProbes), NoAllocHelpers.ExtractArrayFromList(occlusionProbes));
        }

        [FreeFunction]
        [NativeName("CalculateInterpolatedLightAndOcclusionProbes")]
        internal extern static void CalculateInterpolatedLightAndOcclusionProbes_Internal(Vector3[] positions, int positionsCount, SphericalHarmonicsL2[] lightProbes, Vector4[] occlusionProbes);

        [FreeFunction]
        [NativeName("GetSharedLightProbesForScene")]
        public extern static LightProbes GetSharedLightProbesForScene(SceneManagement.Scene scene);

        [FreeFunction]
        [NativeName("GetInstantiatedLightProbesForScene")]
        public extern static LightProbes GetInstantiatedLightProbesForScene(SceneManagement.Scene scene);

        // Positions of the baked light probes.
        public extern Vector3[] positions
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetLightProbePositions")] get;
        }

        [FreeFunction(HasExplicitThis = true)]
        [NativeName("GetLightProbePositionsSelf")]
        public extern Vector3[] GetPositionsSelf();

        [FreeFunction(HasExplicitThis = true)]
        [NativeName("SetLightProbePositionsSelf")]
        public extern bool SetPositionsSelf(Vector3[] positions, bool checkForDuplicatePositions);

        public extern UnityEngine.Rendering.SphericalHarmonicsL2[] bakedProbes
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetBakedCoefficients")] get;

            [FreeFunction(HasExplicitThis = true)]
            [NativeName("SetBakedCoefficients")] set;
        }

        // The number of light probes.
        public extern int count
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetLightProbeCount")] get;
        }

        public extern int countSelf
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetLightProbeCountSelf")] get;
        }

        // The number of cells (tetrahedra + outer cells) the space is divided to.
        public extern int cellCount
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetTetrahedraSize")] get;
        }

        public extern int cellCountSelf
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetTetrahedraSizeSelf")]
            get;
        }

        [FreeFunction]
        [NativeName("GetLightProbeCount")]
        internal static extern int GetCount();
    }
}

namespace UnityEngine
{
    [Obsolete("D3DHDRDisplayBitDepth has been replaced by HDRDisplayBitDepth. (UnityUpgradable) -> HDRDisplayBitDepth", true)]
    public enum D3DHDRDisplayBitDepth
    {
        [Obsolete("D3DHDRDisplayBitDepth::D3DHDRDisplayBitDepth10 has been replaced by HDRDisplayBitDepth::BitDepth10 (UnityUpgradable) -> HDRDisplayBitDepth.BitDepth10", true)]
        D3DHDRDisplayBitDepth10 = 0,
        [Obsolete("D3DHDRDisplayBitDepth::D3DHDRDisplayBitDepth16 has been replaced by HDRDisplayBitDepth::BitDepth16 (UnityUpgradable) -> HDRDisplayBitDepth.BitDepth16", true)]
        D3DHDRDisplayBitDepth16 = 1
    }

    [NativeHeader("Runtime/GfxDevice/HDROutputSettings.h")]
    [UsedByNativeCode]
    public class HDROutputSettings
    {
        private int m_DisplayIndex;

        //Don't allow users to construct these themselves, instead they need to be accessed from an internally managed list
        //This lines up with how multiple displays are handled, and while HDR is currently primary display only this will help with
        //future proofing this implementation, see Display in Display.bindings.cs
        [VisibleToOtherModules("UnityEngine.XRModule")]
        internal HDROutputSettings() { m_DisplayIndex = 0; }
        [VisibleToOtherModules("UnityEngine.XRModule")]
        internal HDROutputSettings(int displayIndex) { this.m_DisplayIndex = displayIndex; }

        public static HDROutputSettings[] displays = new HDROutputSettings[1] { new HDROutputSettings() };
        private static HDROutputSettings _mainDisplay = displays[0];
        public static HDROutputSettings main { get { return _mainDisplay; } }

        public bool active { get { return GetActive(m_DisplayIndex); } }
        public bool available { get { return GetAvailable(m_DisplayIndex); } }
        public bool automaticHDRTonemapping
        {
            get
            {
                return GetAutomaticHDRTonemapping(m_DisplayIndex);
            }
            set
            {
                SetAutomaticHDRTonemapping(m_DisplayIndex, value);
            }
        }
        public ColorGamut displayColorGamut { get { return GetDisplayColorGamut(m_DisplayIndex); } }
        public RenderTextureFormat format { get { return GraphicsFormatUtility.GetRenderTextureFormat(GetGraphicsFormat(m_DisplayIndex)); } }
        public GraphicsFormat graphicsFormat { get { return GetGraphicsFormat(m_DisplayIndex); }  }
        public float paperWhiteNits
        {
            get
            {
                return GetPaperWhiteNits(m_DisplayIndex);
            }
            set
            {
                SetPaperWhiteNits(m_DisplayIndex, value);
            }
        }
        public int maxFullFrameToneMapLuminance { get { return GetMaxFullFrameToneMapLuminance(m_DisplayIndex); } }
        public int maxToneMapLuminance { get { return GetMaxToneMapLuminance(m_DisplayIndex); } }
        public int minToneMapLuminance { get { return GetMinToneMapLuminance(m_DisplayIndex); } }
        public bool HDRModeChangeRequested { get { return GetHDRModeChangeRequested(m_DisplayIndex); } }

        public void RequestHDRModeChange(bool enabled)
        {
            RequestHDRModeChangeInternal(m_DisplayIndex, enabled);
        }

        [Obsolete("SetPaperWhiteInNits is deprecated, please use paperWhiteNits instead.")]
        public static void SetPaperWhiteInNits(float paperWhite)
        {
            int mainDisplay = 0;
            //Set paper white on the primary display
            if (GetAvailable(mainDisplay))
                SetPaperWhiteNits(mainDisplay, paperWhite);
        }

        [FreeFunction("HDROutputSettingsBindings::GetActive", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetActive(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetAvailable", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetAvailable(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetAutomaticHDRTonemapping", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetAutomaticHDRTonemapping(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::SetAutomaticHDRTonemapping", HasExplicitThis = false, ThrowsException = true)]
        extern private static void SetAutomaticHDRTonemapping(int displayIndex, bool scripted);

        [FreeFunction("HDROutputSettingsBindings::GetDisplayColorGamut", HasExplicitThis = false, ThrowsException = true)]
        extern private static ColorGamut GetDisplayColorGamut(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetGraphicsFormat", HasExplicitThis = false, ThrowsException = true)]
        extern private static GraphicsFormat GetGraphicsFormat(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetPaperWhiteNits", HasExplicitThis = false, ThrowsException = true)]
        extern private static float GetPaperWhiteNits(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::SetPaperWhiteNits", HasExplicitThis = false, ThrowsException = true)]
        extern private static void SetPaperWhiteNits(int displayIndex, float paperWhite);

        [FreeFunction("HDROutputSettingsBindings::GetMaxFullFrameToneMapLuminance", HasExplicitThis = false, ThrowsException = true)]
        extern private static int GetMaxFullFrameToneMapLuminance(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetMaxToneMapLuminance", HasExplicitThis = false, ThrowsException = true)]
        extern private static int GetMaxToneMapLuminance(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetMinToneMapLuminance", HasExplicitThis = false, ThrowsException = true)]
        extern private static int GetMinToneMapLuminance(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetHDRModeChangeRequested", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetHDRModeChangeRequested(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::RequestHDRModeChange", HasExplicitThis = false, ThrowsException = true)]
        extern private static void RequestHDRModeChangeInternal(int displayIndex, bool enabled);
    }

    public class ColorGamutUtility
    {
        [FreeFunction(IsThreadSafe = true)]
        extern public static ColorPrimaries GetColorPrimaries(ColorGamut gamut);

        [FreeFunction(IsThreadSafe = true)]
        extern public static WhitePoint GetWhitePoint(ColorGamut gamut);

        [FreeFunction(IsThreadSafe = true)]
        extern public static TransferFunction GetTransferFunction(ColorGamut gamut);
    }
}

namespace UnityEngine.Rendering
{
    [NativeHeader("PlatformDependent/Win/Profiler/PixBindings.h")]
    [NativeConditional("PLATFORM_WIN && ENABLE_PROFILER")]
    public class PIX
    {
        [FreeFunction("PIX::BeginGPUCapture")]
        public static extern void BeginGPUCapture();

        [FreeFunction("PIX::EndGPUCapture")]
        public static extern void EndGPUCapture();

        [FreeFunction("PIX::IsAttached")]
        public static extern bool IsAttached();
    }
}

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public static class ExternalGPUProfiler
    {
        [FreeFunction("ExternalGPUProfilerBindings::BeginGPUCapture")]
        public static extern void BeginGPUCapture();

        [FreeFunction("ExternalGPUProfilerBindings::EndGPUCapture")]
        public static extern void EndGPUCapture();

        [FreeFunction("ExternalGPUProfilerBindings::IsAttached")]
        public static extern bool IsAttached();
    }
}

namespace UnityEngine.Experimental.Rendering
{
    public enum WaitForPresentSyncPoint
    {
        BeginFrame = 0,
        EndFrame = 1
    }

    public enum GraphicsJobsSyncPoint
    {
        EndOfFrame = 0,
        AfterScriptUpdate = 1,
        AfterScriptLateUpdate = 2,
        WaitForPresent = 3
    }

    public static partial class GraphicsDeviceSettings
    {
        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
        extern public static WaitForPresentSyncPoint waitForPresentSyncPoint { get; set; }

        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
        extern public static GraphicsJobsSyncPoint graphicsJobsSyncPoint { get; set; }
    }
}

namespace UnityEngine.Rendering
{
    public static partial class LoadStoreActionDebugModeSettings
    {
        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
        extern public static bool LoadStoreDebugModeEnabled { get; set; }
    }
}
