using System.Collections.Generic;
using UnityEngine;

public class SlotResultChecker : MonoBehaviour
{
    // 3x5 결과 배열 (0~7: 심볼 ID)
    private int[,] _resultGrid = new int[3, 5];

    // 페이라인 정의 (row, col, multiplier, patternName)
    private List<(int[] row, int[] col, float multiplier, string patternName)> _paylines = new List<(int[] row, int[] col, float multiplier, string patternName)>
    {
        // 가로 (3개 이상)
        (new int[] {0,0,0}, new int[] {0,1,2}, 1.0f, "가로"),
        (new int[] {0,0,0}, new int[] {1,2,3}, 1.0f, "가로"),
        (new int[] {0,0,0}, new int[] {2,3,4}, 1.0f, "가로"),
        (new int[] {1,1,1}, new int[] {0,1,2}, 1.0f, "가로"),
        (new int[] {1,1,1}, new int[] {1,2,3}, 1.0f, "가로"),
        (new int[] {1,1,1}, new int[] {2,3,4}, 1.0f, "가로"),
        (new int[] {2,2,2}, new int[] {0,1,2}, 1.0f, "가로"),
        (new int[] {2,2,2}, new int[] {1,2,3}, 1.0f, "가로"),
        (new int[] {2,2,2}, new int[] {2,3,4}, 1.0f, "가로"),
        // 세로 (3개 이상)
        (new int[] {0,1,2}, new int[] {0,0,0}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {1,1,1}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {2,2,2}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {3,3,3}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {4,4,4}, 1.0f, "세로"),
        // 대각 (3개 이상)
        (new int[] {0,1,2}, new int[] {0,1,2}, 1.0f, "대각"),
        (new int[] {0,1,2}, new int[] {2,1,0}, 1.0f, "대각"),
        (new int[] {0,1,2}, new int[] {1,2,3}, 1.0f, "대각"),
        (new int[] {0,1,2}, new int[] {3,2,1}, 1.0f, "대각"),
        (new int[] {0,1,2}, new int[] {2,3,4}, 1.0f, "대각"),
        (new int[] {0,1,2}, new int[] {4,3,2}, 1.0f, "대각"),
        // L (가로 4개)
        (new int[] {0,0,0,0}, new int[] {0,1,2,3}, 2.0f, "L"),
        (new int[] {0,0,0,0}, new int[] {1,2,3,4}, 2.0f, "L"),
        (new int[] {1,1,1,1}, new int[] {0,1,2,3}, 2.0f, "L"),
        (new int[] {1,1,1,1}, new int[] {1,2,3,4}, 2.0f, "L"),
        (new int[] {2,2,2,2}, new int[] {0,1,2,3}, 2.0f, "L"),
        (new int[] {2,2,2,2}, new int[] {1,2,3,4}, 2.0f, "L"),
        // XL (가로 5개)
        (new int[] {0,0,0,0,0}, new int[] {0,1,2,3,4}, 3.0f, "XL"),
        (new int[] {1,1,1,1,1}, new int[] {0,1,2,3,4}, 3.0f, "XL"),
        (new int[] {2,2,2,2,2}, new int[] {0,1,2,3,4}, 3.0f, "XL"),        
        // 지그
        (new int[] {0,1,1,2,2}, new int[] {2,1,3,0,4}, 4.0f, "지그"),
        //(new int[] {0,1,1,2,2}, new int[] {2,3,1,0,4}, 4.0f, "지그"),
        //(new int[] {0,1,1,2,2}, new int[] {2,1,3,4,0}, 4.0f, "지그"),
        //(new int[] {0,1,1,2,2}, new int[] {2,3,1,0,4}, 4.0f, "지그"),
        // 재그
        (new int[] {0,0,1,1,2}, new int[] {0,4,1,3,2}, 4.0f, "재그"),
        //(new int[] {0,0,1,1,2}, new int[] {4,0,1,3,2}, 4.0f, "재그"),
        //(new int[] {0,0,1,1,2}, new int[] {0,4,3,1,2}, 4.0f, "재그"),
        //(new int[] {0,0,1,1,2}, new int[] {4,0,3,1,2}, 4.0f, "재그"),
        // 지상
        (new int[] {0,1,1,2,2,2,2,2}, new int[] {2,1,3,0,1,2,3,4}, 7.0f, "지상"),
        // 천상
        (new int[] {0,0,0,0,0,1,1,2}, new int[] {0,1,2,3,4,1,3,2}, 7.0f, "천상"),
        // 눈
        (new int[] {0,0,0,1,1,1,1,2,2,2}, new int[] {1,2,3,0,1,3,4,1,2,3}, 8.0f, "눈"),
        (new int[] {0,1,2,0,1,2,0,1,2,0,1,2,0,1,2}, new int[] {0,0,0,1,1,1,2,2,2,3,3,3,4,4,4}, 10.0f, "잭팟"),
    };

    public List<(string patternName, float multiplier, List<(int row, int col)> matchedPositions)> CheckResults(int[,] resultGrid)
    {
        _resultGrid = resultGrid;
        var results = new List<(string patternName, float multiplier, List<(int row, int col)>)>();

        foreach (var payline in _paylines)
        {
            if (IsPatternMatched(payline.row, payline.col, out var matchedPositions))
            {
                results.Add((payline.patternName, payline.multiplier, matchedPositions));
            }
        }

        return results;
    }

    private bool IsPatternMatched(int[] rows, int[] cols, out List<(int row, int col)> matchedPositions)
    {
        matchedPositions = new List<(int row, int col)>();

        int symbol = _resultGrid[rows[0], cols[0]];
        matchedPositions.Add((rows[0], cols[0]));

        for (int i = 1; i < rows.Length; i++)
        {
            if (_resultGrid[rows[i], cols[i]] != symbol)
            {
                matchedPositions.Clear();
                return false;
            }
            matchedPositions.Add((rows[i], cols[i]));
        }
        return true;
    }
}
