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
    private float shootTimer = 1.0f; // Initialisiert auf den ShootInterval-Wert
    public Vector2[] currentPath = Array.Empty<Vector2>();
    private int pathIndex = 0;
    private float lastPlayerPathTargetDistance = 0f;
    private const float RepathDistanceThreshold = 32f;
    private bool hasLineOfSightToPlayer = false;

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
    private const float MeleeRange = 16f;
    private const float MeleeTime = 0.5f;

    // Timeout für das Hängenbleiben an einem Pfadpunkt
    private float stuckTimer = 0f;
    private float MaxStuckTime = 1.0f; // Sekunden, bis zum nächsten Punkt gesprungen wird
    private const int MaxActiveDistance = 16 * 48;

    public override void _Ready()
    {
        base._Ready();
        EnsurePlayerReference();
        if (wanderRng == null)
        {
            int seed = APlusPathfinder.Instance.GlobalToMap(Position).GetHashCode();
            wanderRng = new Random(seed);
        }
        MaxStuckTime = moveSpeed / 16f;
        SmallDelay();
    }

    private async void SmallDelay()
    {
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
    }

    public override void _PhysicsProcess(double delta)
    {
        EnsurePlayerReference();
        if (player == null)
            return;
        if (player.Position.DistanceTo(Position) > MaxActiveDistance)
        {
            // Deaktivieren, wenn zu weit weg
            moveDirection = Vector2.Zero;
            return;
        }
        shootTimer -= (float)delta;
        hasLineOfSightToPlayer = HasLineOfSightToPlayer();

        if (hasLineOfSightToPlayer)
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
            else if (currentPath.Length == 0)
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
            currentPath =
                APlusPathfinder.Instance.Calculate(Position, target)?.ToArray()
                ?? Array.Empty<Vector2>();
            pathIndex = 0;
            stuckTimer = 0f;
        }
        if (currentPath == null || currentPath.Length == 0)
        {
            moveDirection = Vector2.Zero;
            stuckTimer = 0f;
            return false;
        }

        while (pathIndex < currentPath.Length)
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
        currentPath = Array.Empty<Vector2>();
        stuckTimer = 0f;
        return true;
    }

    private bool NeedsNewPath(Vector2 target)
    {
        return currentPath.Length == 0
            || (
                currentPath.Length > 0
                && currentPath.Last().DistanceTo(target) > RepathDistanceThreshold
            );
    }

    private void CancelPath()
    {
        currentPath = Array.Empty<Vector2>();
        moveDirection = Vector2.Zero;
    }

    private void MovePath()
    {
        if (currentPath.Length == 0)
            return;
        MoveToTarget(currentPath.Last(), 0);
    }

    private bool HasLineOfSightToPlayer()
    {
        PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;
        Vector2 playerPos = player.shootMarker.GlobalPosition;
        if (RayHitsPlayer(playerPos, space))
            return true;
        return false;
    }

    private bool RayHitsPlayer(Vector2 target, PhysicsDirectSpaceState2D space)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
            shootMarker.GlobalPosition,
            target
        );
        query.CollisionMask = (uint)PhysicsLayer.Player | (uint)PhysicsLayer.World;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        Godot.Collections.Dictionary result = space.IntersectRay(query);
        if (result.Count == 0)
            return false;
        Variant colliderVariant = result["collider"];
        object colliderObj = colliderVariant.Obj;
        if (
            colliderObj is PhysicsBody2D collider
            && (collider.CollisionLayer & (uint)PhysicsLayer.Player) != 0
        )
            return true;
        return false;
    }

    // Entfernt, da optimierte Version weiter unten
    private void WanderRandomly(double delta)
    {
        const int minWanderInterval = 2;
        const int maxWanderInterval = 5;
        const int maxAttempts = 5;
        const int maxWanderDistance = 6; // maximale Pfadlänge

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
            int x = wanderRng.Next(-maxWanderDistance, maxWanderDistance + 1);
            int y = wanderRng.Next(-maxWanderDistance, maxWanderDistance + 1);
            Vector2I candidateTarget =
                APlusPathfinder.Instance.GlobalToMap(Position)
                + Vector2I.Up * y
                + Vector2I.Right * x;

            if (!APlusPathfinder.Instance.IsTileWalkable(candidateTarget))
                continue;
            List<Vector2> candidatePath = APlusPathfinder.Instance.Calculate(
                Position,
                APlusPathfinder.Instance.MapToGlobal(candidateTarget)
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
        GD.PrintErr("Failed to generate wander path after " + maxAttempts + " attempts.");
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

    public override void Kill()
    {
        base.Kill();
        Vector2I tilePosition = APlusPathfinder.Instance.GlobalToMap(GlobalPosition);
        Random random = new Random(tilePosition.GetHashCode());
        if (random.NextDouble() < 0.3)
        {
            WorldGenerator.Instance.SpawnChest(tilePosition);
        }

        QueueFree();
    }
}
