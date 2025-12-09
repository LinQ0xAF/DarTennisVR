using UnityEngine;
using System.Collections;

/// <summary>
/// 세트 시작 전 Prep 단계(카운트다운 등)를 표시하는 UI.
/// SetManager의 이벤트를 받아 UI를 켜고 끈다.
/// </summary>
public class SetPrepUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject _SetPrepPanel;
    [SerializeField] private GameObject _ReadyImage;
    [SerializeField] private GameObject _StartImage;

    [SerializeField] private float _StartMsgDuration = 1.5f;

    [Header("References")]
    [SerializeField] private SetManager setManager;

    private void Start()
    {
        if (setManager == null) setManager = FindFirstObjectByType<SetManager>();

        if (setManager != null)
        {
            setManager.OnSetPreStart += ShowPrep;
            setManager.OnSetStart += HidePrep;
        }
        
        if (_SetPrepPanel != null) 
            _SetPrepPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (setManager != null)
        {
            setManager.OnSetPreStart -= ShowPrep;
            setManager.OnSetStart -= HidePrep;
        }
    }

    private void ShowPrep()
    {
        if (_SetPrepPanel != null) 
            _SetPrepPanel.SetActive(true);
            
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

        if (_SetPrepPanel != null) 
            _SetPrepPanel.SetActive(false);
    }
}
