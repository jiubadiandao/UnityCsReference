// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/AssetDatabase/AssetPreview.bindings.h")]
    [NativeHeader("Editor/Src/Utility/ObjectImages.h")]
    public sealed class AssetPreview
    {
        private const int kSharedClientID = 0;

        public static Texture2D GetAssetPreview(Object asset)
        {
            if (asset != null)
                return GetAssetPreview(asset.GetInstanceID());
            return null;
        }

        internal static Texture2D GetAssetPreview(EntityId entityId)
        {
            return GetAssetPreview(entityId, kSharedClientID);
        }

        [FreeFunction("AssetPreviewBindings::GetAssetPreview")]
        internal static extern Texture2D GetAssetPreview(EntityId entityId, int clientID);

        [FreeFunction("AssetPreviewBindings::HasAssetPreview")]
        internal static extern bool HasAssetPreview(EntityId entityId, int clientID);

        internal static Texture2D GetAssetPreviewFromGUID(string guid)
        {
            return GetAssetPreviewFromGUID(guid, kSharedClientID);
        }

        [FreeFunction("AssetPreviewBindings::GetAssetPreviewFromGUID")]
        internal static extern Texture2D GetAssetPreviewFromGUID(string guid, int clientID);

        public static bool IsLoadingAssetPreview(int instanceID)
        {
            return IsLoadingAssetPreview(instanceID, kSharedClientID);
        }

        public static bool IsLoadingAssetPreview(EntityId entityId)
        {
            return IsLoadingAssetPreview(entityId, kSharedClientID);
        }

        [FreeFunction("AssetPreviewBindings::IsLoadingAssetPreview")]
        internal static extern bool IsLoadingAssetPreview(EntityId entityId, int clientID);

        public static bool IsLoadingAssetPreviews()
        {
            return IsLoadingAssetPreviews(kSharedClientID);
        }

        [FreeFunction("AssetPreviewBindings::IsLoadingAssetPreviews")]
        internal static extern bool IsLoadingAssetPreviews(int clientID);

        internal static bool HasAnyNewPreviewTexturesAvailable()
        {
            return HasAnyNewPreviewTexturesAvailable(kSharedClientID);
        }

        [FreeFunction("AssetPreviewBindings::HasAnyNewPreviewTexturesAvailable")]
        internal static extern bool HasAnyNewPreviewTexturesAvailable(int clientID);

        public static void SetPreviewTextureCacheSize(int size)
        {
            SetPreviewTextureCacheSize(size, kSharedClientID);
        }

        [FreeFunction("AssetPreviewBindings::SetPreviewTextureCacheSize")]
        internal static extern void SetPreviewTextureCacheSize(int size, int clientID);

        [FreeFunction("AssetPreviewBindings::ClearTemporaryAssetPreviews")]
        internal static extern void ClearTemporaryAssetPreviews();

        [FreeFunction("AssetPreviewBindings::DeletePreviewTextureManagerByID")]
        internal static extern void DeletePreviewTextureManagerByID(int clientID);

        public static Texture2D GetMiniThumbnail(Object obj)
        {
            return (Texture2D)GetMiniThumbnailInternal(obj);
        }

        [FreeFunction("TextureForObject")]
        private static extern Texture GetMiniThumbnailInternal(Object obj);

        public static Texture2D GetMiniTypeThumbnail(Type type)
        {
            return GetMiniTypeThumbnailFromType(type);
        }

        [FreeFunction("AssetPreviewBindings::GetMiniTypeThumbnailFromObject")]
        internal static extern Texture2D GetMiniTypeThumbnail(Object obj);

        [FreeFunction("AssetPreviewBindings::GetMiniTypeThumbnailFromClassID")]
        internal static extern Texture2D GetMiniTypeThumbnailFromClassID(int classID);

        [FreeFunction("AssetPreviewBindings::GetMiniTypeThumbnailFromType")]
        internal static extern Texture2D GetMiniTypeThumbnailFromType(Type managedType);
    }
}
