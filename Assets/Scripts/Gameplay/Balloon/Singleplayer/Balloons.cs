using UnityEngine;
using System;

/// <summary>
/// 싱글 플레이 풍선. 매니저가 부여한 인덱스를 들고 다트 충돌 시 매니저에 보고한다.
/// </summary>
public class Balloons : MonoBehaviour
{
    public event Action<Balloons> OnHit;

    public int BalloonIndex { get; private set; }
    private BalloonManager _manager;

    /// <summary>매니저가 Awake/Start에서 호출하는 초기화.</summary>
    public void Initialize(BalloonManager manager, int index)
    {
        _manager = manager;
        BalloonIndex = index;
    }

    /// <summary>다트 충돌 시 호출.</summary>
    public void HitBalloon()
    {
        _manager?.OnBalloonHit(BalloonIndex);

        OnHit?.Invoke(this);
        Debug.Log($"[BalloonObj]:{name} [Active]:HitBalloon invoked");

        // TODO: 이펙트/사운드가 필요하면 여기에서 재생
    }
}

