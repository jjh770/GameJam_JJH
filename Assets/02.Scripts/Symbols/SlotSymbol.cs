using UnityEngine;

[System.Serializable]
public class SlotSymbol
{
    public int SymbolID; // 0~7 (8가지 심볼)
    public Sprite Sprite;
    public string SymbolName;

    public SlotSymbol(int id, Sprite spr, string name)
    {
        SymbolID = id;
        Sprite = spr;
        SymbolName = name;
    }
}
