using UnityEngine;

[System.Serializable]
public class SlotSymbol
{
    public int SymbolID; // 0~7 (8가지 심볼)
    public Sprite Sprite;
    public string SymbolName;

    [Tooltip("심볼의 가치 배율 (1.0 = 기본, 2.0 = 2배)")]
    public float SymbolMultiplier = 1.0f; // 심볼 배율 추가
    public SlotSymbol(int id, Sprite spr, string name)
    {
        SymbolID = id;
        Sprite = spr;
        SymbolName = name;
    }
}
