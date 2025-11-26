using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance { get; private set; }
    public List<CardData> cards = new List<CardData>();

    private void Awake()
    {
        Debug.Log("CardDatabase Awake called");
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromResources();
    }

    void LoadFromResources()
    {
        Debug.Log("CardDatabase LoadFromResources called");
        TextAsset t = Resources.Load<TextAsset>("cards");
        if (t == null) { Debug.LogError("cards.json not found in Resources folder"); return; }
        cards = JsonUtility.FromJson<CardArrayWrapper>("{\"items\":" + t.text + "}").items;
        Debug.Log($"Loaded {cards.Count} cards.");
    }

    public CardData GetById(int id)
    {
        Debug.Log($"CardDatabase GetById called for id: {id}");
        return cards.Find(c => c.id == id);
    }

    [System.Serializable]
    private class CardArrayWrapper { public List<CardData> items; }
}
