// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Color key used by Gradient
    [UsedByNativeCode]
    public struct GradientColorKey
    {
        // Gradient color key
        public GradientColorKey(Color col, float time)
        {
            color = col;
            this.time = time;
        }

        // color of key
        public Color color;

        // time of the key (0 - 1)
        public float time;
    }

    // Alpha key used by Gradient
    [UsedByNativeCode]
    public struct GradientAlphaKey
    {
        // Gradient alpha key
        public GradientAlphaKey(float alpha, float time)
        {
            this.alpha = alpha;
            this.time = time;
        }

        // alpha alpha of key
        public float alpha;

        // time of the key (0 - 1)
        public float time;
    }


    public enum GradientMode
    {
        Blend = 0,              // Keys will blend smoothly when the gradient is evaluated. (Default)
        Fixed = 1,              // An exact key color will be returned when the gradient is evaluated.
        PerceptualBlend = 2     // Keys will blend smoothly when the gradient is evaluated, using Oklab blending (https://bottosson.github.io/posts/oklab/)
    }

    // Gradient used for animating colors
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Export/Math/Gradient.bindings.h")]
    public class Gradient : IEquatable<Gradient>
    {
        [VisibleToOtherModules("UnityEngine.ParticleSystemModule")]
        internal IntPtr m_Ptr;

        private bool m_RequiresNativeCleanup;

        [FreeFunction(Name = "Gradient_Bindings::Init", IsThreadSafe = true)]
        extern static private IntPtr Init();

        [FreeFunction(Name = "Gradient_Bindings::Cleanup", IsThreadSafe = true, HasExplicitThis = true)]
        extern private void Cleanup();

        [FreeFunction("Gradient_Bindings::Internal_Equals", IsThreadSafe = true, HasExplicitThis = true)]
        extern private bool Internal_Equals(IntPtr other);

        [RequiredByNativeCode]
        public Gradient()
        {
            m_Ptr = Init();
            m_RequiresNativeCleanup = true;
        }

        [VisibleToOtherModules("UnityEngine.ParticleSystemModule")]
        internal Gradient(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_RequiresNativeCleanup = false;
        }

        ~Gradient()
        {
            if (m_RequiresNativeCleanup)
                Cleanup();
        }

        // Calculate color at a given time
        [FreeFunction(Name = "Gradient_Bindings::Evaluate", IsThreadSafe = true, HasExplicitThis = true)]
        extern public Color Evaluate(float time);

        extern public GradientColorKey[] colorKeys
        {
            [FreeFunction("Gradient_Bindings::GetColorKeysArray", IsThreadSafe = true, HasExplicitThis = true)] get;
            [FreeFunction("Gradient_Bindings::SetColorKeysWithSpan", IsThreadSafe = true, HasExplicitThis = true)] set;
        }

        extern public GradientAlphaKey[] alphaKeys
        {
            [FreeFunction("Gradient_Bindings::GetAlphaKeysArray", IsThreadSafe = true, HasExplicitThis = true)] get;
            [FreeFunction("Gradient_Bindings::SetAlphaKeysWithSpan", IsThreadSafe = true, HasExplicitThis = true)] set;
        }

        extern public int colorKeyCount
        {
            [FreeFunction("Gradient_Bindings::GetColorKeyCount", IsThreadSafe = true, HasExplicitThis = true)]
            get;
        }

        extern public int alphaKeyCount
        {
            [FreeFunction("Gradient_Bindings::GetAlphaKeyCount", IsThreadSafe = true, HasExplicitThis = true)]
            get;
        }

        public void GetColorKeys(Span<GradientColorKey> keys)
        {
            if (colorKeyCount > keys.Length)
                throw new ArgumentException("Destination array must be large enough to store the keys", "keys");
            GetColorKeysWithSpan(keys);
        }

        public void GetAlphaKeys(Span<GradientAlphaKey> keys)
        {
            if (alphaKeyCount > keys.Length)
                throw new ArgumentException("Destination array must be large enough to store the keys", "keys");
            GetAlphaKeysWithSpan(keys);
        }

        [FreeFunction(Name = "Gradient_Bindings::SetColorKeysWithSpan", HasExplicitThis = true, IsThreadSafe = true)]
        extern public void SetColorKeys(ReadOnlySpan<GradientColorKey> keys);

        [FreeFunction(Name = "Gradient_Bindings::SetAlphaKeysWithSpan", HasExplicitThis = true, IsThreadSafe = true)]
        extern public unsafe void SetAlphaKeys(ReadOnlySpan<GradientAlphaKey> keys);

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "Gradient_Bindings::GetColorKeysWithSpan", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void GetColorKeysWithSpan(Span<GradientColorKey> keys);

        [System.Security.SecurityCritical] // to prevent accidentally making this public in the future
        [FreeFunction(Name = "Gradient_Bindings::GetAlphaKeysWithSpan", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void GetAlphaKeysWithSpan(Span<GradientAlphaKey> keys);

        [NativeProperty(IsThreadSafe = true)] extern public GradientMode mode { get; set; }
        [NativeProperty(IsThreadSafe = true)] extern public ColorSpace colorSpace { get; set; }

        [NativeProperty(IsThreadSafe = true)] extern internal Color constantColor { get; set; }

        // Setup Gradient with an array of color keys and alpha keys
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            SetKeys(colorKeys.AsSpan(), alphaKeys.AsSpan());
        }

        // Setup Gradient with an array of color keys and alpha keys
        [FreeFunction(Name = "Gradient_Bindings::SetKeysWithSpans", HasExplicitThis = true, IsThreadSafe = true)]
        extern public void SetKeys(ReadOnlySpan<GradientColorKey> colorKeys, ReadOnlySpan<GradientAlphaKey> alphaKeys);

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o))
            {
                return false;
            }

            if (ReferenceEquals(this, o))
            {
                return true;
            }

            if (o.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Gradient)o);
        }

        public bool Equals(Gradient other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (m_Ptr.Equals(other.m_Ptr))
            {
                return true;
            }

            return Internal_Equals(other.m_Ptr);
        }

        public override int GetHashCode()
        {
            return m_Ptr.GetHashCode();
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Gradient graident) => graident.m_Ptr;
            public static Gradient ConvertToManaged(IntPtr ptr) => new Gradient(ptr);
        }

    }
} // end of UnityEngine
