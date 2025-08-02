using System;
using System.Collections.Generic;
using Godot;

public partial class YourRunRestartsHere : Control
{
    public static YourRunRestartsHere Instance { get; private set; }

    private CharacterPath ghostPath;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        GetChild(0).SetProcess(false);
    }

    //vom Signal aufgerufen!
    private void ButtonPressed()
    {
        GD.Print("restart");
        MetaMain.SetGhostPath(ghostPath);
        MetaMain.Restart();
    }

    public void PlayerDead(CharacterPath playerPath)
    {
        // Hide and disable interaction initially
        var child = (Control)GetChild(0);
        child.Visible = false;
        child.SetProcess(false);

        // Create a timer for the delay (e.g., 0.5 seconds)
        var timer = new Timer();
        timer.WaitTime = 0.5f;
        timer.OneShot = true;
        AddChild(timer);

        timer.Timeout += () =>
        {
            // Show with animation (e.g., fade in)
            var tween = CreateTween();
            child.Visible = true;
            child.Modulate = new Color(1, 1, 1, 0);
            tween.TweenProperty(child, "modulate:a", 1.0f, 0.5f);
            tween.Finished += () =>
            {
                child.SetProcess(true); // Enable interaction after animation
            };
        };
        timer.Start();
        ghostPath = playerPath;
    }

    //vom Signal aufgerufen!
    private void ButtonPressedGiveUp()
    {
        GD.Print("gave up");
        MetaMain.GiveUp();
    }
}
