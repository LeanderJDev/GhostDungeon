using System.Collections.Generic;
using Godot;

public struct CharacterPath
{
    public List<Vector2> positions;
    public List<CharacterAction> actions;
}

public enum CharacterActionType
{
    Shoot = 0,
    ItemPickup = 1,
    DoorOpen = 2,
}

public struct CharacterAction
{
    public int index; //index of movement
    public CharacterActionType action;
    public Vector2 direction;
}
