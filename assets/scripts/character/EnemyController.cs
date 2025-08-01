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
    // Für Debug State-Anzeige

    // Für Pathfinding-Abbruch
    private int failedPathAttempts = 0;
    private const int MaxFailedPathAttempts = 5;

    [Export]
    public int EnemyType;

    [Export]
    public float ShootRange = 100f;

    [Export]
    public float ShootInterval = 1.0f; // Interval in seconds

    private PlayerController player;
    private float shootTimer = 0f;
    public List<Vector2> currentPath = new List<Vector2>();
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

    // Für Nahkampfangriff
    private float meleeTimer = 0f;
    private const float MeleeRange = 24f;
    private const float MeleeTime = 1.0f;

    // Timeout für das Hängenbleiben an einem Pfadpunkt
    private float stuckTimer = 0f;
    private float MaxStuckTime = 1.0f; // Sekunden, bis zum nächsten Punkt gesprungen wird

    public override void _Ready()
    {
        base._Ready();
        EnsurePlayerReference();
        if (wanderRng == null)
        {
            int seed = Position.GetHashCode();
            wanderRng = new Random(seed);
        }
        MaxStuckTime = moveSpeed / 16f;
    }

    public override void _PhysicsProcess(double delta)
    {
        EnsurePlayerReference();
        if (player == null)
            return;
        shootTimer -= (float)delta;

        if (HasLineOfSightToPlayer())
        {
            lastKnownPlayerPosition = player.Position;
            failedPathAttempts = 0; // Reset on sight
            HandleEngagePlayer(delta);
        }
        else if (lastKnownPlayerPosition != null)
        {
            if (MoveToTarget(lastKnownPlayerPosition.Value, delta))
            {
                lastKnownPlayerPosition = null;
                failedPathAttempts = 0;
                CancelPath();
                wanderTimer = 4;
                WanderRandomly(delta);
            }
            else if (currentPath.Count == 0)
            {
                failedPathAttempts++;
                if (failedPathAttempts >= MaxFailedPathAttempts)
                {
                    lastKnownPlayerPosition = null;
                    failedPathAttempts = 0;
                    CancelPath();
                    WanderRandomly(delta);
                }
            }
        }
        else
        {
            failedPathAttempts = 0;
            WanderRandomly(delta);
        }
        base._PhysicsProcess(delta);
    }

    // Spieler verfolgen oder schießen
    // ...entfernt, neue Version weiter unten...
    private void HandleEngagePlayer(double delta)
    // State-String wird zentral in _PhysicsProcess gesetzt
    {
        Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();
        if (EnemyType == 0)
        {
            MoveToTarget(player.Position, delta);
            // Nahkampfangriff
            if (distance <= MeleeRange)
            {
                meleeTimer += (float)delta;
                if (meleeTimer >= MeleeTime)
                {
                    player.Kill();
                    meleeTimer = 0f;
                }
                CancelPath();
                moveDirection = Vector2.Zero;
                return;
            }
            else
            {
                meleeTimer = 0f;
            }
        }
        else if (EnemyType == 1)
        {
            // Fernkampf
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
    private bool MoveToTarget(Vector2 target, double delta)
    // State-String wird zentral in _PhysicsProcess gesetzt
    {
        float margin = 4f; // Toleranz, um Punkt zu erreichen
        // Robustere Pfadverfolgung mit Timeout pro Pfadpunkt
        if (NeedsNewPath(target))
        {
            currentPath = APlusPathfinder.Instance.Calculate(Position, target);
            pathIndex = 0;
            stuckTimer = 0f;
        }
        if (currentPath == null || currentPath.Count == 0)
        {
            moveDirection = Vector2.Zero;
            stuckTimer = 0f;
            return false;
        }

        while (pathIndex < currentPath.Count)
        {
            Vector2 nextPoint = currentPath[pathIndex] + Vector2.Up * 8f; // Offset des Colliders
            Vector2 toNext = nextPoint - Position;
            if (toNext.Length() > margin)
            {
                moveDirection = toNext.Normalized();
                stuckTimer += (float)delta;
                if (stuckTimer > MaxStuckTime)
                {
                    pathIndex++;
                    stuckTimer = 0f;
                    continue; // Versuche direkt den nächsten Punkt
                }
                return false;
            }
            // Punkt erreicht, zum nächsten
            pathIndex++;
            stuckTimer = 0f;
        }
        // Ziel erreicht
        moveDirection = Vector2.Zero;
        currentPath.Clear();
        stuckTimer = 0f;
        return true;
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
        if (RayHitsPlayer(playerPos, space))
            return true;
        return false;
    }

    private bool RayHitsPlayer(Vector2 target, PhysicsDirectSpaceState2D space)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(Position, target);
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
    // State-String wird zentral in _PhysicsProcess gesetzt
    // State-String wird zentral in _PhysicsProcess gesetzt
    {
        const int minWanderInterval = 1;
        const int maxWanderInterval = 4;
        const int maxAttempts = 10;
        const int maxWanderDistance = 3; // maximale Pfadlänge

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
            player = PlayerController.Instance;
        }
    }
}
