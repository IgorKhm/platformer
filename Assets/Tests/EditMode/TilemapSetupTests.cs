using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[TestFixture]
public class TilemapSetupTests
{
    [Test]
    public void SortingLayers_ContainBackgroundGroundForeground()
    {
        var layers = SortingLayer.layers;
        bool hasBackground = false;
        bool hasGround = false;
        bool hasForeground = false;

        foreach (var layer in layers)
        {
            if (layer.name == "Background") hasBackground = true;
            if (layer.name == "Ground") hasGround = true;
            if (layer.name == "Foreground") hasForeground = true;
        }

        Assert.IsTrue(hasBackground, "Missing 'Background' sorting layer");
        Assert.IsTrue(hasGround, "Missing 'Ground' sorting layer");
        Assert.IsTrue(hasForeground, "Missing 'Foreground' sorting layer");
    }

    [Test]
    public void SortingLayers_BackgroundSortsBelowGround()
    {
        int bgValue = SortingLayer.GetLayerValueFromName("Background");
        int groundValue = SortingLayer.GetLayerValueFromName("Ground");

        Assert.Less(bgValue, groundValue,
            "Background should sort below Ground");
    }

    [Test]
    public void SortingLayers_GroundSortsBelowForeground()
    {
        int groundValue = SortingLayer.GetLayerValueFromName("Ground");
        int fgValue = SortingLayer.GetLayerValueFromName("Foreground");

        Assert.Less(groundValue, fgValue,
            "Ground should sort below Foreground");
    }

    [Test]
    public void GenerateTileAssets_CreatesSpriteFiles()
    {
        TilemapSetup.GenerateTileAssets();

        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tilesets/GroundTile.png"),
            "GroundTile sprite not found");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tilesets/PlatformTile.png"),
            "PlatformTile sprite not found");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Tilesets/WallTile.png"),
            "WallTile sprite not found");
    }

    [Test]
    public void GenerateTileAssets_CreatesTileAssets()
    {
        TilemapSetup.GenerateTileAssets();

        var ground = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/GroundTile.asset");
        var platform = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/PlatformTile.asset");
        var wall = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/WallTile.asset");

        Assert.IsNotNull(ground, "GroundTile asset not found");
        Assert.IsNotNull(platform, "PlatformTile asset not found");
        Assert.IsNotNull(wall, "WallTile asset not found");
    }

    [Test]
    public void GenerateTileAssets_TilesHaveSpritesAssigned()
    {
        TilemapSetup.GenerateTileAssets();

        var ground = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/GroundTile.asset");
        var platform = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/PlatformTile.asset");
        var wall = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tilemaps/WallTile.asset");

        Assert.IsNotNull(ground.sprite, "GroundTile has no sprite");
        Assert.IsNotNull(platform.sprite, "PlatformTile has no sprite");
        Assert.IsNotNull(wall.sprite, "WallTile has no sprite");
    }

    [Test]
    public void GenerateTileAssets_SpritesHaveCorrectPPU()
    {
        TilemapSetup.GenerateTileAssets();

        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Tilesets/GroundTile.png");

        Assert.AreEqual(32, importer.spritePixelsPerUnit, "PPU should be 32");
        Assert.AreEqual(FilterMode.Point, importer.filterMode, "Filter should be Point");
        Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression,
            "Compression should be Uncompressed");
    }
}
