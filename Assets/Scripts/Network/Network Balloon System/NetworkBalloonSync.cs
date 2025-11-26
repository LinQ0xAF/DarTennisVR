using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkBalloonSync : NetworkBehaviour
{
    [Header("Settings")]
    public float SyncInterval = 0.5f; // 0.5초마다 동기화 (매우 낮은 빈도)
    public float CorrectionSpeed = 5.0f; // 보정 속도
    public float SnapDistance = 0.5f; // 오차가 너무 크면 순간이동 시킬 거리

    private Rigidbody _rb;
    private Vector3 _targetPos;
    private Quaternion _targetRot;
    private float _lastSyncTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        // 초기 위치 설정
        _targetPos = transform.position;
        _targetRot = transform.rotation;
    }

    private void Update()
    {
        // [Server] 주기적으로 위치 전송
        if (IsServer)
        {
            if (Time.time - _lastSyncTime > SyncInterval)
            {
                _lastSyncTime = Time.time;
                // 현재 서버의 정확한 위치를 보냄
                SyncPositionClientRpc(transform.position, transform.rotation);
            }
        }
        
        // [Client] 서버가 알려준 위치로 부드럽게 보정
        if (IsClient && !IsServer)
        {
            SmoothCorrection();
        }
    }

    [ClientRpc]
    private void SyncPositionClientRpc(Vector3 serverPos, Quaternion serverRot)
    {
        if (IsServer) return; // 서버는 자기 자신이 기준이므로 무시

        _targetPos = serverPos;
        _targetRot = serverRot;

        // 만약 오차가 너무 크면(예: 50cm 이상), 부드럽게 가는 게 아니라 강제 이동(Snap)
        float dist = Vector3.Distance(transform.position, _targetPos);
        if (dist > SnapDistance)
        {
            transform.position = _targetPos;
            transform.rotation = _targetRot;
            _rb.linearVelocity = Vector3.zero; // 물리 힘 초기화 (선택)
        }
    }

    private void SmoothCorrection()
    {
        // 물리(SpringJoint)가 작동 중이므로, transform을 직접 덮어쓰면 덜덜 떨림.
        // Rigidbody.MovePosition을 쓰거나, 
        // 현재 위치와 목표 위치 사이를 부드럽게 섞어줌 (Lerp)
        
        // SpringJoint가 당기는 힘 + 보정하는 힘을 섞는 방식
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * CorrectionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Time.deltaTime * CorrectionSpeed);
    }
}