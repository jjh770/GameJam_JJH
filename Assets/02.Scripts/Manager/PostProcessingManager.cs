using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class PostProcessingManager : MonoBehaviour
{
    public static PostProcessingManager Instance { get; private set; }

    [Header("Volume Settings")]
    [SerializeField] private Volume _volume;

    private Vignette _vignette;
    private Tween _vignettePulseTween;

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeEffects();
    }

    private void InitializeEffects()
    {
        if (_volume == null)
        {
            Debug.LogError("Volume이 할당되지 않았습니다!");
            return;
        }

        // 각 효과 컴포넌트 가져오기
        _volume.profile.TryGet(out _vignette);
    }

    // Vignette 펄스 효과 시작 (0 ~ 0.3 왕복)
    public void StartVignettePulse(float duration = 2f)
    {
        if (_vignette == null) return;

        // 기존 트윈이 있으면 중지
        StopVignettePulse();

        // 0에서 0.3으로 왕복하는 무한 루프
        _vignettePulseTween = DOTween.To(
            () => _vignette.intensity.value,
            x => _vignette.intensity.value = x,
            0.5f,
            duration
        )
        .SetLoops(-1, LoopType.Yoyo) // -1은 무한 반복, Yoyo는 왕복
        .SetEase(Ease.InOutSine); // 부드러운 사인파 곡선
    }

    // Vignette 펄스 효과 중지
    public void StopVignettePulse()
    {
        if (_vignettePulseTween != null && _vignettePulseTween.IsActive())
        {
            _vignettePulseTween.Kill();
        }
    }

    // Vignette 펄스 효과 중지하고 특정 값으로 복구
    public void StopVignettePulseAndReset(float resetValue = 0f, float fadeDuration = 0.5f)
    {
        StopVignettePulse();

        if (_vignette != null)
        {
            DOTween.To(
                () => _vignette.intensity.value,
                x => _vignette.intensity.value = x,
                resetValue,
                fadeDuration
            ).SetEase(Ease.OutQuad);
        }
    }
}
