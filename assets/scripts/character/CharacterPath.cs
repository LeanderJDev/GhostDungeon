using System.Collections.Generic;
using Godot;

public struct CharacterPath
{
    public List<Vector2> positions;
    public List<CharacterAction> actions;
}

public struct CharacterAction
{
    public int index; //index of movement
    public int action;
    public Vector2 direction;
}
