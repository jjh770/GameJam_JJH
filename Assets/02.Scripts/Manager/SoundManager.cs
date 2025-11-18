using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public enum BGMType
    {
        BGM_1,
        BGM_2,
        BGM_3,
        BGM_4,
        BGM_5,
        BGM_6,
        BGM_7,
        BGM_8,
    }
    public enum SFXType
    {
        CoinDropSmall,
        CoinDropBig,
        CoinDropBigWow,
        GameOver,
        ScoreUp,
        Error,
        SlotMachine,
        NoPattern,
        Jackpot,
    }

    public static SoundManager Instance { get; private set; }
    [SerializeField] private AudioClip[] _sfxClips;
    [SerializeField] private AudioClip[] _bgmClips;
    [SerializeField] private Button _bgmButton;
    private AudioSource _sfxSource;
    private AudioSource _bgmSource;
    private AudioSource _heartSoundSource;
    private Coroutine _heartSoundCoroutine;
    private Coroutine _bgmCoroutine;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = false;
            _bgmSource.volume = 0.2f;
        }
        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }
        if (_heartSoundSource == null)
        {
            _heartSoundSource = gameObject.AddComponent<AudioSource>();
        }
        _bgmButton.onClick.AddListener(PlayRandomBGM);
    }

    private void Start()
    {
        StartRandomBGMPlaylist();
    }

    // 조건부 루프 사운드 재생 (조건이 false일 동안 계속 재생)
    public void PlayLoopSoundWhile(AudioClip clip, System.Func<bool> condition)
    {
        if (clip == null) return;

        StopLoopSound();
        _heartSoundCoroutine = StartCoroutine(LoopSoundWhileCondition(clip, condition));
    }
    // 조건부 루프 사운드 정지
    public void StopLoopSound()
    {
        if (_heartSoundCoroutine != null)
        {
            StopCoroutine(_heartSoundCoroutine);
            _heartSoundCoroutine = null;
        }

        if (_heartSoundSource != null && _heartSoundSource.isPlaying)
        {
            _heartSoundSource.Stop();
        }
    }
    // 조건이 false가 될 때까지 루프
    private IEnumerator LoopSoundWhileCondition(AudioClip clip, System.Func<bool> condition)
    {
        _heartSoundSource.clip = clip;
        _heartSoundSource.Play();

        while (condition())
        {
            // 클립이 끝나면 다시 재생
            if (!_heartSoundSource.isPlaying)
            {
                _heartSoundSource.Play();
            }
            yield return null;
        }

        // 조건 불만족 시 현재 루프가 끝날 때까지 기다림
        yield return new WaitUntil(() => !_heartSoundSource.isPlaying);
    }

    public bool IsLoopSoundPlaying()
    {
        return _heartSoundCoroutine != null && _heartSoundSource != null && _heartSoundSource.isPlaying;
    }

    public void StopLoopSoundImmediately()
    {
        StopLoopSound();
    }
    public void PlayRandomBGM()
    {
        if (_bgmClips == null || _bgmClips.Length == 0)
        {
            Debug.LogWarning("BGM clips array is empty or null");
            return;
        }

        int randomIndex = Random.Range(0, _bgmClips.Length);

        if (_bgmClips[randomIndex] == null)
        {
            Debug.LogWarning($"BGM clip at index {randomIndex} is null");
            return;
        }

        _bgmSource.clip = _bgmClips[randomIndex];
        _bgmSource.Play();
    }
    public void StartRandomBGMPlaylist()
    {
        if (_bgmCoroutine != null)
        {
            StopCoroutine(_bgmCoroutine);
        }
        _bgmCoroutine = StartCoroutine(PlayBGMPlaylist());
    }

    private IEnumerator PlayBGMPlaylist()
    {
        while (true)
        {
            PlayRandomBGM();

            // 현재 BGM이 끝날 때까지 대기
            yield return new WaitWhile(() => _bgmSource.isPlaying);
        }
    }

    public void StopBGMPlaylist()
    {
        if (_bgmCoroutine != null)
        {
            StopCoroutine(_bgmCoroutine);
            _bgmCoroutine = null;
        }
        _bgmSource.Stop();
    }
    public void PlayBGM(AudioClip clip)
    {
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }
    // enum으로 SFX 재생
    public void PlaySFX(SFXType sfxType, float volumeScale = 1f)
    {
        int index = (int)sfxType;

        if (_sfxClips == null || index < 0 || index >= _sfxClips.Length)
        {
            return;
        }

        if (_sfxClips[index] == null)
        {
            return;
        }

        _sfxSource.PlayOneShot(_sfxClips[index], volumeScale);
    }
}
