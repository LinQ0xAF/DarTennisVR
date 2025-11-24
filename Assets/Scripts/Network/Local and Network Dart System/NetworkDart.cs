using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkDart : NetworkBehaviour
{
    [SerializeField] private DartSettingsSO _settings;
    private Rigidbody _rb;
    private MeshRenderer[] _renderers;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<MeshRenderer>();
    }

    [ClientRpc]
    public void InitializeClientRpc(ulong throwerId, Vector3 vel, Vector3 angVel)
    {
        // 1. 물리 시작 (Fire-and-Forget)
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.linearVelocity = vel;
        _rb.angularVelocity = angVel;

        // 2. [최적화 핵심] 던진 본인은 이미 LocalDart를 보고 있으므로,
        //    이 네트워크 다트는 숨겨서 중복 렌더링 방지.
        bool isMine = (NetworkManager.Singleton.LocalClientId == throwerId);
        SetVisuals(!isMine);
    }

    private void SetVisuals(bool show)
    {
        foreach (var r in _renderers) r.enabled = show;
    }

    // [Server Only] 충돌 처리
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // Environment 레이어 체크
        if (collision.gameObject.CompareTag("Environment"))
        {
            _rb.isKinematic = true; // 멈춤
            SyncHitClientRpc(transform.position, transform.rotation);
            StartCoroutine(DespawnDelay());
        }
        // (풍선 충돌 로직 추가 가능)
    }

    [ClientRpc]
    private void SyncHitClientRpc(Vector3 pos, Quaternion rot)
    {
        if (IsServer) return;
        // 위치 보정 (서버와 동일하게)
        transform.position = pos;
        transform.rotation = rot;
        _rb.isKinematic = true;
        // 박히면 다시 보이게 할지 선택 (SetVisuals(true))
    }

    private IEnumerator DespawnDelay()
    {
        yield return new WaitForSeconds(_settings.NetworkLifeTime);
        if(IsSpawned) GetComponent<NetworkObject>().Despawn();
    }
}