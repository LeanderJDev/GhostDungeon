using System;
using System.Collections.Generic;
using Godot;

public partial class MetaMain : Node2D
{
    [Export]
    public PackedScene mainScene;

    [Export]
    public PackedScene ghostScene;

    private static MetaMain Instance;

    private static int respawnNextFrame = -1;
    private List<CharacterPath> ghostPaths = new();

    public MetaMain()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();
        WorldGenerator.Seed = Time.GetDateStringFromSystem().GetHashCode();
        Instance.AddChild(Instance.mainScene.Instantiate());
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (respawnNextFrame == 0)
        {
            respawnNextFrame = -1;
            Node mainScene = Instance.mainScene.Instantiate();
            foreach (CharacterPath ghostPath in ghostPaths)
            {
                GhostController ghost = (GhostController)ghostScene.Instantiate();
                ghost.GlobalPosition = ghostPath.positions[0];
                ghost.ghostPath = ghostPath;
                mainScene.AddChild(ghost);
            }
            Instance.AddChild(mainScene);
        }
        else if (respawnNextFrame > 0)
        {
            respawnNextFrame--;
        }
    }

    public static void SetGhostPath(CharacterPath path)
    {
        Instance.ghostPaths.Add(path);
    }

    public static void RestartWithSameSeed()
    {
        if (Instance.GetChildCount() > 0)
            Instance.GetChild(0).QueueFree();
        respawnNextFrame = 3;
    }

    public static void RestartWithDifferentSeed()
    {
        WorldGenerator.Seed = Random.Shared.Next();
        RestartWithSameSeed();
    }
}
