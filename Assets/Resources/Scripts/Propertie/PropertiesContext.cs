using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public enum PropertiesEnum
{
    Level,
    Exp,
    DeadExp,
    Gold,
    MaxHP,
    HP,
    Damage,
    Earth,
    Water,
    Fire,
    Wind,
    RestoreHP,
    AttackRange,
    AttackSpeed,
}

public class PropertiesContext
{
    public PropertiesEnum Type;
    public float Value;
    public PropertiesContext(PropertiesEnum type, float value)
    {
        Type = type;
        Value = value;
    }
}

public class PlayerPropertiesContext : PropertiesContext
{
    public Text Text;
    public PlayerPropertiesContext(PropertiesEnum type, float value, string textPath) : base(type, value)
    {
        if (textPath != null)
        {
            Text = GameObject.Find(textPath).GetComponent<Text>();
            UpdateText();
        }
    }

    public void UpdateText()
    {
        if (Text == null)
            return;

        if (Type == PropertiesEnum.HP)
        {
            int index = Text.text.IndexOf("/", StringComparison.Ordinal);
            Text.text = Type.ToString() + ": " + Value + Text.text.Substring(index);
        }
        else if (Type == PropertiesEnum.MaxHP)
        {
            int index = Text.text.IndexOf("/", StringComparison.Ordinal);
            Text.text = Text.text.Substring(0, index) + "/" + Value;
        }
        else
            Text.text = Type.ToString() + ": " + Value;
    }
}