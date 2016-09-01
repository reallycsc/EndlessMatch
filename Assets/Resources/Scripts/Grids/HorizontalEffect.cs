using UnityEngine;
using System.Collections;

public class HorizontalEffect : MonoBehaviour {
    public float MoveSpeed = 0.03f;
    private Transform _left;
    private Transform _right;
    private float _minX;
    private float _maxX;

    // Use this for initialization
    void Awake()
    {
        //GameManager.Instance.AnimationCount++;
    }
    void Start()
    {
        _left = transform.FindChild("left");
        _right = transform.FindChild("right");
        float sizeX = _left.GetComponent<SpriteRenderer>().bounds.size.x;
        _minX = GameManager.Instance.ViewportBottomLeft.x - sizeX;
        _maxX = GameManager.Instance.ViewportTopRight.x + sizeX;
    }

    // Update is called once per frame
    void Update()
    {
        if (_left.position.x <= _minX)
            _left.gameObject.SetActive(false);
        if (_right.position.x >= _maxX)
            _right.gameObject.SetActive(false);
        if (!_left.gameObject.activeSelf && !_right.gameObject.activeSelf)
        {
            Destroy(gameObject);
            //GameManager.Instance.AnimationCount--;
        }

        MoveSpeed += Time.deltaTime * 5;
        if (_left.gameObject.activeSelf)
            _left.Translate(MoveSpeed * Vector2.left, Space.World);
        if (_right.gameObject.activeSelf)
            _right.Translate(MoveSpeed * Vector2.right, Space.World);
    }
}
