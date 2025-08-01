using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
Features:
Gegner
Items
Traps

Löcher füllen
*/

public partial class WorldGenerator : Node2D
{
    [Export]
    public TileMapLayer walls;

    [Export]
    public Vector2I roomSize = new Vector2I(12, 12);

    [Export]
    public int roomCount = 8;

    // [Export] Feld für die relativen Wahrscheinlichkeiten der Nachbarn
    [Export]
    public int[] neighbourWeights = [2, 3, 5, 2, 1]; // Index = Nachbarn, Wert = Gewicht

    [Export]
    public PackedScene[] enemies;

    [Export]
    public Vector2[] horizontalDoorCoords;

    [Export]
    public Vector2[] verticalDoorCoords;

    [Export]
    public Vector2I rockCoords = new Vector2I(0, 0);

    private int generatedRoomCount = 0;

    private TileSet tileSet;
    private TileMapPattern groundSquarePattern;
    private TileMapPattern[] roomChoices;
    private Queue<Vector2I> generationQueue = new();
    private List<Vector2I> generatedRooms = new();
    private Random random = new();

    public static readonly Vector2I[] neighbourDirections = new Vector2I[]
    {
        new Vector2I(0, -1), // North
        new Vector2I(1, 0), // East
        new Vector2I(0, 1), // South
        new Vector2I(-1, 0), // West
    };

    public static readonly Vector2I[] eightNeighbourDirections = new Vector2I[]
    {
        new Vector2I(0, 1),
        new Vector2I(1, 1),
        new Vector2I(1, 0),
        new Vector2I(1, -1),
        new Vector2I(0, -1),
        new Vector2I(-1, -1),
        new Vector2I(-1, 0),
        new Vector2I(-1, 1),
    };

    public override void _Ready()
    {
        base._Ready();
        if (walls == null)
        {
            GD.PrintErr("TileMapLayer 'walls' is not set.");
            return;
        }
        tileSet = walls.TileSet;
        if (tileSet == null)
        {
            GD.PrintErr("TileSet is not set in TileMapLayer 'walls'.");
            return;
        }
        groundSquarePattern = tileSet.GetPattern(0);

        int roomChoiceCount = tileSet.GetPatternsCount() - 1;
        roomChoices = Enumerable
            .Range(1, roomChoiceCount)
            .Select(i => tileSet.GetPattern(i))
            .ToArray();
        if (roomChoices.Length == 0)
        {
            GD.PrintErr("No room patterns found in TileSet.");
            return;
        }

        StartGeneration();
    }

    int tries;

    private void StartGeneration()
    {
        tries = 5;
        generatedRooms.Clear();
        GenerateRooms(Vector2I.Zero);
    }

    private void GenerateRooms(Vector2I position)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        GD.Print("Generating Rooms at: ", position);
        walls.Clear();
        generatedRoomCount = 0;

        GD.Print("Target: ", roomCount);
        generationQueue.Clear();
        generationQueue.Enqueue(position);

