using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class JackPotProgressUI : MonoBehaviour
{
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private int _jackpotTarget = 3000;
    [SerializeField] private float _tweenDuration = 0.5f;
    [SerializeField] private float _jackpotMultiplier = 3f;
    private int _jackPotCount = 0;
    private int _targetJackPotCount = 3;
    private Tween _currentTween;
    private Tween _maxValueTween; // ✅ maxValue용 별도 트윈

    void Start()
    {
        BetManager.Instance.OnBetAccumulated += UpdateProgress;
        BetManager.Instance.OnJackpotTriggered += OnJackpotTriggered;
        MoneyManager.Instance.OnGameWin += OnResetJackPotCount;
        _progressSlider.maxValue = _jackpotTarget;
        UpdateProgress(0, _jackpotTarget);
    }

    private void UpdateProgress(int current, int target)
    {
        _currentTween?.Kill();

        _currentTween = DOTween.To(
            () => _progressSlider.value,
            x => _progressSlider.value = x,
            current,
            _tweenDuration
        )
        .SetEase(Ease.OutCubic)
        .OnUpdate(() =>
        {
            int displayValue = Mathf.RoundToInt(_progressSlider.value);
            _progressText.text = $"{displayValue} / {target}";
            UpdateTextColor(displayValue);
        });
    }

    private void UpdateTextColor(int displayValue)
    {
        if (displayValue >= _jackpotTarget)
        {
            _progressSlider.fillRect.GetComponent<Image>().color = Color.green;
        }
        else if (displayValue >= _jackpotTarget * 0.8f)
        {
            _progressSlider.fillRect.GetComponent<Image>().color = Color.yellow;
        }
        else
        {
            _progressSlider.fillRect.GetComponent<Image>().color = Color.white;
        }
    }

    private void OnJackpotTriggered()
    {
        _jackPotCount++;
        Debug.Log($"잭팟 {_jackPotCount}회 달성!");

        if (_jackPotCount == _targetJackPotCount)
        {
            MoneyManager.Instance.WinTheGame();
            return;
        }

        // ✅ 이전 목표값 저장
        int previousTarget = _jackpotTarget;

        // 다음 잭팟 목표를 3배로 증가
        _jackpotTarget = Mathf.RoundToInt(_jackpotTarget * _jackpotMultiplier);

        Debug.Log($"다음 잭팟 목표: {previousTarget} -> {_jackpotTarget}");

        // ✅ 기존 트윈들 중단
        _currentTween?.Kill();
        _maxValueTween?.Kill();

        // ✅ maxValue를 새로운 목표로 애니메이션
        _maxValueTween = DOTween.To(
            () => _progressSlider.maxValue,
            x => _progressSlider.maxValue = x,
            _jackpotTarget,
            _tweenDuration
        )
        .SetEase(Ease.OutCubic);

        // ✅ value를 0으로 리셋하는 애니메이션
        _currentTween = DOTween.To(
            () => _progressSlider.value,
            x => _progressSlider.value = x,
            0, // 0으로 리셋
            _tweenDuration
        )
        .SetEase(Ease.OutCubic)
        .OnUpdate(() =>
        {
            int displayValue = Mathf.RoundToInt(_progressSlider.value);
            _progressText.text = $"{displayValue} / {_jackpotTarget}";
            UpdateTextColor(displayValue);
        })
        .OnComplete(() =>
        {
            Debug.Log($"잭팟 리셋 완료: value={_progressSlider.value}, max={_progressSlider.maxValue}");
        });

        // BetManager에 새로운 목표 설정
        BetManager.Instance.ResetJackpotProgress(_jackpotTarget);
    }

    private void OnResetJackPotCount()
    {
        _jackPotCount = 0;
        _jackpotTarget = 3000;

        // ✅ 기존 트윈들 중단
        _currentTween?.Kill();
        _maxValueTween?.Kill();

        // ✅ 슬라이더를 초기 상태로 리셋
        _progressSlider.maxValue = _jackpotTarget;
        _progressSlider.value = 0;
        _progressText.text = $"0 / {_jackpotTarget}";
        UpdateTextColor(0);

        BetManager.Instance.ResetJackpotProgress(_jackpotTarget);
    }

    private void OnDestroy()
    {
        // ✅ 모든 트윈 정리
        _currentTween?.Kill();
        _maxValueTween?.Kill();

        if (BetManager.Instance != null)
        {
            BetManager.Instance.OnBetAccumulated -= UpdateProgress;
            BetManager.Instance.OnJackpotTriggered -= OnJackpotTriggered;
            MoneyManager.Instance.OnGameWin -= OnResetJackPotCount;
        }
    }
}
