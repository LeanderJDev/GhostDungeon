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
    private string debugState = "";

    // Für Pathfinding-Abbruch
    private int failedPathAttempts = 0;
    private const int MaxFailedPathAttempts = 5;

    [Export]
    public int EnemyType;

    [Export]
    public float ShootRange = 100f;

    [Export]
    public float ShootInterval = 1.0f; // Interval in seconds

    [Export]
    public RichTextLabel debugLabel;

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

    // Für Nahkampfangriff
    private float meleeTimer = 0f;
    private const float MeleeRange = 24f;
    private const float MeleeTime = 1.0f;

    // ...entfernt, neue Version weiter unten...
    public override void _PhysicsProcess(double delta)
    {
        debugState = "Nothing";
        EnsurePlayerReference();
        if (player == null)
            return;
        shootTimer -= (float)delta;

        if (HasLineOfSightToPlayer())
        {
            debugState = "Engage Player";
            lastKnownPlayerPosition = player.Position;
            failedPathAttempts = 0; // Reset on sight
            HandleEngagePlayer(delta);
        }
        else if (lastKnownPlayerPosition != null)
        {
            debugState = "Move to Last Position \n" + pathIndex + " / " + currentPath.Count;
            if (MoveToTarget(lastKnownPlayerPosition.Value, delta, 1f))
            {
                lastKnownPlayerPosition = null;
                failedPathAttempts = 0;
                CancelPath();
                debugState = "Wandering (nach Abbruch)";
                WanderRandomly(delta);
            }
            else if (currentPath.Count == 0)
            {
                debugState = "Path Generation Attempt";
                failedPathAttempts++;
                if (failedPathAttempts >= MaxFailedPathAttempts)
                {
                    GD.Print(
                        "Giving up on last known player position after too many failed path attempts."
                    );
                    lastKnownPlayerPosition = null;
                    failedPathAttempts = 0;
                    CancelPath();
                    debugState = "Wandering (nach Fail)";
                    WanderRandomly(delta);
                }
            }
        }
        else
        {
            debugState = "Wandering";
            failedPathAttempts = 0;
            WanderRandomly(delta);
        }
        base._PhysicsProcess(delta);
        QueueRedraw();
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
                debugState = "Melee Attack";
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
                debugState = "Shoot Player";
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
    private bool MoveToTarget(Vector2 target, double delta, float margin = 1f)
    // State-String wird zentral in _PhysicsProcess gesetzt
    {
        // Robustere Pfadverfolgung
        if (NeedsNewPath(target))
        {
            debugState += " Needs New Path";
            currentPath = APlusPathfinder.Instance.Calculate(Position, target);
            pathIndex = 0;
        }
        if (currentPath == null || currentPath.Count == 0)
        {
            debugState += " No Path Available";
            moveDirection = Vector2.Zero;
            return false;
        }

        // Laufe alle Punkte im Pfad ab, bis Ziel erreicht
        while (pathIndex < currentPath.Count)
        {
            Vector2 nextPoint = currentPath[pathIndex];
            Vector2 toNext = nextPoint - Position;
            if (toNext.Length() > margin)
            {
                debugState += " Next Point reached";
                moveDirection = toNext.Normalized();
                return false;
            }
            // Punkt erreicht, zum nächsten
            pathIndex++;
        }
        // Ziel erreicht
        moveDirection = Vector2.Zero;
        currentPath.Clear();
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
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
            GlobalPosition + Vector2.Up * 8f,
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
    // State-String wird zentral in _PhysicsProcess gesetzt
    // State-String wird zentral in _PhysicsProcess gesetzt
    {
        const int minWanderInterval = 1;
        const int maxWanderInterval = 4;
        const int maxAttempts = 10;
        const int maxWanderDistance = 3; // maximale Pfadlänge
        int seed = Position.GetHashCode();
        wanderRng ??= new Random(seed);

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

    // Debug: State-String über dem Enemy anzeigen

    public override void _Draw()
    {
        if (!string.IsNullOrEmpty(debugState))
        {
            debugLabel.Text = debugState;
        }
    }
}
