using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    public AnimatedSprite2D[] sprites;

    [Export]
    public Node2D shootMarker;

    public bool isDead;

    [Export]
    public Node2D itemDisplayContainer;

    [Export]
    public Label characterNameLabel;

    public List<UpgradeItem> collectedUpgrades = [];
    public List<KeyItem> collectedKeys = [];

    public int maxProjectileBounces =>
        collectedUpgrades.FindAll(x => x.upgradeType == UpgradeType.BouncyProjectiles).Count * 2;

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

    public CharacterCustomisation characterPartSelection = new CharacterCustomisation(
        "",
        HairStyle.None,
        ChestStyle.None,
        PantStyle.None,
        FeetStyle.None
    );

    public override void _Ready()
    {
        base._Ready();
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

    protected void UpdateAnimation(Vector2 velocity = default)
    {
        if (isDead || sprites == null || sprites.Length == 0)
            return;

        Vector2 vel;
        if (velocity == default)
            vel = Velocity;
        else
            vel = velocity;
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
            anim = sprites[0].Animation.ToString().Replace("walk_", "idle_");
            flipH = sprites[0].FlipH;
        }
        foreach (AnimatedSprite2D sprite in sprites)
        {
            if (sprite.Animation != anim)
            {
                sprite.Play(anim);
            }
            sprite.FlipH = flipH;
        }
    }

    protected bool OpenWinChest(Vector2I position)
    {
        MetaMain.Instance.Win();
        Vector2I openWinchestAtlasPos = new(20, 1);
        TileMapLayer tilemap = WorldGenerator.Instance.walls;
        if (tilemap.GetCellAtlasCoords(position) == openWinchestAtlasPos)
        {
            return false;
        }
        tilemap.SetCell(position, 0, openWinchestAtlasPos);
        OpenChest(position, false);
        OpenChest(position + new Vector2I(1, 0), false);
        OpenChest(position + new Vector2I(-1, 0), false);
        OpenChest(position + new Vector2I(0, -1), false);
        OpenChest(position + new Vector2I(1, -1), false);
        OpenChest(position + new Vector2I(-1, -1), false);
        return true;
    }

    protected bool OpenChest(Vector2I? position = null, bool placeOpenChest = true)
    {
        if (position != null && placeOpenChest)
        {
            Vector2I openchestAtlasPos = new(15, 6);
            TileMapLayer tilemap = WorldGenerator.Instance.walls;
            if (tilemap.GetCellAtlasCoords(position.Value) == openchestAtlasPos)
            {
                return false;
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
                if (item is KeyItem key)
                {
                    collectedKeys.Add(key);
                }
                if (item is UpgradeItem upgrade)
                {
                    collectedUpgrades.Add(upgrade);
                    GD.Print($"equipped upgrade of type {upgrade.upgradeType}");
                    if (upgrade.upgradeType == UpgradeType.WalkOnWater)
                    {
                        item.Visible = false;
                        CollisionMask &= ~(uint)PhysicsLayer.Water;
                        UpdateSprites();
                    }
                }
                AddItemToDisplay(item);
            }
        }
        return true;
    }

    protected bool OpenDoor(Vector2I position)
    {
        PlayAudio(doorOpenSound);
        TileMapLayer tilemap = WorldGenerator.Instance.walls;
        Vector2I pos = position;
        int color = tilemap.GetCellSourceId(pos);
        GD.Print("color:");
        GD.Print(color);
        KeyItem matchingKey = collectedKeys.FirstOrDefault(x => (int)x.Color == color);
        if (matchingKey != null)
        {
            GD.Print("door opened");
            collectedKeys.Remove(matchingKey);
            UpdateItemDisplay();
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

    protected void ActuallyShoot(Vector2 direction)
    {
        PlayAudio(shootSound);
        Projectile newProjectile = (Projectile)projectile.Instantiate();
        newProjectile.Position = shootMarker.GlobalPosition + direction.Normalized() * 4;
        newProjectile.direction = direction;
        newProjectile.maxBounce = maxProjectileBounces;
        newProjectile.hitGhosts = HasUpgrade(UpgradeType.GhostShoot);
        newProjectile.SetShooter(this);
        GetParent().AddChild(newProjectile);
        string anim = sprites[0].Animation.ToString().Replace("shoot_", "idle_");
        foreach (AnimatedSprite2D sprite in sprites)
        {
            sprite.Play(anim);
        }
    }

    private void PlayShootAnimation(Vector2 direction)
    {
        if (sprites[0] == null)
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
        foreach (AnimatedSprite2D sprite in sprites)
        {
            sprite.Play(anim);
            sprite.FlipH = flipH;
        }
    }

    public virtual void Kill()
    {
        if (isDead)
            return;
        PlayAudio(dieSound);
        isDead = true;
        moveDirection = Vector2.Zero;
    }

    const int keyDisplayWidth = 4;
    const int upgradeDisplayWidth = 6;

    private void AddItemToDisplay(Item item)
    {
        itemDisplayContainer.AddChild(item);
        UpdateItemDisplay();
    }

    private void UpdateItemDisplay()
    {
        itemDisplayContainer.Position = new Vector2(
            -1 * (collectedKeys.Count - 1) * keyDisplayWidth / 2,
            itemDisplayContainer.Position.Y
        );
        for (int i = 0; i < collectedKeys.Count; i++)
        {
            collectedKeys[i].Position = new Vector2(i * keyDisplayWidth, 0);
        }
        // Only display upgrades that are not WalkOnWater
        var displayUpgrades = collectedUpgrades
            .Where(u => u.upgradeType != UpgradeType.WalkOnWater)
            .ToList();
        int upgradeOffset =
            -(int)itemDisplayContainer.Position.X
            - (displayUpgrades.Count - 1) * upgradeDisplayWidth / 2;
        for (int i = 0; i < displayUpgrades.Count; i++)
        {
            displayUpgrades[i].Position = new Vector2(i * upgradeDisplayWidth + upgradeOffset, 24);
        }
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

    public void UpdateSprites()
    {
        foreach (AnimatedSprite2D sprite in sprites)
        {
            sprite.Visible = false;
        }

        characterNameLabel.Text = characterPartSelection.Name;
        sprites[0].Visible = true; // Always show the body

        sprites[(int)characterPartSelection.Hair].Visible = true;
        sprites[(int)characterPartSelection.Chest].Visible = true;
        sprites[(int)characterPartSelection.Pants].Visible = true;
        if (!HasUpgrade(UpgradeType.WalkOnWater))
            sprites[(int)characterPartSelection.Feet].Visible = true;
        else
            sprites[1].Visible = true;
    }

    public bool HasUpgrade(UpgradeType upgradeType)
    {
        return collectedUpgrades.Any(x => x.upgradeType == upgradeType);
    }
}

//15 104 6

// Sprites for hair, chest, pants and shoes
// An enum would be good to keep track of the current selection
public enum CharacterPart
{
    Hair,
    Chest,
    Pants,
    Shoes,
}

// Each part can have multiple sprites, e.g. different hair styles, chest designs, etc
// The selection can be stored in a list or dictionary, where the key is the CharacterPart
// and the value is the index of the selected sprite for that part
public enum HairStyle
{
    None = 0,
    Dark = 2,
    Clown = 3,
}

public enum ChestStyle
{
    None = 0,
    Shirt = 4,
}

public enum PantStyle
{
    None = 0,
    Shirt = 5,
}

public enum FeetStyle
{
    None = 0,
    Socks = 6,
}

public struct CharacterCustomisation
{
    public string Name;
    public HairStyle Hair;
    public ChestStyle Chest;
    public PantStyle Pants;
    public FeetStyle Feet;

    public CharacterCustomisation(
        string name,
        HairStyle hair,
        ChestStyle chest,
        PantStyle pants,
        FeetStyle feet
    )
    {
        Name = name;
        Hair = hair;
        Chest = chest;
        Pants = pants;
        Feet = feet;
    }
}
