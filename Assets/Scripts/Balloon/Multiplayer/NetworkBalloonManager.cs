using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkBalloonManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    [Range(1, 5)]
    private int _maxBalloonCount = 5;

    [Header("References")]
    public List<NetworkBalloon> BalloonList = new List<NetworkBalloon>();

    [Header("Event Channel")]
    public NetworkBalloonHitChannelSO HitChannel;

    /// <summary>현재 세트에서 사용할 풍선 최대 개수.</summary>
    public int MaxBalloonCount
    {
        get => _maxBalloonCount;
        set => _maxBalloonCount = Mathf.Clamp(value, 1, Mathf.Max(1, BalloonList.Count));
    }

    public override void OnNetworkSpawn()
    {
        Initialize();
    }

    /// 씬 로드 직후나 게임 시작 시점에 호출하여 풍선 시스템을 초기화합니다.
    public void Initialize()
    {
        // 풍선들에게 번호표 부여 (초기화)
        for (int i = 0; i < BalloonList.Count; i++)
        {
            if (BalloonList[i] != null)
            {
                BalloonList[i].Initialize(this, i);
            }
        }

        // 자신의 풍선은 그림자만 보이도록 설정
        if (IsOwner)
        {
            // HideBalloonsForOwner();
        }
    }

    /// [Server] 세트 시작 시 풍선들을 다시 활성화합니다.
    /// count를 지정하면 해당 개수만큼 활성화하고, 지정하지 않으면 기존 설정을 따릅니다.
    public void Server_ResetBalloons(int count = -1)
    {
        if (!IsServer) return;

        if (count != -1)
        {
            _maxBalloonCount = Mathf.Clamp(count, 1, BalloonList.Count);
        }

        ResetBalloonsClientRpc(_maxBalloonCount);
    }

    [ClientRpc]
    private void ResetBalloonsClientRpc(int activeCount)
    {
        for (int i = 0; i < BalloonList.Count; i++)
        {
            if (BalloonList[i] != null)
            {
                // 설정된 개수만큼만 활성화
                bool shouldActive = i < activeCount;
                BalloonList[i].gameObject.SetActive(shouldActive);
            }
        }
    }

    private void HideBalloonsForOwner()
    {
        // 자신의 풍선은 그림자만 보이도록 설정
        foreach (var balloon in BalloonList)
        {
            var renderers = balloon.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    // --- [Server] 다트가 풍선을 맞췄을 때 호출됨 ---
    public void Server_OnBalloonHit(int index)
    {
        if (!IsServer) return;

        // 이미 터진 풍선인지 확인 (중복 방지)
        if (!BalloonList[index].gameObject.activeSelf) return;

        // 시각적 처리 (모든 클라이언트에게 "터뜨려라" 명령)
        PopBalloonClientRpc(index);

        // 로직 처리 위임 (이벤트 채널에 방송)
        // GameManager가 이걸 듣고 HP를 깎을 것임
        if (HitChannel != null)
        {
            HitChannel.RaiseEvent(OwnerClientId, index);
        }
    }

    // --- [Client] 실제 시각적 처리 ---
    [ClientRpc]
    private void PopBalloonClientRpc(int index)
    {
        if (index >= 0 && index < BalloonList.Count)
        {
            // 풍선 비활성화
            BalloonList[index].gameObject.SetActive(false);
            
            // 터지는 이펙트/사운드 재생
            PlayPopEffect(BalloonList[index].transform.position);
        }
    }

    private void PlayPopEffect(Vector3 pos)
    {
        Debug.Log($"Balloon Popped at {pos}");
        // TODO: 파티클 생성 및 사운드 재생
    }
}
