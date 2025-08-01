using System;
using System.Collections;
using Godot;
using Musikspieler.Scripts.RecordView;

public partial class CharacterController : CharacterBody2D
{
    [Export]
    public PackedScene projectile;

    [Export]
    public int moveSpeed = 150;

    [Export]
    public float lerpSpeed = 0.05f;
    protected Vector2 moveDirection;
    private Vector2 moveAcceleration = Vector2.Zero;
    private float maxAcceleration = 10000;

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = SmoothDamp.Step(
            Velocity,
            moveDirection * moveSpeed,
            ref moveAcceleration,
            lerpSpeed,
            maxAcceleration,
            (float)delta
        );
        MoveAndSlide();
    }

    public void Shoot(Vector2 direction)
    {
        Node2D newProjectile = (Node2D)projectile.Instantiate();
        newProjectile.Position = GlobalPosition + direction * 12;
        newProjectile.Rotation = direction.Angle();
        GetTree().Root.AddChild(newProjectile);
    }

    public void Kill()
    {
        if (this is PlayerController)
        {
            GD.Print("Dead");
        }
        else
        {
            QueueFree();
        }
    }
}
