using Godot;

public partial class UpgradeItem : Item
{
    private UpgradeType _upgradeType;
    public UpgradeType upgradeType
    {
        get => _upgradeType;
        set
        {
            _upgradeType = value;
            sprite.Frame = (int)_upgradeType;
        }
    }

    [Export]
    private Sprite2D sprite;

    private static PackedScene upgradeItemPrefab = GD.Load<PackedScene>(
        "res://assets/scenes/upgrade.tscn"
    );

    public static UpgradeItem Instantiate(UpgradeType type)
    {
        var upgradeItem = (UpgradeItem)upgradeItemPrefab.Instantiate();
        upgradeItem.upgradeType = type;
        return upgradeItem;
    }
}

public enum UpgradeType
{
    GhostShoot = 0,
    WalkOnWater = 2,
    BouncyProjectiles = 1,
}
