using UnityEngine;
using System.Collections;

public class BuzzerPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MatchManager _MatchManager;
    [SerializeField] private SingleMatchManager _SingleMatchManager;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _SetStartBuzzerAudioSource;
    [SerializeField] private AudioSource _SetEndBuzzerAudioSource;

    [SerializeField]private float _SetEndDelay = 0.6f;

    private void Awake()
    {
        if(_MatchManager != null && _SingleMatchManager != null)
        {
            FindFirstObjectByType<MatchManager>();
            FindFirstObjectByType<SingleMatchManager>();
        }

        if(_MatchManager != null && _SingleMatchManager == null)
        {
            _MatchManager.OnSetStart += PlaySetStartBuzzer;
            _MatchManager.onSetEnd.AddListener(PlaySetEndBuzzer);
        }
        else if(_SingleMatchManager != null)
        {
            _SingleMatchManager.OnRoundStart += PlaySetStartBuzzer;
            _SingleMatchManager.onRoundEnd.AddListener(PlaySetEndBuzzer);
        }
    }

    private void PlaySetStartBuzzer()
    {
        if(_SetStartBuzzerAudioSource != null)
        {
            _SetStartBuzzerAudioSource.Play();
        }
    }

    private void PlaySetEndBuzzer()
    {
        if(_SetEndBuzzerAudioSource != null)
        {
            StartCoroutine(PlayBuzzerWithDelay(_SetEndDelay));
        }
    }

    private IEnumerator PlayBuzzerWithDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _SetEndBuzzerAudioSource.Play();
    }
}