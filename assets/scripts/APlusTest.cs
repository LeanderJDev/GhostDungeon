using System;
using System.Diagnostics;
using Godot;

public partial class APlusTest : Node2D
{
    [Export]
    public TileMapLayer visulaiseLayer;

    [Export]
    public Node2D startMarker;

    [Export]
    public Node2D targetMarker;

    private Vector2I? dragStart = null;
    private Node2D draggedMarker = null;
    private APlusPathfinder.DebugStruct currentResult = new APlusPathfinder.DebugStruct();

    public override void _Ready()
    {
        base._Ready();
        UpdatePath();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            Vector2 mousePos = GetGlobalMousePosition();
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                float markerRadius = 16f;
                if (
                    startMarker != null
                    && startMarker.GlobalPosition.DistanceTo(mousePos) < markerRadius
                )
                {
                    draggedMarker = startMarker;
                }
                else if (
                    targetMarker != null
                    && targetMarker.GlobalPosition.DistanceTo(mousePos) < markerRadius
                )
                {
                    draggedMarker = targetMarker;
                }
            }
            else if (!mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (draggedMarker != null)
                {
                    draggedMarker = null;
                    UpdatePath();
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (draggedMarker != null)
            {
                Vector2 mousePos = GetGlobalMousePosition();
                draggedMarker.GlobalPosition = mousePos;
            }
        }
    }

    private void UpdatePath()
    {
        if (startMarker == null || targetMarker == null || visulaiseLayer == null)
            return;
        currentResult = APlusPathfinder.Instance.DebugCalculate(
            startMarker.GlobalPosition,
            targetMarker.GlobalPosition
        );

        // Visualisierung der Tiles auf dem TileMapLayer
        visulaiseLayer.Clear();
        visulaiseLayer.SetCell(Vector2I.Zero, 0, new Vector2I(0, 0));
        // Draw open and closed nodes as tiles on the tilemap
        foreach (var node in currentResult.closedList)
        {
            visulaiseLayer.SetCell(node.position, 0, new Vector2I(4, 0));
        }
        if (currentResult.path != null && currentResult.path.Count > 1)
        {
            startMarker.GlobalPosition = currentResult.path[0];
            targetMarker.GlobalPosition = currentResult.path[^1];
            GD.Print($"Path found with {currentResult.path.Count} nodes.");
            // Visualize the path with tiles using AtlasCoord (3,0)
            foreach (Vector2 pos in currentResult.path)
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
