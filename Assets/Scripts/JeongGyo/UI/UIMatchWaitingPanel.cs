using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIMatchWaitingPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UILoadingSpinner _LoadingSpinner;
    [SerializeField] private TextMeshProUGUI _StatusText;
    [SerializeField] private Button _CancelQueueingButton;
    [SerializeField] private GameObject _PanelRoot; 

    [Header("Controller Reference")]
    [SerializeField] private WLANLocalMatchmaker _Matchmaker;

    private void Awake()
    {
        if (_PanelRoot == null) _PanelRoot = gameObject;
    }

    private void OnEnable()
    {
        if (_CancelQueueingButton != null)
            _CancelQueueingButton.onClick.AddListener(OnCancelClicked);

        if (_Matchmaker != null)
        {
            _Matchmaker.OnMatchmakingStarted += HandleMatchmakingStarted;
            _Matchmaker.OnWaitingForOpponent += HandleWaitingForOpponent;
            _Matchmaker.OnJoiningRoom += HandleJoiningRoom;
            _Matchmaker.OnMatchFound += HandleMatchFound;
            _Matchmaker.OnMatchFailed += HandleMatchFailed;
            _Matchmaker.OnMatchCancelled += HandleMatchCancelled; // 추가: 취소 시 패널 닫기
        }
    }

    private void OnDisable()
    {
        if (_CancelQueueingButton != null)
            _CancelQueueingButton.onClick.RemoveListener(OnCancelClicked);

        if (_Matchmaker != null)
        {
            _Matchmaker.OnMatchmakingStarted -= HandleMatchmakingStarted;
            _Matchmaker.OnWaitingForOpponent -= HandleWaitingForOpponent;
            _Matchmaker.OnJoiningRoom -= HandleJoiningRoom;
            _Matchmaker.OnMatchFound -= HandleMatchFound;
            _Matchmaker.OnMatchFailed -= HandleMatchFailed;
            _Matchmaker.OnMatchCancelled -= HandleMatchCancelled;
        }
    }

    private void OnCancelClicked()
    {
        if (_Matchmaker != null)
        {
            _Matchmaker.CancelMatchmaking();
        }
    }

    // --- Event Handlers ---

    private void HandleMatchmakingStarted()
    {
        if(_LoadingSpinner != null) _LoadingSpinner.MatchingStarted();
        UpdateStatus("Searching for rooms...");
    }

    private void HandleMatchCancelled()
    {
        if(_LoadingSpinner != null) _LoadingSpinner.MatchingStopped();
        _PanelRoot.SetActive(false); // Panel close on cancellation
    }

    private void HandleWaitingForOpponent()
    {
        UpdateStatus("Waiting for opponent...\n(Host Mode)");
    }

    private void HandleJoiningRoom(string username)
    {
        UpdateStatus($"Found room! Joining {username}...");
    }

    private void HandleMatchFound()
    {
        UpdateStatus("Match Found! Starting game...");
        // 잠시 후 씬 전환되므로 패널은 켜둔 채로 둠 (또는 페이드아웃)
    }

    private void HandleMatchFailed(string reason)
    {
        UpdateStatus($"Matchmaking Failed: {reason}");
        // close panel after a short delay
        StartCoroutine(ClosePanelAfterDelay(5f));
    }

    private void UpdateStatus(string message)
    {
        if (_StatusText != null)
            _StatusText.text = message;
    }

    private IEnumerator ClosePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _LoadingSpinner?.MatchingStopped();
        _PanelRoot.SetActive(false);
    }
}