        while (generationQueue.Count > 0)
        {
            Vector2I pos = generationQueue.Dequeue();
            if (!GenerateRoom(pos))
                continue;
            generatedRooms.Add(pos);
            generatedRoomCount++;
            if (generatedRoomCount == roomCount)
                break;
        }
        sw.Stop();
        GD.Print($"[Time] GenerateRooms: {sw.ElapsedMilliseconds} ms");
        if (generatedRoomCount != roomCount)
        {
            tries--;
            GD.PrintErr($"Failed. Only generated: {generatedRoomCount}, Tries left: {tries}");
            if (tries > 0)
                GenerateRooms(position);
            else
            {
                GD.PrintErr("No Tries left, Fallback");
                neighbourWeights = [0, 0, 1, 1, 1];
                tries = 5;
                GenerateRooms(position);
            }
        }
        else
        {
            GD.Print($"Generated: {generatedRoomCount}");
            PostGeneration();
        }
    }

    private void PostGeneration()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        CloseDoors();
        sw.Stop();
        GD.Print($"[Time] CloseDoors: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        FillGaps();
        sw.Stop();
        GD.Print($"[Time] FillGaps: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        UpdateDoors();
        sw.Stop();
        GD.Print($"[Time] UpdateDoors: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        UpdateTiles();
        sw.Stop();
        GD.Print($"[Time] UpdateTiles: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        SpawnEnemies();
        sw.Stop();
        GD.Print($"[Time] SpawnEnemies: {sw.ElapsedMilliseconds} ms");

        GD.Print("Done");
    }

    private void CloseDoors()
    {
        GD.Print("Closing Doors");
        foreach (Vector2I room in generatedRooms)
        {
            foreach (Vector2I dir in neighbourDirections)
            {
                Vector2I neighbourPos = room + dir * (roomSize - Vector2I.One);
                if (!generatedRooms.Contains(neighbourPos))
                    CloseDoor(room, dir);
            }
        }
    }

    private void CloseDoor(Vector2I roomPosition, Vector2I direction)
    {
        direction = direction + Vector2I.One;
        // Calculate door position based on direction
        int doorStartX =
            roomPosition.X + direction.X * (roomSize.X / 2) - direction.X / 2 - direction.X % 2;
        int doorEndX = doorStartX + 1 * direction.X % 2;
        int doorStartY =
            roomPosition.Y + direction.Y * (roomSize.Y / 2) - direction.Y / 2 - direction.Y % 2;
        int doorEndY = doorStartY + 1 * direction.Y % 2;

        // Close the door by setting wall tiles in the door area
        for (int x = doorStartX; x <= doorEndX; x++)
        {
            for (int y = doorStartY; y <= doorEndY; y++)
            {
                walls.SetCell(
                    new Vector2I(x, y),
                    walls.GetCellSourceId(roomPosition),
                    walls.GetCellAtlasCoords(roomPosition)
                );
            }
        }
    }

    private void FillGaps()
    {
        GD.Print("Filling Gaps");
        foreach (Vector2I room in generatedRooms)
        {
            foreach (Vector2I dir in eightNeighbourDirections)
            {
                Vector2I neighbourPos = room + dir * (roomSize - Vector2I.One);
                for (int x = neighbourPos.X; x < neighbourPos.X + roomSize.X; x++)
                {
                    for (int y = neighbourPos.Y; y < neighbourPos.Y + roomSize.Y; y++)
                    {
                        Vector2I pos = new Vector2I(x, y);
                        if (walls.GetCellSourceId(pos) == -1)
                            walls.SetCell(pos, 0, rockCoords);
                    }
                }
            }
        }
    }

    private void UpdateDoors()
    {
        GD.Print("Updating Doors");
        foreach (Vector2I room in generatedRooms)
        {
            foreach (Vector2I dir in neighbourDirections)
            {
                Vector2I neighbourPos = room + dir * (roomSize - Vector2I.One);
                if (generatedRooms.Contains(neighbourPos))
                    UpdateDoor(room, dir);
            }
        }
    }

    private void UpdateDoor(Vector2I roomPosition, Vector2I direction)
    {
        direction = direction + Vector2I.One;
        // Calculate door position based on direction
        int doorStartX =
            roomPosition.X + direction.X * (roomSize.X / 2) - direction.X / 2 - direction.X % 2;
        int doorStartY =
            roomPosition.Y + direction.Y * (roomSize.Y / 2) - direction.Y / 2 - direction.Y % 2;

        SetDoor(
            new Vector2I(doorStartX, doorStartY),
            new Vector2I(direction.X % 2, direction.Y % 2)
        );
    }

    private void SpawnEnemies()
    {
        GD.Print("Spawning Enemies");
        if (enemies == null || enemies.Length == 0)
        {
            GD.Print("No enemies to spawn");
            return;
        }
        foreach (Vector2I room in generatedRooms)
        {
            int enemyCount = random.Next(4);
            while (enemyCount > 0)
            {
                int x = random.Next(1, roomSize.X - 1);
                int y = random.Next(1, roomSize.Y - 1);
                Vector2I localPos = new Vector2I(x, y);
                Vector2I worldPos = room + localPos;
                if (!CheckSpace(walls, worldPos))
                    continue;
                if (enemies.Length > 0)
                {
                    var enemyScene = enemies[random.Next(enemies.Length)];
                    var enemy = enemyScene.Instantiate<Node2D>();
                    enemy.Position = worldPos * walls.TileSet.TileSize + Position;
                    GetParent().CallDeferred("add_child", enemy);
                    enemyCount--;
                }
            }
        }
    }

    public static bool CheckSpace(TileMapLayer tileMap, Vector2I position)
    {
        TileData wallTileData = tileMap.GetCellTileData(position);
        if (wallTileData != null)
        {
            if (wallTileData.GetCollisionPolygonsCount(0) != 0)
            {
                return false;
            }
        }
        return true;
    }

    // Neue Struktur für Terrain-Batching
    private Dictionary<(int terrainSet, int terrain), HashSet<Vector2I>> terrainUpdateBatches =
        new();

    // Prüft, ob ein Tile eine Tür ist (anhand der DoorCoords)
    private bool IsDoorTile(Vector2I position)
    {
        int sourceId = walls.GetCellSourceId(position);
        if (sourceId != 1)
            return false;
        Vector2I atlas = walls.GetCellAtlasCoords(position);
        foreach (var v in horizontalDoorCoords)
            if ((Vector2)v == (Vector2)atlas)
                return true;
        foreach (var v in verticalDoorCoords)
            if ((Vector2)v == (Vector2)atlas)
                return true;
        return false;
    }

    // Fügt ein Tile der Batch-Liste hinzu, ruft aber nicht direkt SetCellsTerrainConnect
    private void BatchTileForTerrain(Vector2I position)
    {
        var wallTileData = walls.GetCellTileData(position);
        if (wallTileData == null)
            return;
        if (wallTileData.TerrainSet == -1 || wallTileData.Terrain == -1)
            return;
        var key = (wallTileData.TerrainSet, wallTileData.Terrain);
        if (!terrainUpdateBatches.TryGetValue(key, out var set))
        {
            set = new HashSet<Vector2I>();
            terrainUpdateBatches[key] = set;
        }
        set.Add(position);
    }

    // This will be called by the PlayerController to open a door
    public bool OpenDoor(Vector2 worldPosition, int direction)
    {
        Vector2I localPosition = walls.LocalToMap(walls.ToLocal(worldPosition));
        int sourceId = walls.GetCellSourceId(localPosition);

        if (sourceId == -1)
            return false; // No tile at this position
        Vector2I atlasCoords = walls.GetCellAtlasCoords(localPosition);
        int horizontalDoorIndex = System.Array.IndexOf(horizontalDoorCoords, (Vector2)atlasCoords);
        int verticalDoorIndex = System.Array.IndexOf(verticalDoorCoords, (Vector2)atlasCoords);

        if (horizontalDoorIndex == 0)
        {
            SetDoor(localPosition, Vector2I.Right, direction);
            return true;
        }
        if (horizontalDoorIndex == 1)
        {
            SetDoor(localPosition - Vector2I.Right, Vector2I.Right, direction);
            return true;
        }
        if (verticalDoorIndex == 0)
        {
            SetDoor(localPosition, Vector2I.Down, direction);
            return true;
        }
        if (verticalDoorIndex == 1)
        {
            SetDoor(localPosition - Vector2I.Down, Vector2I.Down, direction);
            return true;
        }
        return false;
    }

    private void SetDoor(Vector2I position, Vector2I direction, int open = 0)
    {
        Vector2[] doorCoords = direction.X == 0 ? verticalDoorCoords : horizontalDoorCoords;

        walls.SetCell(new Vector2I(position.X, position.Y), 1, (Vector2I)doorCoords[open * 2]);
        walls.SetCell(
            new Vector2I(position.X + direction.X, position.Y + direction.Y),
            1,
            (Vector2I)doorCoords[open * 2 + 1]
        );
        // Statt direktem Update: Tiles im Umfeld batchen (ohne Türtiles, keine Duplikate)
        for (int x = position.X - 1; x <= position.X + direction.X + 1; x++)
        {
            for (int y = position.Y - 1; y <= position.Y + direction.Y + 1; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                if (!IsDoorTile(pos))
                    BatchTileForTerrain(pos);
            }
        }
        walls.SetCell(new Vector2I(position.X, position.Y), 1, (Vector2I)doorCoords[open * 2]);
        walls.SetCell(
            new Vector2I(position.X + direction.X, position.Y + direction.Y),
            1,
            (Vector2I)doorCoords[open * 2 + 1]
        );
    }

    private bool GenerateRoom(Vector2I position)
    {
        if (walls.GetCellSourceId(position + Vector2I.One) != -1)
            return false;
        int index = random.Next(roomChoices.Length);
        TileMapPattern roomChoice = roomChoices[index];
        int rotation = random.Next(4);
        TileMapPattern rotatedRoom = RotatePattern(roomChoice, rotation);
        DrawRoom(position, rotatedRoom);

        int neighbourCount = 0;
        int totalWeight = neighbourWeights.Sum();
        int rand = random.Next(totalWeight);
        int cumulative = 0;
        for (int i = 0; i < neighbourWeights.Length; i++)
        {
            cumulative += neighbourWeights[i];
            if (rand < cumulative)
            {
                neighbourCount = i;
                break;
            }
        }
        List<Vector2I> directions = neighbourDirections
            .OrderBy(x => random.Next())
            .Take(neighbourCount)
            .ToList();
        foreach (Vector2I dir in directions)
        {
            generationQueue.Enqueue(position + dir * (roomSize - Vector2I.One));
        }
        return true;
    }

    private void DrawRoom(Vector2I position, TileMapPattern pattern)
    {
        foreach (Vector2I cell in pattern.GetUsedCells())
        {
            Vector2I worldPos = position + cell;
            int sourceId = pattern.GetCellSourceId(cell);
            Vector2I atlasCoords = pattern.GetCellAtlasCoords(cell);
            if (!IsValidTile(sourceId, atlasCoords))
            {
                GD.PrintErr(
                    $"Tile not found at {worldPos} (sourceId: {sourceId}, atlasCoords: {atlasCoords})"
                );
                continue;
            }
            walls.SetCell(worldPos, sourceId, atlasCoords);
        }
    }

    private bool IsValidTile(int sourceId, Vector2I atlasCoords)
    {
        TileSetAtlasSource source = tileSet.GetSource(sourceId) as TileSetAtlasSource;
        if (source == null)
            return false;
        return source.HasTile(atlasCoords);
    }

    private void UpdateTiles()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        GD.Print("Batching Tiles by Terrain...");
        terrainUpdateBatches.Clear();
        var usedCells = walls.GetUsedCells();
        int skipped = 0;
        foreach (Vector2I cell in usedCells)
        {
            if (IsDoorTile(cell))
            {
                skipped++;
                continue;
            }
            BatchTileForTerrain(cell);
        }
        GD.Print($"Skipped {skipped} door tiles");
        int batchCount = 0;
        foreach (var kv in terrainUpdateBatches)
        {
            var cells = kv.Value;
            if (cells.Count == 0)
                continue;
            walls.SetCellsTerrainConnect([.. cells], kv.Key.terrainSet, kv.Key.terrain);
            batchCount++;
        }
        sw.Stop();
        GD.Print(
            $"[Time] UpdateTiles (batched): {sw.ElapsedMilliseconds} ms, {batchCount} batches"
        );
    }

    // UpdateTile wird nicht mehr direkt genutzt, stattdessen BatchTileForTerrain
    private void UpdateTile(Vector2I position)
    {
        BatchTileForTerrain(position);
    }

    // Danke GitHub Copilot (:
    // Rotates a TileMapPattern by 90° increments (0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°)
    private TileMapPattern RotatePattern(TileMapPattern pattern, int rotation)
    {
        rotation = rotation % 4;
        if (rotation == 0)
            return pattern;

        var size = pattern.GetSize();
        var usedCells = pattern.GetUsedCells();
        var rotatedPattern = new TileMapPattern();
        Vector2I newSize = size;

        // Calculate new size for 90°/270° rotations
        if (rotation % 2 == 1)
            newSize = new Vector2I(size.Y, size.X);

        rotatedPattern.SetSize(newSize);

        foreach (var cell in usedCells)
        {
            int sourceId = pattern.GetCellSourceId(cell);
            Vector2I atlasCoords = pattern.GetCellAtlasCoords(cell);
            int altTile = pattern.GetCellAlternativeTile(cell);

            Vector2I newCell = cell;
            switch (rotation)
            {
                case 1: // 90°
                    newCell = new Vector2I(size.Y - 1 - cell.Y, cell.X);
                    break;
                case 2: // 180°
                    newCell = new Vector2I(size.X - 1 - cell.X, size.Y - 1 - cell.Y);
                    break;
                case 3: // 270°
                    newCell = new Vector2I(cell.Y, size.X - 1 - cell.X);
                    break;
            }

            rotatedPattern.SetCell(newCell, sourceId, atlasCoords, altTile);
        }

        return rotatedPattern;
    }
}
