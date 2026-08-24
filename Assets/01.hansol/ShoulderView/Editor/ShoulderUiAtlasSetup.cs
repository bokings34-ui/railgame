using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Railgame.Hansol.ShoulderView.Editor
{
    public static class ShoulderUiAtlasSetup
    {
        private const string AtlasPath =
            "Assets/01.hansol/ShoulderView/UI/Original/RailwayWorkshopAtlas.png";

        public static void ConfigureTheme(ShoulderUiTheme theme)
        {
            if (theme == null || !System.IO.File.Exists(AtlasPath))
                return;
            ConfigureImporter();
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
            if (sprites.Count == 0)
                return;

            theme.ConfigureArtwork(Get(sprites, "WorkshopPanel"), Get(sprites, "UpgradeCard"),
                Get(sprites, "UpgradeCard"), Get(sprites, "WorkshopHeader"), Get(sprites, "PrimaryButton"),
                Get(sprites, "DangerButton"), Get(sprites, "InteractionPrompt"), Get(sprites, "BoltCurrency"));
            EditorUtility.SetDirty(theme);
        }

        private static Sprite Get(IReadOnlyDictionary<string, Sprite> sprites, string name)
        {
            return sprites.TryGetValue(name, out Sprite value) ? value : null;
        }

        private static void ConfigureImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            SpriteDataProviderFactories factories = new();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
                return;
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .Where(rect => !string.IsNullOrEmpty(rect.name))
                .GroupBy(rect => rect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID, StringComparer.Ordinal);
            SpriteRect[] rects =
            {
                Slice("WorkshopHeader", 24f, 463f, 642f, 364f, 58f, existingIds),
                Slice("UpgradeCard", 729f, 427f, 290f, 428f, 42f, existingIds),
                Slice("WorkshopPanel", 1071f, 450f, 303f, 304f, 40f, existingIds),
                Slice("PrimaryButton", 1413f, 510f, 335f, 175f, 42f, existingIds),
                Slice("DangerButton", 107f, 59f, 311f, 315f, 56f, existingIds),
                Slice("FocusBadge", 488f, 36f, 472f, 339f, 0f, existingIds),
                Slice("InteractionPrompt", 950f, 106f, 400f, 209f, 40f, existingIds),
                Slice("BoltCurrency", 1454f, 82f, 206f, 256f, 0f, existingIds)
            };
            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        }

        private static SpriteRect Slice(string name, float x, float y, float width, float height, float border,
            IReadOnlyDictionary<string, GUID> existingIds)
        {
            return new SpriteRect
            {
                name = name,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                rect = new Rect(x, y, width, height),
                border = new Vector4(border, border, border, border),
                spriteID = existingIds.TryGetValue(name, out GUID id) ? id : GUID.Generate()
            };
        }
    }
}
