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
    private float shootCooldownTime = 0.25f; // Dauer der Shoot-Animation in Sekunden

    [Export]
    public PackedScene projectile;

    [Export]
    public AnimatedSprite2D sprite;

    [Export]
    public AnimatedSprite2D shoes;

    [Export]
    public Node2D shootMarker;

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

    [Export]
    public AudioStreamWav doorOpenSound;

    [Export]
    public AudioStreamWav powerUpSound;

    [Export]
    public AudioStreamWav keyPickupSound;

    [Export]
    public AudioStreamWav dieSound;

    [Export]
    public AudioStreamWav shootSound;

    [Export]
    public AudioStreamPlayer2D audioPlayer;

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
            if (Mathf.Abs(vel.X) * 1.2 > Mathf.Abs(vel.Y))
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
            anim = sprite.Animation.ToString().Replace("walk_", "idle_");
            flipH = sprite.FlipH;
        }
        if (sprite.Animation != anim)
        {
            //sprite.Play(anim);
            shoes.Play(anim);
        }
        sprite.FlipH = flipH;
        shoes.FlipH = flipH;
    }

    private IEnumerable<Vector2I> GetTilesInRadius(TileMapLayer tilemap, Vector2 center, int radius)
    {
        Vector2I centerTile = tilemap.LocalToMap(center);
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2I pos = centerTile + new Vector2I(x, y);
                yield return pos;
            }
        }
    }

    protected Vector2I? CheckForChests(int radius = 1)
    {
        TileMapLayer tilemap = WorldGenerator.Instance.walls;
        Vector2 localPos = tilemap.ToLocal(Position);

        foreach (Vector2I pos in GetTilesInRadius(tilemap, localPos, radius))
        {
            TileData data = tilemap.GetCellTileData(pos);
            if (data != null && (string)data.GetCustomData("tileDescription") == "chest")
            {
                OpenChest(pos);
                return pos;
            }
        }
        return null;
    }

    protected void OpenChest(Vector2I? position = null)
    {
        if (position != null)
        {
            Vector2I openchestAtlasPos = new(15, 6);
            TileMapLayer tilemap = WorldGenerator.Instance.walls;
            if (tilemap.GetCellAtlasCoords(position.Value) == openchestAtlasPos)
            {
                return;
            }
            tilemap.SetCell(position.Value, 0, openchestAtlasPos);
        }

        //generate loot
        Random rand = new(
            position == null ? ((Vector2I)Position).GetHashCode() : position.GetHashCode()
        );
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
                PlayAudio(powerUpSound);
            }
            else
            {
                KeyColor color = possibleKeyColors[rand.Next(possibleKeyColors.Length)];
                loot.Add(KeyItem.Instantiate(color));
            }
        }
        if (!audioPlayer.Playing)
        {
            PlayAudio(keyPickupSound);
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

    protected Vector2I? CheckAndUseDoors(int radius = 1)
    {
        TileMapLayer tilemap = WorldGenerator.Instance.walls;
        Vector2 localPos = tilemap.ToLocal(Position);

        foreach (Vector2I pos in GetTilesInRadius(tilemap, localPos, radius))
        {
            TileData data = tilemap.GetCellTileData(pos);
            if (data != null && (string)data.GetCustomData("tileDescription") == "door")
            {
                GD.Print("door found");
                if (OpenDoor(pos))
                {
                    return pos;
                }
            }
        }
        return null;
    }

    protected bool OpenDoor(Vector2I? position = null)
    {
        PlayAudio(doorOpenSound);
        TileMapLayer tilemap = WorldGenerator.Instance.walls;
        if (position == null)
            return false;
        Vector2I pos = position.Value;
        int color = tilemap.GetCellSourceId(pos);
        GD.Print("color:");
        GD.Print(color);
        KeyItem matchingKey = collectedKeys.FirstOrDefault(x => (int)x.Color == color);
        if (matchingKey != null)
        {
            GD.Print("door opened");
            collectedKeys.Remove(matchingKey);
            ReleaseItemFromDisplay(matchingKey);
            matchingKey.QueueFree();
            bool isDoorVertical = tilemap.GetCellAtlasCoords(pos) == new Vector2I(0, 5);
            Vector2 dst = tilemap.MapToLocal(pos) - tilemap.ToLocal(Position);
            bool isDistanceNegative = isDoorVertical ? dst.X > 0 : dst.Y < 0;
            WorldGenerator.Instance.OpenDoor(pos, isDistanceNegative ? 1 : 2);
            return true;
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
        PlayAudio(shootSound);
        Projectile newProjectile = (Projectile)projectile.Instantiate();
        newProjectile.Position = shootMarker.GlobalPosition + direction.Normalized() * 4;
        newProjectile.direction = direction;
        newProjectile.maxBounce = HasBouncyProjectiles ? maxProjectileBounces : 0;
        newProjectile.hitGhosts = CanHitGhosts;
        newProjectile.SetShooter(this);
        GetParent().AddChild(newProjectile);
        sprite.Animation = "idle_right";
        shoes.Animation = "idle_right";
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
        shoes.Play(anim);
        shoes.FlipH = flipH;
    }

    public virtual void Kill()
    {
        if (isDead)
            return;
        PlayAudio(dieSound);
        isDead = true;
        moveDirection = Vector2.Zero;
    }

    const float itemDisplayWidth = 4;

    private void AddItemToDisplay(KeyItem item)
    {
        item.Reparent(itemDisplayContainer, false);
        float posDelta = (collectedKeys.Count - 1) * itemDisplayWidth;
        item.Position = new Vector2(posDelta, 0);
        itemDisplayContainer.Position -= new Vector2(itemDisplayWidth * 0.5f, 0);
        item.Scale = new Vector2(0.5f, 0.5f);
    }

    private void ReleaseItemFromDisplay(KeyItem item)
    {
        item.Reparent(GetViewport(), false);
        item.GlobalPosition = GlobalPosition;
        itemDisplayContainer.Position += new Vector2(itemDisplayWidth * 0.25f, 0);
        for (int i = 0; i < collectedKeys.Count; i++)
        {
            collectedKeys[i].Position = new Vector2(i * itemDisplayWidth, 0);
        }
        item.Scale = new Vector2(1, 1);
    }

    protected void PlayAudio(AudioStreamWav sound, AudioStreamPlayer2D player = null)
    {
        if (player == null)
        {
            player = audioPlayer;
        }
        if (player == null || sound == null)
            return;
        player.Stream = sound;
        // Random pitch shift between 0.9 and 1.1
        player.PitchScale = (float)GD.RandRange(0.95, 1.05);
        player.Play();
    }
}
//15 104 6