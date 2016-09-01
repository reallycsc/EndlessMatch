using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class EnemyPropertie : MonoBehaviour {
    Dictionary<PropertiesEnum, PropertiesContext> _attributeDictionary;
    private Transform _canvasTransform;
    private HPSliderControl _hpSlider;
    private EnemyControl _control;
    private float _tempDamage;
    private float _tempDamageOld;

    // Use this for initialization
    void Awake () {
        _attributeDictionary = new Dictionary<PropertiesEnum, PropertiesContext>();
        _attributeDictionary.Add(PropertiesEnum.Level, new PropertiesContext(PropertiesEnum.Level, 1));
        _attributeDictionary.Add(PropertiesEnum.DeadExp, new PropertiesContext(PropertiesEnum.DeadExp, 0));
        _attributeDictionary.Add(PropertiesEnum.MaxHP, new PropertiesContext(PropertiesEnum.MaxHP, 100));
        _attributeDictionary.Add(PropertiesEnum.HP, new PropertiesContext(PropertiesEnum.HP, 100));
        _attributeDictionary.Add(PropertiesEnum.Damage, new PropertiesContext(PropertiesEnum.Damage, 3));
        _attributeDictionary.Add(PropertiesEnum.Earth, new PropertiesContext(PropertiesEnum.Earth, 0));
        _attributeDictionary.Add(PropertiesEnum.Water, new PropertiesContext(PropertiesEnum.Water, 0));
        _attributeDictionary.Add(PropertiesEnum.Fire, new PropertiesContext(PropertiesEnum.Fire, 0));
        _attributeDictionary.Add(PropertiesEnum.Wind, new PropertiesContext(PropertiesEnum.Wind, 0));
        _attributeDictionary.Add(PropertiesEnum.AttackRange, new PropertiesContext(PropertiesEnum.AttackRange, 1));
        _attributeDictionary.Add(PropertiesEnum.AttackSpeed, new PropertiesContext(PropertiesEnum.AttackSpeed, 1));
    }
    void Start()
    {
        _control = GetComponent<EnemyControl>();
        _canvasTransform = transform.FindChild("Canvas").transform;
        _hpSlider = transform.FindChild("EnemySprite/Canvas/HPSlider").GetComponent<HPSliderControl>();
        _hpSlider.InitHpSlider(_attributeDictionary[PropertiesEnum.HP].Value, _attributeDictionary[PropertiesEnum.MaxHP].Value);
        // Timer
        InvokeRepeating("DamageUpdate", 0, 0.05f);
    }

    // Battle Functions
    void DamageUpdate()
    {
        if (_tempDamage > 0)
        {
            if (_tempDamageOld == _tempDamage)
            {
                _control.BeAttacked();
                BeAttackedAndFlowText(_tempDamage);
                _tempDamageOld = _tempDamage = 0;
            }
            else
                _tempDamageOld = _tempDamage;
        }
    }

    public void BeAttackedAndFlowText(float damage)
    {
        int damageReal = (int)PropertieManager.AttackDamageCalc(damage);
        _attributeDictionary[PropertiesEnum.HP].Value -= damageReal;
        if (_attributeDictionary[PropertiesEnum.HP].Value <= 0)
            _attributeDictionary[PropertiesEnum.HP].Value = 0;
        _hpSlider.UpdateHpSlider(_attributeDictionary[PropertiesEnum.HP].Value, _attributeDictionary[PropertiesEnum.MaxHP].Value);
        Text flowText = Instantiate(Resources.Load("Prefabs/UI/FlowText", typeof(Text)) as Text);
        flowText.transform.SetParent(_canvasTransform);
        flowText.GetComponent<FlowTextControl>().FlowDamageText(damageReal);
    }

    public void CheckHP()
    {
        if (_attributeDictionary[PropertiesEnum.HP].Value <= 0)
        {
            // Die
            _control.Die();
        }
    }

    public void AddTempNumber(float value)
    {
        _tempDamage += value;
    }

    // Getter functions
    public int GetLevel()
    {
        return (int)_attributeDictionary[PropertiesEnum.Level].Value;
    }
    public float GetDamage()
    {
        return (int)_attributeDictionary[PropertiesEnum.Damage].Value;
    }
    public int GetAttackRange()
    {
        return (int)_attributeDictionary[PropertiesEnum.AttackRange].Value;
    }
}
