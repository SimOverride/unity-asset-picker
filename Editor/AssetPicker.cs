using System;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 提供不依赖具体项目程序集的资源选择器打开入口。
    /// </summary>
    public static class AssetPicker
    {
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
    }
}
