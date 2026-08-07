using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AssetPicker.Editor.Tests
{
    /// <summary>
    /// 验证选择窗口打开时的初始文件夹优先级。
    /// </summary>
    public sealed class AssetPickerWindowFolderTests
    {
        private const string TestRoot = "Assets/AssetPickerWindowFolderTests";
        private const string AssignedFolder = TestRoot + "/Assigned";
        private const string PreviousFolder = TestRoot + "/Previous";
        private const string SpritePath = AssignedFolder + "/Hero.png";

        private string previousSavedFolder;

        [SetUp]
        public void SetUp()
        {
            previousSavedFolder = AssetPickerSettings.GetLastFolder(typeof(Sprite));
            CleanupAssets();
            AssetDatabase.CreateFolder("Assets", "AssetPickerWindowFolderTests");
            AssetDatabase.CreateFolder(TestRoot, "Assigned");
            AssetDatabase.CreateFolder(TestRoot, "Previous");
            CreateSprite();
        }

        [TearDown]
        public void TearDown()
        {
            AssetPickerSettings.SaveLastFolder(typeof(Sprite), previousSavedFolder);
            CleanupAssets();
        }

        [Test]
        public void GetInitialFolder_UsesAssignedAssetFolder()
        {
            AssetPickerSettings.SaveLastFolder(typeof(Sprite), PreviousFolder);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            Assert.That(sprite, Is.Not.Null);
            Assert.That(InvokeGetInitialFolder(sprite), Is.EqualTo(AssignedFolder));
        }

        [Test]
        public void GetInitialFolder_UsesLastFolderWhenNoAssignedAsset()
        {
            AssetPickerSettings.SaveLastFolder(typeof(Sprite), PreviousFolder);

            Assert.That(InvokeGetInitialFolder(null), Is.EqualTo(PreviousFolder));
        }

        private static string InvokeGetInitialFolder(UnityEngine.Object current)
        {
            AssetPickerWindow window = ScriptableObject.CreateInstance<AssetPickerWindow>();
            try
            {
                MethodInfo method = typeof(AssetPickerWindow).GetMethod(
                    "GetInitialFolder",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                return method.Invoke(window, new object[] { typeof(Sprite), current }) as string;
            }
            finally
            {
                Object.DestroyImmediate(window);
            }
        }

        private static void CreateSprite()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        private static void CleanupAssets()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
