using System;
using System.Collections.Generic;
using Godot;

public partial class YourRunRestartsHere : Control
{
    public static YourRunRestartsHere Instance { get; private set; }

    [Export]
    public Label restartLabel;

    [Export]
    public Control closeButton;

    private PlayerPath ghostPath;
    private bool paused = false;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        GetChild(0).SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Pause") && ghostPath.positions == null)
        {
            GD.Print("Pause button pressed", paused ? " - Unpausing" : " - Pausing");
            ShowRestartScreen(!paused);
        }
        base._Process(delta);
    }

    //vom Signal aufgerufen!
    public void ButtonPressed()
    {
        MetaMain.Instance.PlaySelectSound();
        GD.Print("restart");
        PlayerController.Instance.Kill();
        MetaMain.SetGhostPath(ghostPath);
        MetaMain.Restart();
    }

    public void PlayerDead(PlayerPath playerPath)
    {
        ghostPath = playerPath;
        restartLabel.Text = $"You died.";
        closeButton.Visible = false;
        ShowRestartScreen();
    }

    public void ShowRestartScreen(bool visible = true)
    {
        if (paused && ghostPath.positions != null)
            return;
        if (visible == paused)
            return; // No change in visibility
        paused = visible;
        if (ghostPath.positions == null)
            MetaMain.Instance.PlaySelectSound();

        var child = (Control)GetChild(0);
        GetTree().Paused = paused;

        // Create a timer for the delay (e.g., 0.5 seconds)
        var timer = new Timer();
        timer.WaitTime = 0.2f;
        timer.OneShot = true;
        AddChild(timer);

        timer.Timeout += () =>
        {
            // Show with animation (e.g., fade in)
            var tween = CreateTween();
            child.Visible = visible;
            child.Modulate = new Color(1, 1, 1, 0);
            tween.TweenProperty(child, "modulate:a", 1.0f, 0.5f);
            tween.Finished += () =>
            {
                child.SetProcess(visible); // Enable interaction after animation
            };
        };
        timer.Start();
    }

    //vom Signal aufgerufen!
    private void ButtonPressedGiveUp()
    {
        MetaMain.Instance.PlaySelectSound();
        GD.Print("gave up");
        MetaMain.GiveUp();
    }
}
