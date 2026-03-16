using NUnit.Framework;
using UnityEngine;

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
    public void SortingLayers_GroundSortsBelowDefault()
    {
        int groundValue = SortingLayer.GetLayerValueFromName("Ground");
        int defaultValue = SortingLayer.GetLayerValueFromName("Default");

        Assert.Less(groundValue, defaultValue,
            "Ground should sort below Default (player renders on Default)");
    }

    [Test]
    public void SortingLayers_DefaultSortsBelowForeground()
    {
        int defaultValue = SortingLayer.GetLayerValueFromName("Default");
        int fgValue = SortingLayer.GetLayerValueFromName("Foreground");

        Assert.Less(defaultValue, fgValue,
            "Default should sort below Foreground");
    }
}
