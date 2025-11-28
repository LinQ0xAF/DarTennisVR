using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkBridge : NetworkBehaviour
{
    private PlayerDartState _state;

    private void Awake()
    {
        _state = GetComponent<PlayerDartState>();
    }

    // 1. 상태 동기화 요청 (장전/잡기/놓기)
    public void UpdateHoldingState(bool isHolding)
    {
        if (IsOwner) UpdateHoldingStateServerRpc(isHolding);
    }

    public void UpdateOffHandDarts(int offHandDelta)
    {
        if (IsOwner) UpdateOffHandDartsServerRpc(offHandDelta);
    }
    
    [ServerRpc]
    private void UpdateHoldingStateServerRpc(bool isHolding)
    {
        // 손 상태 변경
        _state.IsHoldingDart.Value = isHolding;
    }

    [ServerRpc]
    private void UpdateOffHandDartsServerRpc(int offHandDelta)
    {
        // 허리 개수 변경 (Clamp 적용)
        int newOffHandCount = Mathf.Clamp(_state.OffHandDartCount.Value + offHandDelta, 0, 3);
        _state.OffHandDartCount.Value = newOffHandCount;
    } 

    // 2. 투사체 발사 요청
    public void RequestSpawnProjectile(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (IsOwner) SpawnProjectileServerRpc(pos, rot, vel, angVel);
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        var poolManager = FindFirstObjectByType<NetworkDartPoolManager>();
        if (poolManager != null)
        {
            poolManager.Server_SpawnProjectile(OwnerClientId, pos, rot, vel, angVel);
        }
    }
}