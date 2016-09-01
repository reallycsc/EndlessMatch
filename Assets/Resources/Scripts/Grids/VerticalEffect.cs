using UnityEngine;
using System.Collections;

public class VerticalEffect : MonoBehaviour {

    public float MoveSpeed = 0.03f;
    private Transform _up;
    private Transform _down;
    private float _minY;
    private float _maxY;

    // Use this for initialization
    void Awake()
    {
        //GameManager.Instance.AnimationCount++;
    }
    void Start ()
    {
        _up = transform.FindChild("up");
        _down = transform.FindChild("down");
        float sizeY = _up.GetComponent<SpriteRenderer>().bounds.size.y;
        _minY = GameManager.Instance.ViewportBottomLeft.y - sizeY;
        _maxY = GameManager.Instance.ViewportTopRight.y + sizeY;
    }
	
	// Update is called once per frame
	void Update () {
	    if (_up.position.y >= _maxY)
	        _up.gameObject.SetActive(false);
	    if (_down.position.y <= _minY)
	        _down.gameObject.SetActive(false);
	    if (!_up.gameObject.activeSelf && !_down.gameObject.activeSelf)
	    {
            Destroy(gameObject);
            //GameManager.Instance.AnimationCount--;
        }

	    MoveSpeed += Time.deltaTime * 5;
	    if (_up.gameObject.activeSelf)
	        _up.Translate(MoveSpeed*Vector2.up, Space.World);
        if (_down.gameObject.activeSelf)
            _down.Translate(MoveSpeed * Vector2.down, Space.World);
    }
}
