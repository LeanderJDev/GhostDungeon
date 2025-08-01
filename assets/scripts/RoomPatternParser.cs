using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class RoomPatternParser : Node2D
{
    [Export]
    public TileMapLayer walls;

    [Export]
    public Vector2I roomSize = new Vector2I(12, 12);

    private TileSet tileSet;

    [Export]
    public bool SetupPatternsButton
    {
        get => false;
        set
        {
            if (value)
                SetupPatterns();
        }
    }

    public void SetupPatterns()
    {
        if (walls == null)
            return;

        tileSet = walls.TileSet;

        Array<Vector2I> squareRoom = new Array<Vector2I>();
        for (int x = 0; x < roomSize.X; x++)
        {
            for (int y = 0; y < roomSize.Y; y++)
            {
                squareRoom.Add(new Vector2I(x, y));
            }
        }

        while (tileSet.GetPatternsCount() > 0)
        {
            tileSet.RemovePattern(0);
        }

        Queue<Vector2I> positionQueue = new Queue<Vector2I>();
        positionQueue.Enqueue(Vector2I.Zero);

        int maxIterations = 100;

        while (positionQueue.Count > 0)
        {
            Vector2I position = positionQueue.Dequeue();
            GD.Print(position);
            if (walls.GetCellSourceId(position + roomSize - Vector2I.One) == -1)
                continue;

            Array<Vector2I> offsetRoom = new Array<Vector2I>();
            foreach (var cell in squareRoom)
            {
                offsetRoom.Add(cell + position);
            }
            tileSet.AddPattern(walls.GetPattern(offsetRoom));
            Vector2I rightPos = position + Vector2I.Right * (roomSize - Vector2I.One);
            Vector2I downPos = position + Vector2I.Down * (roomSize - Vector2I.One);

            if (!positionQueue.Contains(rightPos))
                positionQueue.Enqueue(rightPos);
            if (!positionQueue.Contains(downPos))
                positionQueue.Enqueue(downPos);
            maxIterations--;
            if (maxIterations == 0)
                break;
        }
    }
}
