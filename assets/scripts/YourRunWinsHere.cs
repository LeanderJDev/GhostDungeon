using System;
using System.Collections.Generic;
using Godot;

public partial class YourRunWinsHere : Control
{
    public static YourRunWinsHere Instance { get; private set; }

    private PlayerPath ghostPath;
    private bool paused = false;

    [Export]
    public GpuParticles2D particles;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        GetChild(0).SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Pause") && paused == true)
        {
            GD.Print("Pause button pressed", paused ? " - Unpausing" : " - Pausing");
            ShowRestartScreen(!paused);
        }
        base._Process(delta);
    }

    //vom Signal aufgerufen!
    private void ButtonPressed()
    {
        YourRunRestartsHere.Instance.ButtonPressed();
    }

    public void PlayerDead(PlayerPath playerPath)
    {
        ShowRestartScreen();
        ghostPath = playerPath;
    }

    public void ShowRestartScreen(bool visible = true)
    {
        if (visible == paused)
            return; // No change in visibility
        if (ghostPath.positions == null)
            MetaMain.Instance.PlaySelectSound();
        paused = visible;

        var child = (Control)GetChild(0);
        GetTree().Paused = paused;

        if (visible)
        {
            particles.Restart();
            particles.Emitting = true;
        }

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
            child.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
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
