using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using static SoundManager;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Player Money")]
    [SerializeField] private int _startMoney = 1000;
    private int _currentMoney;
    private int _displayMoney; // 화면에 표시되는 돈
    [SerializeField] private TMPro.TextMeshProUGUI _winAmountText;

    [Header("Count Animation")]
    [SerializeField] private float _countDuration = 0.5f;
    [SerializeField] private Ease _countEase = Ease.OutQuad;

    [SerializeField] private AudioClip _heartBeatSound;

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
        _winAmountText.enabled = false;
    }
    public void CheckCurrentMoney()
    {
        if (_currentMoney < 10 && _currentMoney >= 0)
        {
            // 300 초과하면 심장 소리 중지
            SoundManager.Instance.StopLoopSound();
        }
        else if (_currentMoney <= 300)
        {
            // 아직 재생 중이 아니면 시작
            if (!SoundManager.Instance.IsLoopSoundPlaying())
            {
                SoundManager.Instance.PlayLoopSoundWhile(_heartBeatSound, () => MoneyManager.Instance.CurrentMoney <= 300);
            }
        }
        if (_currentMoney < 10)
        {
            PostProcessingManager.Instance.StopVignettePulseAndReset(0f, 0.5f);
        }
        else if (_currentMoney <= 100)
        {
            PostProcessingManager.Instance.StartVignettePulse(1f);
        }
        else if (_currentMoney <= 300)
        {
            PostProcessingManager.Instance.StartVignettePulse(2f);
        }
        else if (_currentMoney > 300)
        {
            PostProcessingManager.Instance.StopVignettePulseAndReset(0f, 0.5f);
        }
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

    public IEnumerator WinMoney(float symbolMultiplier, float patternMultiplier, int winAmount)
    {
        _winAmountText.enabled = true;
        SoundManager.Instance.PlaySFX(SFXType.ScoreUp);
        _winAmountText.text = $"심볼X{symbolMultiplier}";
        yield return new WaitForSeconds(0.25f);
        SoundManager.Instance.PlaySFX(SFXType.ScoreUp);
        _winAmountText.text = $"패턴X{patternMultiplier}";
        yield return new WaitForSeconds(0.25f);
        // 패턴마다 누적 점수 업데이트 + 흔들림
        SoundManager.Instance.PlaySFX(SFXType.ScoreUp);
        _winAmountText.text = $"+{winAmount}";
        _winAmountText.rectTransform.DOShakeAnchorPos(1f, new Vector3(10f, 0, 0), 10, 0);
    }

    public void TotalWinMoney(int winTotalMoney)
    {
        if (winTotalMoney <= 100)
        {
            SoundManager.Instance.PlaySFX(SFXType.CoinDropSmall);
            ParticleManager.Instance.PlayParticle("Coin_verysmall", Vector3.zero);
        }
        else if (winTotalMoney <= 200)
        {
            SoundManager.Instance.PlaySFX(SFXType.CoinDropSmall);
            ParticleManager.Instance.PlayParticle("Coin_verysmall", Vector3.zero);
            ParticleManager.Instance.PlayParticle("Coin_small", Vector3.zero);
        }
        else if (winTotalMoney < 500)
        {
            SoundManager.Instance.PlaySFX(SFXType.CoinDropSmall);
            SoundManager.Instance.PlaySFX(SFXType.CoinDropBig);
            ParticleManager.Instance.PlayParticle("Coin_verysmall", Vector3.zero);
            ParticleManager.Instance.PlayParticle("Coin_small", Vector3.zero); 
            ParticleManager.Instance.PlayParticle("Coin_middle", Vector3.zero);
        }
        else if (winTotalMoney >= 500)
        {
            SoundManager.Instance.PlaySFX(SFXType.CoinDropSmall);
            SoundManager.Instance.PlaySFX(SFXType.CoinDropBig);
            SoundManager.Instance.PlaySFX(SFXType.CoinDropBigWow);
            ParticleManager.Instance.PlayParticle("Coin_verysmall", Vector3.zero);
            ParticleManager.Instance.PlayParticle("Coin_small", Vector3.zero);
            ParticleManager.Instance.PlayParticle("Coin_middle", Vector3.zero);
            ParticleManager.Instance.PlayParticle("Coin_big", Vector3.zero);

            _winAmountText.rectTransform.DOShakeAnchorPos(2f, new Vector3(30f, 0, 0), 30, 0);
            _winAmountText.transform.DOScale(2f, 2f).SetLoops(4, LoopType.Yoyo);
            return; 
        }
        // 마지막 강조 흔들림
        _winAmountText.rectTransform.DOShakeAnchorPos(0.3f, new Vector3(20f, 0, 0), 15, 0);
        _winAmountText.transform.DOScale(1.5f, 0.3f).SetLoops(2, LoopType.Yoyo);
    }
    public void InitWinMoney()
    {
        _winAmountText.enabled = false;
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

    public void CheckGameOver()
    {
        // 가장 작은 베팅 금액보다 적으면 게임 오버
        int minBetAmount = 10; // BetManager의 최소 베팅 금액

        if (_currentMoney < minBetAmount)
        {
            SoundManager.Instance.PlaySFX(SFXType.GameOver);

            PopupManager.Instance.ShowGameOverPopup(() => {
                // 게임 재시작 로직
                // 누적 배팅 금액 초기화
                if (BetManager.Instance != null)
                {
                    BetManager.Instance.ResetCumulativeBet();
                }
                ResetMoney(1000); // 초기 자금으로 리셋
            });
        }
    }
}
