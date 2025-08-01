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

    public CharacterPath playerPath = new CharacterPath
    {
        positions = new List<Vector2>(),
        actions = new List<CharacterAction>(),
    };

    private static PlayerController _instance;
    public static PlayerController Instance => _instance;

    private Vector2 startPosition;

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
                new CharacterAction
                {
                    index = playerPath.positions.Count,
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
                sprite.FlipH = false;
            }
            else
            {
                sprite.FlipH = true;
            }
        }

        if (Input.IsActionJustPressed("Interact"))
        {
            GD.Print("checkfor chests");
            if (CheckForChests())
            {
                GD.Print("chest found");
                playerPath.actions.Add(
                    new CharacterAction
                    {
                        index = playerPath.positions.Count,
                        action = CharacterActionType.ItemPickup,
                        direction = Vector2.Zero,
                    }
                );
            }
            if (CheckAndUseDoors())
            {
                GD.Print("opened door");
                playerPath.actions.Add(
                    new CharacterAction
                    {
                        index = playerPath.positions.Count,
                        action = CharacterActionType.DoorOpen,
                        direction = Vector2.Zero,
                    }
                );
            }
        }

        if (Input.IsActionJustPressed("AntiSoftlock"))
        {
            //testing hsdjkfakd
            OpenChest(Random.Shared.Next());
        }

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        playerPath.positions.Add(Position);
    }
}
