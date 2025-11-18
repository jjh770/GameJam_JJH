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
    [SerializeField] private float _jackpotMultiplier = 3f; // 잭팟 목표 배율
    private int _jackPotCount = 0;
    private int _targetJackPotCount = 3;
    private Tween _currentTween; 

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
            // 애니메이션 중 실시간으로 텍스트 업데이트
            int displayValue = Mathf.RoundToInt(_progressSlider.value);
            _progressText.text = $"{displayValue} / {target}";

            // 색상도 실시간으로 업데이트
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
        // 다음 잭팟 목표를 3배로 증가
        _jackpotTarget = Mathf.RoundToInt(_jackpotTarget * _jackpotMultiplier);
        _progressSlider.maxValue = _jackpotTarget;

        Debug.Log($"다음 잭팟 목표: {_jackpotTarget}");

        // BetManager에 새로운 목표 설정
        BetManager.Instance.ResetJackpotProgress(_jackpotTarget);
    }
    private void OnResetJackPotCount()
    {
        _jackPotCount = 0;
        _jackpotTarget = 3000;
        BetManager.Instance.ResetJackpotProgress(_jackpotTarget);
    }

    private void OnDestroy()
    {
        // 트윈 정리
        _currentTween?.Kill();

        if (BetManager.Instance != null)
        {
            BetManager.Instance.OnBetAccumulated -= UpdateProgress;
            BetManager.Instance.OnJackpotTriggered -= OnJackpotTriggered;
            MoneyManager.Instance.OnGameWin -= OnResetJackPotCount;

        }
    }
}
