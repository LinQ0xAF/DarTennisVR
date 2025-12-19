using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(Toggle))]
public class ToggleTextUpdater : MonoBehaviour
{
    [Header("Toggle Text Settings")]
    [SerializeField]
    private string _OffText = "Left";
    [SerializeField]
    private string _OnText = "Right";

    [Header("Color Settings")]
    [SerializeField]
    private Color _OffColor = Color.gray;
    [SerializeField]
    private Color _OnColor = Color.blue;

    private Toggle _Toggle;
    private TextMeshProUGUI _ToggleText;

    void Awake()
    {
        _Toggle = GetComponent<Toggle>();
        _ToggleText = GetComponentInChildren<TextMeshProUGUI>();

        if (_ToggleText == null)
        {
            Debug.LogError("TextMeshProUGUI component not found in Toggle's children!");
            return;
        }

        // 토글 값 변경 구독 및 텍스트 업데이트 콜백 등록
        _Toggle.onValueChanged.AddListener(UpdateToggleText);
    }

    private void Start()
    {
        // 초기 값 설정
        UpdateToggleText(_Toggle.isOn);
    }

    void OnEnable()
    {
        UpdateToggleText(_Toggle.isOn);
    }

    private void UpdateToggleText(bool ToggleValue)
    {
        _ToggleText.text = ToggleValue ? _OnText : _OffText;
        _ToggleText.color = ToggleValue ? _OnColor : _OffColor;
    }

    void OnDestroy()
    {
        // 구독 해제
        if (_Toggle != null)
        {
            _Toggle.onValueChanged.RemoveListener(UpdateToggleText);
        }
    }
}
