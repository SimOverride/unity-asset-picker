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
        private const string SpriteRendererEditorMenu =
            "Tools/项目资源选择器/SpriteRenderer 使用项目资源选择器";
        private const string ImageEditorMenu =
            "Tools/项目资源选择器/Image 使用项目资源选择器";

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

        [MenuItem(SpriteRendererEditorMenu)]
        private static void ToggleSpriteRendererEditor()
        {
            AssetPickerSettings.UseCustomSpriteRendererEditor =
                !AssetPickerSettings.UseCustomSpriteRendererEditor;
            RefreshInspectorViews();
        }

        [MenuItem(SpriteRendererEditorMenu, true)]
        private static bool ValidateSpriteRendererEditorMenu()
        {
            Menu.SetChecked(
                SpriteRendererEditorMenu,
                AssetPickerSettings.UseCustomSpriteRendererEditor);
            return true;
        }

        [MenuItem(ImageEditorMenu)]
        private static void ToggleImageEditor()
        {
            AssetPickerSettings.UseCustomImageEditor =
                !AssetPickerSettings.UseCustomImageEditor;
            RefreshInspectorViews();
        }

        [MenuItem(ImageEditorMenu, true)]
        private static bool ValidateImageEditorMenu()
        {
            Menu.SetChecked(
                ImageEditorMenu,
                AssetPickerSettings.UseCustomImageEditor);
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
