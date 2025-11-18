using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ReelStrip", menuName = "Slot/Reel Strip")]
public class ReelStrip : ScriptableObject
{
    [System.Serializable]
    public class SymbolWeight
    {
        public int SymbolID;
        public int Weight;
    }

    [Header("Original Data")]
    [SerializeField] private List<SymbolWeight> _symbolWeights = new List<SymbolWeight>();

    // 런타임 복사본 (수정 가능)
    [System.NonSerialized]
    private List<SymbolWeight> _runtimeWeights;

    [System.NonSerialized]
    private List<int> _generatedStrip = new List<int>();

    // 외부에서 접근할 수 있는 프로퍼티 (런타임 복사본 반환)
    public List<SymbolWeight> SymbolWeights
    {
        get
        {
            if (_runtimeWeights == null)
            {
                InitializeRuntimeCopy();
            }
            return _runtimeWeights;
        }
    }

    private void OnEnable()
    {
        InitializeRuntimeCopy();
    }

    // 런타임 복사본 생성 (깊은 복사)
    private void InitializeRuntimeCopy()
    {
        _runtimeWeights = new List<SymbolWeight>();

        foreach (var sw in _symbolWeights)
        {
            _runtimeWeights.Add(new SymbolWeight
            {
                SymbolID = sw.SymbolID,
                Weight = sw.Weight
            });
        }

        // 복사 후 스트립 재생성
        GenerateStrip();
    }

    // 스트립 생성
    public void GenerateStrip()
    {
        _generatedStrip.Clear();

        // 런타임 복사본 사용
        var weights = SymbolWeights; // 이렇게 하면 자동으로 복사본이 초기화됨

        foreach (var sw in weights)
        {
            for (int i = 0; i < sw.Weight; i++)
            {
                _generatedStrip.Add(sw.SymbolID);
            }
        }

        Shuffle(_generatedStrip);
    }

    // 랜덤 심볼 가져오기
    public int GetRandomSymbol()
    {
        if (_generatedStrip.Count == 0)
            GenerateStrip();

        return _generatedStrip[UnityEngine.Random.Range(0, _generatedStrip.Count)];
    }

    // 특정 심볼의 Weight 변경 (런타임 복사본만 수정)
    public void SetSymbolWeight(int symbolID, int newWeight)
    {
        var weights = SymbolWeights;

        foreach (var sw in weights)
        {
            if (sw.SymbolID == symbolID)
            {
                sw.Weight = newWeight;
                GenerateStrip(); // Weight 변경 후 스트립 재생성
                return;
            }
        }
        Debug.LogWarning($"심볼 {symbolID}를 찾을 수 없음");
    }

    // 원래 값으로 리셋
    public void ResetToOriginalWeights()
    {
        InitializeRuntimeCopy();
    }

    // 셔플
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // 현재 Weight 출력 (디버그용)
    public void PrintCurrentWeights()
    {
        var weights = SymbolWeights;

        Debug.Log($"=== {name} 현재 Weight ===");
        foreach (var sw in weights)
        {
            Debug.Log($"심볼 {sw.SymbolID}: Weight {sw.Weight}");
        }
    }
}
