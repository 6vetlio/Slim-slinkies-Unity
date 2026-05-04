using UnityEngine;
using UnityEditor;
using System.IO;

public class ConfigureSpriteImportSettings : Editor
{
    [MenuItem("Tools/Configure Hyperloop Sprites")]
    public static void ConfigureHyperloopSprites()
    {
        string basePath = "Assets/CarParallax/Hyperloop";
        
        ConfigureSprite($"{basePath}/Sky.png", false);
        ConfigureSprite($"{basePath}/Cloud.png", false);
        ConfigureSprite($"{basePath}/Skyline.png", false);
        ConfigureSprite($"{basePath}/SpaceNeedle.png", false);
        ConfigureSprite($"{basePath}/Buildings.png", false);
        ConfigureSprite($"{basePath}/GrassLayer3.png", false);
        ConfigureSprite($"{basePath}/GrassLayer2.png", false);
        ConfigureSprite($"{basePath}/GrassLayer1.png", false);
        ConfigureSprite($"{basePath}/GrassBack.png", false);
        ConfigureSprite($"{basePath}/Tubes.png", false);
        ConfigureSprite($"{basePath}/TubeFrontClear.png", false);
        ConfigureSprite($"{basePath}/Grass.png", false);
        ConfigureSprite($"{basePath}/Rails.png", false);
        ConfigureSprite($"{basePath}/Logo.png", false);
        ConfigureSprite($"{basePath}/Interreg.png", false);
        ConfigureSprite($"{basePath}/pink.png", false);
        ConfigureSprite($"{basePath}/DarkHyperloopTrain.png", false);
        ConfigureSprite($"{basePath}/Skyscrapers.png", false);
        ConfigureSprite($"{basePath}/EvilFuckingBuilding.png", false);
        ConfigureSprite($"{basePath}/HyperloopTrain.png", false);
        ConfigureSprite($"{basePath}/Tunnel.png", false);
        ConfigureSprite($"{basePath}/TubeFront.png", false);
        ConfigureSprite($"{basePath}/Silhouettes.png", false);
        
        ConfigureSpriteSheet($"{basePath}/HyperloopTrain-Sheet.png", 2, 1, 272, 32);
        ConfigureSpriteSheet($"{basePath}/Bird-Sheet.png", 6, 1, 16, 16);
        ConfigureSpriteSheet($"{basePath}/TrainAnimations/BulletTrainAnimation-Sheet.png", 12, 1, 363, 48);
        ConfigureSpriteSheet($"{basePath}/TrainAnimations/ArrivaAnimation-Sheet.png", 12, 1, 272, 48);
        
        string uiPath = $"{basePath}/UI";
        ConfigureSprite($"{uiPath}/widebutton.png", false);
        ConfigureSprite($"{uiPath}/basic_textbox_1.png", false);
        ConfigureSprite($"{uiPath}/buyUI.png", false);
        ConfigureSprite($"{uiPath}/button.png", false);
        
        AssetDatabase.Refresh();
        Debug.Log("Hyperloop sprites configured successfully!");
    }
    
    static void ConfigureSprite(string assetPath, bool isMultiple)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogWarning($"Sprite not found: {assetPath}");
            return;
        }
        
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = isMultiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 1;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        
        importer.SaveAndReimport();
    }
    
    static void ConfigureSpriteSheet(string assetPath, int columns, int rows, int spriteWidth, int spriteHeight)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogWarning($"Sprite sheet not found: {assetPath}");
            return;
        }
        
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 1;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = false;
        
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
        
        SpriteMetaData[] spritesheet = new SpriteMetaData[columns * rows];
        int index = 0;
        
        for (int y = rows - 1; y >= 0; y--)
        {
            for (int x = 0; x < columns; x++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{index}";
                meta.rect = new Rect(x * spriteWidth, y * spriteHeight, spriteWidth, spriteHeight);
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);
                spritesheet[index] = meta;
                index++;
            }
        }
        
        importer.spritesheet = spritesheet;
        importer.SaveAndReimport();
    }
}
