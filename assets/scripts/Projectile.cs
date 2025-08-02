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

    private Node2D _shooter;
    private double _shooterImmunityTime = 0.1; // 50ms
    private double _timeSinceShot = 0.0;

    public void SetShooter(Node2D shooter)
    {
        _shooter = shooter;
        _timeSinceShot = 0.0;
    }

    public override void _Ready()
    {
        direction = Transform.X;
        CollisionMask &= ~(1u << 5); // remove hitboxes
        CollisionMask &= ~(1u << 2); // remove entities
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        _timeSinceShot += delta;
        Velocity = direction * speed * (float)delta;
        KinematicCollision2D collisionInfo = MoveAndCollide(Velocity);
        if (_timeSinceShot >= _shooterImmunityTime)
        {
            CollisionMask |= 1 << 5; // hitboxes
            CollisionMask |= 1 << 2; // entities
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
                // Immunität gegen Schützen für 200ms
                if (
                    _shooter != null
                    && characterController == _shooter
                    && _timeSinceShot < _shooterImmunityTime
                )
                    return;
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
                direction = direction.Bounce(collisionInfo.GetNormal());
                maxBounce--;
                if (maxBounce <= 0)
                    QueueFree();
            }
        }
        base._PhysicsProcess(delta);
    }
}
