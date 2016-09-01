using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    #region Singleton
    private static GameManager _mInstance;
    public static GameManager Instance
    {
        get { return _mInstance; }
    }
    #endregion

    public enum GameStage
    {
        SwapStage,
        PlayerStage,
        EnemyStage,
    }

    GameStage _stage = GameStage.SwapStage;

    [HideInInspector] public Vector2 ViewportBottomLeft;
    [HideInInspector] public Vector2 ViewportTopRight;

    public int AnimationCount;
    public int CoroutineCount;

    // Use this for initialization
    void Awake()
    {
        _mInstance = this;
    }
    void Start () {
        // get viewport size
        ViewportBottomLeft = Camera.main.ScreenToWorldPoint(Camera.main.pixelRect.position);
        ViewportTopRight = Camera.main.ScreenToWorldPoint(Camera.main.pixelRect.size);
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    // Game State Functions
    public GameStage GetGameStage()
    {
        return _stage;
    }

    public void ChangeStageTo(GameStage state)
    {
        switch (state)
        {
            case GameStage.PlayerStage:
                break;
            case GameStage.EnemyStage:
                if (EnemyManager.Instance.IsNeedAction())
                    GridManager.Instance.SetAllGridsColor(Color.grey);
                break;
            case GameStage.SwapStage:
                GridManager.Instance.SetAllGridsColor(GridManager.Instance.DefaultGridSpriteColor);
                GridManager.Instance.GridController.ResetTouchObject();
                break;
            default:
                break;
        }
        _stage = state;
    }
}
