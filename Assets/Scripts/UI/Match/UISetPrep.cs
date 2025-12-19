using UnityEngine;
using System.Collections;
using Gameplay.Match.Interfaces;

/// <summary>
/// 세트/라운드 시작 전 Prep 단계(카운트다운 등)를 표시하는 통합 UI.
/// MatchManager(멀티) 또는 SingleMatchManager(싱글)의 이벤트를 받아 UI를 제어한다.
/// </summary>
public class UISetPrep : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchManager _multiManager;
    [SerializeField] private SingleMatchManager _singleManager;

    [Header("UI Components")]
    [SerializeField] private GameObject _PrepPanel;
    [SerializeField] private GameObject _ReadyImage;
    [SerializeField] private GameObject _StartImage;

    [SerializeField] private float _StartMsgDuration = 1.5f;

    private IMatchManager _activeManager;

    private void Awake()
    {
        _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);

        if (_activeManager == null)
        {
            if (_multiManager == null) _multiManager = FindFirstObjectByType<MatchManager>();
            if (_singleManager == null) _singleManager = FindFirstObjectByType<SingleMatchManager>();
            
            _activeManager = (_multiManager as IMatchManager) ?? (_singleManager as IMatchManager);
        }
    }

    private void Start()
    {
        if (_activeManager != null)
        {
            _activeManager.OnSetPreStart += ShowPrep;
            _activeManager.OnSetStart += HidePrep;
        }
        
        if (_PrepPanel != null) 
            _PrepPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_activeManager != null)
        {
            _activeManager.OnSetPreStart -= ShowPrep;
            _activeManager.OnSetStart -= HidePrep;
        }
    }

    private void ShowPrep()
    {
        if (_PrepPanel != null) 
            _PrepPanel.SetActive(true);
            
        if (_ReadyImage != null && _StartImage != null) 
        {
            _ReadyImage.SetActive(true);
            _StartImage.SetActive(false);
        }
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

        if (_PrepPanel != null) 
            _PrepPanel.SetActive(false);
    }
}
