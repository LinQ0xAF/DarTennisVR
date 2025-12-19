using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Gameplay.Match.Interfaces;

/// <summary>
/// 매치 종료 결과를 표시하는 통합 팝업 UI.
/// 멀티(재대결)와 싱글(다시하기) 모드를 모두 지원한다.
/// </summary>
public class UIMatchResultPopUp : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchManager _multiManager;
    [SerializeField] private SingleMatchManager _singleManager;
    
    [Header("Display Settings")]
    public float SpawnDistance = 1.8f;
    public float HeightOffset = 0.0f;

    [Header("Common UI")]
    [SerializeField] private GameObject _MatchResultPanel;
    [SerializeField] private GameObject _WinBanner;
    [SerializeField] private GameObject _LoseBanner;
    [SerializeField] private Button _ExitButton;
    [SerializeField] private TextMeshProUGUI _ResultStatisticsText;

    [Header("Multiplayer Specific")]
    [SerializeField] private GameObject _DrawBanner;
    [SerializeField] private TextMeshProUGUI _OpponentRematchStatusText;
    [SerializeField] private Button _RematchButton;

    [Header("Singleplayer Specific")]
    [SerializeField] private Button _RetryButton;

    private IMatchManager _activeManager;

    private TextMeshProUGUI _ExitButtonText;
    private float _AutoExitTime;
    private bool _IsMatchEnded;
    private int _LastDisplayedSeconds = -1;

#if UNITY_EDITOR
    [SerializeField] private InputActionReference _TestShowResultAction;

    private void OnEnable()
    {
        if (_TestShowResultAction != null)
            _TestShowResultAction.action.performed += ShowResultUITest;
    }
    private void ShowResultUITest(InputAction.CallbackContext context)
    {
        // 테스트용: 멀티면 로컬 승리, 싱글이면 성공으로 가정
        if (_multiManager != null) ShowMultiResult(NetworkManager.Singleton.LocalClientId);
        else ShowSingleResult(true);
    }
