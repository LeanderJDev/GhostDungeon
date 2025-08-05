using System;
using System.Dynamic;
using Godot;

public partial class Projectile : CharacterBody2D
{
    private int speed = 200;
    public Vector2 direction;
    public int maxBounce = 0;
    private bool _hitGhosts = false;
    public bool hitGhosts
    {
        get { return _hitGhosts; }
        set
        {
            if (value)
                CollisionMask |= (uint)PhysicsLayer.Ghost;
            _hitGhosts = value;
        }
    }

    private uint shooterLayer;
    private double shooterImmunityTime = 0.1; // 50ms
    private double timeSinceShot = 0.0;

    public void SetShooter(PhysicsBody2D shooter)
    {
        timeSinceShot = 0.0;
        shooterLayer = shooter.CollisionLayer;
        CollisionMask &= ~shooterLayer;
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        timeSinceShot += delta;
        Velocity = direction * speed * (float)delta;
        KinematicCollision2D collisionInfo = MoveAndCollide(Velocity);
        if (timeSinceShot >= shooterImmunityTime)
        {
            CollisionMask |= shooterLayer;
        }

        if (collisionInfo != null)
        {
            Node2D body = (Node2D)collisionInfo.GetCollider();
            if (body.Name == "hitbox")
            {
                body = (Node2D)body.GetParent();
            }
            if (body is CharacterController characterController)
            {
                if (!hitGhosts && characterController is GhostController)
                    return;
                characterController.Kill();
                QueueFree();
            }
            else if (body is Projectile projectile)
            {
                projectile.QueueFree();
                QueueFree();
            }
            else
            {
                if (maxBounce == 0)
                    QueueFree();
                direction = direction.Bounce(collisionInfo.GetNormal());
                maxBounce--;
            }
        }
        base._PhysicsProcess(delta);
    }
}
