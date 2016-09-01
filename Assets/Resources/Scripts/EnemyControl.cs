using UnityEngine;
using System.Collections;

public class EnemyControl : Grid
{
    [HideInInspector] public bool IsAttacked;
    [HideInInspector] public bool IsDead;

    private GameObject _target;
    private BezierLine _attackLine;
    private EnemyPropertie _propertie;
    private Animator _animator;
    private int _attackRange;

    // Use this for initialization
    new void Awake()
    {
        base.Awake();
        Type = GridType.Enemy;
        Color = GridColor.Enemy;
    }
    void Start()
    {
        name = Type.ToString();
        _attackLine = GetComponentInChildren<BezierLine>();
        _propertie = GetComponent<EnemyPropertie>();
        _animator = GetComponent<Animator>();
        _attackRange = _propertie.GetAttackRange();
    }

    // Update is called once per frame
    void Update () {
        // Get target in attack range
        if (_target == null)
        {
            _target = GridManager.Instance.GetNearestTargetInRange((int)MapPosition.x, (int)MapPosition.y, _attackRange, GridType.Player);
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

    // Battle Functions
    public bool IsNeedAttack()
    {
        return _target;
    }

    public void AttackTarget()
    {
        if (_target == null)
            return;
        if (IsDead)
            return;
        if (IsAttacked)
            return;

        Vector2 sub = _target.GetComponent<Grid>().MapPosition - MapPosition;
        float distance = Mathf.Max(Mathf.Abs(sub.x), Mathf.Abs(sub.y));
        if (distance > 1) // Fly attack when range > 1
        {
            GameObject tmpObj = Instantiate(Resources.Load("Prefabs/AttackFlyItem", typeof(GameObject)) as GameObject);
            tmpObj.transform.parent = transform;
            tmpObj.transform.position = transform.position;
            tmpObj.GetComponent<SpriteRenderer>().sprite = Resources.Load("Sprites/enemy_attack_fly_itemball3", typeof(Sprite)) as Sprite;
            tmpObj.GetComponent<AttackFlyItem>().Source = gameObject;
            tmpObj.GetComponent<AttackFlyItem>().Target = _target;

        }
        else // Just attack
        {
            Vector3 direction = (_target.transform.position - transform.position).normalized;
            BeAttackMove(transform.position + direction, 0.1f);
            _target.GetComponent<PlayerControl>().BeAttacked(gameObject, direction);
        }
        IsAttacked = true;
    }

    public void BeAttacked()
    {
        Vector3 direction = (transform.position - PlayerManager.Instance.Player.transform.position).normalized;
        BeAttackMove(transform.position + direction * 0.2f, 0.1f);
    }

    public void CheckHP()
    {
        _propertie.CheckHP();
    }

    public void Die()
    {
        _animator.SetTrigger("Die");
        IsDead = true;
        GameManager.Instance.AnimationCount++;
    }

    void OnEnemyRemoveDone()
    {
        Destroy(gameObject);
        GridManager.Instance.RemoveEnemyGrid((int)MapPosition.x, (int)MapPosition.y);
        GridManager.Instance.DropAllGridsDownAndCreateEnemy((int)MapPosition.x);
        GridManager.Instance.IsCheckNeeded = true;
        GameManager.Instance.AnimationCount--;
    }
}
