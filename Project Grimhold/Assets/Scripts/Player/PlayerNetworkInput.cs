using Fusion;
using UnityEngine;

public struct PlayerNetworkInput : INetworkInput
{
    public Vector2 MoveDirection;

    public Vector2 AimWorldPosition;

    public NetworkButtons Buttons;
}