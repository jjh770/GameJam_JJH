using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ReelStrip", menuName = "Slot/Reel Strip")]
public class ReelStrip : ScriptableObject
{
    [System.Serializable]
    public class SymbolWeight
    {
        public int symbolID;
        public int weight; // 가중치 (높을수록 자주 출현)
    }

    public List<SymbolWeight> symbolWeights = new List<SymbolWeight>();
    private List<int> generatedStrip = new List<int>();

    // 릴 스트립 생성
    public void GenerateStrip()
    {
        generatedStrip.Clear();

        foreach (var sw in symbolWeights)
        {
            for (int i = 0; i < sw.weight; i++)
            {
                generatedStrip.Add(sw.symbolID);
            }
        }

        // 섞기 (패턴 방지)
        Shuffle(generatedStrip);
    }

    // 랜덤 심볼 가져오기
    public int GetRandomSymbol()
    {
        if (generatedStrip.Count == 0)
            GenerateStrip();

        return generatedStrip[Random.Range(0, generatedStrip.Count)];
    }

    // Fisher-Yates 셔플
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
