using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SlotMachineManager : MonoBehaviour
{
    [Header("Reels")]
    [SerializeField] private Reel[] reels; // 5개의 릴

    [Header("Symbols")]
    [SerializeField] private List<SlotSymbol> allSymbols = new List<SlotSymbol>();

    [Header("Reel Strips")]
    [SerializeField] private ReelStrip[] reelStrips; // 5개 (릴마다 다른 확률 가능)

    [Header("UI")]
    [SerializeField] private Button spinButton;

    [Header("Spin Settings")]
    [SerializeField] private float reelStopDelay = 0.15f; // 릴 간 정지 간격

    private bool isSpinning = false;

    void Start()
    {
        InitializeSlotMachine();
        spinButton.onClick.AddListener(OnSpinButtonClick);
    }

    private void InitializeSlotMachine()
    {
        // 각 릴 초기화
        for (int i = 0; i < reels.Length; i++)
        {
            ReelStrip strip = (i < reelStrips.Length) ? reelStrips[i] : reelStrips[0];
            reels[i].Initialize(strip, allSymbols);
        }
    }

    public void OnSpinButtonClick()
    {
        if (!isSpinning)
        {
            StartCoroutine(SpinSequence());
        }
    }

    private IEnumerator SpinSequence()
    {
        isSpinning = true;
        spinButton.interactable = false;

        // 모든 릴 동시 회전 시작
        foreach (var reel in reels)
        {
            reel.StartSpin();
        }

        // 잠시 대기 후 순차 정지
        yield return new WaitForSeconds(1.0f);

        // 왼쪽부터 차례로 정지
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].StopSpin();
            yield return new WaitForSeconds(reelStopDelay);
        }

        // 모든 릴이 완전히 멈출 때까지 대기
        yield return new WaitUntil(() => AllReelsStopped());

        // 결과 처리
        ProcessResults();

        isSpinning = false;
        spinButton.interactable = true;
    }

    private bool AllReelsStopped()
    {
        foreach (var reel in reels)
        {
            if (reel.IsSpinning)
                return false;
        }
        return true;
    }

    private void ProcessResults()
    {
        // 3x5 결과 매트릭스 출력 (디버그)
        for (int row = 0; row < 3; row++)
        {
            string rowResult = $"Row {row}: ";
            for (int col = 0; col < 5; col++)
            {
                int symbolID = reels[col].FinalResult[row];
                rowResult += $"[{symbolID}] ";
            }
            Debug.Log(rowResult);
        }
    }

    // 디버그: 특정 결과 강제 설정
    public void SetDebugResult(int[][] debugResult)
    {
        for (int i = 0; i < reels.Length && i < debugResult.Length; i++)
        {
            reels[i].StopSpin(debugResult[i]);
        }
    }
}
