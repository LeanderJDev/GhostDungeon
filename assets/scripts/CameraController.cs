using System;
using Godot;

public partial class CameraController : Camera2D
{
    [Export]
    public Node2D Target;

    [Export]
    public int Speed = 10;

    public override void _Process(double delta)
    {
        base._Process(delta);
        Position = Position.Lerp(Target.Position, (float)(Speed * delta));
    }
}
