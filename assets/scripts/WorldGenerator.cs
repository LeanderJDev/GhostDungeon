using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
Features:
Gegner
Items
Traps

+ Löcher füllen



ToDo
+Main Menu
+OnWater Powerup
+Powerup Sprites
+Character Animations
-Traps
+Door Sprite Fixes
-Enemy Spawner
+Loop Manager

ToDo Neu:
Character Customization
Win Screen
Better UI

*/

public partial class WorldGenerator : Node2D
{
    public static WorldGenerator Instance { get; private set; }

    [Export]
    public TileMapLayer walls;

    [Export]
    public TileMapLayer ground;

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
    public PackedScene player;

    [Export]
    public Vector2[] horizontalDoorCoords;

    [Export]
    public Vector2[] verticalDoorCoords;

    [Export]
    public Vector2I rockCoords = new Vector2I(0, 0);

    private int generatedRoomCount = 0;

    [Export]
    public int debugSeed = 0;
    public static int Seed;

    private TileSet tileSet;
    private TileMapPattern[] roomChoices;
    private Queue<Vector2I> generationQueue = new();
    private List<Vector2I> generatedRooms = new();
    private Random random = new();

    // Speichert das Spielerraum für Enemy-Spawn-Exklusion
    private Vector2I? playerRoomSaved = null;

    [Export]
    public int enemySpawningRate = 25; // seconds between enemy spawns

    [Export]
    public int enemySpawningRateVariance = 5; // seconds variance
    private double enemySpawnTimer = 20.0;

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

