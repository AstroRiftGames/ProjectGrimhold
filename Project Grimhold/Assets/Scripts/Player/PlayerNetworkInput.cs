using Fusion;
using UnityEngine;

public struct PlayerNetworkInput : INetworkInput
{
    public Vector2 MoveDirection;

    public Vector2 LookRotation;

    public NetworkButtons Buttons;
}