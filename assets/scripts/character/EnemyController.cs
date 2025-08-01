using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
Raycast Vision
*/
public partial class EnemyController : CharacterController
{
    [Export]
    public int EnemyType;

    [Export]
    public float DetectionRange = 200f;

    [Export]
    public float ShootRange = 100f;

    [Export]
    public float ShootInterval = 1.0f; // Interval in seconds

    private PlayerController player;
    private float shootTimer = 0f;
    private List<Vector2> currentPath = new List<Vector2>();
    private int pathIndex = 0;
    private float lastPlayerPathTargetDistance = 0f;
    private const float RepathDistanceThreshold = 32f;

    public override void _Ready()
    {
        if (player == null)
        {
            player = PlayerController.Instance;
        }
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null)
        {
            GD.Print("Player not set");
            player = PlayerController.Instance;
        }
        Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();

        shootTimer -= (float)delta;

        if (distance <= DetectionRange)
        {
            switch (EnemyType)
            {
                case 0: // Walk towards player
                    MovePath();
                    break;
                case 1: // Shoot towards player
                    if (distance <= ShootRange)
                    {
                        CancelPath();
                        if (shootTimer <= 0f)
                        {
                            Shoot(toPlayer.Normalized());
                            shootTimer = ShootInterval;
                        }
                    }
                    else
                    {
                        MovePath();
                    }
                    break;
            }
        }
        else
        {
            CancelPath();
        }

        base._PhysicsProcess(delta);
    }

    private void CancelPath()
    {
        currentPath = new List<Vector2>();
        moveDirection = Vector2.Zero;
    }

    private void MovePath()
    {
        float margin = 2f;
        // Wenn kein Pfad vorhanden, neuen berechnen
        if (currentPath.Count == 0)
        {
            currentPath = APlusPathfinder.Instance.Calculate(Position, player.Position);
            pathIndex = 0;
            if (currentPath.Count == 0)
                return;
        }

        // Zielpunkt bestimmen
        if (pathIndex >= currentPath.Count)
            pathIndex = currentPath.Count - 1;
        Vector2 targetPosition = currentPath[pathIndex];

        // Die Distanz für Repath wird zum letzten Punkt des Pfads (dem Ziel) gemessen
        Vector2 pathTarget = currentPath.Last();
        float playerToPathTarget = player.Position.DistanceTo(pathTarget);
        if (playerToPathTarget > RepathDistanceThreshold)
        {
            // Spieler ist zu weit weg vom Ziel des aktuellen Pfads, neuen Pfad berechnen
            currentPath = APlusPathfinder.Instance.Calculate(Position, player.Position);
            pathIndex = 0;
            if (currentPath.Count == 0)
            {
                moveDirection = Vector2.Zero;
                return;
            }
            targetPosition = currentPath[pathIndex];
        }

        Vector2 direction = targetPosition - Position;
        if (direction.Length() < margin)
        {
            pathIndex++;
            if (pathIndex >= currentPath.Count)
            {
                // Ziel erreicht
                moveDirection = Vector2.Zero;
                currentPath.Clear();
                return;
            }
            targetPosition = currentPath[pathIndex];
            direction = targetPosition - Position;
        }
        moveDirection = direction.Normalized();
    }
}
