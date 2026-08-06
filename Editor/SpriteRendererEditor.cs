using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 只替换 SpriteRenderer 的 Sprite 字段，并按 Unity 原生面板顺序绘制其他属性。
    /// </summary>
    [CustomEditor(typeof(SpriteRenderer))]
    [CanEditMultipleObjects]
    internal sealed class SpriteRendererEditor : UnityEditor.Editor
    {
        private SerializedProperty spriteProperty;
        private SerializedProperty colorProperty;
        private SerializedProperty flipXProperty;
        private SerializedProperty flipYProperty;
        private SerializedProperty drawModeProperty;
        private SerializedProperty sizeProperty;
        private SerializedProperty adaptiveModeThresholdProperty;
        private SerializedProperty spriteTileModeProperty;
        private SerializedProperty maskInteractionProperty;
        private SerializedProperty spriteSortPointProperty;
        private SerializedProperty materialsProperty;
        private SerializedProperty sortingLayerIdProperty;
        private SerializedProperty sortingOrderProperty;
        private bool showAdditionalSettings = true;
        private UnityEditor.Editor unitySpriteRendererEditor;

        private static readonly Type UnitySpriteRendererEditorType =
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SpriteRendererEditor");

        private void OnEnable()
        {
            spriteProperty = serializedObject.FindProperty("m_Sprite");
            colorProperty = serializedObject.FindProperty("m_Color");
            flipXProperty = serializedObject.FindProperty("m_FlipX");
            flipYProperty = serializedObject.FindProperty("m_FlipY");
            drawModeProperty = serializedObject.FindProperty("m_DrawMode");
            sizeProperty = serializedObject.FindProperty("m_Size");
            adaptiveModeThresholdProperty = serializedObject.FindProperty("m_AdaptiveModeThreshold");
            spriteTileModeProperty = serializedObject.FindProperty("m_SpriteTileMode");
            maskInteractionProperty = serializedObject.FindProperty("m_MaskInteraction");
            spriteSortPointProperty = serializedObject.FindProperty("m_SpriteSortPoint");
            materialsProperty = serializedObject.FindProperty("m_Materials");
            sortingLayerIdProperty = serializedObject.FindProperty("m_SortingLayerID");
            sortingOrderProperty = serializedObject.FindProperty("m_SortingOrder");
        }

        private void OnDisable()
        {
            DestroyUnitySpriteRendererEditor();
        }

        public override void OnInspectorGUI()
        {
            if (!AssetPickerSettings.UseCustomSpriteRendererEditor)
            {
                DrawUnitySpriteRendererInspector();
                return;
            }

            DestroyUnitySpriteRendererEditor();
            serializedObject.Update();

            if (spriteProperty == null)
            {
                EditorGUILayout.HelpBox(
                    "当前 Unity 版本未找到 SpriteRenderer 的 Sprite 属性，已回退到默认属性绘制。",
                    MessageType.Warning);
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // 顺序与 Unity 2022.3 的 SpriteRenderer Inspector 保持一致。
            AssetPickerField.DrawPickerField(
                "Sprite",
                spriteProperty.objectReferenceValue,
                typeof(Sprite),
                ApplySprite,
                spriteProperty.hasMultipleDifferentValues);
            DrawProperty(colorProperty);
            DrawFlipProperties();
            DrawDrawModeProperties();
            DrawProperty(maskInteractionProperty);
            DrawProperty(spriteSortPointProperty);
            DrawMaterialProperty();
            DrawAdditionalSettings();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 通过 Unity 的内部 Editor 类型绘制原版 SpriteRenderer Inspector。
        /// 菜单切换到原版时，其他面板行为和 Unity 版本保持一致。
        /// </summary>
        private void DrawUnitySpriteRendererInspector()
        {
            if (UnitySpriteRendererEditorType != null && unitySpriteRendererEditor == null)
            {
                unitySpriteRendererEditor = CreateUnitySpriteRendererEditor();
            }

            if (unitySpriteRendererEditor != null)
            {
                unitySpriteRendererEditor.OnInspectorGUI();
                return;
            }

            EditorGUILayout.HelpBox(
                "无法创建 Unity 原版 SpriteRenderer Editor，已使用默认属性绘制。",
                MessageType.Warning);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        private UnityEditor.Editor CreateUnitySpriteRendererEditor()
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
                    new object[] { targets, UnitySpriteRendererEditorType }) as UnityEditor.Editor;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
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

        private void DestroyUnitySpriteRendererEditor()
        {
            if (unitySpriteRendererEditor == null)
            {
                return;
            }

            DestroyImmediate(unitySpriteRendererEditor);
            unitySpriteRendererEditor = null;
        }

        private void DrawProperty(SerializedProperty property)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }

        private void DrawFlipProperties()
        {
            if (flipXProperty == null || flipYProperty == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Flip");

            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = flipXProperty.hasMultipleDifferentValues;
            flipXProperty.boolValue = EditorGUILayout.ToggleLeft(
                "X",
                flipXProperty.boolValue,
                GUILayout.Width(40f));
            EditorGUI.showMixedValue = flipYProperty.hasMultipleDifferentValues;
            flipYProperty.boolValue = EditorGUILayout.ToggleLeft(
                "Y",
                flipYProperty.boolValue,
                GUILayout.Width(40f));
            EditorGUI.showMixedValue = previousMixedValue;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDrawModeProperties()
        {
            DrawProperty(drawModeProperty);
            if (drawModeProperty == null || drawModeProperty.enumNames.Length == 0)
            {
                return;
            }

            string drawModeName = drawModeProperty.enumNames[drawModeProperty.enumValueIndex];
            if (string.Equals(drawModeName, "Simple", StringComparison.Ordinal))
            {
                return;
            }

            DrawProperty(sizeProperty);
            if (!string.Equals(drawModeName, "Tiled", StringComparison.Ordinal))
            {
                return;
            }

            DrawProperty(spriteTileModeProperty);
            if (spriteTileModeProperty == null ||
                spriteTileModeProperty.enumNames.Length == 0)
            {
                return;
            }

            string tileModeName = spriteTileModeProperty.enumNames[
                spriteTileModeProperty.enumValueIndex];
            if (string.Equals(tileModeName, "Adaptive", StringComparison.Ordinal))
            {
                DrawProperty(adaptiveModeThresholdProperty);
            }
        }

        private void DrawMaterialProperty()
        {
            if (materialsProperty == null ||
                !materialsProperty.isArray ||
                materialsProperty.arraySize == 0)
            {
                return;
            }

            // SpriteRenderer 的原生面板只显示第一个材质槽，而不是显示数组折叠器。
            SerializedProperty materialProperty = materialsProperty.GetArrayElementAtIndex(0);
            EditorGUILayout.PropertyField(materialProperty, new GUIContent("Material"));
        }

        private void DrawAdditionalSettings()
        {
            showAdditionalSettings = EditorGUILayout.Foldout(
                showAdditionalSettings,
                "Additional Settings",
                true);
            if (!showAdditionalSettings)
            {
                return;
            }

            EditorGUI.indentLevel++;
            DrawSortingLayerProperty();
            DrawProperty(sortingOrderProperty);
            EditorGUI.indentLevel--;
        }

        private void DrawSortingLayerProperty()
        {
            if (sortingLayerIdProperty == null)
            {
                return;
            }

            SortingLayer[] layers = SortingLayer.layers;
            if (layers == null || layers.Length == 0)
            {
                EditorGUILayout.LabelField("Sorting Layer", "Default");
                return;
            }

            int currentLayerId = sortingLayerIdProperty.intValue;
            int currentIndex = FindSortingLayerIndex(layers, currentLayerId);
            bool hasMissingLayer = currentIndex < 0;
            string[] layerNames = new string[layers.Length + (hasMissingLayer ? 1 : 0)];
            int layerOffset = 0;
            if (hasMissingLayer)
            {
                layerNames[0] = "Missing (" + currentLayerId + ")";
                layerOffset = 1;
                currentIndex = 0;
            }

            for (int index = 0; index < layers.Length; index++)
            {
                layerNames[index + layerOffset] = layers[index].name;
            }

            int nextIndex = EditorGUILayout.Popup(
                "Sorting Layer",
                currentIndex,
                layerNames);
            if (hasMissingLayer)
            {
                nextIndex -= 1;
            }

            if (nextIndex >= 0 && nextIndex < layers.Length)
            {
                sortingLayerIdProperty.intValue = layers[nextIndex].id;
            }
        }

        private static int FindSortingLayerIndex(SortingLayer[] layers, int layerId)
        {
            for (int index = 0; index < layers.Length; index++)
            {
                if (layers[index].id == layerId)
                {
                    return index;
                }
            }

            return -1;
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

            Undo.RecordObjects(targets, "选择 Sprite");
            serializedObject.Update();
            spriteProperty.objectReferenceValue = sprite;
            serializedObject.ApplyModifiedProperties();

            foreach (UnityEngine.Object targetObject in targets)
            {
                SpriteRenderer renderer = targetObject as SpriteRenderer;
                if (renderer == null)
                {
                    continue;
                }

                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            Repaint();
        }
    }
}
