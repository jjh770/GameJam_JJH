using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    void Start()
    {
        // MoneyManager 이벤트 구독 (등록)
        MoneyManager.Instance.OnMoneyChanged += UpdateMoneyUI;
        // 초기값 반영
        UpdateMoneyUI(MoneyManager.Instance.CurrentMoney);
    }

    void OnDestroy()
    {
        // 해제 (메모리 누수 방지)
        MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyUI;
    }

    private void UpdateMoneyUI(int value)
    {
        moneyText.text = $"Money : {value}";
    }
}
