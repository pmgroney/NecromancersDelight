using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Kingmaker.UI.MVVM._PCView.Tooltip.Bricks;
using UnityEngine;
using TooltipBrickFeatureHeaderView = Kingmaker.UI.MVVM._VM.Tooltip.Templates.TooltipBrickFeatureHeaderView;

namespace wotr_mod.Patches
{
    internal static class TooltipIconMagnificationPatch
    {
        private const float DefaultIconScale = 1.5f;
        private const float FallbackIconSize = 64f;
        private const float HeaderPadding = 16f;
        private const float FramePadding = 6f;

        private static readonly Dictionary<int, Vector2> OriginalSizes = new Dictionary<int, Vector2>();
        private static readonly HashSet<string> LoggedTargets = new HashSet<string>();

        private static readonly FieldInfo EntityImageContainerField =
            AccessTools.Field(typeof(TooltipBrickEntityHeaderView), "m_ImageContainer");

        private static readonly FieldInfo EntityImageField =
            AccessTools.Field(typeof(TooltipBrickEntityHeaderView), "m_Image");

        private static readonly FieldInfo FeatureIconField =
            AccessTools.Field(typeof(TooltipBrickFeatureHeaderView), "m_Icon");

        private static readonly FieldInfo FeatureLabelField =
            AccessTools.Field(typeof(TooltipBrickFeatureHeaderView), "m_Label");

        private static readonly FieldInfo FeatureFrameIconField =
            AccessTools.Field(typeof(TooltipBrickFeatureHeaderView), "m_FrameIcon");

        private static readonly FieldInfo FeatureRoundBorderField =
            AccessTools.Field(typeof(TooltipBrickFeatureHeaderView), "m_RoundBorder");

        [HarmonyPatch(typeof(TooltipBrickEntityHeaderView), "BindViewImplementation")]
        private static class EntityHeaderPatch
        {
            [HarmonyPostfix]
            private static void Postfix(TooltipBrickEntityHeaderView __instance)
            {
                if (!IsEnabled())
                {
                    return;
                }

                var container = EntityImageContainerField?.GetValue(__instance) as GameObject;
                var image = EntityImageField?.GetValue(__instance) as Component;

                var scaled = 0;
                var iconSize = Vector2.zero;
                AddScaledSize(ref iconSize, Magnify(container, "entity image container"), ref scaled);
                AddScaledSize(ref iconSize, Magnify(image != null ? image.gameObject : null, "entity image"), ref scaled);
                ReserveIconColumn(container, iconSize, "entity image column");
                FitHeaderContainer(GetGameObject(__instance), iconSize, "entity header root");
                LogOnce("TooltipBrickEntityHeaderView", scaled);
            }
        }

        [HarmonyPatch(typeof(TooltipBrickFeatureHeaderView), "BindViewImplementation")]
        private static class FeatureHeaderPatch
        {
            [HarmonyPostfix]
            private static void Postfix(TooltipBrickFeatureHeaderView __instance)
            {
                if (!IsEnabled())
                {
                    return;
                }

                var icon = FeatureIconField?.GetValue(__instance) as Component;
                var label = FeatureLabelField?.GetValue(__instance) as Component;
                var frame = FeatureFrameIconField?.GetValue(__instance) as Component;
                var roundBorder = FeatureRoundBorderField?.GetValue(__instance) as GameObject;

                var root = GetGameObject(__instance);
                var iconColumn = FindIconColumn(
                    root,
                    icon != null ? icon.gameObject : roundBorder,
                    label != null ? label.gameObject : null);
                var scaled = 0;
                var iconSize = Vector2.zero;
                AddScaledSize(
                    ref iconSize,
                    Magnify(
                        icon != null ? icon.gameObject : null,
                        "feature header icon",
                        FallbackIconSize),
                    ref scaled);
                var frameSize = iconSize == Vector2.zero
                    ? Vector2.zero
                    : new Vector2(iconSize.x + FramePadding, iconSize.y + FramePadding);
                SizeFrame(frame != null ? frame.gameObject : null, frameSize, "feature header frame");
                SizeFrame(roundBorder, frameSize, "feature header round border");
                var reservedSize = MaxSize(iconSize, frameSize);
                ReserveIconColumn(iconColumn, reservedSize, "feature icon column layout");
                FitHeaderContainer(root, reservedSize, "feature header root");
                SizeFrame(icon != null ? icon.gameObject : null, iconSize, "feature header icon final");
                SizeFrame(frame != null ? frame.gameObject : null, frameSize, "feature header frame final");
                SizeFrame(roundBorder, frameSize, "feature header round border final");
                LogOnce("TooltipBrickFeatureHeaderView", scaled);
            }
        }

