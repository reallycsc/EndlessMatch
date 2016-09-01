using UnityEngine;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    #region Singleton
    private static EnemyManager _mInstance;
    public static EnemyManager Instance
    {
        get { return _mInstance; }
    }
    #endregion
    public GameObject EnemyPrefab;

    private EnemyControl[] _enemyCtrls;

    // Use this for initialization
    void Awake()
    {
        _mInstance = this;
    }
    void Start ()
    {
    }
	
	// Update is called once per frame
	void Update () {
        if (GameManager.Instance.GetGameStage() != GameManager.GameStage.EnemyStage)
            return;

        if (GameManager.Instance.AnimationCount > 0)
            return;

        // Update enemy list if there is null due to enemy's dead
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            if (!enemyCtrl)
                _enemyCtrls = GetComponentsInChildren<EnemyControl>();

        // Check all enemys' HP and remove dead
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            enemyCtrl.CheckHP();
        if (GameManager.Instance.AnimationCount > 0)
            return;

        // All enemys attack their target
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            enemyCtrl.AttackTarget();
        if (GameManager.Instance.AnimationCount > 0)
            return;

        // restore all enemy condition
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            enemyCtrl.IsAttacked = false;

        GameManager.Instance.ChangeStageTo(GameManager.GameStage.SwapStage);
    }

    public bool IsNeedAction()
    {
        bool isNeedAction = false;
        // Update enemy list if there is null due to enemy's dead
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            if (!enemyCtrl)
                _enemyCtrls = GetComponentsInChildren<EnemyControl>();
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            if (enemyCtrl.IsNeedAttack())
                isNeedAction = true;
        return isNeedAction;
    }

    public GameObject InstantiateEnemy(Vector2 position)
    {
        GameObject tmpObj = Instantiate(EnemyPrefab);
        tmpObj.transform.SetParent(transform);
        tmpObj.transform.localPosition = position;
        _enemyCtrls = GetComponentsInChildren<EnemyControl>();
        return tmpObj;
    }

    public void SetAllEnemysMovable(bool movable)
    {
        foreach (EnemyControl enemyCtrl in _enemyCtrls)
            enemyCtrl.IsMovable = movable;
    }
}
