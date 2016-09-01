using UnityEngine;
using System.Collections;

public class PlayerManager : MonoBehaviour {
    #region Singleton
    private static PlayerManager _mInstance;
    public static PlayerManager Instance
    {
        get { return _mInstance; }
    }
    #endregion

    [HideInInspector] public bool IsPlayerDone = false;
    [HideInInspector] public Vector3 PlayerPosition;
    [HideInInspector] public GameObject Player;

    private PlayerControl _playerControl;
    private PlayerPropertie _playerPropertie;

    // Use this for initialization
    void Awake()
    {
        _mInstance = this;
    }

    void Start ()
    {
    }
	
	// Update is called once per frame
	void Update ()
	{
        if (GameManager.Instance.GetGameStage() != GameManager.GameStage.PlayerStage)
            return;

        GameManager.Instance.ChangeStageTo(GameManager.GameStage.EnemyStage);
    }

    public GameObject InstantiatePlayer(Vector2 position)
    {
        if (Player != null)
            return null;

        Player = Instantiate(Resources.Load("Prefabs/Player", typeof(GameObject)) as GameObject);
        Player.transform.SetParent(transform);
        Player.transform.localPosition = position;
        PlayerPosition = Player.transform.position;
        _playerControl = Player.GetComponent<PlayerControl>();
        _playerPropertie = Player.GetComponent<PlayerPropertie>();
        return Player;
    }

    // Getter Functions
    public PlayerPropertie GetPlayerPropertie()
    {
        return _playerPropertie;
    }

    public PlayerControl GetPlayerControl()
    {
        return _playerControl;
    }
}
