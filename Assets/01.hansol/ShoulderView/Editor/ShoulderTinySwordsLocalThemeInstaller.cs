using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Railgame.Hansol.ShoulderView.Editor
{
    public static class ShoulderTinySwordsLocalThemeInstaller
    {
        private const string DefaultSource = @"D:\Downloads\Tiny Swords (Free Pack)";
        private const string BaseThemePath =
            "Assets/01.hansol/ShoulderView/Demo/ShoulderViewWorkshopTheme.asset";
        private const string LocalRoot =
            "Assets/01.hansol/ShoulderView/UI/ThirdParty/TinySwordsLocal";
        private const string LocalThemePath = LocalRoot + "/ShoulderViewTinySwordsLocalTheme.asset";

        private static readonly (string Source, string Target)[] Files =
        {
            ("UI Elements/UI Elements/Icons/Icon_10.png", "UpgradeGear.png")
        };

        [MenuItem("Railgame/Hansol/Install Local Tiny Swords UI Theme")]
        public static void Install()
        {
            string sourceRoot = ReadArgument("-tiny-swords-path") ?? DefaultSource;
            if (!Directory.Exists(sourceRoot))
                throw new DirectoryNotFoundException($"Tiny Swords source not found: {sourceRoot}");

            Directory.CreateDirectory(LocalRoot);
            foreach ((string source, string target) in Files)
            {
                string sourcePath = Path.Combine(sourceRoot, source.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("Required Tiny Swords UI file is missing.", sourcePath);
                File.Copy(sourcePath, Path.Combine(LocalRoot, target), true);
            }

            AssetDatabase.Refresh();
            foreach (var file in Files)
                ConfigureSprite(LocalRoot + "/" + file.Target);
            AssetDatabase.Refresh();

            ShoulderUiTheme baseTheme = AssetDatabase.LoadAssetAtPath<ShoulderUiTheme>(BaseThemePath);
            if (baseTheme == null)
                throw new InvalidOperationException("Build the Shoulder View demo once before installing this theme.");

            AssetDatabase.DeleteAsset(LocalThemePath);
            ShoulderUiTheme localTheme = UnityEngine.Object.Instantiate(baseTheme);
            localTheme.name = "ShoulderViewTinySwordsLocalTheme";
            Sprite gear = LoadSprite("UpgradeGear.png");
            localTheme.ConfigureArtwork(baseTheme.GetSprite(ShoulderUiRole.Panel),
                baseTheme.GetSprite(ShoulderUiRole.Card), gear, baseTheme.GetSprite(ShoulderUiRole.Header),
                baseTheme.GetSprite(ShoulderUiRole.PrimaryButton), baseTheme.GetSprite(ShoulderUiRole.DangerButton),
                baseTheme.GetSprite(ShoulderUiRole.Prompt),
                baseTheme.GetSprite(ShoulderUiRole.CurrencyIcon));
            localTheme.ConfigurePressedArtwork(baseTheme.GetPressedSprite(ShoulderUiRole.PrimaryButton),
                baseTheme.GetPressedSprite(ShoulderUiRole.DangerButton));
            AssetDatabase.CreateAsset(localTheme, LocalThemePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"SHOULDER_VIEW_TINY_SWORDS_LOCAL_THEME_READY path={LocalThemePath}");
        }

        private static void ConfigureSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Texture importer unavailable: {assetPath}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = Vector4.zero;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(LocalRoot + "/" + fileName);
        }

        private static string ReadArgument(string key)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
                if (string.Equals(arguments[index], key, StringComparison.OrdinalIgnoreCase))
                    return arguments[index + 1];
            return null;
        }
    }
}
