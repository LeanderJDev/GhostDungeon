using System;
using System.Collections.Generic;
using Godot;

public partial class MetaMain : Node2D
{
    [Export]
    public PackedScene mainScene;

    [Export]
    public PackedScene ghostScene;

    [Export]
    public CanvasLayer mainMenu;

    [Export]
    public AudioStreamWav selectSound;

    [Export]
    public AudioStreamPlayer uiPlayer;

    public static MetaMain Instance;

    private static int respawnNextFrame = -1;
    private List<CharacterPath> ghostPaths = new();

    private Node currentMainScene;

    public MetaMain()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();
        mainMenu.Visible = true;
        mainMenu.SetProcess(true);
        WorldGenerator.Seed = Time.GetUnixTimeFromSystem().GetHashCode();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (respawnNextFrame == 0)
        {
            respawnNextFrame = -1;
            if (Instance.currentMainScene != null)
            {
                GD.Print("aaaaaaaaaaaaaaaaaaaaaaaaaahhhhhhhhh jhjfasjhfajkhdsadjkhakjhsdfgakjsf");
            }

            currentMainScene = Instance.mainScene.Instantiate();
            foreach (CharacterPath ghostPath in ghostPaths)
            {
                GhostController ghost = (GhostController)ghostScene.Instantiate();
                ghost.GlobalPosition = ghostPath.positions[0];
                ghost.ghostPath = ghostPath;
                currentMainScene.AddChild(ghost);
                GD.Print("Added Ghost");
            }
            GetTree().Paused = false; // Unpause the game after respawn
            Instance.AddChild(currentMainScene);
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

    public static void Restart()
    {
        Instance.currentMainScene?.QueueFree();
        Instance.currentMainScene = null;
        respawnNextFrame = 3;
    }

    public static void GiveUp()
    {
        Instance._GiveUp();
    }

    public void _GiveUp()
    {
        GD.PrintErr("gave up");
        WorldGenerator.Seed = Time.GetDatetimeStringFromSystem().GetHashCode();
        mainMenu.Visible = true;
        mainMenu.SetProcess(true);
        ghostPaths.Clear();
        currentMainScene.QueueFree();
        Instance.currentMainScene = null;
    }

    public void OnMainMenuStart()
    {
        PlaySelectSound();
        mainMenu.Visible = false;
        mainMenu.SetProcess(false);
        Restart();
    }

    public void OnMainMenuQuit()
    {
        PlaySelectSound();
        GD.Print("Quitting game...");
        while (uiPlayer.IsPlaying()) { } // Wait for sound to finish
        GetTree().Quit();
    }

    public void PlaySelectSound()
    {
        if (uiPlayer != null && selectSound != null)
        {
            uiPlayer.Stream = selectSound;
            uiPlayer.Play();
        }
        else
        {
            GD.PrintErr("UI Player or Select Sound not set in MetaMain.");
        }
    }

    public void Win()
    {
        GD.Print("You win!");
        YourRunWinsHere.Instance.ShowRestartScreen(true);
    }
}
