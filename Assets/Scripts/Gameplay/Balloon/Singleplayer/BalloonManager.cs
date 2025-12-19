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

    [Header("Effect Pool")]
    [SerializeField] private GameObject _PopEffectPrefab;
    [SerializeField] private int _PopEffectPoolSize = 3;
    private List<GameObject> _PopEffectPool = new List<GameObject>();

    private int remainingCount = 0;
    private bool initialized = false;

    /// <summary>모든 풍선이 제거되었을 때 알림.</summary>
    public event System.Action OnAllBalloonsCleared;

    /// <summary>하나의 풍선이 제거되었을 때 알림.</summary>
    public event System.Action<int> OnBalloonPop;

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

    private void Awake()
    {
        InitializeBalloons();
        InitializeEffectPool();
    }

    private void InitializeEffectPool()
    {
        if (_PopEffectPrefab == null) return;

        for (int i = 0; i < _PopEffectPoolSize; i++)
        {
            GameObject obj = Instantiate(_PopEffectPrefab, transform);
            obj.SetActive(false);
            _PopEffectPool.Add(obj);
        }
    }

    public GameObject GetEffectFromPool()
    {
        foreach (var obj in _PopEffectPool)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }
        return null;
    }

    /// <summary>씬에 배치된 풍선에 인덱스를 부여한다.</summary>
    private void InitializeBalloons()
    {
        if (BalloonList == null || BalloonList.Count == 0)
            return;

        for (int i = 0; i < BalloonList.Count; i++)
        {
            if (BalloonList[i] != null)
                BalloonList[i].Initialize(this, i);
        }

        initialized = true;
    }

    /// <summary>
    /// 주어진 개수로 풍선을 초기화/리셋한다.
    /// </summary>
    public void ResetBalloons(int targetCount)
    {
        if (BalloonList == null || BalloonList.Count == 0)
            return;

        if (!initialized)
            InitializeBalloons();

        int activeCount = Mathf.Clamp(targetCount, 1, BalloonList.Count);
        balloonCurrentNumber = activeCount;
        remainingCount = activeCount;

        for (int i = 0; i < BalloonList.Count; i++)
        {
            var balloon = BalloonList[i];
            bool shouldActive = i < activeCount;

            balloon.gameObject.SetActive(shouldActive);
        }
    }

    /// <summary>풍선 객체에서 히트 보고를 받을 때 호출.</summary>
    public void OnBalloonHit(int index)
    {
        if (BalloonList == null || index < 0 || index >= BalloonList.Count)
            return;

        GameObject effect = GetEffectFromPool();
        if (effect != null)
        {
            effect.transform.position = BalloonList[index].transform.position;
            effect.SetActive(true);
        }

        BalloonList[index].gameObject.SetActive(false);

        remainingCount = Mathf.Max(0, remainingCount - 1);
        Debug.Log($"[BalloonManager] Remaining:{remainingCount}");
        OnBalloonPop?.Invoke(RemainingBalloonCount);

        if (remainingCount == 0)
            OnAllBalloonsCleared?.Invoke();
    }
}
