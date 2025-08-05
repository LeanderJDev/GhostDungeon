using System;
using System.Collections.Generic;
using Godot;

/*
Items:
Abpraller (Langsamer, aber kann an Wänden abprallen)
Ghost Kill (Kann Geister töten)
Über Wasser Laufen/Schweben
*/

public partial class PlayerController : CharacterController
{
    [Export]
    public PackedScene ghost;

    [Export]
    public bool immortal;

    [Export]
    public AudioStreamWav stepSound;

    [Export]
    public AudioStreamWav waterStepSound;

    [Export]
    public AudioStreamPlayer2D stepPlayer;

    [Export]
    public Marker2D feet;

    public PlayerPath playerPath = new PlayerPath
    {
        positions = new List<Vector2>(),
        actions = new Dictionary<int, CharacterAction>(),
        characterCustomisation = new CharacterCustomisation(),
    };

    private static PlayerController _instance;
    public static PlayerController Instance => _instance;

    private Vector2 startPosition;

    [Signal]
    public delegate void upgradesChangedEventHandler();

    public override void _Ready()
    {
        _instance = this;
        startPosition = Position;
        base._Ready();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _instance = null;
    }

    public override void _Process(double delta)
    {
        if (isDead)
            return;

        Vector2 direction = Input.GetVector("Left", "Right", "Up", "Down");

        moveDirection = direction;

        if (Input.IsActionJustPressed("Shoot"))
        {
            Vector2 mouseGlobalPos = GetGlobalMousePosition();
            Vector2 shootDirection = (mouseGlobalPos - GlobalPosition).Normalized();
            Shoot(shootDirection);
            playerPath.actions.Add(
                playerPath.positions.Count,
                new CharacterAction
                {
                    action = CharacterActionType.Shoot,
                    direction = shootDirection,
                }
            );
        }
        if (isShooting)
        {
            Vector2 mouseGlobalPos = GetGlobalMousePosition();
            queuedShootDirection = (mouseGlobalPos - GlobalPosition).Normalized();
            if (queuedShootDirection.X > 0)
            {
                foreach (AnimatedSprite2D sprite in sprites)
                {
                    sprite.FlipH = false;
                }
            }
            else
            {
                foreach (AnimatedSprite2D sprite in sprites)
                {
                    sprite.FlipH = true;
                }
            }
        }

        if (Input.IsActionJustPressed("Interact"))
        {
            Vector2I? doorPosition = CheckAndDoInteraction("door", OpenDoor);
            if (doorPosition != null)
            {
                GD.Print("opened door");
                playerPath.actions.Add(
                    playerPath.positions.Count,
                    new CharacterAction
                    {
                        action = CharacterActionType.DoorOpen,
                        direction = doorPosition.Value,
                    }
                );
            }
            else
            {
                Vector2I? chestPosition = CheckAndDoInteraction("winchest", OpenWinChest, 2);
                if (chestPosition == null)
                {
                    chestPosition = CheckAndDoInteraction("chest", pos => OpenChest(pos));
                }
                if (chestPosition != null)
                {
                    GD.Print("chest found");
                    playerPath.actions.Add(
                        playerPath.positions.Count,
                        new CharacterAction
                        {
                            action = CharacterActionType.ItemPickup,
                            direction = chestPosition.Value,
                        }
                    );
                    EmitSignal(SignalName.upgradesChanged);
                }
            }
        }

        if (Input.IsActionJustPressed("AntiSoftlock"))
        {
            //testing hsdjkfakd
            OpenChest(APlusPathfinder.Instance.GlobalToMap(GlobalPosition));
            playerPath.actions.Add(
                playerPath.positions.Count,
                new CharacterAction
                {
                    action = CharacterActionType.AntiSoftlock,
                    direction = APlusPathfinder.Instance.GlobalToMap(GlobalPosition),
                }
            );
            EmitSignal(SignalName.upgradesChanged);
        }

        base._Process(delta);
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

    private Vector2I? CheckAndDoInteraction(
        string tileDescription,
        Func<Vector2I, bool> callback,
        int radius = 1
    )
    {
        TileMapLayer tilemap = WorldGenerator.Instance.walls;
        Vector2 localPos = tilemap.ToLocal(Position);
        Vector2 mouseDirection = (GetGlobalMousePosition() - GlobalPosition).Normalized();

        Vector2I? bestPos = null;
        float bestAngle = float.MaxValue;

        foreach (Vector2I pos in GetTilesInRadius(tilemap, localPos, radius))
        {
            TileData data = tilemap.GetCellTileData(pos);
            if (data != null && (string)data.GetCustomData("tileDescription") == tileDescription)
            {
                Vector2 worldPos = tilemap.MapToLocal(pos);
                Vector2 toTile = (worldPos - GlobalPosition).Normalized();
                float angle = Mathf.Abs(mouseDirection.AngleTo(toTile));
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestPos = pos;
                }
            }
        }

        if (bestPos != null)
        {
            if (callback(bestPos.Value))
                return bestPos;
        }
        return null;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        playerPath.positions.Add(Position);
        if (Velocity.Length() > 0.1f && !stepPlayer.Playing)
        {
            if (APlusPathfinder.Instance.IsWater(feet.GlobalPosition))
            {
                PlayAudio(waterStepSound, stepPlayer);
            }
            else
            {
                PlayAudio(stepSound, stepPlayer);
            }
        }
    }

    public override void Kill()
    {
        base.Kill();
        GD.Print("Dead");
        if (immortal == true)
        {
            isDead = false;
            return;
        }
        playerPath.characterCustomisation = characterPartSelection;
        YourRunRestartsHere.Instance.PlayerDead(playerPath);
    }
}
