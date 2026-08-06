using System;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 绘制保留 Unity 原版选择器的资源字段，并提供按文件夹筛选入口。
    /// </summary>
    public static class AssetPickerField
    {
        /// <summary>
        /// 绘制不包含 Unity 原生对象选择按钮的资源字段。
        /// 点击字段本身会打开当前项目的文件夹资源选择窗口。
        /// </summary>
        public static UnityEngine.Object DrawPickerField(
            string label,
            UnityEngine.Object value,
            Type objectType,
            Action<UnityEngine.Object> onSelected,
            bool showMixedValue = false,
            params GUILayoutOption[] options)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            Rect position = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight,
                options);
            Rect fieldRect = EditorGUI.PrefixLabel(position, new GUIContent(label));
            GUIContent content = GetPickerContent(value, objectType, showMixedValue);

            if (GUI.Button(fieldRect, content, EditorStyles.objectField))
            {
                AssetPicker.Open(
                    objectType,
                    value,
                    onSelected,
                    label + " - 按文件夹选择");
            }

            HandleDragAndDrop(fieldRect, objectType, onSelected);
            return value;
        }

        /// <summary>
        /// 绘制原版资源字段及右侧文件夹选择按钮。
        /// 返回值是原版 ObjectField 当前值；文件夹窗口的结果通过回调返回。
        /// </summary>
        public static UnityEngine.Object Draw(
            string label,
            UnityEngine.Object value,
            Type objectType,
            Action<UnityEngine.Object> onFolderAssetSelected,
            params GUILayoutOption[] options)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            Rect position = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight,
                options);
            const float folderButtonWidth = 26f;
            const float spacing = 2f;
            Rect objectFieldRect = new Rect(
                position.x,
                position.y,
                position.width - folderButtonWidth - spacing,
                position.height);
            Rect folderButtonRect = new Rect(
                objectFieldRect.xMax + spacing,
                position.y,
                folderButtonWidth,
                position.height);

            UnityEngine.Object selected = EditorGUI.ObjectField(
                objectFieldRect,
                label,
                value,
                objectType,
                false);

            GUIContent folderContent = EditorGUIUtility.IconContent("Folder Icon");
            if (folderContent == null)
            {
                folderContent = new GUIContent("...");
            }

            folderContent.tooltip = "按文件夹选择资源";
            if (GUI.Button(folderButtonRect, folderContent, EditorStyles.miniButton))
            {
                AssetPicker.Open(
                    objectType,
                    value,
                    onFolderAssetSelected,
                    label + " - 按文件夹选择");
            }

            return selected;
        }

        private static GUIContent GetPickerContent(
            UnityEngine.Object value,
            Type objectType,
            bool showMixedValue)
        {
            if (showMixedValue)
            {
                GUIContent mixedContent = EditorGUIUtility.ObjectContent(null, objectType);
                return new GUIContent("—", mixedContent.image, "多个对象的 Sprite 值不同");
            }

            GUIContent content = EditorGUIUtility.ObjectContent(value, objectType);
            if (content == null)
            {
                return new GUIContent("None (" + objectType.Name + ")");
            }

            content.tooltip = "点击打开按文件夹选择窗口";
            return content;
        }

        private static void HandleDragAndDrop(
            Rect fieldRect,
            Type objectType,
            Action<UnityEngine.Object> onSelected)
        {
            Event currentEvent = Event.current;
            if (!fieldRect.Contains(currentEvent.mousePosition) ||
                (currentEvent.type != EventType.DragUpdated &&
                 currentEvent.type != EventType.DragPerform))
            {
                return;
            }

            UnityEngine.Object draggedObject = DragAndDrop.objectReferences != null &&
                                               DragAndDrop.objectReferences.Length > 0
                ? DragAndDrop.objectReferences[0]
                : null;
            if (draggedObject == null || !objectType.IsAssignableFrom(draggedObject.GetType()))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                onSelected?.Invoke(draggedObject);
            }

            currentEvent.Use();
        }
    }
}


