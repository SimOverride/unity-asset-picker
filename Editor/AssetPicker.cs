using System;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 提供资源选择器打开入口和内置组件 Inspector 切换菜单。
    /// </summary>
    public static class AssetPicker
    {
        private const string UseProjectSpriteRendererEditorMenu =
            "Tools/项目资源选择器/SpriteRenderer/使用项目资源选择器";
        private const string UseUnitySpriteRendererEditorMenu =
            "Tools/项目资源选择器/SpriteRenderer/使用 Unity 原版 Editor";
        private const string UseProjectImageEditorMenu =
            "Tools/项目资源选择器/Image/使用项目资源选择器";
        private const string UseUnityImageEditorMenu =
            "Tools/项目资源选择器/Image/使用 Unity 原版 Editor";

        public static void Open(
            Type objectType,
            UnityEngine.Object current,
            Action<UnityEngine.Object> onSelected,
            string title = "资源选择器")
        {
            AssetPickerWindow.Open(objectType, current, title, onSelected);
        }

        [MenuItem("Tools/项目资源选择器/打开选择窗口")]
        private static void OpenSelectionWindowFromMenu()
        {
            OpenSelectionWindow();
        }

        /// <summary>
        /// 打开默认筛选 Sprite、并可在窗口内切换类型的选择器。
        /// </summary>
        public static void OpenSelectionWindow()
        {
            UnityEngine.Object current = Selection.activeObject is Sprite
                ? Selection.activeObject
                : null;
            AssetPickerWindow.Open(
                typeof(Sprite),
                current,
                "资源选择器",
                selected => Selection.activeObject = selected,
                true);
        }

        [MenuItem(UseProjectSpriteRendererEditorMenu)]
        private static void UseProjectSpriteRendererEditor()
        {
            AssetPickerSettings.UseCustomSpriteRendererEditor = true;
            RefreshInspectorViews();
        }

        [MenuItem(UseProjectSpriteRendererEditorMenu, true)]
        private static bool ValidateUseProjectSpriteRendererEditor()
        {
            Menu.SetChecked(
                UseProjectSpriteRendererEditorMenu,
                AssetPickerSettings.UseCustomSpriteRendererEditor);
            return true;
        }

        [MenuItem(UseUnitySpriteRendererEditorMenu)]
        private static void UseUnitySpriteRendererEditor()
        {
            AssetPickerSettings.UseCustomSpriteRendererEditor = false;
            RefreshInspectorViews();
        }

        [MenuItem(UseUnitySpriteRendererEditorMenu, true)]
        private static bool ValidateUseUnitySpriteRendererEditor()
        {
            Menu.SetChecked(
                UseUnitySpriteRendererEditorMenu,
                !AssetPickerSettings.UseCustomSpriteRendererEditor);
            return true;
        }

        [MenuItem(UseProjectImageEditorMenu)]
        private static void UseProjectImageEditor()
        {
            AssetPickerSettings.UseCustomImageEditor = true;
            RefreshInspectorViews();
        }

        [MenuItem(UseProjectImageEditorMenu, true)]
        private static bool ValidateUseProjectImageEditor()
        {
            Menu.SetChecked(
                UseProjectImageEditorMenu,
                AssetPickerSettings.UseCustomImageEditor);
            return true;
        }

        [MenuItem(UseUnityImageEditorMenu)]
        private static void UseUnityImageEditor()
        {
            AssetPickerSettings.UseCustomImageEditor = false;
            RefreshInspectorViews();
        }

        [MenuItem(UseUnityImageEditorMenu, true)]
        private static bool ValidateUseUnityImageEditor()
        {
            Menu.SetChecked(
                UseUnityImageEditorMenu,
                !AssetPickerSettings.UseCustomImageEditor);
            return true;
        }

        /// <summary>
        /// 菜单切换后立即刷新已打开的 Inspector。
        /// </summary>
        private static void RefreshInspectorViews()
        {
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