    public WorldGenerator()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        Instance = null;
        base._ExitTree();
    }

    public override void _Ready()
    {
        if (debugSeed != 0)
            Seed = debugSeed;
        base._Ready();
        if (walls == null || ground == null)
        {
            GD.PrintErr("TileMapLayer 'walls' or 'ground' is not set.");
            return;
        }
        tileSet = walls.TileSet;
        if (tileSet == null)
        {
            GD.PrintErr("TileSet is not set in TileMapLayer 'walls'.");
            return;
        }
        int roomChoiceCount = tileSet.GetPatternsCount();
        roomChoices = new TileMapPattern[roomChoiceCount];
        for (int i = 0; i < roomChoiceCount; i++)
            roomChoices[i] = tileSet.GetPattern(i);
        if (roomChoices.Length == 0)
        {
            GD.PrintErr("No room patterns found in TileSet.");
            return;
        }
        random = Seed != 0 ? new Random(Seed) : new Random();
        GD.Print($"Found {roomChoices.Length} room patterns in TileSet.");
        GD.Print("Starting world generation...");
        StartGeneration();
    }

    public override void _PhysicsProcess(double delta)
    {
        enemySpawnTimer -= delta;
        if (enemySpawnTimer <= 0.0)
        {
            enemySpawnTimer = random.Next(
                enemySpawningRate - enemySpawningRateVariance,
                enemySpawningRate + enemySpawningRateVariance
            );
            SpawnEnemies(0, 2, (int)Time.GetTicksMsec());
        }
    }

    public void SpawnChest(Vector2I tilePosition)
    {
        walls.SetCell(
            tilePosition,
            0,
            new Vector2I(14, 6) // Assuming this is the chest tile
        );
    }

    int tries;

    private void StartGeneration()
    {
        tries = 3;
        generatedRooms.Clear();
        GenerateRooms(Vector2I.Zero);
    }

    private void GenerateRooms(Vector2I position)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        GD.Print("Generating Rooms at: ", position);
        walls.Clear();
        ground.Clear();
        generatedRoomCount = 0;
        generatedRooms.Clear();

        GD.Print("Target: ", roomCount);
        generationQueue.Clear();
        generationQueue.Enqueue(position);

        int maxRoomIterations = roomCount * 6;
        int roomIterations = 0;
        while (generationQueue.Count > 0 && roomIterations < maxRoomIterations)
        {
            roomIterations++;
            Vector2I pos = generationQueue.Dequeue();
            if (!GenerateRoom(pos))
            {
                continue;
            }
            generatedRoomCount++;
            generatedRooms.Add(pos);
            roomIterations++;
            if (generatedRoomCount == roomCount)
                break;
        }
        if (roomIterations >= maxRoomIterations)
        {
            GD.PrintErr(
                $"Aborted GenerateRooms after {maxRoomIterations} iterations to prevent infinite loop."
            );
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
        SpawnPlayer();
        sw.Stop();
        GD.Print($"[Time] SpawnPlayer: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        if (debugSeed == 0)
            SpawnEnemies();
        sw.Stop();
        GD.Print($"[Time] SpawnEnemies: {sw.ElapsedMilliseconds} ms");
        GD.Print("Done");
    }

    // Sucht die beiden am weitesten entfernten Räume mit Pfad und spawnt Spieler/markiert Schatzraum
    private void SpawnPlayer()
    {
        if (generatedRooms.Count < 2)
        {
            GD.PrintErr("Not enough rooms to spawn player and treasure.");
            return;
        }
        int maxDistance = -1;
        List<Vector2> bestPath = null;
        Vector2I playerRoom = generatedRooms[0];
        Vector2I treasureRoom = generatedRooms[1];
        Vector2 playerStart = Vector2.Zero;
        Vector2 treasureStart = Vector2.Zero;
        // Hilfsfunktion: finde begehbare Position im Raum
        Vector2? FindWalkableInRoom(Vector2I room)
        {
            List<Vector2I> possiblePositions = new();
            for (int x = 1; x < roomSize.X - 1; x++)
            for (int y = 1; y < roomSize.Y - 1; y++)
                possiblePositions.Add(room + new Vector2I(x, y));
            // Fisher-Yates Shuffle für Effizienz
            for (int i = possiblePositions.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (possiblePositions[i], possiblePositions[j]) = (
                    possiblePositions[j],
                    possiblePositions[i]
                );
            }
            foreach (var pos in possiblePositions)
            {
                if (CheckSpace(walls, ground, pos))
                    return (Vector2)walls.MapToLocal(pos);
            }
            return null;
        }

        // Vergleiche alle Raum-Paare
        for (int i = 0; i < generatedRooms.Count; i++)
        {
            for (int j = i + 1; j < generatedRooms.Count; j++)
            {
                Vector2I roomA = generatedRooms[i];
                Vector2I roomB = generatedRooms[j];
                Vector2? startA = FindWalkableInRoom(roomA);
                Vector2? startB = FindWalkableInRoom(roomB);

                if (startA == null || startB == null)
                {
                    GD.PrintErr($"No walkable position found in room {roomA} or {roomB}");
                    break;
                }
                List<Vector2> path = APlusPathfinder.Instance.Calculate(
                    startA.Value,
                    startB.Value,
                    ignoreDoors: true
                );

                if (path != null && path.Count > maxDistance)
                {
                    maxDistance = path.Count;
                    bestPath = path;
                    playerRoom = roomA;
                    treasureRoom = roomB;
                    playerStart = startA.Value;
                    treasureStart = startB.Value;
                }
            }
        }
        // Speichere das Spielerraum für Enemy-Spawn-Exklusion
        playerRoomSaved = playerRoom;
        if (maxDistance < 1)
        {
            GD.PrintErr("No valid path between any two rooms.");
            return;
        }
        // Spieler spawnen
        Node2D playerNode = (Node2D)player.Instantiate();
        Vector2 playerPos = bestPath[0];
        playerNode.Position = playerPos;
        GetParent().CallDeferred("add_child", playerNode);
        // Schatzraum markieren (z.B. als Property oder Print)
        GD.Print($"Treasure room at {treasureRoom}");
        GD.Print($"Treasure spawn at {treasureStart}");
        GD.Print($"Player room at {playerRoom}");
        GD.Print($"Player spawn at {playerPos}");
        walls.SetCell((Vector2I)walls.LocalToMap(treasureStart), 0, new Vector2I(16, 0));
        // Optional: Hier könnte man ein Property setzen oder ein Objekt spawnen
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
        TileData wallTileData = walls.GetCellTileData(roomPosition);
        if (wallTileData == null)
        {
            GD.PrintErr("No wall tile data found at room position.");
            return;
        }
        walls.SetCellsTerrainConnect(
            [
                new Vector2I(doorStartX, doorStartY),
                new Vector2I(doorStartX, doorEndY),
                new Vector2I(doorEndX, doorStartY),
                new Vector2I(doorEndX, doorEndY),
            ],
            wallTileData.TerrainSet,
            wallTileData.Terrain
        );
    }

    private void FillGaps()
    {
        GD.Print("Filling Gaps");
        foreach (Vector2I room in generatedRooms)
        {
            foreach (Vector2I dir in eightNeighbourDirections)
            {
                Vector2I neighbourPos = room + dir * (roomSize - Vector2I.One);
                if (generatedRooms.Contains(neighbourPos))
                    continue; // Skip if neighbour is a room
                for (int x = neighbourPos.X; x < neighbourPos.X + roomSize.X; x++)
                {
                    for (int y = neighbourPos.Y; y < neighbourPos.Y + roomSize.Y; y++)
                    {
                        Vector2I pos = new Vector2I(x, y);
                        if (walls.GetCellSourceId(pos) == -1 && ground.GetCellSourceId(pos) == -1)
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

    // This will be called by the PlayerController to open a door
    public bool OpenDoor(Vector2I tilePosition, int direction) //1 == oben, rechts, 2 == unten, links
    {
        int sourceId = walls.GetCellSourceId(tilePosition);

        if (sourceId == -1)
            return false; // No tile at this position
        Vector2I atlasCoords = walls.GetCellAtlasCoords(tilePosition);
        int horizontalDoorIndex = System.Array.IndexOf(horizontalDoorCoords, (Vector2)atlasCoords);
        int verticalDoorIndex = System.Array.IndexOf(verticalDoorCoords, (Vector2)atlasCoords);

        if (horizontalDoorIndex == 0)
        {
            SetDoor(tilePosition, Vector2I.Right, direction);
            return true;
        }
        if (horizontalDoorIndex == 1)
        {
            SetDoor(tilePosition - Vector2I.Right, Vector2I.Right, direction);
            return true;
        }
        if (verticalDoorIndex == 0)
        {
            SetDoor(tilePosition, Vector2I.Down, direction);
            return true;
        }
        if (verticalDoorIndex == 1)
        {
            SetDoor(tilePosition - Vector2I.Down, Vector2I.Down, direction);
            return true;
        }
        return false;
    }

    private void SetDoor(Vector2I position, Vector2I direction, int open = 0)
    {
        Vector2[] doorCoords = direction.X == 0 ? verticalDoorCoords : horizontalDoorCoords;
        // Use a seeded random based on the position for deterministic door color
        int seed = (position + direction).GetHashCode();
        Random doorRandom = new Random(seed);
        int doorColor = doorRandom.Next(1, 6);
        if (doorColor > 4)
        {
            doorColor = -1;
        }
        walls.SetCell(
            new Vector2I(position.X, position.Y),
            doorColor,
            (Vector2I)doorCoords[open * 2]
        );
        walls.SetCell(
            new Vector2I(position.X + direction.X, position.Y + direction.Y),
            doorColor,
            (Vector2I)doorCoords[open * 2 + 1]
        );
    }

    private void SpawnEnemies(int minEnemyCount = 1, int maxEnemyCount = 5, int seed = 0)
    {
        GD.Print("Spawning Enemies");
        if (enemies == null || enemies.Length == 0)
        {
            GD.Print("No enemies to spawn");
            return;
        }
        foreach (Vector2I room in generatedRooms)
        {
            // Spielerraum überspringen
            if (playerRoomSaved != null && room == playerRoomSaved.Value && seed == 0)
                continue;
            Random enemyRandom = new Random(room.GetHashCode() + seed);
            int enemyCount = enemyRandom.Next(minEnemyCount, maxEnemyCount + 1);
            int maxEnemyTries = enemyCount * 3;
            int tries = 0;
            while (enemyCount > 0 && tries < maxEnemyTries)
            {
                tries++;
                int x = enemyRandom.Next(1, roomSize.X - 1);
                int y = enemyRandom.Next(1, roomSize.Y - 1);
                Vector2I localPos = new Vector2I(x, y);
                Vector2I worldPos = room + localPos;
                if (!CheckSpace(walls, ground, worldPos))
                {
                    continue;
                }
                if (
                    PlayerController.Instance != null
                    && PlayerController.Instance.Position.DistanceTo(
                        worldPos * walls.TileSet.TileSize
                    )
                        < 16 * 3
                )
                {
                    continue; // Skip if too close to player
                }
                if (enemies.Length > 0)
                {
                    var enemyScene = enemies[enemyRandom.Next(enemies.Length)];
                    var enemy = enemyScene.Instantiate<Node2D>();
                    enemy.Position = worldPos * walls.TileSet.TileSize + Vector2.Up * 6 + Position;
                    GetParent().CallDeferred("add_child", enemy);
                    enemyCount--;
                }
            }
            if (tries >= maxEnemyTries && enemyCount > 0)
            {
                GD.PrintErr(
                    $"SpawnEnemies: Aborted after {maxEnemyTries} tries in room {room}, {enemyCount} enemies not spawned."
                );
            }
        }
    }

    /*
    CheckSpace:
    Die Methode ist robust, aber du könntest sie als Extension-Method für TileMapLayer schreiben, um sie überall einfacher zu nutzen.
    */
    public static bool CheckSpace(
        TileMapLayer wallTileMap,
        TileMapLayer groundTileMap,
        Vector2I position
    )
    {
        if (groundTileMap.GetCellSourceId(position) == -1)
            return false; // No ground tile, space is not walkable
        TileData groundTileData = groundTileMap.GetCellTileData(position);
        if (groundTileData != null && groundTileData.GetCollisionPolygonsCount(0) != 0)
            return false;
        if (wallTileMap.GetCellSourceId(position) == -1)
            return true; // No wall tile, space is free
        TileData wallTileData = wallTileMap.GetCellTileData(position);
        if (wallTileData != null && wallTileData.GetCollisionPolygonsCount(0) != 0)
            return false;
        return true;
    }

    // Neue Struktur für Terrain-Batching
    private Dictionary<(int terrainSet, int terrain), HashSet<Vector2I>> wallsUpdateBatches = new();
    private Dictionary<(int terrainSet, int terrain), HashSet<Vector2I>> groundUpdateBatches =
        new();

    // Prüft, ob ein Tile eine Tür ist (anhand der DoorCoords)
    private bool IsDoorTile(Vector2I position)
    {
        int sourceId = walls.GetCellSourceId(position);
        if (sourceId == 0)
            return false;
        return true;
    }

    private void UpdateTiles()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        GD.Print("Batching Tiles by Terrain...");
        wallsUpdateBatches.Clear();
        groundUpdateBatches.Clear();
        int skipped = 0;
        // walls
        foreach (var cell in walls.GetUsedCells())
        {
            if (IsDoorTile(cell))
            {
                skipped++;
                continue;
            }
            BatchTileForTerrain(cell, walls);
        }
        // ground
        foreach (var cell in ground.GetUsedCells())
        {
            BatchTileForTerrain(cell, ground);
        }
        GD.Print($"Skipped {skipped} door tiles");
        int batchCount = 0;
        foreach (var kv in wallsUpdateBatches)
        {
            var cells = kv.Value;
            GD.Print(
                $"Batching {cells.Count} cells for terrain {kv.Key.terrainSet}, {kv.Key.terrain}"
            );
            if (cells.Count == 0)
                continue;
            walls.SetCellsTerrainConnect([.. cells], kv.Key.terrainSet, kv.Key.terrain);
            batchCount++;
        }
        foreach (var kv in groundUpdateBatches)
        {
            var cells = kv.Value;
            GD.Print(
                $"Batching {cells.Count} cells for terrain {kv.Key.terrainSet}, {kv.Key.terrain}"
            );
            if (cells.Count == 0)
                continue;
            ground.SetCellsTerrainConnect([.. cells], kv.Key.terrainSet, kv.Key.terrain);
            batchCount++;
        }

        sw.Stop();
        GD.Print(
            $"[Time] UpdateTiles (batched): {sw.ElapsedMilliseconds} ms, {batchCount} batches"
        );
    }

    // Fügt ein Tile der Batch-Liste hinzu, ruft aber nicht direkt SetCellsTerrainConnect
    private void BatchTileForTerrain(Vector2I position, TileMapLayer tilemap)
    {
        var tileData = tilemap.GetCellTileData(position);
        if (tileData == null)
            return;
        if (tileData.TerrainSet == -1 || tileData.Terrain == -1)
            return;
        var key = (tileData.TerrainSet, tileData.Terrain);
        Dictionary<(int, int), HashSet<Vector2I>> batchDict =
            tilemap == walls ? wallsUpdateBatches : groundUpdateBatches;
        if (!batchDict.TryGetValue(key, out var set))
        {
            set = new HashSet<Vector2I>();
            batchDict[key] = set;
        }
        set.Add(position);
    }

    private bool GenerateRoom(Vector2I position)
    {
        if (ground.GetCellSourceId(position + Vector2I.One) != -1)
            return false;
        int index = random.Next(roomChoices.Length / 2) * 2;
        TileMapPattern wallChoice = roomChoices[index];
        TileMapPattern groundChoice = roomChoices[index + 1];
        int rotation = random.Next(4);
        TileMapPattern rotatedWallRoom = RotatePattern(wallChoice, rotation);
        TileMapPattern rotatedGroundRoom = RotatePattern(groundChoice, rotation);
        DrawRoom(position, rotatedWallRoom, rotatedGroundRoom);

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

    private void DrawRoom(
        Vector2I position,
        TileMapPattern wallPattern,
        TileMapPattern groundPattern
    )
    {
        walls.SetPattern(position, wallPattern);
        ground.SetPattern(position, groundPattern);
    }

    private bool IsValidTile(int sourceId, Vector2I atlasCoords)
    {
        if (sourceId < 0 || sourceId >= tileSet.GetSourceCount())
            return false;
        TileSetAtlasSource source = tileSet.GetSource(sourceId) as TileSetAtlasSource;
        if (source == null)
            return false;
        return source.HasTile(atlasCoords);
    }

    // UpdateTile wird nicht mehr direkt genutzt, stattdessen BatchTileForTerrain
    private void UpdateTile(Vector2I position)
    {
        BatchTileForTerrain(position, walls);
        BatchTileForTerrain(position, ground);
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
