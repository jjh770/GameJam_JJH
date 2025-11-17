using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class SymbolIdentifier : MonoBehaviour
{
    public int SymbolID { get; private set; }
    private SpriteRenderer _spriteRenderer;
    private Color _HighlightColor = new Color(1, 1, 1, 0.5f);
    private Color _originalColor = new Color(1, 1, 1, 1);
    private ParticleManager _particleManager;
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        _particleManager = ParticleManager.Instance;
        
    }

    public Sequence BlinkHighlight(int blinkCount = 3, float blinkDuration = 0.2f)
    {
        if (_spriteRenderer == null) return null;

        Sequence blinkSequence = DOTween.Sequence();

        for (int i = 0; i < blinkCount; i++)
        {
            blinkSequence.Append(_spriteRenderer.DOColor(_HighlightColor, blinkDuration * 0.5f));
            blinkSequence.Join(transform.DOPunchScale(Vector3.one * 0.1f, blinkDuration, 2, 0.5f));
            blinkSequence.JoinCallback(() =>
            {
                _particleManager.PlayParticle("Star_Blue", transform.position);
            });
            blinkSequence.Append(_spriteRenderer.DOColor(_originalColor, blinkDuration * 0.5f));
        }

        return blinkSequence;
    }

    public void SetSymbol(int symbolID, Sprite sprite)
    {
        SymbolID = symbolID;
        if (_spriteRenderer == null) return;
        _spriteRenderer.sprite = sprite;
    }

    public void SetSymbolFromData(SlotSymbol symbolData)
    {
        if (symbolData == null) return;
        SymbolID = symbolData.SymbolID;

        if (_spriteRenderer == null) return;
        _spriteRenderer.sprite = symbolData.Sprite;
    }

    // 랜덤 심볼 설정
    public void SetRandomSymbol(ReelStrip reelStrip, List<SlotSymbol> allSymbols)
    {
        if (reelStrip == null || allSymbols == null) return;

        int randomSymbolID = reelStrip.GetRandomSymbol();
        SetSymbolByID(randomSymbolID, allSymbols);
    }

    // ID로 심볼 설정
    public void SetSymbolByID(int symbolID, List<SlotSymbol> allSymbols)
    {
        SlotSymbol symbolData = allSymbols.Find(s => s.SymbolID == symbolID);
        if (symbolData != null)
        {
            SetSymbolFromData(symbolData);
        }
    }
}
