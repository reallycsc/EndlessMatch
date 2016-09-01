using UnityEngine;
using System.Collections;
using System.Globalization;
using UnityEngine.UI;

public class FlowTextControl : MonoBehaviour
{
    private Text _text;
    private Animator _animator;
    private Outline _outline;
	// Use this for initialization
	void Awake ()
	{
	    _text = GetComponent<Text>();
	    _animator = GetComponent<Animator>();
	    _outline = GetComponent<Outline>();
	}
	
	// Update is called once per frame
	void Update () {
	}

    public void FlowDamageText(float damage)
    {
        _text.text = "-" + damage.ToString(CultureInfo.CurrentCulture);
        _animator.SetTrigger("DamageTextFlow");
        //GameManager.Instance.AnimationCount++;
    }

    public void FlowAddHPText(float addhp)
    {
        _text.text = "+" + addhp.ToString(CultureInfo.CurrentCulture);
        _text.color = Color.green;
        _animator.SetTrigger("DamageTextFlow");
        //GameManager.Instance.AnimationCount++;
    }

    public void FlowTempNumber(float number, Color color)
    {
        _text.text = "+" + number.ToString(CultureInfo.CurrentCulture);
        _text.color = color;
        _outline.effectColor = Color.white;
        _animator.SetTrigger("TempNumberTextFlow");
        //GameManager.Instance.AnimationCount++;
    }

    public void OnFlowDone()
    {
        Destroy(gameObject);
        //GameManager.Instance.AnimationCount--;
    }
}
