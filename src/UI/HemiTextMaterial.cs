using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HemiTweaks
{
    internal static class HemiTextMaterial
    {
        private static readonly Dictionary<int, Material> plain = new Dictionary<int, Material>();

        private static bool propertyIdsReady;

        internal static void ApplyPlain(TMP_Text label)
        {
            if (label == null)
                return;

            TMP_FontAsset font = label.font;
            if (font == null)
                return;

            int key = font.GetInstanceID();
            if (!plain.TryGetValue(key, out Material material) || material == null)
            {
                material = Create(font);
                if (material == null)
                    return;
                Write(material, new Color(0f, 0f, 0f, 0f), 0f, 0f);
                plain[key] = material;
            }

            if (label.fontSharedMaterial != material)
                label.fontSharedMaterial = material;
        }

        internal static void ClearPlain()
        {
            foreach (Material material in plain.Values)
            {
                if (material != null)
                    UnityEngine.Object.Destroy(material);
            }
            plain.Clear();
        }

        internal static Material Create(TMP_FontAsset font)
        {
            Material source = font == null ? null : font.material;
            if (source == null)
                return null;

            EnsurePropertyIds();
            return new Material(source)
            {
                name = source.name + " (HemiTweaks)",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        internal static void Write(Material material, Color shadow, float offsetX, float offsetY)
        {
            if (material == null)
                return;

            EnsurePropertyIds();

            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0f));

            float limit = OffsetLimit(material);
            offsetX = HemiNumberSafety.Clamp(offsetX, -limit, limit, 0.5f);
            offsetY = HemiNumberSafety.Clamp(offsetY, -limit, limit, -0.5f);

            bool hasShadow = shadow.a > 0.001f
                && (Mathf.Abs(offsetX) > 0.001f || Mathf.Abs(offsetY) > 0.001f);
            if (hasShadow)
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, shadow);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, offsetX);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, offsetY);
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            }
            else
            {
                material.DisableKeyword(ShaderUtilities.Keyword_Underlay);
                material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0f));
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            }
        }

        private static float OffsetLimit(Material material)
        {
            if (material == null || !material.HasProperty(ShaderUtilities.ID_GradientScale))
                return MaximumShadowOffset;

            float gradientScale = material.GetFloat(ShaderUtilities.ID_GradientScale);
            if (gradientScale < 0.01f)
                return MaximumShadowOffset;

            float limit = (gradientScale - 2.25f) / gradientScale;
            return Mathf.Clamp(limit, 0f, MaximumShadowOffset);
        }

        internal const float MaximumShadowOffset = 0.75f;

        private static void EnsurePropertyIds()
        {
            if (propertyIdsReady)
                return;
            propertyIdsReady = true;
            try
            {
                ShaderUtilities.GetShaderPropertyIDs();
            }
            catch
            {
            }
        }
    }

    internal sealed class HemiTextMaterialSlot
    {
        private Material material;
        private TMP_FontAsset font;
        private Color shadow;
        private float offsetX;
        private float offsetY;
        private bool written;

        internal Material Resolve(TMP_FontAsset fontAsset, Color shadowColor, float x, float y, out bool rewritten)
        {
            rewritten = false;
            if (fontAsset == null)
                return null;

            if (material == null || font != fontAsset)
            {
                Destroy();
                material = HemiTextMaterial.Create(fontAsset);
                font = fontAsset;
                written = false;
            }

            if (material == null)
                return null;

            x = HemiNumberSafety.Clamp(x, -HemiTextMaterial.MaximumShadowOffset, HemiTextMaterial.MaximumShadowOffset, 0.5f);
            y = HemiNumberSafety.Clamp(y, -HemiTextMaterial.MaximumShadowOffset, HemiTextMaterial.MaximumShadowOffset, -0.5f);

            if (!written || shadow != shadowColor || !Mathf.Approximately(offsetX, x) || !Mathf.Approximately(offsetY, y))
            {
                HemiTextMaterial.Write(material, shadowColor, x, y);
                shadow = shadowColor;
                offsetX = x;
                offsetY = y;
                written = true;
                rewritten = true;
            }

            return material;
        }

        internal void Destroy()
        {
            if (material != null)
                UnityEngine.Object.Destroy(material);
            material = null;
            font = null;
            written = false;
        }
    }
}
