using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerPropertie : MonoBehaviour {
    Dictionary<PropertiesEnum, PlayerPropertiesContext> _attributeDictionary;
    private Transform _canvasTransform;
    private HPSliderControl _hpSlider;
    private float _tempHP;
    private float _tempHPOld;

    // Use this for initialization
    void Awake()
    {
        _attributeDictionary = new Dictionary<PropertiesEnum, PlayerPropertiesContext>();
        _attributeDictionary.Add(PropertiesEnum.Level, new PlayerPropertiesContext(PropertiesEnum.Level, 1, null));
        _attributeDictionary.Add(PropertiesEnum.Exp, new PlayerPropertiesContext(PropertiesEnum.Exp, 0, "Canvas/Upper UI/ExpText"));
        _attributeDictionary.Add(PropertiesEnum.Gold, new PlayerPropertiesContext(PropertiesEnum.Gold, 0, "Canvas/Upper UI/GoldText"));
        _attributeDictionary.Add(PropertiesEnum.MaxHP, new PlayerPropertiesContext(PropertiesEnum.MaxHP, 10, "Canvas/Upper UI/HPText"));
        _attributeDictionary.Add(PropertiesEnum.HP, new PlayerPropertiesContext(PropertiesEnum.HP, 10, "Canvas/Upper UI/HPText"));
        _attributeDictionary.Add(PropertiesEnum.Earth, new PlayerPropertiesContext(PropertiesEnum.Earth, 1, "Canvas/Down UI/EarthText"));
        _attributeDictionary.Add(PropertiesEnum.Water, new PlayerPropertiesContext(PropertiesEnum.Water, 1, "Canvas/Down UI/WaterText"));
        _attributeDictionary.Add(PropertiesEnum.Fire, new PlayerPropertiesContext(PropertiesEnum.Fire, 1, "Canvas/Down UI/FireText"));
        _attributeDictionary.Add(PropertiesEnum.Wind, new PlayerPropertiesContext(PropertiesEnum.Wind, 1, "Canvas/Down UI/WindText"));
        _attributeDictionary.Add(PropertiesEnum.RestoreHP, new PlayerPropertiesContext(PropertiesEnum.RestoreHP, 1, null));
        _attributeDictionary.Add(PropertiesEnum.AttackRange, new PlayerPropertiesContext(PropertiesEnum.AttackRange, 10, null));
        _attributeDictionary.Add(PropertiesEnum.AttackSpeed, new PlayerPropertiesContext(PropertiesEnum.AttackSpeed, 1, null));
    }
    void Start()
    {
        _canvasTransform = transform.FindChild("Canvas").transform;
        _hpSlider = transform.FindChild("PlayerSprite/Canvas/HPSlider").GetComponent<HPSliderControl>();
        _hpSlider.InitHpSlider(_attributeDictionary[PropertiesEnum.HP].Value, _attributeDictionary[PropertiesEnum.MaxHP].Value);
        // Timer
        InvokeRepeating("HPUpdate", 0, 0.05f);
    }

    // Battle Functions
    void HPUpdate()
    {
        if (_tempHP > 0)
        {
            if (_tempHPOld == _tempHP)
            {
                AddHP(_tempHP);
                _tempHPOld = _tempHP = 0;
            }
            else
                _tempHPOld = _tempHP;
        }
    }

    void AddHP(float hp)
    {
        float maxHP = _attributeDictionary[PropertiesEnum.MaxHP].Value;
        float curHP = _attributeDictionary[PropertiesEnum.HP].Value;
        curHP = Mathf.Min(curHP + hp, maxHP);
        _attributeDictionary[PropertiesEnum.HP].Value = curHP;
        _hpSlider.UpdateHpSlider(curHP, maxHP);
        Text damageText = Instantiate(Resources.Load("Prefabs/UI/FlowText", typeof(Text)) as Text);
        damageText.transform.SetParent(_canvasTransform);
        damageText.GetComponent<FlowTextControl>().FlowAddHPText(hp);
    }

    public void BeAttacked(GameObject enemy)
    {
        int damage = (int)PropertieManager.AttackDamageCalc(enemy.GetComponent<EnemyPropertie>().GetDamage());
        _attributeDictionary[PropertiesEnum.HP].Value -= damage;
        if (_attributeDictionary[PropertiesEnum.HP].Value <= 0)
        {
            _attributeDictionary[PropertiesEnum.HP].Value = _attributeDictionary[PropertiesEnum.MaxHP].Value;
            // Die
        }
        _attributeDictionary[PropertiesEnum.HP].UpdateText();
        _hpSlider.UpdateHpSlider(_attributeDictionary[PropertiesEnum.HP].Value, _attributeDictionary[PropertiesEnum.MaxHP].Value);
        Text damageText = Instantiate(Resources.Load("Prefabs/UI/FlowText", typeof (Text)) as Text);
        damageText.transform.SetParent(_canvasTransform);
        damageText.GetComponent<FlowTextControl>().FlowDamageText(damage);
    }

    // PropertiesEnum
    public void AddPropertie(PropertiesEnum type)
    {
        _attributeDictionary[type].Value += 1;
        _attributeDictionary[type].UpdateText();
    }

    public void AddTempNumber(PropertiesEnum type)
    {
        switch (type)
        {
            case PropertiesEnum.RestoreHP:
                _tempHP += _attributeDictionary[PropertiesEnum.RestoreHP].Value;
                break;
            default:
                break;
        }
    }

    // Getter functions
    public float GetPropertie(PropertiesEnum type)
    {
        return _attributeDictionary[type].Value;
    }

    public int GetAttackRange()
    {
        return (int)_attributeDictionary[PropertiesEnum.AttackRange].Value;
    }
}
