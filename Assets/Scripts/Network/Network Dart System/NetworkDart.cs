using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class NetworkDart : NetworkBehaviour
{
    private Rigidbody _DartRigidbody;
    private bool _IsFlying = false;
    public float MaxLifetime = 10.0f;

    private void Awake()
    {
        _DartRigidbody = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // _DartRigidbody.isKinematic = true;
        _IsFlying = false;
        // _DartRigidbody.useGravity = false;
        _DartRigidbody.linearVelocity = Vector3.zero;
        _DartRigidbody.angularVelocity = Vector3.zero;
    }

    // 부모가 바뀔 때 호출되는 콜백 오버라이드
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        base.OnNetworkObjectParentChanged(parentNetworkObject);
        if(parentNetworkObject != null)
        {
            // 부모가 설정되었을 때 (플레이어 손에 잡혔을 때)
            _DartRigidbody.isKinematic = true;
            _IsFlying = false;
            // 자연스러운 위치 설정을 위해 offset 조정 필요시 추가
        }
        // else
        // {
        //     // 부모가 해제되었을 때 (던져질 때)
        //     _DartRigidbody.isKinematic = false;
        //     _IsFlying = true;
        // }
    }

    // Fire-and-Forget
    
    // client(owner)가 호출
    [ServerRpc(RequireOwnership = false)]
    public void ThrowDart_ServerRpc(Vector3 velocity, Vector3 angularVelocity)
    {
        // if (!IsServer) return;

        // 부모 해제
        GetComponent<NetworkObject>().TryRemoveParent();

        // 서버 물리 연산 시작
        ApplyPhysics(transform.position, transform.rotation, velocity, angularVelocity);

        // 모든 클라이언트에 물리 적용 명령 전송
        ThrowDart_ClientRpc(transform.position, transform.rotation, velocity, angularVelocity);

    }

    [ClientRpc]
    private void ThrowDart_ClientRpc(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
    {
        // 서버에서 이미 적용했으므로 서버는 무시
        if (IsServer) return;
        ApplyPhysics(position, rotation, velocity, angularVelocity);
    }

    private void ApplyPhysics(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
    {
        transform.position = position;
        transform.rotation = rotation;

        _DartRigidbody.isKinematic = false;
        _DartRigidbody.useGravity = true;
        _DartRigidbody.linearVelocity = velocity;
        _DartRigidbody.angularVelocity = angularVelocity;

        _IsFlying = true;

        // 자동 파괴 코루틴 시작
    }

    // 충돌

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !_IsFlying) return;

        // 충돌 위치 보정
        SyncImpactClientRpc(transform.position, transform.rotation);

        // "Environment" 태그를 가진 오브젝트와 충돌했을 때
        if (collision.gameObject.CompareTag("Environment"))
        {
            _IsFlying = false;
            _DartRigidbody.isKinematic = true;

            // 충돌 일정 시간 후 반납 코루틴
            StartCoroutine(ReturnToPoolAfterDelay(2.0f));
        }
    }

    [ClientRpc]
    private void SyncImpactClientRpc(Vector3 position, Quaternion rotation)
    {
        // 서버에서 이미 적용했으므로 서버는 무시
        if (IsServer) return;

        transform.position = position;
        transform.rotation = rotation;

        _DartRigidbody.isKinematic = true;
        _IsFlying = false;
    }

    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        GetComponent<NetworkObject>().Despawn(); 
    }

}
