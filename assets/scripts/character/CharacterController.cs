using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Musikspieler.Scripts.RecordView;

public partial class CharacterController : CharacterBody2D
{
    [Export]
    public PackedScene projectile;

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
        Velocity = SmoothDamp.Step(
            Velocity,
            moveDirection * moveSpeed,
            ref moveAcceleration,
            lerpSpeed,
            maxAcceleration,
            (float)delta
        );
        MoveAndSlide();
    }

    protected bool CheckForChests()
    {
        Vector2I openchestAtlasPos = new(15, 6);

        TileMapLayer tilemap = WorldGenerator.Instance.walls;

        Vector2I tilePos = tilemap.LocalToMap(tilemap.ToLocal(Position));
        foreach (Vector2I pos in tilemap.GetSurroundingCells(tilePos))
        {
            int id = tilemap.GetCellSourceId(pos);
            TileData data = tilemap.GetCellTileData(pos);
            if (data != null && (string)data.GetCustomData("tileDescription") == "chest")
            {
                tilemap.SetCell(pos, id, openchestAtlasPos);
                OpenChest(pos.X * pos.Y);
                return true;
            }
        }
        return false;
    }

    protected void OpenChest(int seed)
    {
        //generate loot
        Random rand = new(seed);
        Item item = (rand.Next() % 7) switch //weiße schlüssel ausgeschlossen
        {
            0 => KeyItem.Instantiate(KeyColor.Red),
            1 => KeyItem.Instantiate(KeyColor.Turquoise),
            2 => KeyItem.Instantiate(KeyColor.Green),
            3 => KeyItem.Instantiate(KeyColor.Violet),
            4 => UpgradeItem.Instantiate(UpgradeType.BouncyProjectiles),
            5 => UpgradeItem.Instantiate(UpgradeType.WalkOnWater),
            6 => UpgradeItem.Instantiate(UpgradeType.GhostShoot),
            7 => KeyItem.Instantiate(KeyColor.White),
            _ => null,
        };

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
        Projectile newProjectile = (Projectile)projectile.Instantiate();
        newProjectile.Position = GlobalPosition + direction * 12;
        newProjectile.Rotation = direction.Angle();
        newProjectile.maxBounce = HasBouncyProjectiles ? maxProjectileBounces : 0;
        newProjectile.hitGhosts = CanHitGhosts;
        GetTree().Root.AddChild(newProjectile);
    }

    public void Kill()
    {
        isDead = true;
        if (this is PlayerController player)
        {
            GD.Print("Dead");
            if (player.immortal != true)
            {
                YourRunRestartsHere.Instance.PlayerDead();
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