        private static Vector2 Magnify(GameObject target, string label, float maxOriginalDimension = 0f)
        {
            if (target == null)
            {
                return Vector2.zero;
            }

            var rect = target.GetComponent<RectTransform>();
            if (rect == null)
            {
                return Vector2.zero;
            }

            var originalSize = GetOriginalSize(rect, maxOriginalDimension);
            var targetSize = originalSize * CurrentIconScale();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);

            var layoutElement = target.GetComponent("LayoutElement");
            SetLayoutDimension(layoutElement, "minWidth", targetSize.x);
            SetLayoutDimension(layoutElement, "minHeight", targetSize.y);
            SetLayoutDimension(layoutElement, "preferredWidth", targetSize.x);
            SetLayoutDimension(layoutElement, "preferredHeight", targetSize.y);

            if (LoggedTargets.Add(label))
            {
                Main.Log(
                    $"Tooltip icon magnification touched {label}; original={originalSize.x:0.#}x{originalSize.y:0.#}, target={targetSize.x:0.#}x{targetSize.y:0.#}.");
            }

            return targetSize;
        }

        private static bool IsEnabled()
        {
            return CurrentIconScale() > 1f;
        }

        private static float CurrentIconScale()
        {
            switch (Main.Settings?.TooltipIconMagnificationMode ?? 1)
            {
                case 0:
                    return 0f;
                case 2:
                    return 2f;
                default:
                    return DefaultIconScale;
            }
        }

        private static void SizeFrame(GameObject target, Vector2 size, string label)
        {
            if (target == null || size == Vector2.zero)
            {
                return;
            }

            var rect = target.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

            var layoutElement = target.GetComponent("LayoutElement");
            SetLayoutDimension(layoutElement, "minWidth", size.x);
            SetLayoutDimension(layoutElement, "minHeight", size.y);
            SetLayoutDimension(layoutElement, "preferredWidth", size.x);
            SetLayoutDimension(layoutElement, "preferredHeight", size.y);

            if (LoggedTargets.Add(label + " sized"))
            {
                Main.Log($"Tooltip icon magnification sized {label}; size={size.x:0.#}x{size.y:0.#}.");
            }
        }

        private static void ReserveIconColumn(GameObject target, Vector2 iconSize, string label)
        {
            if (target == null || iconSize == Vector2.zero)
            {
                return;
            }

            var reservedSize = new Vector2(iconSize.x + HeaderPadding, iconSize.y + HeaderPadding);
            FitRectAtLeast(target, reservedSize);

            var layoutElement = GetOrAddLayoutElement(target);
            SetLayoutDimensionAtLeast(layoutElement, "minWidth", reservedSize.x);
            SetLayoutDimensionAtLeast(layoutElement, "preferredWidth", reservedSize.x);
            SetLayoutDimensionAtLeast(layoutElement, "minHeight", reservedSize.y);
            SetLayoutDimensionAtLeast(layoutElement, "preferredHeight", reservedSize.y);

            if (LoggedTargets.Add(label))
            {
                Main.Log($"Tooltip icon magnification adjusted {label}; reserved={reservedSize.x:0.#}x{reservedSize.y:0.#}.");
            }
        }

        private static void AddScaledSize(ref Vector2 currentSize, Vector2 scaledSize, ref int scaled)
        {
            if (scaledSize == Vector2.zero)
            {
                return;
            }

            currentSize = new Vector2(
                Math.Max(currentSize.x, scaledSize.x),
                Math.Max(currentSize.y, scaledSize.y));
            scaled++;
        }

