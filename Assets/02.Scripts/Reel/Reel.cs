using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class Reel : MonoBehaviour
{
    [Header("Reel Components")]
    [SerializeField] private Transform _reelContainer;
    [SerializeField] private GameObject[] _symbol;
    private SpriteRenderer[] _symbolSlots;

    [Header("Reel Settings")]
    [SerializeField] private float _spinSpeed = 10f;
    [SerializeField] private float _minSpinDuration = 1.5f;
    [SerializeField] private float _stopDuration = 0.5f;
    [SerializeField] private float _symbolHeight = 2f;

    private ReelStrip _reelStrip;
    private List<SlotSymbol> _allSymbols;
    private bool _isSpinning = false;
    private bool _shouldStop = false;
    private int[] _finalResult = new int[3];

    private List<GameObject> _symbolPool = new List<GameObject>();

    // 결과 심볼을 미리 큐에 넣어 자연스럽게 순환
    private Queue<int> _resultQueue = new Queue<int>();
    private bool _resultPrepared = false;

    public bool IsSpinning => _isSpinning;
    public int[] FinalResult => _finalResult;

    private void Awake()
    {
        _symbolSlots = new SpriteRenderer[_symbol.Length];

        for (int i = 0; i < _symbol.Length; i++)
        {
            _symbolSlots[i] = _symbol[i].GetComponent<SpriteRenderer>();
        }
    }
    public void Initialize(ReelStrip strip, List<SlotSymbol> symbols)
    {
        _reelStrip = strip;
        _allSymbols = symbols;
        _reelStrip.GenerateStrip();

        CreateSymbolPool();
        SetRandomSymbols();
    }

    private void CreateSymbolPool()
    {
        foreach (var slot in _symbolSlots)
        {
            SymbolIdentifier identifier = slot.gameObject.GetComponent<SymbolIdentifier>();
            if (identifier == null)
            {
                identifier = slot.gameObject.AddComponent<SymbolIdentifier>();
            }

            _symbolPool.Add(slot.gameObject);
        }

        // 위쪽 추가 심볼
        GameObject topSymbol = new GameObject("Symbol_Top");
        topSymbol.transform.SetParent(_reelContainer);
        topSymbol.transform.localPosition = new Vector3(0, _symbolHeight * 2, 0);

        SymbolIdentifier topIdentifier = topSymbol.AddComponent<SymbolIdentifier>();
        SpriteRenderer topRenderer = topSymbol.GetComponent<SpriteRenderer>();
        topRenderer.sortingLayerName = "Symbol";
        topRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        _symbolPool.Insert(0, topSymbol);

        // 아래쪽 추가 심볼
        GameObject bottomSymbol = new GameObject("Symbol_Bottom");
        bottomSymbol.transform.SetParent(_reelContainer);
        bottomSymbol.transform.localPosition = new Vector3(0, -_symbolHeight * 2, 0);

        SymbolIdentifier bottomIdentifier = bottomSymbol.AddComponent<SymbolIdentifier>();
        SpriteRenderer bottomRenderer = bottomSymbol.GetComponent<SpriteRenderer>();
        bottomRenderer.sortingLayerName = "Symbol";
        bottomRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        _symbolPool.Add(bottomSymbol);
    }

    public void StartSpin()
    {
        if (!_isSpinning)
            StartCoroutine(SpinRoutine());
    }

    public void StopSpin(int[] targetSymbols = null)
    {
        _shouldStop = true;

        if (targetSymbols != null)
        {
            _finalResult = targetSymbols;
        }
        else
        {
            for (int i = 0; i < 3; i++)
                _finalResult[i] = _reelStrip.GetRandomSymbol();
        }

        // 결과를 큐에 준비 (나중에 자연스럽게 순환됨)
        PrepareResultQueue();
    }

    // 결과 심볼을 큐에 준비 (역순으로 - 위에서 아래로 내려오므로)
    private void PrepareResultQueue()
    {
        _resultQueue.Clear();

        // 위에서부터 차례로 나타날 순서
        // 추가 랜덤 심볼 몇 개 + 최종 결과 3개
        for (int i = 0; i < 3; i++)
        {
            _resultQueue.Enqueue(_reelStrip.GetRandomSymbol());
        }

        // 최종 결과 심볼들 (위 → 중간 → 아래)
        for (int i = 0; i < 3; i++)
        {
            _resultQueue.Enqueue(_finalResult[i]);
        }

        _resultPrepared = true;
    }

    private IEnumerator SpinRoutine()
    {
        _isSpinning = true;
        _shouldStop = false;
        _resultPrepared = false;
        float elapsedTime = 0f;

        while (!_shouldStop || elapsedTime < _minSpinDuration)
        {
            foreach (var symbolObj in _symbolPool)
            {
                symbolObj.transform.position += Vector3.down * _spinSpeed * Time.deltaTime;
            }

            if (_symbolPool[_symbolPool.Count - 1].transform.localPosition.y < -_symbolHeight * 2.5f)
            {
                RecycleBottomSymbol();
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(SmoothStop());
        _isSpinning = false;
    }

    private void RecycleBottomSymbol()
    {
        GameObject bottomSymbol = _symbolPool[_symbolPool.Count - 1];
        GameObject topSymbol = _symbolPool[0];

        float newY = topSymbol.transform.localPosition.y + _symbolHeight;
        bottomSymbol.transform.localPosition = new Vector3(0, newY, 0);

        SymbolIdentifier identifier = bottomSymbol.GetComponent<SymbolIdentifier>();
        if (identifier != null)
        {
            if (_resultPrepared && _resultQueue.Count > 0)
            {
                // 큐에서 결과 심볼 ID 가져오기
                int symbolID = _resultQueue.Dequeue();
                identifier.SetSymbolByID(symbolID, _allSymbols);
            }
            else
            {
                // 랜덤 심볼
                identifier.SetRandomSymbol(_reelStrip, _allSymbols);
            }
        }

        _symbolPool.RemoveAt(_symbolPool.Count - 1);
        _symbolPool.Insert(0, bottomSymbol);
    }

    public SymbolIdentifier GetSymbolIdentifierAtRow(int row)
    {
        // row: 0 = 위, 1 = 중간, 2 = 아래
        // _symbolPool 구조: [0]=맨위추가, [1]=위, [2]=중간, [3]=아래, [4]=맨아래추가
        int poolIndex = row + 1; // row 0,1,2 → poolIndex 1,2,3

        if (poolIndex >= 0 && poolIndex < _symbolPool.Count)
        {
            return _symbolPool[poolIndex].GetComponent<SymbolIdentifier>();
        }
        return null;
    }

    // 부드럽게 멈추기
    private IEnumerator SmoothStop()
    {
        float timer = 0f;

        while (timer < _stopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / _stopDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            float currentSpeed = Mathf.Lerp(_spinSpeed, 0f, easedT);

            foreach (var symbolObj in _symbolPool)
            {
                symbolObj.transform.position += Vector3.down * currentSpeed * Time.deltaTime;
            }

            if (_symbolPool[_symbolPool.Count - 1].transform.localPosition.y < -_symbolHeight * 2.5f)
            {
                RecycleBottomSymbol();
            }

            yield return null;
        }

        SnapToGrid();
        yield return new WaitForSeconds(0.15f);
        UpdateFinalResult();
    }
    // 그리드에 딱 맞게 정렬
    private void SnapToGrid()
    {
        for (int i = 0; i < _symbolPool.Count; i++)
        {
            Vector3 currenPosition = _symbolPool[i].transform.localPosition;
            float targetY = _symbolHeight * (2 - i);
            _symbolPool[i].transform.DOLocalMove(new Vector3(0, targetY, 0), 0.1f).SetEase(Ease.Linear);
        }
    }
    // 시작 시 랜덤하게 배치
    private void SetRandomSymbols()
    {
        for (int i = 0; i < _symbolPool.Count; i++)
        {
            SymbolIdentifier identifier = _symbolPool[i].GetComponent<SymbolIdentifier>();
            if (identifier != null)
            {
                identifier.SetRandomSymbol(_reelStrip, _allSymbols);
            }
        }
    }
    // 
    private void UpdateFinalResult()
    {
        for (int i = 0; i < 3; i++)
        {
            int poolIndex = i + 1;
            SymbolIdentifier identifier = _symbolPool[poolIndex].GetComponent<SymbolIdentifier>();
            if (identifier == null) return;
            _finalResult[i] = identifier.SymbolID;

        }
    }
}
