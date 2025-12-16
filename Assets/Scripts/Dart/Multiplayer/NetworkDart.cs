using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class NetworkDart : NetworkBehaviour
{
    [SerializeField] private DartSettingsSO _settings;
    [Header("Audio")]
    [SerializeField] private AudioClip flyingSound;

    private Rigidbody _rb;
    private MeshRenderer[] _renderers;
    private AudioSource _audioSource;
    private bool _HasHit = false; // 중복 충돌 방지용 변수
    private ulong _ThrowerId; // 던진 사람의 ClientId

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _renderers = GetComponentsInChildren<MeshRenderer>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1.0f;
    }

    public override void OnNetworkSpawn()
    {
        // 클라이언트(Remote)의 물리는 무조건 끕니다.
        // 위치는 오직 NetworkTransform에 의해서만 움직여야 합니다.
        if (!IsServer)
        {
            _rb.isKinematic = true; 
        }
        _HasHit = false;
        
        // 스폰되자마자 소리 재생 (날아가는 중)
        PlayFlyingSound();
    }

    private void PlayFlyingSound()
    {
        if (_audioSource != null && flyingSound != null)
        {
            _audioSource.clip = flyingSound;
            _audioSource.Play();
        }
    }

    private void StopFlyingSound()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    // [Server Only] 서버가 직접 호출하는 초기화 함수
    public void Server_Initialize(ulong throwerId, Vector3 velocity, Vector3 angularVel)
    {
        // 부모 해제
        GetComponent<NetworkObject>().TryRemoveParent();

        // [서버에서만 물리 시작]
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.linearVelocity = velocity;
        _rb.angularVelocity = angularVel;

        // [중요] 던진 본인(Owner)에게 "너는 로컬 다트 보고 있으니까 이건 숨겨"라고 알려줌
        HideVisualsClientRpc(throwerId);
        _ThrowerId = throwerId;
    }

    // 2. 시각 처리 (본인 숨기기용)
    [ClientRpc]
    private void HideVisualsClientRpc(ulong throwerId)
    {
        // 던진 당사자라면, 네트워크 다트를 잠시 숨깁니다. (본인은 부드러운 LocalDart를 보고 있으니까)
        if (NetworkManager.Singleton.LocalClientId == throwerId)
        {
            SetVisuals(false);
            // 본인은 로컬 다트 소리를 들을 테니 네트워크 다트 소리는 끔
            StopFlyingSound();
        }
        else
        {
            // 남들은 보여야 함
            SetVisuals(true);
        }
    }

    private void SetVisuals(bool show)
    {
        foreach (var r in _renderers) r.enabled = show;
    }

    // 풍선 충돌 처리 (Server Only)
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || _HasHit) return; // 서버만 처리 & 이미 맞았으면 무시

        // 태그 확인
        if (other.CompareTag("Balloons")) 
        {
            // Network Balloon 스크립트 찾기
            NetworkBalloon collisionBalloon = other.gameObject.GetComponentInParent<NetworkBalloon>();

            if (collisionBalloon != null && collisionBalloon.OwnerClientId != _ThrowerId)
            {
                _HasHit = true; // 중복 방지

                // 풍선 로직 호출 (서버 -> 매니저 -> ClientRpc)
                collisionBalloon.OnHitByDart();
            }
        }
    }

    // 환경 충돌 처리 (Server Only)
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return; // 오직 서버만 충돌 감지

        if (collision.gameObject.CompareTag("Environment"))
        {
            // 서버에서 멈추면 NetworkTransform을 통해 클라이언트들도 멈춘 위치로 동기화됨
            _rb.isKinematic = true; 
            _rb.useGravity = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            SyncHitClientRpc(transform.position, transform.rotation);
            StartCoroutine(DespawnDelay());
        }
    }

    [ClientRpc]
    private void SyncHitClientRpc(Vector3 pos, Quaternion rot)
    {
        // 위치 보정
        transform.position = pos;
        transform.rotation = rot;
        _rb.isKinematic = true;
        _rb.useGravity = false;

        StopFlyingSound();

        // 충돌한 순간에는 모든 클라이언트(본인 포함)에게 네트워크 다트를 보여줍니다.
        SetVisuals(true);
    }

    private IEnumerator DespawnDelay()
    {
        yield return new WaitForSeconds(_settings.NetworkLifeTime);
        if(IsSpawned) GetComponent<NetworkObject>().Despawn();
    }

    public override void OnNetworkDespawn()
    {
        StopFlyingSound();
        base.OnNetworkDespawn();
    }
}