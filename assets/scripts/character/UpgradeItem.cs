using Godot;

public partial class UpgradeItem : Item
{
    public UpgradeType upgradeType;

    private static PackedScene upgradeItemPrefab = GD.Load<PackedScene>("res://assets/scenes/upgrade.tscn");

    public static UpgradeItem Instantiate(UpgradeType type)
    {
        var upgradeItem = (UpgradeItem)upgradeItemPrefab.Instantiate();
        upgradeItem.upgradeType = type;
        return upgradeItem;
    }
}

public enum UpgradeType
{
    BouncyProjectiles,
    GhostShoot,
    WalkOnWater,
}