#endif

    private void Start()
    {
        // 1. 인스펙터 할당 우선
        _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);

        // 2. 없으면 구체적인 타입으로 검색
        if (_activeManager == null)
        {
            if (_multiManager == null) _multiManager = FindFirstObjectByType<MatchManager>();
            if (_singleManager == null) _singleManager = FindFirstObjectByType<SingleMatchManager>();
            
            _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);
        }

        if (_ExitButton != null)
        {
            _ExitButton.onClick.AddListener(OnExitButtonClicked);
            _ExitButtonText = _ExitButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        // 멀티플레이 설정
        if (_multiManager != null)
        {
            _multiManager.OnMatchResult += ShowMultiResult;
            _multiManager.OnRematchStatusChanged += HandleRematchStatusChanged;
            
            if (_RematchButton != null)
            {
                _RematchButton.gameObject.SetActive(true);
                _RematchButton.onClick.AddListener(OnRematchButtonClicked);
            }
            if (_OpponentRematchStatusText != null)
                _OpponentRematchStatusText.gameObject.SetActive(false);
                
            // 싱글 버튼 숨기기
            if (_RetryButton != null) _RetryButton.gameObject.SetActive(false);
        }
        // 싱글플레이 설정
        else if (_singleManager != null)
        {
            _singleManager.OnMatchResult += ShowSingleResult;
            
            if (_RetryButton != null)
            {
                _RetryButton.gameObject.SetActive(true);
                _RetryButton.onClick.AddListener(OnRetryButtonClicked);
            }
            
            // 멀티 버튼 숨기기
            if (_RematchButton != null) _RematchButton.gameObject.SetActive(false);
            if (_OpponentRematchStatusText != null) _OpponentRematchStatusText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_multiManager != null)
        {
            _multiManager.OnMatchResult -= ShowMultiResult;
            _multiManager.OnRematchStatusChanged -= HandleRematchStatusChanged;
        }
        else if (_singleManager != null)
        {
            _singleManager.OnMatchResult -= ShowSingleResult;
        }

#if UNITY_EDITOR
        if (_TestShowResultAction != null)
            _TestShowResultAction.action.performed -= ShowResultUITest;
#endif
    }

    private void Update()
    {
        if (_IsMatchEnded && _ExitButtonText != null)
        {
            float remaining = Mathf.Max(0, _AutoExitTime - Time.time);
            int currentSeconds = Mathf.CeilToInt(remaining);

            if (currentSeconds != _LastDisplayedSeconds)
            {
                _LastDisplayedSeconds = currentSeconds;
                _ExitButtonText.text = $"Exit ({currentSeconds}s)";
            }
        }
    }

    private void ShowPanel()
    {
        if (_MatchResultPanel == null) return;

        _IsMatchEnded = true;
        _LastDisplayedSeconds = -1;
        
        float waitSeconds = 5f;
        if (_activeManager != null) waitSeconds = _activeManager.MatchEndWaitSeconds;
        
        _AutoExitTime = Time.time + waitSeconds;

        _MatchResultPanel.SetActive(true);

        Transform cameraTr = Camera.main.transform;
        Vector3 spawnPos = cameraTr.position + (cameraTr.forward * SpawnDistance);
        spawnPos.y += HeightOffset;

        _MatchResultPanel.transform.position = spawnPos;

        Vector3 lookPos = _MatchResultPanel.transform.position - cameraTr.position;
        lookPos.y = 0;
        if (lookPos != Vector3.zero)
        {
            _MatchResultPanel.transform.rotation = Quaternion.LookRotation(lookPos);
        }
    }

    // 멀티플레이 결과 표시
    private void ShowMultiResult(ulong? winnerId)
    {
        if (winnerId == null)
        {
            if (_WinBanner != null) _WinBanner.SetActive(false);
            if (_LoseBanner != null) _LoseBanner.SetActive(false);
            if (_DrawBanner != null) _DrawBanner.SetActive(true);
        }
        else
        {
            bool isWinner = (winnerId.Value == NetworkManager.Singleton.LocalClientId);
            if (_WinBanner != null) _WinBanner.SetActive(isWinner);
            if (_LoseBanner != null) _LoseBanner.SetActive(!isWinner);
            if (_DrawBanner != null) _DrawBanner.SetActive(false);
        }
        ShowPanel();
    }

    // 싱글플레이 결과 표시
    private void ShowSingleResult(bool isSuccess)
    {
        if (_WinBanner != null) _WinBanner.SetActive(isSuccess);
        if (_LoseBanner != null) _LoseBanner.SetActive(!isSuccess);
        if (_DrawBanner != null) _DrawBanner.SetActive(false);
        
        ShowPanel();
    }

    private void OnExitButtonClicked()
    {
        if (_activeManager != null)
        {
            _activeManager.ReturnToLobby();
        }
    }

    // --- Multiplayer Rematch ---
    private void HandleRematchStatusChanged(ulong clientId, bool requested)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (_RematchButton != null)
            {
                _RematchButton.interactable = false;
                if (_OpponentRematchStatusText != null)
                {
                    _OpponentRematchStatusText.text = "Rematch Requested!";
                    _OpponentRematchStatusText.color = Color.white;
                    _OpponentRematchStatusText.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            if (_OpponentRematchStatusText != null)
            {
                _OpponentRematchStatusText.text = "Opponent wants a rematch!";
                _OpponentRematchStatusText.color = Color.green;
                _OpponentRematchStatusText.gameObject.SetActive(true);
            }
        }
    }

    private void OnRematchButtonClicked()
    {
        if (_multiManager != null) _multiManager.RequestRematch();
    }

    // --- Singleplayer Retry ---
    private void OnRetryButtonClicked()
    {
        if (_singleManager != null) _singleManager.RestartMatch();
        if (_MatchResultPanel != null) _MatchResultPanel.SetActive(false);
    }
}
