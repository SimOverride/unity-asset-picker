using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor.Tests
{
    /// <summary>
    /// 验证资源类型、搜索条件和索引缓存对文件夹树及资源列表的影响。
    /// </summary>
    public sealed class AssetPickerQueryTests
    {
        private const string TestRoot = "Assets/AssetPickerTests";
        private const string SpriteFolder = TestRoot + "/Sprites";
        private const string MaterialFolder = TestRoot + "/Materials";
        private const string PrefabFolder = TestRoot + "/Prefabs";
        private const string EmptyFolder = TestRoot + "/Empty";

        [SetUp]
        public void SetUp()
        {
            CleanupAssets();
            AssetDatabase.CreateFolder("Assets", "AssetPickerTests");
            AssetDatabase.CreateFolder(TestRoot, "Sprites");
            AssetDatabase.CreateFolder(TestRoot, "Materials");
            AssetDatabase.CreateFolder(TestRoot, "Prefabs");
            AssetDatabase.CreateFolder(TestRoot, "Empty");
            CreateSprite("Hero.png");
            CreateMaterial("Hero.mat");
            CreatePrefab("Hero.prefab");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetPickerQuery.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            CleanupAssets();
        }

        [Test]
        public void BuildFolders_FiltersByTypeAndSearch()
        {
            List<AssetPickerFolder> spriteFolders = AssetPickerQuery.BuildFolders(typeof(Sprite), true, string.Empty);
            Assert.That(HasFolder(spriteFolders, SpriteFolder), Is.True);
            Assert.That(HasFolder(spriteFolders, MaterialFolder), Is.False);
            Assert.That(HasFolder(spriteFolders, EmptyFolder), Is.False);

            List<AssetPickerFolder> searchFolders = AssetPickerQuery.BuildFolders(typeof(Sprite), true, "Hero");
            Assert.That(HasFolder(searchFolders, SpriteFolder), Is.True);
            Assert.That(HasFolder(searchFolders, MaterialFolder), Is.False);
        }

        [Test]
        public void FindAssets_UsesCacheUntilProjectRefresh()
        {
            List<AssetPickerEntry> initialAssets = AssetPickerQuery.FindAssets(typeof(Material), MaterialFolder, false, string.Empty);
            Assert.That(initialAssets, Has.Count.EqualTo(1));

            CreateMaterial("Second.mat");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            List<AssetPickerEntry> cachedAssets = AssetPickerQuery.FindAssets(typeof(Material), MaterialFolder, false, string.Empty);
            Assert.That(cachedAssets, Has.Count.EqualTo(1));

            AssetPickerQuery.ClearCache();
            List<AssetPickerEntry> refreshedAssets = AssetPickerQuery.FindAssets(typeof(Material), MaterialFolder, false, string.Empty);
            Assert.That(refreshedAssets, Has.Count.EqualTo(2));
        }

        [Test]
        public void BuildFolders_CanShowAllFolders()
        {
            List<AssetPickerFolder> allFolders = AssetPickerQuery.BuildFolders(typeof(Sprite), false, "NoSuchAsset");
            Assert.That(HasFolder(allFolders, SpriteFolder), Is.True);
            Assert.That(HasFolder(allFolders, MaterialFolder), Is.True);
            Assert.That(HasFolder(allFolders, EmptyFolder), Is.True);
        }

        /// <summary>
        /// 确认包含子物体的 Prefab 不会被拆成多个资源条目。
        /// </summary>
        [Test]
        public void FindAssets_PrefabReturnsOnlyRootObject()
        {
            List<AssetPickerEntry> prefabAssets = AssetPickerQuery.FindAssets(
                typeof(GameObject),
                PrefabFolder,
                false,
                string.Empty);

            Assert.That(prefabAssets, Has.Count.EqualTo(1));
            Assert.That(prefabAssets[0].Name, Is.EqualTo("Hero"));
            Assert.That(prefabAssets[0].Asset, Is.TypeOf<GameObject>());
        }

        private static bool HasFolder(List<AssetPickerFolder> folders, string path)
        {
            foreach (AssetPickerFolder folder in folders)
            {
                if (folder.Path.Equals(path, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateSprite(string fileName)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            string path = SpriteFolder + "/" + fileName;
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        private static void CreateMaterial(string fileName)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(material, MaterialFolder + "/" + fileName);
        }

        /// <summary>
        /// 创建带子物体的测试 Prefab，覆盖真实 Prefab 层级结构。
        /// </summary>
        private static void CreatePrefab(string fileName)
        {
            GameObject root = new GameObject("Hero");
            GameObject child = new GameObject("Child");
            child.transform.SetParent(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + fileName);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void CleanupAssets()
        {
            AssetPickerQuery.ClearCache();
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
