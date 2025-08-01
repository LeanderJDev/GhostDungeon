using Godot;
using System;
using System.Collections.Generic;

public partial class YourRunRestartsHere : Control
{
	public static YourRunRestartsHere Instance {  get; private set; }

	public override void _Ready()
	{
		base._Ready();
		Instance = this;
		GetChild(0).SetProcess(false);
	}

	//vom Signal aufgerufen!
	private void ButtonPressed()
	{
		RestartGame();
	}

	public void PlayerDead()
	{
		((Control)GetChild(0)).Visible = true;
		GetChild(0).SetProcess(true);

	}

	public void RestartGame()
	{
		GetTree().ReloadCurrentScene();
	}
}
