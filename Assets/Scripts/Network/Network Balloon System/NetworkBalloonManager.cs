using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkBalloonManager : NetworkBehaviour
{
    [Header("Settings")]
    [Range(1, 5)]
    public int MaxBalloonCount = 5;

    [Header("References")]
    public List<NetworkBalloon> BalloonList = new List<NetworkBalloon>();

    [Header("Event Channel")]
    public NetworkBalloonHitChannelSO HitChannel;

    private void Awake()
    {
        // 풍선들에게 번호표 부여 (초기화)
        for (int i = 0; i < BalloonList.Count; i++)
        {
            if (BalloonList[i] != null)
            {
                BalloonList[i].Initialize(this, i);
                // 처음엔 켜두고, 게임 시작 시 설정에 따라 끌 수도 있음
                // BalloonList[i].gameObject.SetActive(true); 
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

    // 게임 리셋용 함수
    // [ClientRpc]
    // public void ResetBalloonsClientRpc()
    // {
    //     foreach(var b in BalloonList) b.gameObject.SetActive(true);
    // }
}