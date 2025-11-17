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
    [SerializeField] private Button _testButton;
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeStrength = 10f;

    [Header("Spin Settings")]
    [SerializeField] private float _reelStopDelay = 0.15f; // 릴 간 정지 간격
    [SerializeField] private int _betAmount = 10; // 베팅액

    [Header("Result Checker")]
    [SerializeField] private SlotResultChecker _resultChecker;

    private bool _isSpinning = false;
    private MoneyManager _moneyManager;
    private BetManager _betManager;
    void Start()
    {
        InitializeSlotMachine();
        _moneyManager = MoneyManager.Instance;
        _betManager = BetManager.Instance;
        _spinButton.onClick.AddListener(OnSpinButtonClick);
        _testButton.onClick.AddListener(OnTestButtonClick);
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
            Debug.LogWarning("잔액이 부족합니다!");
            return;
        }
        StartCoroutine(SpinSequence());
    }
    public void OnTestButtonClick()
    {
        int[][] debugResult = new int[][]
        {
            new int[] {0, 0, 0},
            new int[] {0, 0, 0},
            new int[] {0, 0, 0},
            new int[] {0, 0, 0},
            new int[] {0, 0, 0}
        };
        SetDebugResult(debugResult);
        ProcessResults();
    }

    private IEnumerator SpinSequence()
    {
        // BetManager를 통해 배팅 금액 차감
        if (!_betManager.PlaceBet()) yield break;
        _isSpinning = true;
        _spinButton.interactable = false;
        _betManager.SetBetButtonsInteractable(false);

        foreach (var reel in _reels)
        {
            reel.StartSpin();
        }

        yield return new WaitForSeconds(1.0f);

        for (int i = 0; i < _reels.Length; i++)
        {
            _reels[i].StopSpin();
            yield return new WaitForSeconds(_reelStopDelay);
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
            yield return StartCoroutine(HighlightAndPayPatterns(results));
        }

        _isSpinning = false;
        _spinButton.interactable = true;
        _betManager.SetBetButtonsInteractable(true);
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

            _moneyManager.WinMoney(totalWinAmount);


            yield return new WaitForSeconds(0.5f);
        }

        // 최종 확정
        if (totalWinAmount > 0)
        {
            _moneyManager.TotalWinMoney(totalWinAmount);
            if (totalWinAmount >= 500)
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

    // 디버그: 특정 결과 강제 설정
    public void SetDebugResult(int[][] debugResult)
    {
        for (int i = 0; i < _reels.Length && i < debugResult.Length; i++)
        {
            _reels[i].StopSpin(debugResult[i]);
        }
    }
}
