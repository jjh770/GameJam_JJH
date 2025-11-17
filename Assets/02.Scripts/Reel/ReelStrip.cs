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

    public List<SymbolWeight> SymbolWeights = new List<SymbolWeight>();
    private List<int> _generatedStrip = new List<int>();

    public void GenerateStrip()
    {
        _generatedStrip.Clear();

        foreach (var sw in SymbolWeights)
        {
            for (int i = 0; i < sw.Weight; i++)
            {
                _generatedStrip.Add(sw.SymbolID);
            }
        }

        Shuffle(_generatedStrip);
    }

    public int GetRandomSymbol()
    {
        if (_generatedStrip.Count == 0)
            GenerateStrip();

        return _generatedStrip[UnityEngine.Random.Range(0, _generatedStrip.Count)];
    }

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
}
