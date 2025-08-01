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

    public override void _Process(double delta)
    {
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
                    action = 0,
                    direction = shootDirection,
                }
            );
        }

        if (Input.IsActionJustPressed("ui_accept"))
        {
            Node2D newGhost = (Node2D)ghost.Instantiate();
            GhostController ghostController = (GhostController)newGhost;
            // Clone the playerPath to avoid sharing the same reference
            CharacterPath clonedPath = new CharacterPath
            {
                positions = new List<Vector2>(playerPath.positions),
                actions = new List<CharacterAction>(playerPath.actions),
            };
            ghostController.ghostPath = clonedPath;
            newGhost.Position = startPosition;
            GetTree().Root.AddChild(newGhost);
        }

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        playerPath.positions.Add(Position);
    }
}
