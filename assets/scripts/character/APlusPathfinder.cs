using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;

public partial class APlusPathfinder : Node2D
{
    [Export]
    public TileMapLayer ground;

    [Export]
    public TileMapLayer walls;

    private static APlusPathfinder _instance;
    public static APlusPathfinder Instance => _instance;

    // Für Debugging
    private List<PathNode> lastClosedList = new List<PathNode>();

    private Vector2I targetNode = Vector2I.One * -1;

    public override void _Ready()
    {
        _instance = this;
        base._Ready();
    }

    public List<Vector2> Calculate(Vector2 start, Vector2 target, bool ignoreDoors = false)
    {
        Vector2I startNode = GlobalToMap(start);
        targetNode = GlobalToMap(target);

        if (!IsTileWalkable(start, ignoreDoors) || !IsTileWalkable(target, ignoreDoors))
        {
            return new List<Vector2>();
        }

        // PriorityQueue für offene Knoten
        PriorityQueue<Vector2I, int> openQueue = new PriorityQueue<Vector2I, int>();
        HashSet<Vector2I> closedSet = new HashSet<Vector2I>();
        Dictionary<Vector2I, int> gScore = new Dictionary<Vector2I, int>();
        Dictionary<Vector2I, Vector2I?> cameFrom = new Dictionary<Vector2I, Vector2I?>();

        openQueue.Enqueue(startNode, 0);
        gScore[startNode] = 0;
        cameFrom[startNode] = null;

        // Debug: Liste der geschlossenen Knoten
        lastClosedList.Clear();

        while (openQueue.Count > 0)
        {
            Vector2I current = openQueue.Dequeue();
            if (closedSet.Contains(current))
                continue;
            closedSet.Add(current);
            lastClosedList.Add(new PathNode(current, gScore[current], cameFrom[current]));

            if (current == targetNode)
            {
                // Pfad rekonstruieren
                List<Vector2> path = new List<Vector2>();
                Vector2I? step = current;
                while (step != null)
                {
                    Vector2 tileCenter = MapToGlobal(step.Value);
                    path.Insert(0, tileCenter);
                    step = cameFrom[step.Value];
                }
                // GD.Print("Path found");
                return path;
            }

            List<Vector2I> neighbours = GetWalkableNeighboursSimple(current, ignoreDoors);
            for (int i = 0; i < neighbours.Count; i++)
            {
                Vector2I neighbour = neighbours[i];
                if (closedSet.Contains(neighbour))
                    continue;

                int tentativeG = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbour) || tentativeG < gScore[neighbour])
                {
                    cameFrom[neighbour] = current;
                    gScore[neighbour] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbour, targetNode);
                    openQueue.Enqueue(neighbour, f);
                }
            }
        }
        //GD.Print("Couldn't find Path");
        return new List<Vector2>();
    }

    public struct DebugStruct
    {
        public List<Vector2> path;
        public List<PathNode> closedList;
    }

    public DebugStruct DebugCalculate(Vector2 start, Vector2 target)
    {
        return new DebugStruct
        {
            path = Calculate(start, target, ignoreDoors: true),
            closedList = lastClosedList,
        };
    }

    public bool IsTileWalkable(Vector2 position, bool ignoreDoors = false, bool isLocal = false)
    {
        Vector2I node = (Vector2I)position;
        if (!isLocal)
            node = GlobalToMap(position);
        if (ground.GetCellSourceId(node) == -1)
            return false; // Kein Boden vorhanden
        if (walls.GetCellSourceId(node) > 0 && ignoreDoors)
            return true;

        return WorldGenerator.CheckSpace(walls, ground, node);
    }

    // Neue Nachbarsfunktion für Vector2I
    private List<Vector2I> GetWalkableNeighboursSimple(Vector2I node, bool ignoreDoors = false)
    {
        List<Vector2I> neighbours = new List<Vector2I>();
        for (int i = 0; i < WorldGenerator.neighbourDirections.Length; i++)
        {
            Vector2I direction = WorldGenerator.neighbourDirections[i];
            Vector2I position = node + direction;
            if (IsTileWalkable(position, ignoreDoors, isLocal: true))
            {
                neighbours.Add(position);
            }
        }
        return neighbours;
    }

    // Alte Nachbarsfunktion entfernt, da nicht mehr benötigt

    public int ManhattanDistance(Vector2I a, Vector2I b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    public Vector2I GlobalToMap(Vector2 globalPosition)
    {
        return walls.LocalToMap(walls.ToLocal(globalPosition));
    }

    public Vector2 MapToGlobal(Vector2I mapPosition)
    {
        return walls.ToGlobal(walls.MapToLocal(mapPosition));
    }

    public bool IsWater(Vector2 globalPosition)
    {
        Vector2I mapPosition = GlobalToMap(globalPosition);
        if (ground.GetCellSourceId(mapPosition) == -1)
        {
            return false;
        }
        TileData tileData = ground.GetCellTileData(mapPosition);
        if (tileData == null)
        {
            return false;
        }
        if (tileData.TerrainSet != 0)
        {
            return false;
        }
        if (tileData.Terrain == 0)
        {
            return true;
        }
        return false;
    }
}

public class PathNode
{
    public Vector2I position;
    public int g;
    public Vector2I? predecessor;

    public PathNode(Vector2I position, int g, Vector2I? predecessor)
    {
        this.position = position;
        this.g = g;
        this.predecessor = predecessor;
    }
}
