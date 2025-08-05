using System;
using System.Collections.Generic;
using Godot;

public partial class GhostController : CharacterController
{
    public GhostPath ghostPath;
    private int pathIndex = 0;
    private Vector2 targetPosition;

    [Export]
    public Node2D ghostSprite;

    private Vector2 ghostPosition;

    public override void _Ready()
    {
        if (ghostSprite == null)
        {
            GD.PrintErr("Ghost sprite not set in GhostController.");
            return;
        }
        if (ghostPath.actions == null)
        {
            ghostPath.actions = new Dictionary<int, CharacterAction>();
        }
        ghostPosition = Position;
        characterPartSelection = ghostPath.characterCustomisation;
        UpdateSprites();
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        float lagSpeed = 2f; // je kleiner, desto mehr "Lag"
        Vector2 prePosition = ghostSprite.GlobalPosition;
        ghostPosition = ghostPosition.Lerp(Position, (float)(delta * lagSpeed));
        ghostSprite.GlobalPosition = ghostPosition;
        UpdateAnimation(ghostSprite.GlobalPosition - prePosition);
    }

    public override void _PhysicsProcess(double delta)
    {
        targetPosition = ghostPath.positions[pathIndex];
        Vector2 direction = targetPosition - Position;
        if (direction.Length() < 5)
            direction = Vector2.Zero;
        moveDirection = direction.Normalized();

        if (ghostPath.actions != null && ghostPath.actions.TryGetValue(pathIndex, out var action))
        {
            switch (action.action)
            {
                case CharacterActionType.Shoot:
                    Shoot(action.direction);
                    break;
                case CharacterActionType.ItemPickup:
                case CharacterActionType.AntiSoftlock:
                    OpenChest((Vector2I)action.direction);
                    break;
                case CharacterActionType.DoorOpen:
                    OpenDoor((Vector2I)action.direction);
                    break;
            }
        }

        pathIndex++;
        if (pathIndex >= ghostPath.positions.Length)
        {
            QueueFree();
        }

        base._PhysicsProcess(delta);
    }

    public override void Kill()
    {
        base.Kill();
        QueueFree();
    }
}
