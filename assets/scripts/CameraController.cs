using System;
using System.Runtime;
using Godot;

public partial class CameraController : Camera2D
{
    [Export]
    public int Speed = 10;
    private Node2D Target;

    public override void _Process(double delta)
    {
        if (Target == null)
        {
            Target = PlayerController.Instance;
        }
        base._Process(delta);
        Position = Position.Lerp(Target.Position, (float)(Speed * delta));
    }
}
