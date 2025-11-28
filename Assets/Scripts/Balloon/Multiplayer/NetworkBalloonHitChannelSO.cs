using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "NetworkBalloonHitChannel", menuName = "Dart Game/Events/Network Balloon Hit Channel")]
public class NetworkBalloonHitChannelSO : ScriptableObject
{
    // 매개변수: (맞은 사람의 ClientID, 터진 풍선 Index)
    public event UnityAction<ulong, int> OnPlayerHit;

    public void RaiseEvent(ulong victimId, int balloonIndex)
    {
        if (OnPlayerHit != null)
        {
            OnPlayerHit.Invoke(victimId, balloonIndex);
        }
        else
        {
            Debug.LogWarning("아무도 NetworkBalloonHitChannel을 듣고 있지 않습니다 (GameManager 확인 필요)");
        }
    }
}