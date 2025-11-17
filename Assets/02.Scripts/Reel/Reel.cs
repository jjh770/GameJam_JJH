using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Reel : MonoBehaviour
{
    [Header("Reel Components")]
    [SerializeField] private SpriteRenderer[] symbolSlots; // Image → SpriteRenderer
    [SerializeField] private Transform reelContainer; // RectTransform → Transform

    [Header("Reel Settings")]
    [SerializeField] private float spinSpeed = 10f; // 회전 속도 (units/sec)
    [SerializeField] private float minSpinDuration = 1.5f;
    [SerializeField] private float stopDuration = 0.5f;
    [SerializeField] private float symbolHeight = 2f; // 심볼 간격

    private ReelStrip reelStrip;
    private List<SlotSymbol> allSymbols;
    private bool isSpinning = false;
    private bool shouldStop = false;
    private int[] finalResult = new int[3];

    public bool IsSpinning => isSpinning;
    public int[] FinalResult => finalResult;

    public void Initialize(ReelStrip strip, List<SlotSymbol> symbols)
    {
        reelStrip = strip;
        allSymbols = symbols;
        reelStrip.GenerateStrip();
        SetRandomSymbols();
    }

    public void StartSpin()
    {
        if (!isSpinning)
            StartCoroutine(SpinRoutine());
    }

    public void StopSpin(int[] targetSymbols = null)
    {
        shouldStop = true;

        if (targetSymbols != null && targetSymbols.Length == 3)
            finalResult = targetSymbols;
        else
        {
            for (int i = 0; i < 3; i++)
                finalResult[i] = reelStrip.GetRandomSymbol();
        }
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        shouldStop = false;
        float elapsedTime = 0f;

        while (!shouldStop || elapsedTime < minSpinDuration)
        {
            // Transform.position 사용
            reelContainer.position += Vector3.down * spinSpeed * Time.deltaTime;

            if (reelContainer.localPosition.y <= -symbolHeight)
            {
                reelContainer.localPosition += new Vector3(0, symbolHeight, 0);
                ShiftSymbolsDown();
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(SmoothStop());
        isSpinning = false;
    }

    private void ShiftSymbolsDown()
    {
        int newSymbolID = reelStrip.GetRandomSymbol();
        SlotSymbol newSymbol = allSymbols.Find(s => s.symbolID == newSymbolID);

        if (newSymbol != null)
            symbolSlots[0].sprite = newSymbol.sprite;

        SpriteRenderer temp = symbolSlots[0];
        for (int i = 0; i < symbolSlots.Length - 1; i++)
            symbolSlots[i] = symbolSlots[i + 1];
        symbolSlots[symbolSlots.Length - 1] = temp;
    }

    private IEnumerator SmoothStop()
    {
        for (int i = 0; i < 3; i++)
        {
            SlotSymbol symbol = allSymbols.Find(s => s.symbolID == finalResult[i]);
            if (symbol != null)
                symbolSlots[i].sprite = symbol.sprite;
        }

        float timer = 0f;
        Vector3 startPos = reelContainer.localPosition;
        Vector3 targetPos = Vector3.zero;

        while (timer < stopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / stopDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            reelContainer.localPosition = Vector3.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        reelContainer.localPosition = targetPos;
    }

    private void SetRandomSymbols()
    {
        for (int i = 0; i < symbolSlots.Length; i++)
        {
            int randomSymbolID = reelStrip.GetRandomSymbol();
            SlotSymbol symbol = allSymbols.Find(s => s.symbolID == randomSymbolID);
            if (symbol != null)
                symbolSlots[i].sprite = symbol.sprite;
        }
    }
}
