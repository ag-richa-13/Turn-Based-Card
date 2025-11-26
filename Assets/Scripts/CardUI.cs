using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI powerText;
    public TextMeshProUGUI abilityText;
    public Button button;

    private bool selected = false;
    private System.Action onClickCallback;

    public void Initialize(CardData data, System.Action onClick)
    {
        if (data == null)
        {
            Debug.LogWarning("CardUI.Initialize called with null data");
            return;
        }

        nameText.text = data.name;
        costText.text = $"Cost: {data.cost}";
        powerText.text = $"Power: {data.power}";
        abilityText.text = $"{data.ability.type} ({data.ability.value})";

        onClickCallback = onClick;

        button.onClick.RemoveAllListeners();
        if (onClickCallback != null)
            button.onClick.AddListener(() => onClickCallback.Invoke());
    }

    public void ToggleSelected()
    {
        selected = !selected;
        // No selectionHighlight here anymore (handled by Unity Editor)
    }
}
