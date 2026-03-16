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
    public void SortingLayers_GroundSortsBelowForeground()
    {
        int groundValue = SortingLayer.GetLayerValueFromName("Ground");
        int fgValue = SortingLayer.GetLayerValueFromName("Foreground");

        Assert.Less(groundValue, fgValue,
            "Ground should sort below Foreground");
    }
}
