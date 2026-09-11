using System;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace HemiTweaks
{
    internal static class HemiImageDecoder
    {
        private static MethodInfo loadImage;
        private static bool resolved;

        internal static bool TryDecode(Texture2D texture, byte[] data)
        {
            if (texture == null || !HemiImageSafety.TryGetDimensions(data, out int width, out int height))
                return false;

            return TryDecodeValidated(texture, data, width, height);
        }

        private static bool TryDecodeValidated(Texture2D texture, byte[] data, int width, int height)
        {
            MethodInfo method = Resolve();
            if (method == null)
                return false;

            object result = method.Invoke(null, new object[] { texture, data });
            return result is bool decoded && decoded && HemiImageSafety.IsWithinDimensions(texture.width, texture.height)
                && texture.width == width && texture.height == height;
        }

        internal static Texture2D LoadFile(string path, string what, long maximumPixels = HemiImageSafety.MaximumPixels)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            Texture2D texture = null;
            try
            {
                if (!HemiAssetSafety.IsLocalPath(path))
                {
                    MelonLogger.Warning(what + " must use a local file path.");
                    return null;
                }
                if (!File.Exists(path))
                {
                    MelonLogger.Warning(what + " was not found: " + path);
                    return null;
                }

                byte[] data = HemiStreamSafety.ReadFile(path, HemiImageSafety.MaximumFileBytes);
                if (!HemiImageSafety.TryGetDimensions(data, out int width, out int height)
                    || (long)width * height > maximumPixels)
                {
                    MelonLogger.Warning(what + " has an unsupported image header or exceeds its image budget: " + path);
                    return null;
                }

                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!TryDecodeValidated(texture, data, width, height))
                {
                    MelonLogger.Warning(what + " could not be decoded: " + path);
                    return null;
                }

                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                Texture2D result = texture;
                texture = null;
                return result;
            }
            catch (Exception exception)
            {
                MelonLogger.Warning(what + " failed to load (" + path + "): " + exception.Message);
                return null;
            }
            finally
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            }
        }

        private static MethodInfo Resolve()
        {
            if (resolved)
                return loadImage;

            resolved = true;
            Type conversion = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", false)
                ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine", false);
            loadImage = conversion?.GetMethod(
                "LoadImage",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(Texture2D), typeof(byte[]) },
                null);

            if (loadImage == null)
                MelonLogger.Warning("UnityEngine.ImageConversion.LoadImage was not found; images cannot be loaded.");

            return loadImage;
        }
    }
}
