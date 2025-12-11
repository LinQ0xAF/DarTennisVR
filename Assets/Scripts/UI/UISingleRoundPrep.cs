using UnityEngine;
using System.Collections;

/// <summary>
/// 라운드 시작 전 Prep 단계(카운트다운 등)를 표시하는 UI.
/// RoundManager의 이벤트를 받아 UI를 켜고 끈다.
/// </summary>
public class UISingleRoundPrep : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject _RoundPrepPanel;
    [SerializeField] private GameObject _ReadyImage;
    [SerializeField] private GameObject _StartImage;

    [SerializeField] private float _StartMsgDuration = 1.5f;

    [Header("References")]
    [SerializeField] private SingleMatchManager _MatchManager;

    private void Start()
    {
        if (_MatchManager == null) _MatchManager = FindFirstObjectByType<SingleMatchManager>();

        if (_MatchManager != null)
        {
            _MatchManager.OnRoundPreStart += ShowPrep;
            _MatchManager.OnRoundStart += HidePrep;
        }
        
        if (_RoundPrepPanel != null) 
            _RoundPrepPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_MatchManager != null)
        {
            _MatchManager.OnRoundPreStart -= ShowPrep;
            _MatchManager.OnRoundStart -= HidePrep;
        }
    }

    private void ShowPrep()
    {
        if (_RoundPrepPanel != null) 
            _RoundPrepPanel.SetActive(true);
            
        if (_ReadyImage != null && _StartImage != null) 
            _ReadyImage.SetActive(true);
            _StartImage.SetActive(false);
    }

    private void HidePrep()
    {
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        if (_ReadyImage != null && _StartImage != null) 
        {
            _ReadyImage.SetActive(false);
            _StartImage.SetActive(true);
        }

        yield return new WaitForSeconds(_StartMsgDuration);

        if (_RoundPrepPanel != null) 
            _RoundPrepPanel.SetActive(false);
    }
}
