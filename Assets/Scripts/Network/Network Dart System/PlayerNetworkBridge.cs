using UnityEngine;
using Unity.Netcode;

public class PlayerNetworkBridge : NetworkBehaviour
{
    [SerializeField] private DartSpawnChannelSO _DartSpawnChannel;

    public override void OnNetworkSpawn()
    {
        if (IsOwner && _DartSpawnChannel != null)
        {
            _DartSpawnChannel.OnSpawnRequested += Client_OnSpawnRequested;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && _DartSpawnChannel != null)
        {
            _DartSpawnChannel.OnSpawnRequested -= Client_OnSpawnRequested;
        }
    }

    private void Client_OnSpawnRequested(ulong clientId, Vector3 pos, Quaternion rot, bool isRightHand)
    {
        if (clientId != OwnerClientId) return;

        RequestReloadServerRpc(pos, rot, isRightHand);
    }

    [ServerRpc]
    private void RequestReloadServerRpc(Vector3 pos, Quaternion rot, bool isRightHand)
    {
        var dartPoolManager = FindFirstObjectByType<NetworkDartPoolManager>();
        if (dartPoolManager != null)
        {
            dartPoolManager.Server_OnSpawnRequested(OwnerClientId, pos, rot, isRightHand);
        }
    }
}
