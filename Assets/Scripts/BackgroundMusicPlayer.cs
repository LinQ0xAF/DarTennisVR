using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField]
    private GamePersonalDataManager _GameSettings;

    private AudioSource _AudioSource;
    private float _musicVolume = 1.0f;

    private void Awake()
    {
        if (_GameSettings == null)
        {
            Debug.LogError("BackGroundMusicPlayer: GamePersonalDataManager 참조가 인스펙터에 할당되지 않았습니다!", this);
            return;
        }

        _AudioSource = GetComponent<AudioSource>();

        // Subscribe to Master volume setting change event
        _GameSettings.OnMasterVolumeChanged += UpdateBGMSettings;

        _AudioSource.playOnAwake = true;
        _AudioSource.loop = true;
    }

    private void Start()
    {
        UpdateBGMSettings(_GameSettings.masterVolume);
    }

    private void UpdateBGMSettings(float volume)
    {
        _musicVolume = volume;
        if (_AudioSource != null)
        {
            _AudioSource.volume = _musicVolume;
        }
    }

    private void OnDestroy()
    {
        if (_GameSettings != null)
        {
            _GameSettings.OnMasterVolumeChanged -= UpdateBGMSettings;
        }
    }
}