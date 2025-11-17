using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BetManager : MonoBehaviour
{
    public static BetManager Instance { get; private set; }

    [Header("Bet Settings")]
    [SerializeField] private int[] _betAmounts = { 10, 20, 50, 100, 200 };
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
        _betAmountText.text = $"{CurrentBetAmount}";

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

    // 베팅 금액 차감
    public bool PlaceBet()
    {
        if (!CanPlaceBet())
        {
            Debug.LogWarning("배팅 금액이 부족합니다!");
            return false;
        }

        return MoneyManager.Instance.SpendMoney(CurrentBetAmount);
    }

    // 패턴 배율에 배팅 배율을 곱한 최종 승리 금액 계산
    public int CalculateWinAmount(float patternMultiplier)
    {
        // 기준 금액(10원) × 패턴 배율 × 배팅 배율
        return Mathf.RoundToInt(BASE_BET * patternMultiplier * CurrentBetMultiplier);
    }
}
