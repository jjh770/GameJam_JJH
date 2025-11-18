using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineManager : MonoBehaviour
{
    [Header("Reels")]
    [SerializeField] private Reel[] _reels; // 5개의 릴
    [Header("Symbols")]
    [SerializeField] private List<SlotSymbol> _allSymbols = new List<SlotSymbol>();
    [Header("Reel Strips")]
    [SerializeField] private ReelStrip[] _reelStrips; // 5개 (릴마다 다른 확률 가능)
    [Header("UI")]
    [SerializeField] private Button _spinButton;
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeStrength = 10f;

    [Header("Spin Settings")]
    [SerializeField] private float _reelStopDelay = 0.15f; // 릴 간 정지 간격
    [SerializeField] private int _betAmount = 10; // 베팅액

    [Header("Result Checker")]
    [SerializeField] private SlotChecker _resultChecker;
    [Header("Jackpot Settings")]
    [SerializeField] private int _jackpotSymbolID = 0; // 잭팟에 사용할 심볼 ID

    private bool _isSpinning = false;
    private MoneyManager _moneyManager;
    private BetManager _betManager;
    void Start()
    {
        InitializeSlotMachine();
        _moneyManager = MoneyManager.Instance;
        _betManager = BetManager.Instance;
        _spinButton.onClick.AddListener(OnSpinButtonClick);
    }

    private void InitializeSlotMachine()
    {
        // 각 릴 초기화
        for (int i = 0; i < _reels.Length; i++)
        {
            ReelStrip strip = (i < _reelStrips.Length) ? _reelStrips[i] : _reelStrips[0];
            _reels[i].Initialize(strip, _allSymbols);
        }
    }

    public void OnSpinButtonClick()
    {
        if (_isSpinning) return;
        // BetManager를 통한 배팅 확인 및 차감
        if (!_betManager.CanPlaceBet())
        {
            PopupManager.Instance.ShowInsufficientFundsPopup();
            return;
        }
        StartCoroutine(SpinSequence());
    }

    private IEnumerator SpinSequence()
    {
        // 잭팟 체크는 배팅 **전에** 수행 (이전 스핀의 누적 결과로 판단)
        bool shouldTriggerJackpot = _betManager.ShouldTriggerJackpot;
        AdjustDifficultyByMoney(_moneyManager.CurrentMoney);

        // BetManager를 통해 배팅 금액 차감
        if (!_betManager.PlaceBet()) yield break;
        _isSpinning = true;
        SoundManager.Instance.PlaySFX(SoundManager.SFXType.SlotMachine);
        _spinButton.interactable = false;
        _betManager.SetBetButtonsInteractable(false);


        if (shouldTriggerJackpot)
        {
            Debug.Log("잭팟 발동!");
            // 랜덤 심볼 선택 (고배율 심볼 중에서)
            int jackpotSymbol = GetRandomJackpotSymbol();
            SetJackpotResult(jackpotSymbol);
            _betManager.ResetCumulativeBet();
            _betManager.TriggerJackpot(); 
        }

        foreach (var reel in _reels)
        {
            reel.StartSpin();
        }

        yield return new WaitForSeconds(2f);

        // 잭팟이면 동시 정지, 아니면 순차 정지
        if (shouldTriggerJackpot)
        {
            foreach (var reel in _reels)
            {
                reel.StopSpin();
            }
        }
        else
        {
            for (int i = 0; i < _reels.Length; i++)
            {
                _reels[i].StopSpin();
                yield return new WaitForSeconds(_reelStopDelay);
            }
        }

        yield return new WaitUntil(() => AllReelsStopped());

        // 결과 체크
        int[,] resultGrid = new int[3, 5];
        for (int col = 0; col < 5; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                resultGrid[row, col] = _reels[col].FinalResult[row];
            }
        }

        var results = _resultChecker.CheckResults(resultGrid);

        // 하이라이트 + 돈 지급
        if (results.Count > 0)
        {
            if (shouldTriggerJackpot)
            {
                SoundManager.Instance.PlaySFX(SoundManager.SFXType.Jackpot);
            }
            yield return StartCoroutine(HighlightAndPayPatterns(results));
        }
        else if (results.Count == 0)
        {
            SoundManager.Instance.PlaySFX(SoundManager.SFXType.NoPattern);
        }
        // 스핀 종료 후 다음 스핀 잭팟 준비 체크
        _betManager.CheckAndPrepareJackpot();

        _moneyManager.CheckCurrentMoney();
        _moneyManager.CheckGameOver();
        _isSpinning = false;
        _spinButton.interactable = true;
        _betManager.SetBetButtonsInteractable(true);
        AdjustDifficultyByMoney(_moneyManager.CurrentMoney);
    }

    // 랜덤 잭팟 심볼 선택 (고배율 심볼 중에서)
    private int GetRandomJackpotSymbol()
    {
        // 배율이 높은 심볼들만 필터링 (예: SymbolMultiplier >= 5)
        var highValueSymbols = _allSymbols.FindAll(s => s.SymbolMultiplier >= 1f);
        return highValueSymbols[UnityEngine.Random.Range(0, highValueSymbols.Count)].SymbolID;
    }

    // 잭팟 결과 강제 설정 (5x3 전부 같은 심볼)
    private void SetJackpotResult(int symbolID)
    {
        JackPotSymbolWeight(symbolID);
    }

    private IEnumerator HighlightAndPayPatterns(List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)> results)
    {
        _spinButton.interactable = false;
        int totalWinAmount = 0;

        foreach (var result in results)
        {
            Sequence sequence = DOTween.Sequence();

            foreach (var pos in result.matchedPositions)
            {
                var symbolIdentifier = _reels[pos.col].GetSymbolIdentifierAtRow(pos.row);
                if (symbolIdentifier != null)
                {
                    Sequence blink = symbolIdentifier.BlinkHighlight(2, 0.2f);
                    sequence.Join(blink);
                }
            }

            // 심볼 배율 가져오기
            SlotSymbol matchedSymbol = _allSymbols.Find(s => s.SymbolID == result.symbolID);
            float symbolMultiplier = matchedSymbol != null ? matchedSymbol.SymbolMultiplier : 1.0f;
            Debug.Log(symbolMultiplier);
            // 최종 배율 = 패턴 배율 × 심볼 배율
            float finalMultiplier = result.multiplier * symbolMultiplier;

            // 승리 금액 계산
            int actualWinAmount = _betManager.CalculateWinAmount(finalMultiplier);
            totalWinAmount += actualWinAmount;

            yield return sequence.WaitForCompletion();

            yield return _moneyManager.WinMoney(symbolMultiplier, result.multiplier, totalWinAmount);

            yield return new WaitForSeconds(0.3f);
        }

        // 최종 확정
        if (totalWinAmount > 0)
        {
            _moneyManager.TotalWinMoney(totalWinAmount);
            if (totalWinAmount >= 3000)
            {
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            _moneyManager.AddMoney(totalWinAmount);
            Debug.Log($"Total Win: {totalWinAmount}");

            // 텍스트 초기화 (선택사항)
            _moneyManager.InitWinMoney();
        }

        _spinButton.interactable = true;
    }

    private bool AllReelsStopped()
    {
        foreach (var reel in _reels)
        {
            if (reel.IsSpinning)
                return false;
        }
        return true;
    }

    private IEnumerator HighlightMatchedPatterns(List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)> results)
    {
        _spinButton.interactable = false;

        foreach (var result in results)
        {
            Sequence sequence = DOTween.Sequence();

            // 각 심볼의 깜빡임을 시퀀스에 추가 (동시 실행)
            foreach (var pos in result.matchedPositions)
            {
                var symbolIdentifier = _reels[pos.col].GetSymbolIdentifierAtRow(pos.row);
                if (symbolIdentifier != null)
                {
                    Sequence blink = symbolIdentifier.BlinkHighlight();
                    sequence.Join(blink); 
                }
            }

            // 시퀀스가 끝날 때까지 대기
            yield return sequence.WaitForCompletion();
            yield return new WaitForSeconds(0.15f);
        }

        _spinButton.interactable = true;
    }

    private IEnumerator ProcessResults()
    {
        int[,] resultGrid = new int[3, 5];
        for (int col = 0; col < 5; col++)
        {
            for (int row = 0; row < 3; row++)
            {
                resultGrid[row, col] = _reels[col].FinalResult[row];
            }
        }

        var results = _resultChecker.CheckResults(resultGrid);

        foreach (var result in results)
        {
            Debug.Log($"Pattern: {result.patternName}, Multiplier: {result.multiplier}");
        }

        if (results.Count > 0)
        {
            yield return StartCoroutine(HighlightMatchedPatterns(results));
        }
    }

    // 특정 릴의 특정 심볼 Weight 변경
    public void ModifySymbolWeight(int reelIndex, int symbolID, int newWeight)
    {
        if (reelIndex < 0 || reelIndex >= _reelStrips.Length)
        {
            Debug.LogWarning($"릴 인덱스 범위 초과: {reelIndex}");
            return;
        }

        _reelStrips[reelIndex].SetSymbolWeight(symbolID, newWeight);
    }
    public void JackPotSymbolWeight(int symbolID)
    {
        for (int i = 0; i < _reelStrips.Length; i++)
        {
            for (int j = 0; j < 8; j++) 
            {
                _reelStrips[i].SetSymbolWeight(j, 0);
            }
            _reelStrips[i].SetSymbolWeight(symbolID, 1);
        }
    }
    // 난이도 조정
    public void SetDifficulty(string difficulty)
    {
        foreach (var strip in _reelStrips)
        {
            switch (difficulty.ToLower())
            {
                case "very easy":  // 고가치 심볼 출현 빈도 매우 높음
                    strip.SetSymbolWeight(0, 8);
                    strip.SetSymbolWeight(1, 8);
                    strip.SetSymbolWeight(2, 8);
                    strip.SetSymbolWeight(3, 8);
                    strip.SetSymbolWeight(4, 6);
                    strip.SetSymbolWeight(5, 6);
                    strip.SetSymbolWeight(6, 28);
                    strip.SetSymbolWeight(7, 28);
                    break;

                case "easy":  // 고가치 심볼 출현 빈도 높음
                    strip.SetSymbolWeight(0, 12);
                    strip.SetSymbolWeight(1, 12);
                    strip.SetSymbolWeight(2, 8);
                    strip.SetSymbolWeight(3, 8);
                    strip.SetSymbolWeight(4, 10);
                    strip.SetSymbolWeight(5, 10);
                    strip.SetSymbolWeight(6, 20);
                    strip.SetSymbolWeight(7, 20);
                    break;
                case "hard":  // 저가치 심볼 위주
                    strip.SetSymbolWeight(0, 20);  
                    strip.SetSymbolWeight(1, 20);  
                    strip.SetSymbolWeight(2, 15);  
                    strip.SetSymbolWeight(3, 10);  
                    strip.SetSymbolWeight(4, 10);   
                    strip.SetSymbolWeight(5, 10);
                    strip.SetSymbolWeight(6, 10);
                    strip.SetSymbolWeight(7, 5);   
                    break;

                case "very hard":  // 고가치 심볼 거의 안나옴
                    strip.SetSymbolWeight(0, 15);
                    strip.SetSymbolWeight(1, 15);
                    strip.SetSymbolWeight(2, 15);
                    strip.SetSymbolWeight(3, 15);
                    strip.SetSymbolWeight(4, 15); 
                    strip.SetSymbolWeight(5, 15); 
                    strip.SetSymbolWeight(6, 5);
                    strip.SetSymbolWeight(7, 5);
                    break;

                case "normal":
                default:
                    strip.ResetToOriginalWeights();
                    break;
            }
        }
        Debug.Log($"난이도 {difficulty.ToUpper()}로 설정");
    }

    // 현재 돈에 따라 동적 난이도 조정
    public void AdjustDifficultyByMoney(int currentMoney)
    {
        if (currentMoney < 200)
        {
            SetDifficulty("very easy");
        }
        else if (currentMoney <= 1000)
        {
            SetDifficulty("easy");
        }
        else if (currentMoney >= 3000)
        {
            SetDifficulty("hard");
        }
        else if (currentMoney >= 5000)
        {
            SetDifficulty("hard");
        }
        else
        {
            SetDifficulty("normal");
        }
    }

    // 모든 Weight 리셋
    public void ResetAllWeights()
    {
        foreach (var strip in _reelStrips)
        {
            strip.ResetToOriginalWeights();
        }
    }

    // 현재 Weight 확인 (디버그)
    public void DebugPrintWeights()
    {
        foreach (var strip in _reelStrips)
        {
            strip.PrintCurrentWeights();
        }
    }
}
