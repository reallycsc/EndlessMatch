using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerControl : Grid
{
    private GameObject _target;
    private BezierLine _attackLine;
    private PlayerPropertie _propertie;
    private int _attackRange;

    // Use this for initialization
    new void Awake()
    {
        base.Awake();
        Type = GridType.Player;
        Color = GridColor.Player;
    }
    void Start()
    {
        name = Type.ToString();
        _attackLine = transform.GetComponentInChildren<BezierLine>();
        _propertie = gameObject.GetComponent<PlayerPropertie>();
        _attackRange = _propertie.GetAttackRange();
    }

    // Update is called once per frame
    void Update ()
    {
        // Get target in attack range
        if (_target == null)
        {
            _target = GridManager.Instance.GetNearestTargetInRange((int)MapPosition.x, (int)MapPosition.y, _attackRange, GridType.Enemy);
            if (_target != null)
            {
                _attackLine.ShowLine();
                _attackLine.EndObject = _target;
                _attackLine.DrawLine();
            }
            else
            {
                _attackLine.HideLine();
            }
        }
        else // Hide attack line when target out of attack range
        {
            Vector2 sub = _target.GetComponent<Grid>().MapPosition - MapPosition;
            float distance = Mathf.Max(Mathf.Abs(sub.x), Mathf.Abs(sub.y));
            if (distance > _attackRange)
            {
                _target = null;
                _attackLine.HideLine();
            }
        }
    }

    public void BeAttacked(GameObject enemy, Vector3 attackDirection)
    {
        _propertie.BeAttacked(enemy);
        BeAttackMove(transform.position + attackDirection * 0.2f, 0.1f);
    }

    public GameObject GetTarget()
    {
        return _target;
    }
}
