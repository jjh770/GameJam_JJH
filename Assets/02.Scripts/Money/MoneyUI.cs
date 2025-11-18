using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    void Start()
    {
        MoneyManager.Instance.OnMoneyChanged += UpdateMoneyUI;
        UpdateMoneyUI(MoneyManager.Instance.CurrentMoney);
    }

    void OnDestroy()
    {
        MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyUI;
    }

    private void UpdateMoneyUI(int value)
    {

        moneyText.text = $"Money : {value}";
    }
}
