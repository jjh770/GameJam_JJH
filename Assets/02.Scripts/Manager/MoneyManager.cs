using UnityEngine;
using System;
using DG.Tweening;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Player Money")]
    [SerializeField] private int _startMoney = 1000;
    private int _currentMoney;
    private int _displayMoney; // 화면에 표시되는 돈

    [Header("Count Animation")]
    [SerializeField] private float _countDuration = 0.5f;
    [SerializeField] private Ease _countEase = Ease.OutQuad;

    public int CurrentMoney => _currentMoney;

    public event Action<int> OnMoneyChanged;

    private Tween _countTween;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        _currentMoney = _startMoney;
        _displayMoney = _startMoney;
    }

    public void AddMoney(int amount)
    {
        // 실제 돈은 즉시 추가
        _currentMoney += amount;

        // 화면 표시는 부드럽게 카운팅
        if (_countTween != null && _countTween.IsActive())
        {
            _countTween.Kill();
        }

        _countTween = DOTween.To(
            () => _displayMoney,
            x => {
                _displayMoney = x;
                OnMoneyChanged?.Invoke(_displayMoney);
            },
            _currentMoney,
            _countDuration
        ).SetEase(_countEase);
    }

    public bool SpendMoney(int amount)
    {
        if (_currentMoney < amount) return false;

        _currentMoney -= amount;
        _displayMoney = _currentMoney; // 소비는 즉시 반영

        if (_countTween != null && _countTween.IsActive())
        {
            _countTween.Kill();
        }

        OnMoneyChanged?.Invoke(_currentMoney);
        return true;
    }

    public bool HasEnoughMoney(int amount)
    {
        return _currentMoney >= amount;
    }

    public void ResetMoney(int value)
    {
        if (_countTween != null && _countTween.IsActive())
        {
            _countTween.Kill();
        }
        _currentMoney = value;
        _displayMoney = value;
        OnMoneyChanged?.Invoke(_currentMoney);
    }
}
