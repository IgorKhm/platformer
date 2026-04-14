using UnityEngine;
using UnityEditor;
using System.IO;

public static class SpikeSpriteTool
{
    [MenuItem("Tools/Generate Spike Sprite")]
    public static void Generate()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = Color.clear;
        var spike = new Color(0.8f, 0.2f, 0.2f, 1f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        // Filled triangle: base (1,0)-(30,0), apex (15.5, 31)
        float apexX = 15.5f;
        float apexY = 31f;
        float baseLeft = 1f;
        float baseRight = 30f;

        for (int y = 0; y < size; y++)
        {
            float t = y / apexY;
            int left = Mathf.CeilToInt(Mathf.Lerp(baseLeft, apexX, t));
            int right = Mathf.FloorToInt(Mathf.Lerp(baseRight, apexX, t));
            for (int x = left; x <= right; x++)
                tex.SetPixel(x, y, spike);
        }

        tex.Apply();

        string dir = "Assets/Art/Hazards";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/Spike.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        Debug.Log("Spike sprite generated at " + path);
    }
}
