using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/*
Items:
Abpraller (Langsamer, aber kann an Wänden abprallen)
Ghost Kill (Kann Geister töten)
Über Wasser Laufen/Schweben
*/

public partial class CharacterSelection : CharacterController
{
    [Export]
    public AudioStreamWav stepSound;

    [Export]
    public AudioStreamPlayer2D stepPlayer;

    [Export]
    public Control characterSelectionContainer;

    private string anim = "idle";
    private string direction = "down";

    public override void _Ready()
    {
        GetTree().Paused = true; // Pause the game during character selection
        characterPartSelection = MetaMain.GetLastOutfit();
        UpdateSprites();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (sprites[0].Animation.ToString().StartsWith("walk_") && !stepPlayer.Playing)
        {
            {
                PlayAudio(stepSound, stepPlayer);
            }
        }
        if (sprites[0].Animation.ToString().StartsWith("shoot_") && !sprites[0].IsPlaying())
        {
            ActuallyShoot(sprites[0].FlipH ? Vector2.Left : Vector2.Right);
        }
    }

    public static readonly HairStyle[] HairStylesByIndex = new HairStyle[]
    {
        HairStyle.None,
        HairStyle.Dark,
        HairStyle.Clown,
    };

    public void ChangeHead(int delta)
    {
        MetaMain.Instance.PlaySelectSound();
        characterPartSelection.Hair = HairStylesByIndex[
            (
                Array.IndexOf(HairStylesByIndex, characterPartSelection.Hair)
                + delta
                + HairStylesByIndex.Length
            ) % HairStylesByIndex.Length
        ];
        UpdateSprites();
    }

    public static readonly ChestStyle[] ChestStylesByIndex = new ChestStyle[]
    {
        ChestStyle.None,
        ChestStyle.Shirt,
    };

    public void ChangeChest(int delta)
    {
        MetaMain.Instance.PlaySelectSound();
        characterPartSelection.Chest = ChestStylesByIndex[
            (
                Array.IndexOf(ChestStylesByIndex, characterPartSelection.Chest)
                + delta
                + ChestStylesByIndex.Length
            ) % ChestStylesByIndex.Length
        ];
        UpdateSprites();
    }

    public static readonly PantStyle[] PantStylesByIndex = new PantStyle[]
    {
        PantStyle.None,
        PantStyle.Shirt,
    };

    public void ChangePants(int delta)
    {
        MetaMain.Instance.PlaySelectSound();
        characterPartSelection.Pants = PantStylesByIndex[
            (
                Array.IndexOf(PantStylesByIndex, characterPartSelection.Pants)
                + delta
                + PantStylesByIndex.Length
            ) % PantStylesByIndex.Length
        ];
        UpdateSprites();
    }

    public static readonly FeetStyle[] FeetStylesByIndex = new FeetStyle[]
    {
        FeetStyle.None,
        FeetStyle.Socks,
    };

    public void ChangeFeet(int delta)
    {
        MetaMain.Instance.PlaySelectSound();
        characterPartSelection.Feet = FeetStylesByIndex[
            (
                Array.IndexOf(FeetStylesByIndex, characterPartSelection.Feet)
                + delta
                + FeetStylesByIndex.Length
            ) % FeetStylesByIndex.Length
        ];
        UpdateSprites();
    }

    public void PlayAnimation()
    {
        string animation = anim + "_" + direction;
        if (animation.StartsWith("shoot_"))
        {
            animation = animation.Replace("_down", "_left");
            animation = animation.Replace("_up", "_right");
        }
        foreach (AnimatedSprite2D sprite in sprites)
        {
            sprite.Play(animation.Replace("_left", "_right"));
            sprite.FlipH = animation.EndsWith("_left");
        }
    }

    public void SetAnimation(int index)
    {
        MetaMain.Instance.PlaySelectSound();
        anim = index switch
        {
            0 => "idle",
            1 => "walk",
            2 => "shoot",
            _ => "idle",
        };

        PlayAnimation();
    }

    public void SetDirection(int index)
    {
        MetaMain.Instance.PlaySelectSound();
        direction = index switch
        {
            0 => "down",
            1 => "up",
            2 => "left",
            3 => "right",
            _ => "down",
        };

        PlayAnimation();
    }

    public void SetCharacterName(string name)
    {
        characterPartSelection.Name = name;
    }

    public void FinishSelection()
    {
        MetaMain.Instance.PlaySelectSound();
        PlayerController.Instance.characterPartSelection = characterPartSelection;
        GetTree().Paused = false; // Unpause the game after character selection
        PlayerController.Instance.UpdateSprites();
        characterSelectionContainer.Visible = false;
        characterSelectionContainer.QueueFree();
    }
}
