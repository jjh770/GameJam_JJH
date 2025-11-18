using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetManager : MonoBehaviour
{
    public static BetManager Instance { get; private set; }

    [Header("Jackpot Settings")]
    [SerializeField] private int _jackpotTriggerAmount = 3000; // 잭팟 발동 누적 금액
    private int _cumulativeBetAmount = 0; // 누적 배팅 금액
    private bool _jackpotReadyForNextSpin = false; // 다음 스핀에 잭팟 준비됨
    public int CumulativeBetAmount => _cumulativeBetAmount;
    public int RemainingToJackpot => Mathf.Max(0, _jackpotTriggerAmount - _cumulativeBetAmount);

    // 현재 스핀에서 잭팟을 발동할지 여부 (다음 스핀용 플래그)
    public bool ShouldTriggerJackpot => _jackpotReadyForNextSpin;

    public event Action<int, int> OnBetAccumulated; // (현재 누적, 목표 금액)
    public event Action OnJackpotTriggered;

    [Header("Bet Settings")]
    [SerializeField] private int[] _betAmounts = { 10, 20, 50, 100, 200, 500, 1000 };
    private int _currentBetIndex = 0;
    private const int BASE_BET = 10; // 기준 배팅 금액

    [Header("UI References")]
    [SerializeField] private Button _increaseBetButton;
    [SerializeField] private Button _decreaseBetButton;
    [SerializeField] private TextMeshProUGUI _betAmountText;
    [SerializeField] private TextMeshProUGUI _betMultiplierText; // "1x", "2x" 등 표시

    public int CurrentBetAmount => _betAmounts[_currentBetIndex];
    public float CurrentBetMultiplier => (float)CurrentBetAmount / BASE_BET;

    public event Action<int> OnBetChanged;

    [Header("Count Animation")]
    [SerializeField] private float _countDuration = 0.5f;
    [SerializeField] private Ease _countEase = Ease.OutQuad;
    private Tween _betTween;
    private int _displayBetAmount; // 화면에 표시되는 베팅 금액

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (_increaseBetButton == null || _decreaseBetButton == null)
        {
            return;
        }

        _decreaseBetButton.onClick.AddListener(DecreaseBet);
        _increaseBetButton.onClick.AddListener(IncreaseBet);

        // 초기 표시 금액 설정
        _displayBetAmount = CurrentBetAmount;
        UpdateBetUI();
        UpdateButtonStates();
    }

    public void IncreaseBet()
    {
        if (_currentBetIndex < _betAmounts.Length - 1)
        {
            _currentBetIndex++;
            UpdateBetUI();
            UpdateButtonStates();
            OnBetChanged?.Invoke(CurrentBetAmount);
        }
    }

    public void DecreaseBet()
    {
        if (_currentBetIndex > 0)
        {
            _currentBetIndex--;
            UpdateBetUI();
            UpdateButtonStates();
            OnBetChanged?.Invoke(CurrentBetAmount);
        }
    }

    private void UpdateBetUI()
    {
        // 기존 애니메이션이 진행 중이면 중지
        if (_betTween != null && _betTween.IsActive())
        {
            _betTween.Kill();
        }

        // 배팅 금액 카운팅 애니메이션
        _betTween = DOTween.To(
            () => _displayBetAmount,
            x => {
                _displayBetAmount = x;
                _betAmountText.text = $"{_displayBetAmount}";
            },
            CurrentBetAmount,
            _countDuration
        ).SetEase(_countEase);

        // 배율 텍스트는 즉시 업데이트
        if (_betMultiplierText != null)
        {
            _betMultiplierText.text = $"배팅 금액 (배수 : {CurrentBetMultiplier}x)";
        }
    }

    // 배팅 버튼 활성화/비활성화 (스핀 중 사용)
    public void SetBetButtonsInteractable(bool interactable)
    {
        if (interactable)
        {
            // 활성화할 때는 기존 로직에 따라 조건부 활성화
            UpdateButtonStates();
        }
        else
        {
            // 비활성화할 때는 무조건 비활성화
            _increaseBetButton.interactable = false;
            _decreaseBetButton.interactable = false;
        }
    }

    private void UpdateButtonStates()
    {
        // 최소/최대 베팅에서 버튼 비활성화
        _decreaseBetButton.interactable = _currentBetIndex > 0;
        _increaseBetButton.interactable = _currentBetIndex < _betAmounts.Length - 1;
    }

    // 현재 베팅 금액으로 스핀 가능한지 체크
    public bool CanPlaceBet()
    {
        return MoneyManager.Instance.HasEnoughMoney(CurrentBetAmount);
    }

    public bool PlaceBet()
    {
        if (!CanPlaceBet())
        {
            PopupManager.Instance.ShowInsufficientFundsPopup();
            return false;
        }

        if (MoneyManager.Instance.SpendMoney(CurrentBetAmount))
        {
            // 배팅 후 누적 금액 증가
            _cumulativeBetAmount += CurrentBetAmount;
            OnBetAccumulated?.Invoke(_cumulativeBetAmount, _jackpotTriggerAmount);

            Debug.Log($"누적 배팅: {_cumulativeBetAmount}/{_jackpotTriggerAmount}");

            return true;
        }

        return false;
    }

    // 스핀 직후에 호출: 다음 스핀에서 잭팟을 터뜨릴지 체크
    public void CheckAndPrepareJackpot()
    {
        if (_cumulativeBetAmount >= _jackpotTriggerAmount && !_jackpotReadyForNextSpin)
        {
            _jackpotReadyForNextSpin = true;
        }
    }
    // 잭팟 트리거 호출 메서드 (외부에서 호출 가능)
    public void TriggerJackpot()
    {
        OnJackpotTriggered?.Invoke();
        _jackpotReadyForNextSpin = false; // 잭팟 발동 후 플래그 리셋
    }
    // 잭팟 트리거 후 리셋
    public void ResetCumulativeBet()
    {
        _cumulativeBetAmount = 0;
        OnBetAccumulated?.Invoke(_cumulativeBetAmount, _jackpotTriggerAmount);
        _jackpotReadyForNextSpin = false; // 게임 오버 시 플래그도 리셋
        Debug.Log("누적 배팅 금액 리셋");
    }
    // 패턴 배율에 배팅 배율을 곱한 최종 승리 금액 계산
    public int CalculateWinAmount(float patternMultiplier)
    {
        // 기준 금액(10원) × 패턴 배율 × 배팅 배율
        return Mathf.RoundToInt(BASE_BET * patternMultiplier * CurrentBetMultiplier);
    }
    // BetManager.cs에 추가
    public void ResetJackpotProgress(int newTarget)
    {
        // 새로운 잭팟 목표 설정
        _jackpotTriggerAmount = newTarget;

        // 누적 배팅 금액 초기화
        _cumulativeBetAmount = 0;

        // 잭팟 준비 플래그 리셋
        _jackpotReadyForNextSpin = false;

        // UI 업데이트를 위한 이벤트 발생
        OnBetAccumulated?.Invoke(_cumulativeBetAmount, _jackpotTriggerAmount);

        Debug.Log($"잭팟 리셋 완료. 새로운 목표: {_jackpotTriggerAmount}");
    }
}
