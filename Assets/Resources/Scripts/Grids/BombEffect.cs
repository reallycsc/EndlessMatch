using UnityEngine;
using System.Collections;

public class BombEffect : MonoBehaviour
{
    public float Duration = 0.3f;
    public float ScaleStart = 0.2f;
    public float ScaleEnd = 7.0f;

    private float _timeAdded;
    private float _scaleStep;
    // Use this for initialization
    void Awake()
    {
        //GameManager.Instance.AnimationCount++;
    }
    void Start () {
	    transform.localScale = new Vector3(ScaleStart, ScaleStart, 1);
        _scaleStep = (ScaleEnd - ScaleStart)/Duration;
    }
	
	// Update is called once per frame
	void Update () {
	    if (_timeAdded >= Duration)
	    {
            Destroy(gameObject);
            //GameManager.Instance.AnimationCount--;
        }

	    float scale = _scaleStep * _timeAdded + ScaleStart;
        transform.localScale = new Vector3(scale, scale, 1);

        _timeAdded += Time.deltaTime;
	}
}
