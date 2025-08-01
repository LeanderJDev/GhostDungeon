using System;
using Godot;

[Tool]
public partial class DoorDuplication : Node2D
{
    [Export]
    public TileSet tileSet;

    [Export]
    public Texture2D[] doorTextures;

    [Export]
    public bool SetupDoorsButton
    {
        get => false;
        set
        {
            if (value)
                SetupDoors();
        }
    }

    public void SetupDoors()
    {
        if (tileSet == null)
            return;

        // The second tilesetsource is one of the doors
        // this script should duplicate that source and replace the texture
        if (tileSet.GetSourceCount() < 2)
        {
            GD.PrintErr("TileSet does not have enough sources to duplicate doors.");
            return;
        }
        TileSetSource originalSource = tileSet.GetSource(1);
        if (originalSource == null)
        {
            GD.PrintErr("Original door source is null.");
            return;
        }
        for (int i = 0; i < doorTextures.Length; i++)
        {
            TileSetAtlasSource newSource = (TileSetAtlasSource)originalSource.Duplicate();
            newSource.Texture = doorTextures[i];
            tileSet.AddSource(newSource, i + 2);
            GD.Print($"Added door source: {newSource}");
        }
    }
}
