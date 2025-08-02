using System;
using Godot;

public partial class GhostController : CharacterController
{
    public CharacterPath ghostPath;
    private int pathIndex = 0;
    private Vector2 targetPosition;

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        targetPosition = ghostPath.positions[pathIndex];
        Vector2 direction = targetPosition - Position;
        if (direction.Length() < 10)
            direction = Vector2.Zero;
        moveDirection = direction.Normalized();

        if (ghostPath.actions != null && ghostPath.actions.Count > 0)
        {
            int actionIndex = pathIndex;
            foreach (var action in ghostPath.actions)
            {
                if (action.index == actionIndex)
                {
                    switch (action.action)
                    {
                        case CharacterActionType.Shoot:
                            Shoot(action.direction);
                            break;
                        case CharacterActionType.ItemPickup:
                            CheckForChests();
                            break;
                        case CharacterActionType.DoorOpen:
                            CheckAndUseDoors();
                            break;
                    }
                }
            }
        }
        pathIndex++;
        if (pathIndex >= ghostPath.positions.Count)
        {
            QueueFree();
        }
        base._PhysicsProcess(delta);
    }
}
