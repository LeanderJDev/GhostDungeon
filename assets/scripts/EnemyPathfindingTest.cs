using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class EnemyPathfindingTest : Node2D
{
    [Export]
    public EnemyController enemy;

    [Export]
    public TileMapLayer visulaiseLayer;

    public override void _PhysicsProcess(double delta)
    {
        UpdatePath();
    }

    private List<Vector2> GetEnemyPath()
    {
        // Greife auf das currentPath-Feld des Enemies zu
        // (falls private: public Property oder Methode im EnemyController bereitstellen)
        return enemy.currentPath != null ? enemy.currentPath.ToList() : new List<Vector2>();
    }

    private void UpdatePath()
    {
        var path = GetEnemyPath();
        // Visualisierung der Tiles auf dem TileMapLayer
        visulaiseLayer.Clear();
        visulaiseLayer.SetCell(Vector2I.Zero, 0, new Vector2I(0, 0));
        // Draw open and closed nodes as tiles on the tilemap
        if (path.Count > 1)
        {
            // Visualize the path with tiles using AtlasCoord (3,0)
            foreach (Vector2 pos in path)
            {
                visulaiseLayer.SetCell(
                    (Vector2I)visulaiseLayer.LocalToMap(pos),
                    0,
                    new Vector2I(3, 0)
                );
            }
        }
    }
}
