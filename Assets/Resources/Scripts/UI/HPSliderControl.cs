using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HPSliderControl : MonoBehaviour
{
    private Slider _slider;
    private bool _isNeedMove;
    private float _newValue;
    private float _oldValue;
    private float _moveDuration = 0.3f;
    private float _addTime;
    // Use this for initialization
    void Awake()
    {
        _slider = GetComponent<Slider>();
    }
	
	// Update is called once per frame
	void Update () {
	    if (_isNeedMove)
	    {
	        _addTime += Time.deltaTime;
	        if (_addTime >= _moveDuration)
	        {
	            _isNeedMove = false;
	            _addTime = 0;

	        }
	        else
	        {
                _slider.value = _oldValue - (_oldValue - _newValue) * _addTime / _moveDuration;
            }
	    }
	}

    public void InitHpSlider(float hp, float maxhp)
    {
        _slider.value = hp / maxhp;
    }

    public void UpdateHpSlider(float hp, float maxhp)
    {
        _oldValue = _slider.value;
        _newValue = hp / maxhp;
        _isNeedMove = true;
    }
}
