using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlotChecker : MonoBehaviour
{
    private int[,] _resultGrid = new int[3, 5];

    private List<(int[] row, int[] col, float multiplier, string patternName)> _paylines = new List<(int[] row, int[] col, float multiplier, string patternName)>
    {
        // 가로 (3개)
        (new int[] {0,0,0}, new int[] {0,1,2}, 1.0f, "가로"),
        (new int[] {0,0,0}, new int[] {1,2,3}, 1.0f, "가로"),
        (new int[] {0,0,0}, new int[] {2,3,4}, 1.0f, "가로"),
        (new int[] {1,1,1}, new int[] {0,1,2}, 1.0f, "가로"),
        (new int[] {1,1,1}, new int[] {1,2,3}, 1.0f, "가로"),
        (new int[] {1,1,1}, new int[] {2,3,4}, 1.0f, "가로"),
        (new int[] {2,2,2}, new int[] {0,1,2}, 1.0f, "가로"),
        (new int[] {2,2,2}, new int[] {1,2,3}, 1.0f, "가로"),
        (new int[] {2,2,2}, new int[] {2,3,4}, 1.0f, "가로"),
        
        // 세로 (3개)
        (new int[] {0,1,2}, new int[] {0,0,0}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {1,1,1}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {2,2,2}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {3,3,3}, 1.0f, "세로"),
        (new int[] {0,1,2}, new int[] {4,4,4}, 1.0f, "세로"),
        
        // 대각 (3개)
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
        
        // 재그
        (new int[] {0,0,1,1,2}, new int[] {0,4,1,3,2}, 4.0f, "재그"),
        
        // 지상
        (new int[] {0,1,1,2,2,2,2,2}, new int[] {2,1,3,0,1,2,3,4}, 7.0f, "지상"),
        
        // 천상
        (new int[] {0,0,0,0,0,1,1,2}, new int[] {0,1,2,3,4,1,3,2}, 7.0f, "천상"),
        
        // 눈
        (new int[] {0,0,0,1,1,1,1,2,2,2}, new int[] {1,2,3,0,1,3,4,1,2,3}, 8.0f, "눈"),
        
        // 잭팟
        (new int[] {0,1,2,0,1,2,0,1,2,0,1,2,0,1,2}, new int[] {0,0,0,1,1,1,2,2,2,3,3,3,4,4,4}, 10.0f, "잭팟"),
    };

    public List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)> CheckResults(int[,] resultGrid)
    {
        _resultGrid = resultGrid;
        var allMatches = new List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)>();

        // 모든 패턴 매칭 검사
        foreach (var payline in _paylines)
        {
            if (IsPatternMatched(payline.row, payline.col, out var matchedPositions, out int symbolID))
            {
                allMatches.Add((payline.patternName, payline.multiplier, symbolID, matchedPositions));
            }
        }

        // 포함 관계 기반 필터링
        var filteredResults = FilterByInclusionRule(allMatches);

        return filteredResults;
    }

    // 포함 관계 기반 필터링 (작은 패턴이 큰 패턴에 완전히 포함되면 제거)
    private List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)> FilterByInclusionRule(
        List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)> matches)
    {
        var result = new List<(string patternName, float multiplier, int symbolID, List<(int row, int col)> matchedPositions)>();

        // 심볼별로 그룹화
        var groupedBySymbol = matches.GroupBy(m => m.symbolID);

        foreach (var group in groupedBySymbol)
        {
            var symbolMatches = group.ToList();

            // 배율 높은 순으로 정렬
            symbolMatches = symbolMatches.OrderByDescending(m => m.multiplier).ToList();

            for (int i = 0; i < symbolMatches.Count; i++)
            {
                bool isIncluded = false;
                var currentPositions = new HashSet<(int row, int col)>(symbolMatches[i].matchedPositions);

                // 자신보다 배율이 높은 패턴들과 비교
                for (int j = 0; j < i; j++)
                {
                    var higherPositions = new HashSet<(int row, int col)>(symbolMatches[j].matchedPositions);

                    // 현재 패턴이 더 큰 패턴에 완전히 포함되는지 확인
                    if (currentPositions.IsSubsetOf(higherPositions))
                    {
                        isIncluded = true;
                        break;
                    }
                }

                // 다른 패턴에 포함되지 않으면 결과에 추가
                if (!isIncluded)
                {
                    result.Add(symbolMatches[i]);
                }
            }
        }

        return result;
    }

    private bool IsPatternMatched(int[] rows, int[] cols, out List<(int row, int col)> matchedPositions, out int symbolID)
    {
        matchedPositions = new List<(int row, int col)>();
        symbolID = -1;

        int symbol = _resultGrid[rows[0], cols[0]];
        symbolID = symbol;
        matchedPositions.Add((rows[0], cols[0]));

        for (int i = 1; i < rows.Length; i++)
        {
            if (_resultGrid[rows[i], cols[i]] != symbol)
            {
                matchedPositions.Clear();
                symbolID = -1;
                return false;
            }
            matchedPositions.Add((rows[i], cols[i]));
        }
        return true;
    }
}
