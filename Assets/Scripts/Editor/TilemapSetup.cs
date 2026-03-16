using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.IO;

public static class TilemapSetup
{
    private struct TileDefinition
    {
        public string name;
        public Color color;
    }

    private static readonly TileDefinition[] TileDefinitions = new[]
    {
        new TileDefinition { name = "GroundTile",   color = new Color(0.35f, 0.35f, 0.35f) },
        new TileDefinition { name = "PlatformTile", color = new Color(0.50f, 0.50f, 0.50f) },
        new TileDefinition { name = "WallTile",     color = new Color(0.40f, 0.42f, 0.45f) },
    };

    private const string SpritePath = "Assets/Art/Tilesets";
    private const string TileAssetPath = "Assets/Tilemaps";
    private const int PixelsPerUnit = 32;
    private const int TextureSize = 32;

    [MenuItem("Tools/Tilemap/Generate Tile Assets")]
    public static void GenerateTileAssets()
    {
        EnsureDirectoryExists(SpritePath);
        EnsureDirectoryExists(TileAssetPath);

        foreach (var def in TileDefinitions)
        {
            var sprite = CreateTileSprite(def.name, def.color);
            CreateTileAsset(def.name, sprite);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Tile assets generated successfully! Check Assets/Art/Tilesets/ and Assets/Tilemaps/");
    }

    private static Sprite CreateTileSprite(string tileName, Color color)
    {
        // Create a solid-color 32x32 texture
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        var pixels = new Color[TextureSize * TextureSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();

        // Save as PNG
        string pngPath = $"{SpritePath}/{tileName}.png";
        File.WriteAllBytes(pngPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        // Import with correct settings
        AssetDatabase.ImportAsset(pngPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

    private static void CreateTileAsset(string tileName, Sprite sprite)
    {
        string assetPath = $"{TileAssetPath}/{tileName}.asset";

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = Color.white;

        AssetDatabase.CreateAsset(tile, assetPath);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
