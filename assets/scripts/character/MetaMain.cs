using System;
using System.Collections.Generic;
using Godot;

public partial class MetaMain : Node2D
{
    [Export]
    public PackedScene mainScene;

    private static MetaMain Instance;

    private static bool respawnNextFrame;

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
        if (respawnNextFrame)
        {
            respawnNextFrame = false;
            Instance.AddChild(Instance.mainScene.Instantiate());
        }
    }

    public static void RestartWithSameSeed()
    {
        if (Instance.GetChildCount() > 0)
            Instance.GetChild(0).QueueFree();
        respawnNextFrame = true;
    }

    public static void RestartWithDifferentSeed()
    {
        WorldGenerator.Seed = Random.Shared.Next();
        RestartWithSameSeed();
    }
}
