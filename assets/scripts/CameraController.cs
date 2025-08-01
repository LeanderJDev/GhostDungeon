using System;
using System.Runtime;
using Godot;

public partial class CameraController : Camera2D
{
    [Export]
    public int Speed = 10;

    public override void _PhysicsProcess(double delta)
    {
        if (PlayerController.Instance != null && !IsInstanceValid(PlayerController.Instance))
            return;
        base._Process(delta);
        if (PlayerController.Instance == null)
            return;
        Position = Position.Lerp(PlayerController.Instance.Position, (float)(Speed * delta));
    }
}
