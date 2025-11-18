using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JackPotProgressUI : MonoBehaviour
{
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private int _jackpotTarget = 3000;

    void Start()
    {
        BetManager.Instance.OnBetAccumulated += UpdateProgress;
        _progressSlider.maxValue = _jackpotTarget;
        UpdateProgress(0, _jackpotTarget);
    }

    private void UpdateProgress(int current, int target)
    {
        _progressSlider.value = current;
        _progressText.text = $"{current} / {target}";

        // 거의 도달했을 때 색상 변경 등 연출
        
        if (current >= target)
        {
            _progressSlider.fillRect.GetComponent<Image>().color = Color.green;
        }
        else if (current >= target * 0.8f)
        {
            _progressSlider.fillRect.GetComponent<Image>().color = Color.yellow;
        }
    }
}
