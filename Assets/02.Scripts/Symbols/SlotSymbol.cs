using UnityEngine;

[System.Serializable]
public class SlotSymbol
{
    public int symbolID; // 0~7 (8가지 심볼)
    public Sprite sprite;
    public string symbolName;

    public SlotSymbol(int id, Sprite spr, string name)
    {
        symbolID = id;
        sprite = spr;
        symbolName = name;
    }
}
