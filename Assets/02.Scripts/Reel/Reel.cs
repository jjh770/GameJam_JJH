using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Reel : MonoBehaviour
{
    [Header("Reel Components")]
    [SerializeField] private SpriteRenderer[] symbolSlots; // Image 대신
    [SerializeField] private Transform reelContainer; // RectTransform 대신

    [Header("Reel Settings")]
    [SerializeField] private float spinSpeed = 1000f; // 회전 속도
    [SerializeField] private float minSpinDuration = 1.5f; // 최소 회전 시간
    [SerializeField] private float stopDuration = 0.5f; // 정지까지 걸리는 시간
    [SerializeField] private float symbolHeight = 200f; // 각 심볼의 높이

    private ReelStrip reelStrip;
    private List<SlotSymbol> allSymbols;
    private bool isSpinning = false;
    private bool shouldStop = false;
    private int[] finalResult = new int[3]; // 최종 결과 (위, 중앙, 아래)

    // 심볼 풀 (재사용)
    private List<Image> symbolPool = new List<Image>();
    private Queue<int> upcomingSymbols = new Queue<int>();

    public bool IsSpinning => isSpinning;
    public int[] FinalResult => finalResult;

    // 초기화
    public void Initialize(ReelStrip strip, List<SlotSymbol> symbols)
    {
        reelStrip = strip;
        allSymbols = symbols;
        reelStrip.GenerateStrip();

        // 초기 심볼 설정
        SetRandomSymbols();
    }

    // 스핀 시작
    public void StartSpin()
    {
        if (!isSpinning)
        {
            StartCoroutine(SpinRoutine());
        }
    }

    // 스핀 정지 신호
    public void StopSpin(int[] targetSymbols = null)
    {
        shouldStop = true;

        // 목표 심볼이 있으면 설정 (디버그용)
        if (targetSymbols != null && targetSymbols.Length == 3)
        {
            finalResult = targetSymbols;
        }
        else
        {
            // 랜덤 결과 생성
            for (int i = 0; i < 3; i++)
            {
                finalResult[i] = reelStrip.GetRandomSymbol();
            }
        }
    }

    // 스핀 루틴
    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        shouldStop = false;
        float elapsedTime = 0f;
        float currentSpeed = spinSpeed;

        // 빠르게 회전
        while (!shouldStop || elapsedTime < minSpinDuration)
        {
            // 심볼을 아래로 이동 (무한 스크롤)
            reelContainer.position += Vector3.down * spinSpeed * Time.deltaTime;

            // 심볼이 일정 거리 이동하면 재배치
            if (reelContainer.position.y <= -symbolHeight)
            {
                reelContainer.position += new Vector3(0, symbolHeight);
                ShiftSymbolsDown();
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 부드럽게 정지
        yield return StartCoroutine(SmoothStop());

        isSpinning = false;
    }

    // 심볼 순환 (무한 스크롤)
    private void ShiftSymbolsDown()
    {
        // 맨 위 심볼을 새로운 랜덤 심볼로 교체
        int newSymbolID = reelStrip.GetRandomSymbol();
        SlotSymbol newSymbol = allSymbols.Find(s => s.symbolID == newSymbolID);

        if (newSymbol != null)
        {
            symbolSlots[0].sprite = newSymbol.sprite;
        }

        // 배열 회전 (아래로)
        SpriteRenderer temp = symbolSlots[0];
        for (int i = 0; i < symbolSlots.Length - 1; i++)
        {
            symbolSlots[i] = symbolSlots[i + 1];
        }
        symbolSlots[symbolSlots.Length - 1] = temp;
    }

    // 부드러운 정지 (EaseOut)
    private IEnumerator SmoothStop()
    {
        // 최종 결과를 심볼 슬롯에 설정
        for (int i = 0; i < 3; i++)
        {
            SlotSymbol symbol = allSymbols.Find(s => s.symbolID == finalResult[i]);
            if (symbol != null)
            {
                symbolSlots[i].sprite = symbol.sprite;
            }
        }

        float timer = 0f;
        Vector2 startPos = reelContainer.position;
        Vector2 targetPos = Vector2.zero;

        while (timer < stopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / stopDuration;

            // EaseOutCubic 커브
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            reelContainer.position = Vector2.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        reelContainer.position = targetPos;
    }

    // 랜덤 심볼 초기 설정
    private void SetRandomSymbols()
    {
        for (int i = 0; i < symbolSlots.Length; i++)
        {
            int randomSymbolID = reelStrip.GetRandomSymbol();
            SlotSymbol symbol = allSymbols.Find(s => s.symbolID == randomSymbolID);

            if (symbol != null)
            {
                symbolSlots[i].sprite = symbol.sprite;
            }
        }
    }
}
