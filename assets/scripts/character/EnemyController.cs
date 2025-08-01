using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
Raycast Vision
Wandering
*/
public partial class EnemyController : CharacterController
{
    [Export]
    public int EnemyType;

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

    // Für zufälliges Wandern (Deklarationen entfernt, da bereits vorhanden)

    // Letzte bekannte Spielerposition (für Suchen nach Sichtverlust)
    private Vector2? lastKnownPlayerPosition = null;

    private List<Vector2> wanderPath = null;
    private int wanderPathIndex = 0;
    private Random wanderRng = null;
    private float wanderTimer = 0f;
    private float wanderTimeout = 0f;
    private const float MaxWanderTimeout = 5f; // Sekunden bis Abbruch

    // ...entfernt, neue Version weiter unten...
    public override void _PhysicsProcess(double delta)
    {
        EnsurePlayerReference();
        if (player == null)
            return;
        shootTimer -= (float)delta;

        if (HasLineOfSightToPlayer())
        {
            lastKnownPlayerPosition = player.Position;
            HandleEngagePlayer(delta);
        }
        else if (lastKnownPlayerPosition != null)
        {
            if (MoveToTarget(lastKnownPlayerPosition.Value, delta, 2f))
            {
                lastKnownPlayerPosition = null;
                CancelPath();
                WanderRandomly(delta);
            }
        }
        else
        {
            WanderRandomly(delta);
        }
        base._PhysicsProcess(delta);
    }

    // Spieler verfolgen oder schießen
    // ...entfernt, neue Version weiter unten...
    private void HandleEngagePlayer(double delta)
    {
        Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();
        if (EnemyType == 0)
        {
            MoveToTarget(player.Position, delta);
        }
        else if (EnemyType == 1)
        {
            if (distance <= ShootRange)
            {
                CancelPath();
                TryShoot(toPlayer);
            }
            else
            {
                MoveToTarget(player.Position, delta);
            }
        }
    }

    private void TryShoot(Vector2 direction)
    {
        if (shootTimer <= 0f)
        {
            Shoot(direction.Normalized());
            shootTimer = ShootInterval;
        }
    }

    // Allgemeine Zielverfolgung (auch für letzte bekannte Position)
    // Gibt true zurück, wenn Ziel erreicht
    // ...entfernt, neue Version weiter unten...
    private bool MoveToTarget(Vector2 target, double delta, float margin = 2f)
    {
        if (NeedsNewPath(target))
        {
            currentPath = APlusPathfinder.Instance.Calculate(Position, target);
            pathIndex = 0;
            if (currentPath.Count == 0)
            {
                moveDirection = Vector2.Zero;
                return false;
            }
        }
        if (pathIndex >= currentPath.Count)
            pathIndex = currentPath.Count - 1;
        Vector2 targetPosition = currentPath[pathIndex];
        Vector2 direction = targetPosition - Position;
        if (direction.Length() < margin)
        {
            pathIndex++;
            if (pathIndex >= currentPath.Count)
            {
                moveDirection = Vector2.Zero;
                currentPath.Clear();
                return true;
            }
            targetPosition = currentPath[pathIndex];
            direction = targetPosition - Position;
        }
        moveDirection = direction.Normalized();
        return false;
    }

    private bool NeedsNewPath(Vector2 target)
    {
        return currentPath.Count == 0
            || (
                currentPath.Count > 0
                && currentPath.Last().DistanceTo(target) > RepathDistanceThreshold
            );
    }

    // ...entfernt, neue Version weiter unten...
    private void CancelPath()
    {
        currentPath.Clear();
        moveDirection = Vector2.Zero;
    }

    // ...entfernt, neue Version weiter unten...
    // Für Kompatibilität, falls noch irgendwo aufgerufen
    private void MovePath()
    {
        if (currentPath.Count == 0)
            return;
        MoveToTarget(currentPath.Last(), 0);
    }

