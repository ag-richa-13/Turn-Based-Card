using System;

[Serializable]
public class CardAbility
{
    public string type;
    public int value;
}

[Serializable]
public class CardData
{
    public int id;
    public string name;
    public int cost;
    public int power;
    public CardAbility ability;
}
