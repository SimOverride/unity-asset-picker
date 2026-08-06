using System;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 保存资源选择器的本地编辑器偏好，不写入项目资产或运行时配置。
    /// </summary>
    internal static class AssetPickerSettings
    {
        private const string LastFolderKeyPrefix = "SimOverride.AssetPicker.LastFolder.";
        private const string ShowOnlyMatchingFoldersKey = "SimOverride.AssetPicker.ShowOnlyMatchingFolders";
        private const string ThumbnailSizeIndexKey = "SimOverride.AssetPicker.ThumbnailSizeIndex";
        private const string FolderPanelWidthKey = "SimOverride.AssetPicker.FolderPanelWidth";
        private const string DefaultFolder = "Assets";

        public static bool ShowOnlyMatchingFolders
        {
            get => EditorPrefs.GetBool(ShowOnlyMatchingFoldersKey, true);
            set => EditorPrefs.SetBool(ShowOnlyMatchingFoldersKey, value);
        }

        public static int ThumbnailSizeIndex
        {
            get => Mathf.Clamp(EditorPrefs.GetInt(ThumbnailSizeIndexKey, 1), 0, 2);
            set => EditorPrefs.SetInt(ThumbnailSizeIndexKey, Mathf.Clamp(value, 0, 2));
        }

        public static float FolderPanelWidth
        {
            get => Mathf.Clamp(EditorPrefs.GetFloat(FolderPanelWidthKey, 230f), 180f, 420f);
            set => EditorPrefs.SetFloat(FolderPanelWidthKey, Mathf.Clamp(value, 180f, 420f));
        }

        public static string GetLastFolder(Type assetType)
        {
            string folder = EditorPrefs.GetString(GetKey(assetType), DefaultFolder);
            return IsProjectFolder(folder) ? folder : DefaultFolder;
        }

        public static bool TryGetLastFolder(Type assetType, out string folder)
        {
            folder = EditorPrefs.GetString(GetKey(assetType), string.Empty);
            return IsProjectFolder(folder);
        }

        public static void SaveLastFolder(Type assetType, string folder)
        {
            if (assetType == null || !IsProjectFolder(folder))
            {
                return;
            }

            EditorPrefs.SetString(GetKey(assetType), folder);
        }

        private static string GetKey(Type assetType)
        {
            string typeName = assetType == null
                ? typeof(UnityEngine.Object).AssemblyQualifiedName
                : assetType.AssemblyQualifiedName;
            return LastFolderKeyPrefix + typeName;
        }

        private static bool IsProjectFolder(string folder)
        {
            return !string.IsNullOrWhiteSpace(folder) &&
                   (folder.Equals("Assets", StringComparison.Ordinal) ||
                    folder.StartsWith("Assets/", StringComparison.Ordinal)) &&
                   AssetDatabase.IsValidFolder(folder);
        }
    }
}
