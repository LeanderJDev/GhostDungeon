using Godot;

public partial class KeyItem : Item
{
    private KeyColor _color;

    public KeyColor Color
    {
        get => _color;
        set
        {
            _color = value;
            sprite.Frame = (int)_color;
        }
            
    }
    [Export] private Sprite2D sprite;

    private static PackedScene keyPrefab = GD.Load<PackedScene>("res://assets/scenes/key.tscn");

    public static KeyItem Instantiate(KeyColor color)
    {
        var keyItem = (KeyItem)keyPrefab.Instantiate();
        keyItem.Color = color;
        return keyItem;
    }
}

public enum KeyColor
{
    White = 0,
    Green = 2,
    Violet = 4,
    Turquoise = 3,
    Red = 1,
}
