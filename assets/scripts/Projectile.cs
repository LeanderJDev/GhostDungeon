using System;
using System.Dynamic;
using Godot;

public partial class Projectile : CharacterBody2D
{
    private int speed = 200;
    private Vector2 direction;
    public int maxBounce;
    private bool _hitGhosts = false;
    public bool hitGhosts
    {
        get { return _hitGhosts; }
        set
        {
            if (value)
                CollisionMask |= 1 << 4;
            _hitGhosts = value;
        }
    }

    public override void _Ready()
    {
        direction = Transform.X;
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = direction * speed * (float)delta;
        KinematicCollision2D collisionInfo = MoveAndCollide(Velocity);

        if (collisionInfo != null)
        {
            Node2D body = (Node2D)collisionInfo.GetCollider();
            if (body is CharacterController characterController)
            {
                if (!hitGhosts && characterController is GhostController)
                    return;
                characterController.Kill();
                QueueFree();
            }
            else
            {
                direction = direction.Bounce(collisionInfo.GetNormal());
                maxBounce--;
                if (maxBounce <= 0)
                    QueueFree();
            }
        }
        base._PhysicsProcess(delta);
    }
}
