using System;

[Flags]
public enum PhysicsLayer : uint
{
    World = 1 << 0,
    Water = 1 << 1,
    Projectile = 1 << 2,
    Player = 1 << 3,
    Ghost = 1 << 4,
    Enemies = 1 << 5,
}
