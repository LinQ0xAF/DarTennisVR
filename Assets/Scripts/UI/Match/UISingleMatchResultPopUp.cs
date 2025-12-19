using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // TextMeshPro 사용 시
using UnityEngine.UI;

public class UISingleMatchResultPopUp : MonoBehaviour
{
    [Header("MatchManager Reference")]
    [SerializeField] private SingleMatchManager _MatchManager;
    
    [Header("Display Settings")]
    public float SpawnDistance = 1.8f;   // 눈앞 몇 미터?
    public float HeightOffset = 0.0f;    // 눈높이 조절

    [Header("UI Components")]
    [SerializeField] private GameObject _MatchResultPanel;
    [SerializeField] private GameObject _WinBanner;
    [SerializeField] private GameObject _LoseBanner;
    [SerializeField] private TextMeshProUGUI _ResultStatisticsText;
    [SerializeField] private Button _RetryButton;
    [SerializeField] private Button _ExitButton;

#if UNITY_EDITOR
    [SerializeField] private InputActionReference _TestShowResultAction;

    private void OnEnable()
    {
        if (_TestShowResultAction != null)
        {
            _TestShowResultAction.action.performed += ShowResultUITest;
        }
    }
    private void ShowResultUITest(InputAction.CallbackContext context)
    {
        // 테스트용: 로컬 플레이어가 승리한 것으로 가정
        ShowResultUI(true);
    }
#endif

// matchmanager에서 이벤트 추가 후 연결 예정
    private void Start()
    {
        if (_MatchManager != null)
        {
            _MatchManager.OnMatchResult += ShowResultUI;
        }
        if (_ExitButton != null)
        {
            _ExitButton.onClick.AddListener(OnExitButtonClicked);
        }
        if( _RetryButton != null)
        {
            _RetryButton.onClick.AddListener(OnRetryButtonClicked);
        }
        
    }

    private void OnDestroy()
    {
        if (_MatchManager != null)
        {
            _MatchManager.OnMatchResult -= ShowResultUI;
        }
#if UNITY_EDITOR
        if (_TestShowResultAction != null)
        {
            _TestShowResultAction.action.performed -= ShowResultUITest;
        }
#endif
    }

    // 이벤트가 발생하면 호출됨
    private void ShowResultUI(bool isSuccess)
    {
        if(_MatchResultPanel == null) return;
    
        // 승패 이미지 설정
        if (_WinBanner != null  && _LoseBanner != null)
        {
            _WinBanner.gameObject.SetActive(isSuccess);
            _LoseBanner.gameObject.SetActive(!isSuccess);
        }
        
        // VR 카메라(HMD) 위치 찾기
        Transform cameraTr = Camera.main.transform;
        
        // 패널 생성 위치 계산 (카메라 앞쪽)
        // 시선 방향으로 거리만큼 띄우고, 수직 높이는 약간 보정하거나 그대로 둠
        Vector3 spawnPos = cameraTr.position + (cameraTr.forward * SpawnDistance);
        spawnPos.y += HeightOffset; 

        _MatchResultPanel.transform.position = spawnPos;

        // 패널이 플레이어를 바라보게 회전 (Billboarding)
        // Y축 회전만 적용하여 패널이 기울어지지 않게 하는 것이 읽기 편함
        Vector3 lookPos = _MatchResultPanel.transform.position - cameraTr.position;
        lookPos.y = 0; // 수직 회전 제거
        if (lookPos != Vector3.zero)
        {
            _MatchResultPanel.transform.rotation = Quaternion.LookRotation(lookPos);
        }

        // 표시될 내용 설정 완료 후 UI 패널 활성화
        _MatchResultPanel.SetActive(true);


        // 결과 통계 텍스트 설정 (각 세트 당 점수 등?)
        if (_ResultStatisticsText != null)
        {
            // _ResultStatisticsText.text = 
        }
        
        // (부가 기능) 다시하기 / 나가기 버튼 기능 활성화 등...
    }

    private void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        _MatchResultPanel.SetActive(false); // 에디터에서는 그냥 UI 닫기
#endif
        // 매치 종료 처리 (매치메이커/네트워크 매니저 등과 연동 필요)
        if (_MatchManager != null)
        {
            _MatchManager.ReturnToLobby();
        }
    }

    private void OnRetryButtonClicked()
    {
        // 리매치 요청 처리 (매치메이커/네트워크 매니저 등과 연동 필요)
        if (_MatchManager != null)
        {
            _MatchManager.RestartMatch();
        }
        _MatchResultPanel.SetActive(false);
    }
}