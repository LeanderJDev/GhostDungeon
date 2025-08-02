using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Musikspieler.Scripts.RecordView;

public partial class CharacterController : CharacterBody2D
{
    protected bool isShooting = false;
    protected Vector2 queuedShootDirection = Vector2.Zero;
    private float shootCooldown = 0.0f;
    private float shootCooldownTime = 0.4f; // Dauer der Shoot-Animation in Sekunden

    [Export]
    public PackedScene projectile;

    [Export]
    public AnimatedSprite2D sprite;

    public bool isDead;

    [Export]
    public Node2D itemDisplayContainer;

    public List<UpgradeItem> collectedUpgrades = [];
    public List<KeyItem> collectedKeys = [];

    public bool CanWalkOnWater =>
        collectedUpgrades.Any(x => x.upgradeType == UpgradeType.WalkOnWater);
    public bool CanHitGhosts => collectedUpgrades.Any(x => x.upgradeType == UpgradeType.GhostShoot);
    public bool HasBouncyProjectiles =>
        collectedUpgrades.Any(x => x.upgradeType == UpgradeType.BouncyProjectiles);

    public int maxProjectileBounces = 3;

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
        itemDisplayContainer.Position += new Vector2(itemDisplayWidth * 0.5f, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Blockiere Bewegung und Animation während Schuss
        if (isShooting)
        {
            shootCooldown -= (float)delta;
            if (shootCooldown <= 0.0f)
            {
                isShooting = false;
                // Jetzt Projektil erzeugen
                ActuallyShoot(queuedShootDirection);
                queuedShootDirection = Vector2.Zero;
            }
            // Keine Bewegung/Animation während Schuss
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        Velocity = SmoothDamp.Step(
            Velocity,
            moveDirection * moveSpeed,
            ref moveAcceleration,
            lerpSpeed,
            maxAcceleration,
            (float)delta
        );
        MoveAndSlide();
        // Animationen setzen
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (isDead || sprite == null)
            return;

        Vector2 vel = Velocity;
        string anim = "idle_down";
        bool flipH = false;

        if (vel.Length() > 5f)
        {
            // Laufanimation
            if (Mathf.Abs(vel.X) > Mathf.Abs(vel.Y))
            {
                // Horizontal dominiert
                if (vel.X > 0)
                {
                    anim = "walk_right";
                    flipH = false;
                }
                else
                {
                    anim = "walk_right";
                    flipH = true;
                }
            }
            else
            {
                // Vertikal dominiert
                if (vel.Y > 0)
                {
                    anim = "walk_down";
                }
                else
                {
                    anim = "walk_up";
                }
            }
        }
        else
        {
            // Idle
            Vector2 dir = moveDirection;
            if (dir.Length() < 0.1f)
                dir = Vector2.Down; // Default
            if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
            {
                if (dir.X > 0)
                {
                    anim = "idle_right";
                    flipH = false;
                }
                else
                {
                    anim = "idle_right";
                    flipH = true;
                }
            }
            else
            {
                if (dir.Y > 0)
                {
                    anim = "idle_down";
                }
                else
                {
                    anim = "idle_up";
                }
            }
        }

        if (sprite.Animation != anim)
            sprite.Play(anim);
        sprite.FlipH = flipH;
    }

    protected bool CheckForChests()
    {
        Vector2I openchestAtlasPos = new(15, 6);

        TileMapLayer tilemap = WorldGenerator.Instance.walls;

        Vector2I tilePos = tilemap.LocalToMap(tilemap.ToLocal(Position));
        foreach (Vector2I pos in tilemap.GetSurroundingCells(tilePos).Concat(new[] { tilePos }))
        {
            int id = tilemap.GetCellSourceId(pos);
            TileData data = tilemap.GetCellTileData(pos);
            if (data != null && (string)data.GetCustomData("tileDescription") == "chest")
            {
                tilemap.SetCell(pos, id, openchestAtlasPos);
                OpenChest(pos.GetHashCode());
                return true;
            }
        }
        return false;
    }

    protected void OpenChest(int seed)
    {
        //generate loot
        Random rand = new(seed);
        List<Item> loot = new();

        int itemCount = rand.Next(1, 4); // 1-3 items

        // First two items are always keys (random color except white)
        KeyColor[] possibleKeyColors =
        {
            KeyColor.Red,
            KeyColor.Turquoise,
            KeyColor.Green,
            KeyColor.Violet,
        };
        for (int i = 0; i < Math.Min(2, itemCount); i++)
        {
            KeyColor color = possibleKeyColors[rand.Next(possibleKeyColors.Length)];
            loot.Add(KeyItem.Instantiate(color));
        }

        // Third item (if any) has a chance to be an upgrade
        if (itemCount == 2)
        {
            bool giveUpgrade = rand.NextDouble() < 0.5; // 70% chance for upgrade, adjust as needed
            if (giveUpgrade)
            {
                UpgradeType[] upgrades =
                {
                    UpgradeType.BouncyProjectiles,
                    UpgradeType.WalkOnWater,
                    UpgradeType.GhostShoot,
                };
                UpgradeType upgrade = upgrades[rand.Next(upgrades.Length)];
                loot.Add(UpgradeItem.Instantiate(upgrade));
            }
            else
            {
                KeyColor color = possibleKeyColors[rand.Next(possibleKeyColors.Length)];
                loot.Add(KeyItem.Instantiate(color));
            }
        }

        foreach (Item item in loot)
        {
            if (item != null)
            {
                AddChild(item);
                if (item is KeyItem key)
                {
                    collectedKeys.Add(key);
                    AddItemToDisplay(key);
                }
                if (item is UpgradeItem upgrade)
                {
                    collectedUpgrades.Add(upgrade);
                    GD.Print($"equipped upgrade of type {upgrade.upgradeType}");
                    if (upgrade.upgradeType == UpgradeType.WalkOnWater)
                    {
                        CollisionMask = 1 << 1;
                    }
                }
            }
        }
    }

    protected bool CheckAndUseDoors()
    {
        TileMapLayer tilemap = WorldGenerator.Instance.walls;

        Vector2I tilePos = tilemap.LocalToMap(tilemap.ToLocal(Position));
        foreach (Vector2I pos in tilemap.GetSurroundingCells(tilePos))
        {
            TileData data = tilemap.GetCellTileData(pos);
            if (data != null && (string)data.GetCustomData("tileDescription") == "door")
            {
                GD.Print("door found");
                int color = tilemap.GetCellSourceId(pos);
                GD.Print("color:");
                GD.Print(color);
                KeyItem matchingKey = collectedKeys.FirstOrDefault(x => (int)x.Color == color);
                if (matchingKey != null)
                {
                    GD.Print("door opened");
                    collectedKeys.Remove(matchingKey);
                    ReleaseItemToDisplay(matchingKey);
                    matchingKey.QueueFree();
                    bool isDoorVertical = tilemap.GetCellAtlasCoords(pos) == new Vector2I(0, 5);
                    Vector2 dst = tilemap.MapToLocal(pos) - tilemap.ToLocal(Position);
                    bool isDistanceNegative = isDoorVertical ? dst.X > 0 : dst.Y < 0;
                    WorldGenerator.Instance.OpenDoor(pos, isDistanceNegative ? 1 : 2);
                    return true;
                }
            }
        }
        return false;
    }

    public void Shoot(Vector2 direction)
    {
        if (isShooting)
            return;
        isShooting = true;
        shootCooldown = shootCooldownTime;
        queuedShootDirection = direction;
        PlayShootAnimation(direction);
    }

    private void ActuallyShoot(Vector2 direction)
    {
        Projectile newProjectile = (Projectile)projectile.Instantiate();
        float offset = Mathf.Lerp(8, 16, Mathf.Clamp(direction.Normalized().Dot(Vector2.Up), 0, 1));
        offset = 4;
        newProjectile.Position = GlobalPosition + direction.Normalized() * offset;
        newProjectile.Rotation = direction.Angle();
        newProjectile.maxBounce = HasBouncyProjectiles ? maxProjectileBounces : 0;
        newProjectile.hitGhosts = CanHitGhosts;
        newProjectile.SetShooter(this);
        GetParent().AddChild(newProjectile);
    }

    private void PlayShootAnimation(Vector2 direction)
    {
        if (sprite == null)
            return;
        string anim = "shoot_right";
        bool flipH = false;

        if (direction.X > 0)
        {
            flipH = false;
        }
        else
        {
            flipH = true;
        }
        sprite.Play(anim);
        sprite.FlipH = flipH;
    }

    public void Kill()
    {
        if (isDead)
            return;
        isDead = true;
        moveDirection = Vector2.Zero;
        if (this is PlayerController player)
        {
            GD.Print("Dead");
            if (player.immortal != true)
            {
                YourRunRestartsHere.Instance.PlayerDead(player.playerPath);
            }
        }
        else
        {
            QueueFree();
        }
    }

    const float itemDisplayWidth = 4;

    private void AddItemToDisplay(KeyItem item)
    {
        item.Reparent(itemDisplayContainer, false);
        float posDelta = collectedKeys.Count * itemDisplayWidth;
        item.Position = new Vector2(posDelta, 0);
        itemDisplayContainer.Position -= new Vector2(itemDisplayWidth * 0.5f, 0);
        item.Scale = new Vector2(0.5f, 0.5f);
    }

    private void ReleaseItemToDisplay(KeyItem item)
    {
        item.Reparent(GetViewport(), false);
        item.GlobalPosition = GlobalPosition;
        itemDisplayContainer.Position += new Vector2(itemDisplayWidth * 0.5f, 0);
        for (int i = 0; i < collectedKeys.Count; i++)
        {
            collectedKeys[i].Position = new Vector2(i * itemDisplayWidth, 0);
        }
        item.Scale = new Vector2(1, 1);
    }
}
