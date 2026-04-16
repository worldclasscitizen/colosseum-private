using Fusion;
using UnityEngine;

namespace Colosseum.Network
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 Direction;
        public NetworkBool IsJumpPressed;
    }
}