    // Prüft, ob eine direkte Sichtlinie zum Spieler besteht (Raycast auf Layer 1)
    // ...entfernt, neue Version weiter unten...
    private bool HasLineOfSightToPlayer()
    {
        PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;
        Vector2 playerPos = player.GlobalPosition;
        float radius = 4f;
        Vector2[] offsets = new Vector2[]
        {
            Vector2.Zero,
            new Vector2(-radius, 0),
            new Vector2(radius, 0),
            new Vector2(0, -radius),
            new Vector2(0, radius),
        };
        foreach (Vector2 offset in offsets)
        {
            Vector2 target = playerPos + offset;
            if (RayHitsPlayer(target, space))
                return true;
        }
        return false;
    }

    private bool RayHitsPlayer(Vector2 target, PhysicsDirectSpaceState2D space)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
            GlobalPosition,
            target
        );
        query.CollisionMask = (1 << 1) | (1 << 2);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        Godot.Collections.Dictionary result = space.IntersectRay(query);
        if (result.Count == 0)
            return false;
        if (result.TryGetValue("collider", out Variant collider))
        {
            Node colliderNode = ((Godot.Variant)collider).As<Node>();
            if (colliderNode != null && colliderNode.GetInstanceId() == player.GetInstanceId())
                return true;
        }
        return false;
    }

    // Entfernt, da optimierte Version weiter unten
    private void WanderRandomly(double delta)
    {
        const int minWanderInterval = 1;
        const int maxWanderInterval = 4;
        const int maxAttempts = 10;
        const int maxWanderDistance = 3; // maximale Pfadlänge
        wanderRng ??= new Random();

        if (wanderPath == null || wanderPath.Count == 0)
        {
            if (HandleWanderTimer(delta, minWanderInterval, maxWanderInterval))
                return;
            TryGenerateWanderPath(
                maxAttempts,
                maxWanderDistance,
                minWanderInterval,
                maxWanderInterval
            );
            if (wanderPath == null || wanderPath.Count == 0)
                return;
        }

        wanderTimeout -= (float)delta;
        if (wanderTimeout <= 0f)
        {
            wanderPath = null;
            moveDirection = Vector2.Zero;
            wanderTimer = wanderRng.Next(minWanderInterval, maxWanderInterval);
            return;
        }

        if (wanderPath != null && wanderPathIndex < wanderPath.Count)
        {
            Vector2 target = wanderPath[wanderPathIndex];
            Vector2 dir = target - Position;
            if (dir.Length() < 1f)
            {
                wanderPathIndex++;
                if (wanderPathIndex >= wanderPath.Count)
                {
                    wanderTimer = wanderRng.Next(minWanderInterval, maxWanderInterval);
                    wanderPath = null;
                    moveDirection = Vector2.Zero;
                    return;
                }
                target = wanderPath[wanderPathIndex];
                dir = target - Position;
            }
            moveDirection = dir.Normalized();
        }
        else
        {
            wanderPath = null;
            moveDirection = Vector2.Zero;
        }
    }

    private bool HandleWanderTimer(double delta, int minWanderInterval, int maxWanderInterval)
    {
        if (wanderTimer > 0f)
        {
            wanderTimer -= (float)delta;
            moveDirection = Vector2.Zero;
            return true;
        }
        return false;
    }

    private void TryGenerateWanderPath(
        int maxAttempts,
        int maxWanderDistance,
        int minWanderInterval,
        int maxWanderInterval
    )
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = (float)(wanderRng.NextDouble() * Math.PI * 2);
            Vector2 candidateTarget =
                Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * maxWanderDistance * 16;

            if (!APlusPathfinder.Instance.IsTileWalkable(candidateTarget))
                continue;
            List<Vector2> candidatePath = APlusPathfinder.Instance.Calculate(
                Position,
                candidateTarget
            );
            if (candidatePath != null && candidatePath.Count > 1)
            {
                int cropLen = Math.Min(maxWanderDistance, candidatePath.Count);
                wanderPath = candidatePath.GetRange(0, cropLen);
                wanderPathIndex = 0;
                wanderTimeout = MaxWanderTimeout;
                return;
            }
        }
        wanderPath = null;
        wanderTimer = wanderRng.Next(minWanderInterval, maxWanderInterval);
        moveDirection = Vector2.Zero;
    }

    private void EnsurePlayerReference()
    {
        if (player == null)
        {
            GD.Print("Player not set");
            player = PlayerController.Instance;
        }
    }
}
