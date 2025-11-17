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
    [SerializeField] private TMPro.TextMeshProUGUI _winAmountText;
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeStrength = 10f;

    [Header("Spin Settings")]
    [SerializeField] private float _reelStopDelay = 0.15f; // 릴 간 정지 간격
    [SerializeField] private int _betAmount = 10; // 베팅액

    [Header("Result Checker")]
    [SerializeField] private SlotResultChecker _resultChecker;

    private bool _isSpinning = false;
    private MoneyManager _moneyManager;

    void Start()
    {
        InitializeSlotMachine();
        _moneyManager = MoneyManager.Instance;
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
        if (!_moneyManager.SpendMoney(_betAmount)) yield break;
        _isSpinning = true;
        _spinButton.interactable = false;

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
    }

    private IEnumerator HighlightAndPayPatterns(List<(string patternName, float multiplier, List<(int row, int col)> matchedPositions)> results)
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

            int winAmount = Mathf.RoundToInt(_betAmount * result.multiplier);
            totalWinAmount += winAmount;

            yield return sequence.WaitForCompletion();

            // 패턴마다 누적 점수 업데이트 + 흔들림
            _winAmountText.text = $"+{totalWinAmount}";
            _winAmountText.rectTransform.DOShakeAnchorPos(0.2f, new Vector3(10f, 0, 0), 10, 0);

            yield return new WaitForSeconds(0.5f);
        }

        // 최종 확정
        if (totalWinAmount > 0)
        {
            // 마지막 강조 흔들림
            _winAmountText.rectTransform.DOShakeAnchorPos(0.3f, new Vector3(20f, 0, 0), 15, 0);
            _winAmountText.transform.DOScale(1.5f, 0.3f).SetLoops(2, LoopType.Yoyo);

            yield return new WaitForSeconds(0.8f);

            _moneyManager.AddMoney(totalWinAmount);
            Debug.Log($"Total Win: {totalWinAmount}");

            // 텍스트 초기화 (선택사항)
            _winAmountText.text = "";
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

    private IEnumerator HighlightMatchedPatterns(List<(string patternName, float multiplier, List<(int row, int col)> matchedPositions)> results)
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
