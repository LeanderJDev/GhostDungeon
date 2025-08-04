using System.Collections.Generic;
using Godot;

public struct PlayerPath
{
    public List<Vector2> positions;
    public Dictionary<int, CharacterAction> actions;
    public CharacterCustomisation characterCustomisation;
}

public struct GhostPath
{
    public Vector2[] positions;
    public Dictionary<int, CharacterAction> actions;
    public CharacterCustomisation characterCustomisation;
}

public enum CharacterActionType
{
    Shoot = 0,
    ItemPickup = 1,
    DoorOpen = 2,
    AntiSoftlock = 3,
}

public struct CharacterAction
{
    public CharacterActionType action;
    public Vector2 direction;
}
