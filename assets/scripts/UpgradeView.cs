using System;
using Godot;

public partial class UpgradeView : Control
{
    private PlayerController playerController;

    [Export]
    public TextureRect walkOnWaterView;

    [Export]
    public TextureRect hitGhostView;

    [Export]
    public TextureRect bouncyProjectilesView;

    public override void _Ready()
    {
        // Wait until PlayerController.Instance is set, then subscribe to upgradesChanged
        async void WaitForPlayerController()
        {
            while (PlayerController.Instance == null)
            {
                await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            }
            PlayerController.Instance.upgradesChanged += UpdateView;
            playerController = PlayerController.Instance;
            UpdateView();
        }
        WaitForPlayerController();
    }

    public void UpdateView()
    {
        if (playerController.HasUpgrade(UpgradeType.WalkOnWater))
            walkOnWaterView.Visible = true;
        if (playerController.HasUpgrade(UpgradeType.GhostShoot))
            hitGhostView.Visible = true;
        if (playerController.HasUpgrade(UpgradeType.BouncyProjectiles))
            bouncyProjectilesView.Visible = true;
    }
}
