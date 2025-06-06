// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderStyleUtilities
    {
        // Private Utilities

        static void GetInlineStyleSheetAndRule(VisualTreeAsset vta, VisualElement element, out StyleSheet styleSheet, out StyleRule styleRule)
        {
            var vea = element.GetVisualElementAsset();
            styleSheet = vta.GetOrCreateInlineStyleSheet();
            styleRule = vta.GetOrCreateInlineStyleRule(vea);
        }

        static void GetInlineStyleSheetAndRule(VisualTreeAsset vta, VisualElementAsset vea,
            out StyleSheet styleSheet, out StyleRule styleRule)
        {
            styleSheet = vta.GetOrCreateInlineStyleSheet();
            styleRule = vta.GetOrCreateInlineStyleRule(vea);
        }

        static StyleProperty GetOrCreateStylePropertyByStyleName(StyleSheet styleSheet, StyleRule styleRule, string styleName)
        {
            var styleProperty = styleSheet.FindLastProperty(styleRule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(styleRule, styleName);

            return styleProperty;
        }

        // Inline StyleSheet Value Setters

        public static void SetInlineStyleValue(VisualTreeAsset vta, VisualElement element, string styleName, float value)
        {
            GetInlineStyleSheetAndRule(vta, element, out StyleSheet styleSheet, out StyleRule styleRule);
            SetStyleSheetRuleValueAsDimension(styleSheet, styleRule, styleName, value);
            element?.UpdateInlineRule(styleSheet, styleRule);
        }

        public static void SetInlineStyleValue(VisualTreeAsset vta, VisualElementAsset vea, VisualElement element, string styleName, float value)
        {
            GetInlineStyleSheetAndRule(vta, vea, out StyleSheet styleSheet, out StyleRule styleRule);
            SetStyleSheetRuleValue(styleSheet, styleRule, styleName, value);
            element?.UpdateInlineRule(styleSheet, styleRule);
        }

        public static void SetInlineStyleValue(VisualTreeAsset vta, VisualElement element, string styleName, Enum value)
        {
            GetInlineStyleSheetAndRule(vta, element, out StyleSheet styleSheet, out StyleRule styleRule);
            SetStyleSheetRuleValue(styleSheet, styleRule, styleName, value);
            element?.UpdateInlineRule(styleSheet, styleRule);
        }

        public static void SetInlineStyleValue(VisualTreeAsset vta, VisualElementAsset vea, VisualElement element, string styleName, Color value)
        {
            GetInlineStyleSheetAndRule(vta, vea, out StyleSheet styleSheet, out StyleRule styleRule);
            SetStyleSheetRuleValue(styleSheet, styleRule, styleName, value);
            element?.UpdateInlineRule(styleSheet, styleRule);
        }

        // StyleSheet Value Setters

        static void SetStyleSheetRuleValue(StyleSheet styleSheet, StyleRule styleRule, string styleName, float value)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetFloat(styleSheet, value);
        }

        static void SetStyleSheetRuleValueAsDimension(StyleSheet styleSheet, StyleRule styleRule, string styleName, float value)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetDimension(styleSheet, new Dimension { unit = Dimension.Unit.Pixel, value = value });
        }

        static void SetStyleSheetRuleValue(StyleSheet styleSheet, StyleRule styleRule, string styleName, Enum value)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetEnum(styleSheet, value);
        }

        static void SetStyleSheetRuleValue(StyleSheet styleSheet, StyleRule styleRule, string styleName, Color value)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetColor(styleSheet, value);
        }

        public static string GenerateElementTargetedSelector(VisualElement documentElement)
        {
            string elementTargetedSelector;
            var classList = documentElement?.classList;

            // if element has name, use that to target it
            if (!string.IsNullOrEmpty(documentElement?.name))
            {
                elementTargetedSelector = $"#{documentElement.name}";
            }
            // if element has no name, use its class to target it
            else if (classList != null && classList.Count > 0)
            {
                elementTargetedSelector = $".{classList[^1]}";
            }
            // if element has no class, use its type to target it
            else
            {
                elementTargetedSelector = documentElement?.typeName;
            }

            // add its parents name or class or type to the selector
            if (documentElement?.parent != null && !BuilderSharedStyles.IsDocumentElement(documentElement.parent))
            {
                elementTargetedSelector = GenerateElementTargetedSelector(documentElement.parent) + " > " + elementTargetedSelector;
            }

            return elementTargetedSelector;
        }
    }
}
