using UnityEngine;

public class NetworkBalloon : MonoBehaviour
{
    // 매니저가 부여해 줄 고유 번호
    public int BalloonIndex { get; private set; }
    private NetworkBalloonManager _manager;
    public ulong OwnerClientId;

    // 초기화 (매니저가 Awake에서 호출)
    public void Initialize(NetworkBalloonManager manager, int index)
    {
        _manager = manager;
        BalloonIndex = index;
        OwnerClientId = manager.OwnerClientId;
    }

    // [Server Only] 서버의 NetworkDart가 충돌 시 이 함수를 호출함
    public void OnHitByDart()
    {
        // 매니저에게 "나(Index) 터졌어"라고 보고
        if (_manager != null)
        {
            _manager.Server_OnBalloonHit(BalloonIndex);
        }
    }
}