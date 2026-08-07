using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 按文件夹筛选并直接选择单个 Unity 资源的编辑器窗口。
    /// </summary>
    internal sealed class AssetPickerWindow : EditorWindow
    {
        private const float FolderPanelMinWidth = 180f;
        private const float FolderPanelMaxWidth = 420f;
        private const float FolderResizeHandleWidth = 4f;
        private static readonly float[] ThumbnailSizes = { 52f, 80f, 112f };
        private static readonly string[] ThumbnailSizeNames = { "小图标", "中图标", "大图标" };
        private static readonly Type[] SelectableTypes =
        {
            typeof(Sprite),
            typeof(Material),
            typeof(Texture2D),
            typeof(GameObject),
            typeof(ScriptableObject),
        };
        private static readonly string[] SelectableTypeNames =
        {
            "Sprite",
            "Material",
            "Texture2D",
            "Prefab",
            "ScriptableObject",
        };

        private Type requiredType;
        private Action<UnityEngine.Object> onSelected;
        private UnityEngine.Object selectedAsset;
        private bool allowTypeSelection;
        private string selectedFolder;
        private string searchText = string.Empty;
        private bool includeSubfolders = true;
        private bool showOnlyMatchingFolders = true;
        private int thumbnailSizeIndex = 1;
        private float folderPanelWidth = 230f;
        private bool resizingFolderPanel;
        private bool assetsDirty = true;
        private Vector2 folderScrollPosition;
        private Vector2 assetScrollPosition;
        private List<AssetPickerFolder> folders = new List<AssetPickerFolder>();
        private List<AssetPickerEntry> assets = new List<AssetPickerEntry>();
        private HashSet<string> expandedFolders = new HashSet<string>(StringComparer.Ordinal);
        private Dictionary<int, Texture2D> previewCache = new Dictionary<int, Texture2D>();

        public static void Open(
            Type objectType,
            UnityEngine.Object current,
            string title,
            Action<UnityEngine.Object> selectedCallback,
            bool allowTypeSelection = false)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            string windowTitle = string.IsNullOrWhiteSpace(title) ? "资源选择器" : title;
            AssetPickerWindow window = GetWindow<AssetPickerWindow>(true, windowTitle, true);
            window.minSize = new Vector2(720f, 420f);
            window.Initialize(
                objectType,
                current,
                windowTitle,
                selectedCallback,
                allowTypeSelection);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            minSize = new Vector2(720f, 420f);
        }

        private void OnDisable()
        {
            // 关闭窗口时保存当前浏览位置，下次按相同资源类型打开时恢复。
            AssetPickerSettings.SaveLastFolder(requiredType, selectedFolder);
            AssetPickerSettings.FolderPanelWidth = folderPanelWidth;
            onSelected = null;
        }

        private void OnProjectChange()
        {
            AssetPickerQuery.ClearCache();
            previewCache.Clear();
            RefreshFolders();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void Initialize(
            Type objectType,
            UnityEngine.Object current,
            string title,
            Action<UnityEngine.Object> selectedCallback,
            bool canSelectType)
        {
            requiredType = objectType ?? typeof(Sprite);
            onSelected = selectedCallback;
            selectedAsset = current;
            allowTypeSelection = canSelectType;
            titleContent = new GUIContent(
                string.IsNullOrWhiteSpace(title) ? "资源选择器" : title);
            selectedFolder = GetInitialFolder(objectType, current);
            searchText = string.Empty;
            includeSubfolders = true;
            showOnlyMatchingFolders = AssetPickerSettings.ShowOnlyMatchingFolders;
            thumbnailSizeIndex = AssetPickerSettings.ThumbnailSizeIndex;
            folderPanelWidth = AssetPickerSettings.FolderPanelWidth;
            folderScrollPosition = Vector2.zero;
            assetScrollPosition = Vector2.zero;
            expandedFolders.Clear();
            expandedFolders.Add("Assets");
            previewCache.Clear();
            RefreshFolders();
        }

        private void OnGUI()
        {
            if (requiredType == null)
            {
                EditorGUILayout.HelpBox("没有指定资源类型，无法打开资源选择器。", MessageType.Error);
                return;
            }

            DrawToolbar();
            if (assetsDirty)
            {
                RefreshAssets();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawFolderPanel();
                DrawFolderResizeHandle();
                DrawAssetPanel();
            }

            EditorGUILayout.LabelField(
                "当前文件夹：" + selectedFolder,
                EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                "点击资源即可选择，窗口会保持打开；关闭窗口不会修改项目资源。",
                EditorStyles.miniLabel);
            HandleKeyboardInput();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (allowTypeSelection)
                {
                    int currentTypeIndex = GetTypeIndex(requiredType);
                    int nextTypeIndex = EditorGUILayout.Popup(
                        currentTypeIndex,
                        SelectableTypeNames,
                        EditorStyles.toolbarPopup,
                        GUILayout.Width(110f));
                    if (nextTypeIndex != currentTypeIndex &&
                        nextTypeIndex >= 0 &&
                        nextTypeIndex < SelectableTypes.Length)
                    {
                        ChangeRequiredType(SelectableTypes[nextTypeIndex]);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "类型：" + GetTypeDisplayName(requiredType),
                        EditorStyles.label,
                        GUILayout.Width(150f));
                }

                string nextSearchText = EditorGUILayout.TextField(
                    searchText,
                    EditorStyles.toolbarTextField,
                    GUILayout.ExpandWidth(true));
                if (!string.Equals(nextSearchText, searchText, StringComparison.Ordinal))
                {
                    searchText = nextSearchText;
                    RefreshFolders();
                }

                bool nextIncludeSubfolders = GUILayout.Toggle(
                    includeSubfolders,
                    "包含子文件夹",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(100f));
                if (nextIncludeSubfolders != includeSubfolders)
                {
                    includeSubfolders = nextIncludeSubfolders;
                    assetsDirty = true;
                }

                bool nextShowOnlyMatchingFolders = GUILayout.Toggle(
                    showOnlyMatchingFolders,
                    "仅显示匹配文件夹",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(126f));
                if (nextShowOnlyMatchingFolders != showOnlyMatchingFolders)
                {
                    showOnlyMatchingFolders = nextShowOnlyMatchingFolders;
                    AssetPickerSettings.ShowOnlyMatchingFolders = showOnlyMatchingFolders;
                    RefreshFolders();
                }

                int nextThumbnailSizeIndex = EditorGUILayout.Popup(
                    thumbnailSizeIndex,
                    ThumbnailSizeNames,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(76f));
                if (nextThumbnailSizeIndex != thumbnailSizeIndex)
                {
                    thumbnailSizeIndex = nextThumbnailSizeIndex;
                    AssetPickerSettings.ThumbnailSizeIndex = thumbnailSizeIndex;
                    Repaint();
                }

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                {
                    AssetPickerQuery.ClearCache();
                    previewCache.Clear();
                    RefreshFolders();
                }
            }
        }

        private void ChangeRequiredType(Type nextType)
        {
            if (nextType == null || nextType == requiredType)
            {
                return;
            }

            AssetPickerSettings.SaveLastFolder(requiredType, selectedFolder);
            requiredType = nextType;
            selectedFolder = AssetPickerSettings.GetLastFolder(requiredType);
            selectedAsset = null;
            assetScrollPosition = Vector2.zero;
            RefreshFolders();
        }

        private static int GetTypeIndex(Type type)
        {
            for (int index = 0; index < SelectableTypes.Length; index++)
            {
                if (SelectableTypes[index] == type)
                {
                    return index;
                }
            }

            return 0;
        }

        private static string GetTypeDisplayName(Type type)
        {
            int index = GetTypeIndex(type);
            return SelectableTypes[index] == type
                ? SelectableTypeNames[index]
                : type.Name;
        }

        private void DrawFolderPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(folderPanelWidth)))
            {
                EditorGUILayout.LabelField("文件夹", EditorStyles.boldLabel);
                folderScrollPosition = EditorGUILayout.BeginScrollView(folderScrollPosition);

                foreach (AssetPickerFolder folder in folders)
                {
                    if (!IsFolderVisible(folder))
                    {
                        continue;
                    }

                    DrawFolderRow(folder);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawFolderResizeHandle()
        {
            Rect handle = GUILayoutUtility.GetRect(
                FolderResizeHandleWidth,
                1f,
                GUILayout.Width(FolderResizeHandleWidth),
                GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(handle, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.16f, 0.16f)
                : new Color(0.72f, 0.72f, 0.72f));
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeHorizontal);

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && handle.Contains(currentEvent.mousePosition))
            {
                resizingFolderPanel = true;
                currentEvent.Use();
            }

            if (!resizingFolderPanel)
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDrag)
            {
                folderPanelWidth = Mathf.Clamp(
                    currentEvent.mousePosition.x,
                    FolderPanelMinWidth,
                    FolderPanelMaxWidth);
                Repaint();
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                resizingFolderPanel = false;
                AssetPickerSettings.FolderPanelWidth = folderPanelWidth;
                currentEvent.Use();
            }
        }

        private void DrawFolderRow(AssetPickerFolder folder)
        {
            Rect row = EditorGUILayout.GetControlRect(false, 22f);
            int indent = folder.Depth * 16;
            Rect contentRect = new Rect(
                row.x + indent,
                row.y,
                Mathf.Max(0f, row.width - indent),
                row.height);
            Rect foldoutRect = new Rect(
                contentRect.x,
                contentRect.y,
                16f,
                contentRect.height);

            if (folder.Path == selectedFolder)
            {
                Color selectionColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.48f, 0.72f, 0.85f)
                    : new Color(0.32f, 0.55f, 0.85f, 0.85f);
                EditorGUI.DrawRect(row, selectionColor);
            }

            if (folder.HasChildren)
            {
                bool expanded = expandedFolders.Contains(folder.Path);
                bool nextExpanded = EditorGUI.Foldout(
                    foldoutRect,
                    expanded,
                    GUIContent.none);
                if (nextExpanded != expanded)
                {
                    if (nextExpanded)
                    {
                        expandedFolders.Add(folder.Path);
                    }
                    else
                    {
                        expandedFolders.Remove(folder.Path);
                    }
                }
            }

            Rect labelRect = new Rect(
                contentRect.x + 18f,
                contentRect.y,
                Mathf.Max(0f, contentRect.width - 18f),
                contentRect.height);
            GUIContent folderContent = EditorGUIUtility.IconContent("Folder Icon");
            if (folderContent == null)
            {
                folderContent = new GUIContent();
            }

            folderContent.text = folder.Name;
            GUIStyle labelStyle = folder.Path == selectedFolder
                ? EditorStyles.whiteLabel
                : EditorStyles.label;
            if (GUI.Button(labelRect, folderContent, labelStyle))
            {
                selectedFolder = folder.Path;
                AssetPickerSettings.SaveLastFolder(requiredType, selectedFolder);
                EnsureFolderExpansion();
                assetsDirty = true;
            }
        }

        private void DrawAssetPanel()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(
                    "资源（" + assets.Count + "）",
                    EditorStyles.boldLabel);
                assetScrollPosition = EditorGUILayout.BeginScrollView(assetScrollPosition);

                if (assets.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "当前文件夹中没有符合条件的资源。",
                        MessageType.Info);
                }
                else
                {
                    DrawAssetGrid();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawAssetGrid()
        {
            float assetCellWidth = GetAssetCellWidth();
            float availableWidth = Mathf.Max(
                assetCellWidth,
                position.width - folderPanelWidth - FolderResizeHandleWidth - 24f);
            int columnCount = Mathf.Max(
                1,
                Mathf.FloorToInt(availableWidth / assetCellWidth));

            for (int index = 0; index < assets.Count; index++)
            {
                if (index % columnCount == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                DrawAssetEntry(assets[index]);

                bool isLastEntry = index == assets.Count - 1;
                bool isLastColumn = (index + 1) % columnCount == 0;
                if (isLastEntry || isLastColumn)
                {
                    if (isLastEntry && !isLastColumn)
                    {
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private void DrawAssetEntry(AssetPickerEntry entry)
        {
            float assetCellWidth = GetAssetCellWidth();
            float assetCellHeight = GetAssetCellHeight();
            float assetIconSize = GetAssetIconSize();
            Rect cell = GUILayoutUtility.GetRect(
                assetCellWidth,
                assetCellHeight,
                GUILayout.Width(assetCellWidth),
                GUILayout.Height(assetCellHeight));
            if (GUI.Button(cell, GUIContent.none, EditorStyles.objectField))
            {
                SelectAsset(entry.Asset);
            }

            if (entry.Asset == selectedAsset)
            {
                Color selectionColor = EditorGUIUtility.isProSkin
                    ? new Color(0.24f, 0.48f, 0.72f, 0.55f)
                    : new Color(0.32f, 0.55f, 0.85f, 0.55f);
                EditorGUI.DrawRect(cell, selectionColor);
            }

            Rect iconRect = new Rect(
                cell.x + (cell.width - assetIconSize) * 0.5f,
                cell.y + 6f,
                assetIconSize,
                assetIconSize);
            Texture2D preview = GetAssetPreview(entry);
            if (preview == null)
            {
                preview = AssetPreview.GetMiniThumbnail(entry.Asset);
            }

            if (preview != null)
            {
                GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
            }

            Rect nameRect = new Rect(
                cell.x + 4f,
                iconRect.yMax + 4f,
                cell.width - 8f,
                cell.height - assetIconSize - 10f);
            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
            };
            GUI.Label(nameRect, entry.Name, nameStyle);
        }

        private float GetAssetIconSize()
        {
            return ThumbnailSizes[Mathf.Clamp(thumbnailSizeIndex, 0, ThumbnailSizes.Length - 1)];
        }

        private float GetAssetCellWidth()
        {
            return GetAssetIconSize() + 24f;
        }

        private float GetAssetCellHeight()
        {
            return GetAssetIconSize() + 42f;
        }

        private Texture2D GetAssetPreview(AssetPickerEntry entry)
        {
            int instanceId = entry.Asset.GetInstanceID();
            if (previewCache.TryGetValue(instanceId, out Texture2D cachedPreview))
            {
                return cachedPreview;
            }

            Texture2D preview = AssetPreview.GetAssetPreview(entry.Asset);
            if (preview != null)
            {
                previewCache[instanceId] = preview;
            }

            return preview;
        }

        private void HandleKeyboardInput()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }

        private void SelectAsset(UnityEngine.Object asset)
        {
            if (asset == null || !requiredType.IsAssignableFrom(asset.GetType()))
            {
                return;
            }

            AssetPickerSettings.SaveLastFolder(requiredType, selectedFolder);
            selectedAsset = asset;
            onSelected?.Invoke(asset);
            Repaint();
            GUIUtility.ExitGUI();
        }

        private void RefreshFolders()
        {
            folders = AssetPickerQuery.BuildFolders(
                requiredType,
                showOnlyMatchingFolders,
                searchText);
            if (!IsSelectableFolder(selectedFolder))
            {
                selectedFolder = AssetPickerSettings.GetLastFolder(requiredType);
            }

            if (!IsSelectableFolder(selectedFolder))
            {
                selectedFolder = "Assets";
            }

            EnsureFolderExpansion();
            assetsDirty = true;
        }

        private bool IsSelectableFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return false;
            }

            foreach (AssetPickerFolder selectableFolder in folders)
            {
                if (selectableFolder.Path.Equals(folder, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureFolderExpansion()
        {
            expandedFolders.Add("Assets");
            expandedFolders.RemoveWhere(folder => !AssetDatabase.IsValidFolder(folder));

            string currentFolder = selectedFolder;
            while (!string.IsNullOrWhiteSpace(currentFolder) &&
                   !currentFolder.Equals("Assets", StringComparison.Ordinal))
            {
                expandedFolders.Add(currentFolder);
                currentFolder = GetParentFolder(currentFolder);
            }
        }

        private bool IsFolderVisible(AssetPickerFolder folder)
        {
            if (folder.Path.Equals("Assets", StringComparison.Ordinal))
            {
                return true;
            }

            string parentFolder = GetParentFolder(folder.Path);
            while (!string.IsNullOrWhiteSpace(parentFolder))
            {
                if (!expandedFolders.Contains(parentFolder))
                {
                    return false;
                }

                if (parentFolder.Equals("Assets", StringComparison.Ordinal))
                {
                    return true;
                }

                parentFolder = GetParentFolder(parentFolder);
            }

            return false;
        }

        private static string GetParentFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return string.Empty;
            }

            int separatorIndex = folder.LastIndexOf('/');
            return separatorIndex <= 0
                ? string.Empty
                : folder.Substring(0, separatorIndex);
        }

        private void RefreshAssets()
        {
            assets = AssetPickerQuery.FindAssets(
                requiredType,
                selectedFolder,
                includeSubfolders,
                searchText);
            assetsDirty = false;
        }

        private string GetInitialFolder(Type objectType, UnityEngine.Object current)
        {
            // 资源字段已有项目资源时，优先定位到该资源所在文件夹，便于继续查找同目录资源。
            if (current != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(current).Replace('\\', '/');
                string currentFolder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (IsKnownFolder(currentFolder))
                {
                    return currentFolder;
                }
            }

            // 属性为空，或当前对象不是可定位的项目资源时，恢复该类型上次关闭时的位置。
            if (AssetPickerSettings.TryGetLastFolder(objectType, out string lastFolder))
            {
                return lastFolder;
            }

            return AssetPickerSettings.GetLastFolder(objectType);
        }

        private bool IsKnownFolder(string folder)
        {
            return !string.IsNullOrWhiteSpace(folder) &&
                   AssetDatabase.IsValidFolder(folder) &&
                   (folder.Equals("Assets", StringComparison.Ordinal) ||
                    folder.StartsWith("Assets/", StringComparison.Ordinal));
        }
    }
}
