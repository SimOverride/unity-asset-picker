using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor
{
    /// <summary>
    /// 资源选择器使用的文件夹和资源查询结果。
    /// </summary>
    internal sealed class AssetPickerFolder
    {
        public string Path;
        public string Name;
        public int Depth;
        public bool HasChildren;
    }

    internal sealed class AssetPickerEntry
    {
        public UnityEngine.Object Asset;
        public string Path;
        public string Name;
    }

    /// <summary>
    /// 通过 AssetDatabase 查询项目资源，不维护额外索引文件。
    /// </summary>
    internal static class AssetPickerQuery
    {
        private const string ProjectAssetRoot = "Assets";
        private static readonly Dictionary<Type, List<AssetPickerEntry>> AssetCache =
            new Dictionary<Type, List<AssetPickerEntry>>();

        public static List<AssetPickerFolder> BuildFolders(
            Type requiredType,
            bool onlyMatchingFolders,
            string searchText)
        {
            List<AssetPickerFolder> result = new List<AssetPickerFolder>();
            HashSet<string> foldersWithRequiredAssets = onlyMatchingFolders
                ? FindFoldersWithRequiredAssets(requiredType, searchText)
                : null;
            AddFolder(result, ProjectAssetRoot, 0, foldersWithRequiredAssets);
            return result;
        }

        public static void ClearCache()
        {
            AssetCache.Clear();
        }

        public static List<AssetPickerEntry> FindAssets(
            Type requiredType,
            string selectedFolder,
            bool includeSubfolders,
            string searchText)
        {
            List<AssetPickerEntry> result = new List<AssetPickerEntry>();
            if (requiredType == null || !AssetDatabase.IsValidFolder(selectedFolder))
            {
                return result;
            }

            List<AssetPickerEntry> indexedAssets = GetIndexedAssets(requiredType);
            string normalizedSearch = searchText?.Trim() ?? string.Empty;

            foreach (AssetPickerEntry entry in indexedAssets)
            {
                if (!IsInSelectedFolder(entry.Path, selectedFolder, includeSubfolders) ||
                    !MatchesSearch(entry.Asset, entry.Path, normalizedSearch))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        private static List<AssetPickerEntry> GetIndexedAssets(Type requiredType)
        {
            if (AssetCache.TryGetValue(requiredType, out List<AssetPickerEntry> cachedAssets))
            {
                return cachedAssets;
            }

            List<AssetPickerEntry> result = new List<AssetPickerEntry>();
            if (requiredType == null)
            {
                return result;
            }

            string[] guids = AssetDatabase.FindAssets(
                GetTypeFilter(requiredType),
                new[] { ProjectAssetRoot });
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                if (assets == null || assets.Length == 0)
                {
                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    assets = mainAsset == null
                        ? Array.Empty<UnityEngine.Object>()
                        : new[] { mainAsset };
                }

                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset == null || !requiredType.IsAssignableFrom(asset.GetType()))
                    {
                        continue;
                    }

                    string key = assetPath + "|" + asset.name + "|" + asset.GetType().AssemblyQualifiedName;
                    if (!keys.Add(key))
                    {
                        continue;
                    }

                    result.Add(new AssetPickerEntry
                    {
                        Asset = asset,
                        Path = assetPath,
                        Name = asset.name,
                    });
                }
            }

            cachedAssets = result
                .OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            AssetCache[requiredType] = cachedAssets;
            return cachedAssets;
        }

        private static HashSet<string> FindFoldersWithRequiredAssets(
            Type requiredType,
            string searchText)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal)
            {
                ProjectAssetRoot,
            };
            string normalizedSearch = searchText?.Trim() ?? string.Empty;
            foreach (AssetPickerEntry entry in GetIndexedAssets(requiredType))
            {
                if (!MatchesSearch(entry.Asset, entry.Path, normalizedSearch))
                {
                    continue;
                }

                string folderPath = Path.GetDirectoryName(entry.Path)?.Replace('\\', '/');
                while (!string.IsNullOrWhiteSpace(folderPath) &&
                       folderPath.StartsWith(ProjectAssetRoot, StringComparison.Ordinal))
                {
                    result.Add(folderPath);
                    if (folderPath.Equals(ProjectAssetRoot, StringComparison.Ordinal))
                    {
                        break;
                    }

                    folderPath = GetParentFolder(folderPath);
                }
            }

            return result;
        }

        private static void AddFolder(
            List<AssetPickerFolder> folders,
            string folderPath,
            int depth,
            HashSet<string> foldersWithRequiredAssets)
        {
            if (foldersWithRequiredAssets != null &&
                !foldersWithRequiredAssets.Contains(folderPath))
            {
                return;
            }

            string folderName = folderPath == ProjectAssetRoot
                ? ProjectAssetRoot
                : Path.GetFileName(folderPath);
            string[] children = AssetDatabase.GetSubFolders(folderPath)
                .Select(child => child.Replace('\\', '/'))
                .Where(child => foldersWithRequiredAssets == null ||
                                foldersWithRequiredAssets.Contains(child))
                .ToArray();
            folders.Add(new AssetPickerFolder
            {
                Path = folderPath,
                Name = folderName,
                Depth = depth,
                HasChildren = children.Length > 0,
            });

            Array.Sort(children, StringComparer.OrdinalIgnoreCase);
            foreach (string child in children)
            {
                AddFolder(folders, child, depth + 1, foldersWithRequiredAssets);
            }
        }

        private static string GetParentFolder(string folder)
        {
            int separatorIndex = folder.LastIndexOf('/');
            return separatorIndex <= 0
                ? string.Empty
                : folder.Substring(0, separatorIndex);
        }

        private static string GetTypeFilter(Type requiredType)
        {
            if (requiredType == typeof(UnityEngine.Object))
            {
                return string.Empty;
            }

            // Unity 的资源搜索对预制体使用 Prefab 类型名，而字段实际返回 GameObject。
            return requiredType == typeof(GameObject)
                ? "t:Prefab"
                : "t:" + requiredType.Name;
        }

        private static bool IsInSelectedFolder(
            string assetPath,
            string selectedFolder,
            bool includeSubfolders)
        {
            string normalizedFolder = selectedFolder.TrimEnd('/');
            string prefix = normalizedFolder + "/";
            if (!assetPath.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (includeSubfolders)
            {
                return true;
            }

            return assetPath.LastIndexOf('/') == normalizedFolder.Length;
        }

        private static bool MatchesSearch(
            UnityEngine.Object asset,
            string assetPath,
            string searchText)
        {
            return string.IsNullOrEmpty(searchText) ||
                   asset.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   assetPath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