        private static void FitHeaderContainer(GameObject target, Vector2 iconSize, string label)
        {
            if (target == null || iconSize == Vector2.zero)
            {
                return;
            }

            var targetHeight = iconSize.y + HeaderPadding;
            FitRectAtLeast(target, new Vector2(0f, targetHeight));

            var layoutElement = GetOrAddLayoutElement(target);
            SetLayoutDimensionAtLeast(layoutElement, "minHeight", targetHeight);
            SetLayoutDimensionAtLeast(layoutElement, "preferredHeight", targetHeight);

            if (LoggedTargets.Add(label))
            {
                Main.Log($"Tooltip icon magnification adjusted {label}; reservedHeight={targetHeight:0.#}.");
            }
        }

        private static GameObject GetGameObject(object instance)
        {
            var component = instance as Component;
            return component != null ? component.gameObject : null;
        }

        private static GameObject FindIconColumn(GameObject root, GameObject icon, GameObject label)
        {
            if (icon == null)
            {
                return null;
            }

            if (root == null || label == null)
            {
                return icon.transform.parent != null ? icon.transform.parent.gameObject : icon;
            }

            var labelTransform = label.transform;
            var current = icon.transform;
            var child = current;
            while (current != null)
            {
                if (IsDescendantOf(labelTransform, current))
                {
                    return child != null ? child.gameObject : icon;
                }

                if (current.gameObject == root)
                {
                    break;
                }

                child = current;
                current = current.parent;
            }

            return icon.transform.parent != null ? icon.transform.parent.gameObject : icon;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            var current = child;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Vector2 MaxSize(Vector2 a, Vector2 b)
        {
            return new Vector2(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        }

        private static void FitRectAtLeast(GameObject target, Vector2 size)
        {
            var rect = target?.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            var currentSize = rect.sizeDelta;
            if (currentSize.x <= 0f || currentSize.y <= 0f)
            {
                currentSize = rect.rect.size;
            }

            if (size.x > 0f && currentSize.x < size.x)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            }

            if (size.y > 0f && currentSize.y < size.y)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
        }

        private static Component GetOrAddLayoutElement(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            var layoutElement = target.GetComponent("LayoutElement");
            if (layoutElement != null)
            {
                return layoutElement;
            }

            var layoutElementType = AccessTools.TypeByName("UnityEngine.UI.LayoutElement");
            return layoutElementType != null ? target.AddComponent(layoutElementType) : null;
        }

        private static Vector2 GetOriginalSize(RectTransform rect, float maxDimension = 0f)
        {
            var key = rect.GetInstanceID();
            if (OriginalSizes.TryGetValue(key, out var originalSize))
            {
                return originalSize;
            }

            originalSize = rect.sizeDelta;
            if (originalSize.x <= 0f || originalSize.y <= 0f)
            {
                originalSize = rect.rect.size;
            }

            if (originalSize.x <= 0f || originalSize.y <= 0f)
            {
                originalSize = new Vector2(FallbackIconSize, FallbackIconSize);
            }

            if (maxDimension > 0f)
            {
                originalSize = new Vector2(
                    Math.Min(originalSize.x, maxDimension),
                    Math.Min(originalSize.y, maxDimension));
            }

            OriginalSizes[key] = originalSize;
            return originalSize;
        }

        private static void SetLayoutDimension(Component layoutElement, string propertyName, float value)
        {
            var property = layoutElement?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            try
            {
                property.SetValue(layoutElement, value, null);
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to set tooltip icon layout {propertyName}: {ex.Message}");
            }
        }

        private static void SetLayoutDimensionAtLeast(Component layoutElement, string propertyName, float value)
        {
            var property = layoutElement?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            try
            {
                var current = property.GetValue(layoutElement, null);
                if (current is float currentValue && currentValue >= value)
                {
                    return;
                }

                property.SetValue(layoutElement, value, null);
            }
            catch (Exception ex)
            {
                Main.Log($"Failed to adjust tooltip header layout {propertyName}: {ex.Message}");
            }
        }

        private static void LogOnce(string target, int scaled)
        {
            if (scaled <= 0 || !LoggedTargets.Add(target))
            {
                return;
            }

            Main.Log($"Tooltip icon magnification applied to {target}; scaled {scaled} object(s).");
        }
    }
}
