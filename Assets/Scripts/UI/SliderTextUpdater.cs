using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[RequireComponent(typeof(Slider))]
public class SliderTextUpdater : MonoBehaviour
{
    public enum DisplayFormat
    {
        Integer,
        Time_30Sec_Steps,
        Percentage
    }

    [Header("Format Settings")]
    [SerializeField]
    private DisplayFormat _DisplayFormat = DisplayFormat.Integer;

    private Slider _Slider;
    private TextMeshProUGUI _SliderValueText;

    void Awake()
    {
        _Slider = GetComponent<Slider>();
        _SliderValueText = GetComponentInChildren<TextMeshProUGUI>();

        if (_SliderValueText == null)
        {
            Debug.LogError("TextMeshProUGUI component not found in Slider's children!");
            return;
        }

        // 슬라이더 값 변경 구독 및 텍스트 업데이트 콜백 등록
        _Slider.onValueChanged.AddListener(UpdateSliderText);
    }

    private void Start()
    {
        // 초기 값 설정
        UpdateSliderText(_Slider.value);
    }

    private void UpdateSliderText(float value)
    {
        switch (_DisplayFormat)
        {
            case DisplayFormat.Integer:
                _SliderValueText.text = Mathf.RoundToInt(value).ToString();
                break;

            case DisplayFormat.Time_30Sec_Steps:
                int totalSeconds = Mathf.RoundToInt(value) * 30;
                if(totalSeconds == 0)
                {
                    _SliderValueText.text = "∞";
                    break;
                }
                TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
                _SliderValueText.text = timeSpan.ToString(@"m\:ss");
                break;
            
            case DisplayFormat.Percentage:
                _SliderValueText.text = Mathf.RoundToInt(value * 100).ToString();   // 볼륨 용도
                break;

            default:
                _SliderValueText.text = value.ToString();
                break;
        }
    }

    void OnDestroy()
    {
        // 구독 해제
        if (_Slider != null)
        {
            _Slider.onValueChanged.RemoveListener(UpdateSliderText);
        }
    }
}
