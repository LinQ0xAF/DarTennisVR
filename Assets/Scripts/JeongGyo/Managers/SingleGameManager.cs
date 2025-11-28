using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// 싱글 플레이용 게임 흐름 매니저.
/// - SingleGameplayInitializer의 제한 시간/시작 시각을 사용해 타임업/세트 흐름 관리
/// - BalloonManager의 풍선 소진을 감시해 세트 종료
/// - 세트가 남아 있으면 다음 세트로 넘어가고, 없으면 매치 종료 후 메인 씬으로 복귀 옵션 제공
/// </summary>
public class SingleGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SingleGameplayInitializer gameplayInitializer;
    [SerializeField] private BalloonManager balloonManager;

    [Header("Set/Match Settings")]
    [SerializeField, Min(1)] private int totalSets = 1; // 초기값(초기화 시 초기화 스크립트 값으로 대체)
    [SerializeField, Min(1)] private int balloonsPerSet = 1; // 세트당 풍선 수(초기화 시 덮어씀)
    [SerializeField] private float setEndPauseSeconds = 3f; // 세트 종료 후 대기 시간

    [Header("Scene Flow")]
    [SerializeField] private bool returnToMainOnMatchEnd = true;
    [SerializeField] private string mainSceneName = "EnteranceCopy";

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent onSetEnd;            // 세트 종료 시 알림
    [SerializeField] private UnityEvent onPrepareNextSet;    // 다음 세트 준비 시 알림
    [SerializeField] private UnityEvent onGameEnd;           // 매치 종료 시 알림
    [SerializeField] private UnityEvent onTimeUp;            // 제한 시간 만료 시 알림
    [SerializeField] private UnityEvent onAllBalloonsCleared;// 모든 풍선이 사라졌을 때 알림

    /// <summary>세트 결과 알림(클리어 여부).</summary>
    public event System.Action<bool> OnSetResult;
    /// <summary>세트 수 설정 알림.</summary>
    public event System.Action<int> OnSetsConfigured;

    private bool gameEnded;
    private bool setEnding;
    private bool setsConfigured;
    private int currentSetIndex = 1;

    private void Awake()
    {
        if (gameplayInitializer == null)
            gameplayInitializer = FindFirstObjectByType<SingleGameplayInitializer>();

        // Initializer가 들고 있는 BalloonManager를 우선 사용
        if (balloonManager == null && gameplayInitializer != null)
            balloonManager = gameplayInitializer.LocalBalloonManager;

        if (balloonManager == null)
            balloonManager = FindFirstObjectByType<BalloonManager>();
    }

    private void OnEnable()
    {
        if (balloonManager != null)
            balloonManager.OnAllBalloonsCleared += HandleAllBalloonsCleared;
    }

    private void OnDisable()
    {
        if (balloonManager != null)
            balloonManager.OnAllBalloonsCleared -= HandleAllBalloonsCleared;
    }

    private IEnumerator Start()
    {
        // Initializer가 준비될 때까지 대기
        while (gameplayInitializer != null && !gameplayInitializer.IsInitialized)
            yield return null;

        ConfigureFromInitializer();
        PrepareSet(currentSetIndex);
    }

    private void Update()
    {
        if (gameEnded || setEnding || gameplayInitializer == null || !gameplayInitializer.IsInitialized)
            return;

        CheckTimeLimit();
    }

    private void CheckTimeLimit()
    {
        float elapsed = gameplayInitializer.GetElapsedLocalSeconds();
        float remain = gameplayInitializer.TimeLimitSeconds - elapsed;
        if (remain > 0f)
            return;

        StartSetEndSequence(isTimeUp: true);
    }

    private void HandleAllBalloonsCleared()
    {
        if (setEnding || gameEnded)
            return;

        onAllBalloonsCleared?.Invoke();
        StartSetEndSequence(isTimeUp: false);
    }

    /// <summary>
    /// 외부에서 강제 종료할 때 사용할 수 있는 메서드(예: 테스트용).
    /// </summary>
    public void ForceEndGame()
    {
        TriggerGameEnd(isTimeUp: false);
    }

    private void StartSetEndSequence(bool isTimeUp)
    {
        if (setEnding || gameEnded)
            return;

        setEnding = true;
        bool hasNextSet = currentSetIndex < totalSets;
        int nextSetIndex = hasNextSet ? currentSetIndex + 1 : currentSetIndex;

        onSetEnd?.Invoke();
        OnSetResult?.Invoke(!isTimeUp); // 시간초과가 아니면 성공으로 간주
        StartCoroutine(SetEndRoutine(hasNextSet, nextSetIndex, isTimeUp));
    }

    private IEnumerator SetEndRoutine(bool hasNextSet, int nextSetIndex, bool isTimeUp)
    {
        yield return new WaitForSeconds(setEndPauseSeconds);

        setEnding = false;
        if (gameEnded)
            yield break;

        if (hasNextSet)
        {
            PrepareSet(nextSetIndex);
        }
        else
        {
            TriggerGameEnd(isTimeUp);
        }
    }

    private void PrepareSet(int setIndex)
    {
        currentSetIndex = Mathf.Max(1, setIndex);
        onPrepareNextSet?.Invoke();

        ResetBalloonsForSet();
        ResetTimerToNow();
    }

    private void ResetBalloonsForSet()
    {
        if (balloonManager != null)
            balloonManager.ResetBalloons(balloonsPerSet);
    }

    private void ResetTimerToNow()
    {
        if (gameplayInitializer != null)
            gameplayInitializer.SetMatchStartTime(Time.time);
    }

    private void ConfigureFromInitializer()
    {
        if (gameplayInitializer == null || setsConfigured)
            return;

        totalSets = Mathf.Max(1, gameplayInitializer.SetCount);
        balloonsPerSet = Mathf.Max(1, gameplayInitializer.BalloonCount);
        setsConfigured = true;

        OnSetsConfigured?.Invoke(totalSets);
    }

    /// <summary>현재 총 세트 수.</summary>
    public int TotalSets => totalSets;

    private void TriggerGameEnd(bool isTimeUp)
    {
        if (gameEnded)
            return;

        gameEnded = true;

        if (isTimeUp)
            onTimeUp?.Invoke();

        onGameEnd?.Invoke();
        Debug.Log($"SingleGameManager: 매치 종료 | reason={(isTimeUp ? "time_up" : "balloons_cleared")}", this);

        if (returnToMainOnMatchEnd && !string.IsNullOrWhiteSpace(mainSceneName))
            SceneManager.LoadScene(mainSceneName);
    }
}
