using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 只替换 Image 的 Source Image 字段，并按 Unity 原生面板顺序绘制其他属性。
    /// </summary>
    [CustomEditor(typeof(Image))]
    [CanEditMultipleObjects]
    internal sealed class ImageEditor : UnityEditor.Editor
    {
        private SerializedProperty spriteProperty;
        private SerializedProperty colorProperty;
        private SerializedProperty materialProperty;
        private SerializedProperty raycastTargetProperty;
        private SerializedProperty raycastPaddingProperty;
        private SerializedProperty maskableProperty;
        private SerializedProperty imageTypeProperty;
        private SerializedProperty fillCenterProperty;
        private SerializedProperty fillMethodProperty;
        private SerializedProperty fillOriginProperty;
        private SerializedProperty fillClockwiseProperty;
        private SerializedProperty fillAmountProperty;
        private SerializedProperty preserveAspectProperty;
        private SerializedProperty useSpriteMeshProperty;
        private SerializedProperty pixelsPerUnitMultiplierProperty;
        private bool showRaycastPadding = true;
        private UnityEditor.Editor unityImageEditor;

        private static readonly Type UnityImageEditorType = FindUnityImageEditorType();

        private void OnEnable()
        {
            spriteProperty = serializedObject.FindProperty("m_Sprite");
            colorProperty = serializedObject.FindProperty("m_Color");
            materialProperty = serializedObject.FindProperty("m_Material");
            raycastTargetProperty = serializedObject.FindProperty("m_RaycastTarget");
            raycastPaddingProperty = serializedObject.FindProperty("m_RaycastPadding");
            maskableProperty = serializedObject.FindProperty("m_Maskable");
            imageTypeProperty = serializedObject.FindProperty("m_Type");
            fillCenterProperty = serializedObject.FindProperty("m_FillCenter");
            fillMethodProperty = serializedObject.FindProperty("m_FillMethod");
            fillOriginProperty = serializedObject.FindProperty("m_FillOrigin");
            fillClockwiseProperty = serializedObject.FindProperty("m_FillClockwise");
            fillAmountProperty = serializedObject.FindProperty("m_FillAmount");
            preserveAspectProperty = serializedObject.FindProperty("m_PreserveAspect");
            useSpriteMeshProperty = serializedObject.FindProperty("m_UseSpriteMesh");
            pixelsPerUnitMultiplierProperty = serializedObject.FindProperty("m_PixelsPerUnitMultiplier");
        }

        private void OnDisable()
        {
            DestroyUnityImageEditor();
        }

        public override void OnInspectorGUI()
        {
            if (!AssetPickerSettings.UseCustomImageEditor)
            {
                DrawUnityImageInspector();
                return;
            }

            DestroyUnityImageEditor();
            serializedObject.Update();

            if (spriteProperty == null)
            {
                EditorGUILayout.HelpBox(
                    "当前 Unity 版本未找到 Image 的 Source Image 属性，已回退到默认属性绘制。",
                    MessageType.Warning);
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Source Image 是唯一替换的资源字段，其他字段保持 Image 原生顺序和布局。
            AssetPickerField.DrawPickerField(
                "Source Image",
                spriteProperty.objectReferenceValue,
                typeof(Sprite),
                ApplySprite,
                spriteProperty.hasMultipleDifferentValues);
            DrawProperty(colorProperty);
            DrawProperty(materialProperty);
            DrawProperty(raycastTargetProperty);
            DrawRaycastPadding();
            DrawProperty(maskableProperty);
            DrawImageTypeProperties();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 通过 Unity UGUI 包中的内部 Editor 绘制原版 Image Inspector。
        /// </summary>
        private void DrawUnityImageInspector()
        {
            if (UnityImageEditorType != null && unityImageEditor == null)
            {
                unityImageEditor = CreateUnityImageEditor();
            }

            if (unityImageEditor != null)
            {
                unityImageEditor.OnInspectorGUI();
                return;
            }

            EditorGUILayout.HelpBox(
                "无法创建 Unity 原版 Image Editor，已使用默认属性绘制。",
                MessageType.Warning);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        private UnityEditor.Editor CreateUnityImageEditor()
        {
            MethodInfo createEditorMethod = FindCreateEditorMethod();
            if (createEditorMethod == null)
            {
                return null;
            }

            try
            {
                return createEditorMethod.Invoke(
                    null,
                    new object[] { targets, UnityImageEditorType }) as UnityEditor.Editor;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        private static Type FindUnityImageEditorType()
        {
            Type editorType = Type.GetType("UnityEditor.UI.ImageEditor, UnityEditor.UI");
            if (editorType != null)
            {
                return editorType;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                editorType = assembly.GetType("UnityEditor.UI.ImageEditor");
                if (editorType != null)
                {
                    return editorType;
                }
            }

            return null;
        }

        private static MethodInfo FindCreateEditorMethod()
        {
            foreach (MethodInfo method in typeof(UnityEditor.Editor).GetMethods(
                         BindingFlags.Public | BindingFlags.Static))
            {
                if (!string.Equals(method.Name, "CreateEditor", StringComparison.Ordinal))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(UnityEngine.Object[]) &&
                    parameters[1].ParameterType == typeof(Type))
                {
                    return method;
                }
            }

            return null;
        }

        private void DestroyUnityImageEditor()
        {
            if (unityImageEditor == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(unityImageEditor);
            unityImageEditor = null;
        }

        private void DrawProperty(SerializedProperty property)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void DrawRaycastPadding()
        {
            if (raycastPaddingProperty == null)
            {
                return;
            }

            showRaycastPadding = EditorGUILayout.Foldout(
                showRaycastPadding,
                "Raycast Padding",
                true);
            if (!showRaycastPadding)
            {
                return;
            }

            Vector4 padding = raycastPaddingProperty.vector4Value;
            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = raycastPaddingProperty.hasMultipleDifferentValues;

            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            padding.x = EditorGUILayout.FloatField("Left", padding.x);
            padding.y = EditorGUILayout.FloatField("Bottom", padding.y);
            padding.z = EditorGUILayout.FloatField("Right", padding.z);
            padding.w = EditorGUILayout.FloatField("Top", padding.w);
            if (EditorGUI.EndChangeCheck())
            {
                raycastPaddingProperty.vector4Value = padding;
            }

            EditorGUI.indentLevel--;
            EditorGUI.showMixedValue = previousMixedValue;
        }

        private void DrawImageTypeProperties()
        {
            if (imageTypeProperty == null || spriteProperty.objectReferenceValue == null)
            {
                return;
            }

            DrawProperty(imageTypeProperty);
            if (imageTypeProperty.hasMultipleDifferentValues ||
                imageTypeProperty.enumValueIndex < 0 ||
                imageTypeProperty.enumValueIndex >= imageTypeProperty.enumNames.Length)
            {
                return;
            }

            Image.Type imageType = (Image.Type)imageTypeProperty.enumValueIndex;
            EditorGUI.indentLevel++;
            if (imageType == Image.Type.Sliced || imageType == Image.Type.Tiled)
            {
                if (imageType == Image.Type.Sliced)
                {
                    DrawProperty(fillCenterProperty);
                }

                DrawProperty(pixelsPerUnitMultiplierProperty);
            }
            else if (imageType == Image.Type.Filled)
            {
                DrawProperty(fillMethodProperty);
                DrawProperty(fillOriginProperty);
                DrawProperty(fillAmountProperty);
                DrawProperty(fillClockwiseProperty);
            }

            if (imageType == Image.Type.Simple)
            {
                DrawProperty(useSpriteMeshProperty);
            }

            if (imageType == Image.Type.Simple || imageType == Image.Type.Filled)
            {
                DrawProperty(preserveAspectProperty);
                if (GUILayout.Button("Set Native Size"))
                {
                    SetNativeSize();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void SetNativeSize()
        {
            Undo.RecordObjects(targets, "设置 Image 原生尺寸");
            foreach (UnityEngine.Object targetObject in targets)
            {
                Image image = targetObject as Image;
                if (image == null)
                {
                    continue;
                }

                image.SetNativeSize();
                EditorUtility.SetDirty(image);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }
        }

        /// <summary>
        /// 将窗口或拖拽返回的 Sprite 写回所有当前编辑目标，并保留撤销能力。
        /// </summary>
        private void ApplySprite(UnityEngine.Object selected)
        {
            Sprite sprite = selected as Sprite;
            if (sprite == null)
            {
                return;
            }

            Undo.RecordObjects(targets, "选择 Image Sprite");
            serializedObject.Update();
            spriteProperty.objectReferenceValue = sprite;
            serializedObject.ApplyModifiedProperties();

            foreach (UnityEngine.Object targetObject in targets)
            {
                Image image = targetObject as Image;
                if (image == null)
                {
                    continue;
                }

                image.DisableSpriteOptimizations();
                EditorUtility.SetDirty(image);
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }

            Repaint();
        }
    }
}
