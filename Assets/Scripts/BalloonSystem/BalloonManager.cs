using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로컬(싱글) 풍선 관리. 개수만큼 활성화하고 터질 때 비활성화한다.
/// ResetBalloons로 세트마다 리셋 가능하도록 전체 목록을 유지한다.
/// </summary>
public class BalloonManager : MonoBehaviour
{
    [Range(1, 5)]
    [SerializeField] private int balloonNumber = 1;
    [SerializeField] private int balloonCurrentNumber = 1;
    [SerializeField] public List<Balloons> BalloonList = new List<Balloons>(); // 인스펙터에서 넣은 순서를 유지

    private int remainingCount = 0;

    /// <summary>모든 풍선이 제거되었을 때 알림.</summary>
    public event System.Action OnAllBalloonsCleared;

    /// <summary>현재 세트에서 남은 풍선 수.</summary>
    public int RemainingBalloonCount => remainingCount;

    public int BalloonNumber
    {
        get => balloonNumber;
        set => balloonNumber = Mathf.Clamp(value, 1, 5);
    }

    public int BalloonCurrentNumber
    {
        get => balloonCurrentNumber;
        set => balloonCurrentNumber = Mathf.Clamp(value, 1, 5);
    }

    private void Start()
    {
        ResetBalloons(balloonNumber);
    }

    /// <summary>
    /// 주어진 개수로 풍선을 초기화/리셋한다.
    /// </summary>
    public void ResetBalloons(int targetCount)
    {
        if (BalloonList == null || BalloonList.Count == 0)
            return;

        int activeCount = Mathf.Clamp(targetCount, 1, BalloonList.Count);
        balloonCurrentNumber = activeCount;
        remainingCount = activeCount;

        for (int i = 0; i < BalloonList.Count; i++)
        {
            var balloon = BalloonList[i];
            bool shouldActive = i < activeCount;

            balloon.OnHit -= HandleBalloonHit; // 중복 방지
            balloon.gameObject.SetActive(shouldActive);

            if (shouldActive)
                balloon.OnHit += HandleBalloonHit;
        }
    }

    private void HandleBalloonHit(Balloons b)
    {
        if (BalloonList == null)
            return;

        int index = BalloonList.IndexOf(b);
        if (index < 0 || index >= BalloonList.Count)
            return;

        var balloon = BalloonList[index];
        balloon.OnHit -= HandleBalloonHit;
        balloon.gameObject.SetActive(false);

        remainingCount = Mathf.Max(0, remainingCount - 1);
        Debug.Log($"[BalloonManager] Remaining:{remainingCount}");

        if (remainingCount == 0)
            OnAllBalloonsCleared?.Invoke();
    }
}
