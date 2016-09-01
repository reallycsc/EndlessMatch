using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Grid : MonoBehaviour
{
    public enum GridColor
    {
        Heart,
        Coin,
        Earth,
        Water,
        Fire,
        Wind,
        Magic,
        Player,
        Enemy,
    }

    public enum GridType
    {
        Player,
        Enemy,
        ChainCheckMin,
        Normal,
        Bomb,
        Vertical,
        Horizontal,
        Magic,
        ChainCheckMax,
    }

    public Vector2 MapPosition;
    [HideInInspector] public GridColor Color = GridColor.Water;
    [HideInInspector] public GridType Type = GridType.Normal;
    [HideInInspector] public GridType NeedToBeType = GridType.Normal;
    [HideInInspector] public bool IsChainH = false;
    [HideInInspector] public bool IsChainV = false;
    [HideInInspector] public bool IsMovable = true;
    [HideInInspector] public bool IsRemoving = false;

    private float _moveDuration = 0.2f;
    private float _dropDuration = 0.5f;
    private Animation _animation;
    private SpriteRenderer _spriteRenderer;
    private GameObject _targetFlyTo;

    // Use this for initialization
    protected void Awake()
    {
        Type = GridType.Normal;
        Color = (GridColor)Random.Range((int)GridColor.Heart, (int)GridColor.Wind + 1);
        _animation = GetComponent<Animation>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    void Start()
    {
        if (Color <= GridColor.Magic)
            _spriteRenderer.sprite = GridManager.Instance.GridItemSprites[(int)Color];
        name = Type + "_" + Color;
    }

    // Animation Functions
    public void Show()
    {
        if (!_animation)
            return;
        if (_animation.IsPlaying("GridShow"))
            return;
        if (_spriteRenderer.color.a > 0)
            return;

        _animation.Play("GridShow");
        GameManager.Instance.AnimationCount++;
    }

    public void MoveTo(Vector3 destination)
    {
        if (!_animation)
            return;
        AnimationClip animclip = new AnimationClip();
#if UNITY_5
        animclip.legacy = true;
#endif

        animclip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, transform.localPosition.x, _moveDuration, destination.x));
        animclip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, transform.localPosition.y, _moveDuration, destination.y));
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = _moveDuration;
        animEvent.functionName = "OnMoveToDone";
        animclip.AddEvent(animEvent);

        _animation.AddClip(animclip, "MoveTo");
        _animation.Play("MoveTo");
        Destroy(animclip, _moveDuration);

        GameManager.Instance.AnimationCount++;

        GridManager.Instance.MovedGridsList.Add(this);
    }

    public void MoveToAndMoveBack(Vector3 destination)
    {
        MoveToAndMoveBack(destination, _moveDuration);
    }

    public void MoveToAndMoveBack(Vector3 destination, float moveDuration)
    {
        if (!_animation || !IsChainGrid() || IsRemoving)
            return;
        if (_animation.isPlaying)
            return;

        AnimationClip animclip1 = new AnimationClip();
#if UNITY_5
        animclip1.legacy = true;
#endif
        animclip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, transform.localPosition.x, moveDuration, destination.x));
        animclip1.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, transform.localPosition.y, moveDuration, destination.y));
        _animation.AddClip(animclip1, "MoveTo");
        Destroy(animclip1, moveDuration);

        AnimationClip animclip2 = new AnimationClip();
#if UNITY_5
        animclip2.legacy = true;
#endif
        animclip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, destination.x, moveDuration, transform.localPosition.x));
        animclip2.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, destination.y, moveDuration, transform.localPosition.y));
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = moveDuration * 2;
        animEvent.functionName = "OnMoveToDone";
        animclip2.AddEvent(animEvent);
        _animation.AddClip(animclip2, "MoveBack");
        Destroy(animclip2, moveDuration * 2);

        _animation.Play("MoveTo");
        _animation.PlayQueued("MoveBack");

        GameManager.Instance.AnimationCount++;
    }

    protected void BeAttackMove(Vector3 destination, float duration)
    {
        if (!_animation)
            return;
        if (_animation.isPlaying)
            return;

        float durationAll = duration * 2;
        AnimationClip animclip1 = new AnimationClip();
#if UNITY_5
        animclip1.legacy = true;
#endif
        animclip1.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, transform.localPosition.x, duration, destination.x));
        animclip1.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, transform.localPosition.y, duration, destination.y));
        _animation.AddClip(animclip1, "MoveTo");
        Destroy(animclip1, duration);

        AnimationClip animclip2 = new AnimationClip();
#if UNITY_5
        animclip2.legacy = true;
#endif
        animclip2.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, destination.x, duration, transform.localPosition.x));
        animclip2.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, destination.y, duration, transform.localPosition.y));
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = durationAll;
        animEvent.functionName = "OnMoveToDone";
        animclip2.AddEvent(animEvent);
        _animation.AddClip(animclip2, "MoveBack");
        Destroy(animclip2, durationAll);

        _animation.Play("MoveTo");
        _animation.PlayQueued("MoveBack");

        GameManager.Instance.AnimationCount++;
    }

    public void DropTo(Vector3 destination, GridManager.DropDirection dropDirection)
    {
        if (!_animation)
            return;
        if (_animation.isPlaying)
            _animation.Stop();

        AnimationClip animclip = new AnimationClip();
#if UNITY_5
        animclip.legacy = true;
#endif
        switch (dropDirection)
        {
            case GridManager.DropDirection.Down:
                animclip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, transform.localPosition.x, _dropDuration, destination.x));
                animclip.SetCurve("", typeof(Transform), "localPosition.y", new AnimationCurve(
                    new Keyframe(0, transform.localPosition.y),
                    new Keyframe(_dropDuration - 0.2f, destination.y - 0.1f),
                    new Keyframe(_dropDuration - 0.1f, destination.y + 0.1f),
                    new Keyframe(_dropDuration, destination.y)));
                break;
            case GridManager.DropDirection.Up:
                animclip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, transform.localPosition.x, _dropDuration, destination.x));
                animclip.SetCurve("", typeof(Transform), "localPosition.y", new AnimationCurve(
                    new Keyframe(0, transform.localPosition.y),
                    new Keyframe(_dropDuration - 0.2f, destination.y + 0.1f),
                    new Keyframe(_dropDuration - 0.1f, destination.y - 0.1f),
                    new Keyframe(_dropDuration, destination.y)));
                break;
            case GridManager.DropDirection.Left:
                animclip.SetCurve("", typeof(Transform), "localPosition.x", new AnimationCurve(
                    new Keyframe(0, transform.localPosition.x),
                    new Keyframe(_dropDuration - 0.2f, destination.x - 0.1f),
                    new Keyframe(_dropDuration - 0.1f, destination.x + 0.1f),
                    new Keyframe(_dropDuration, destination.x)));
                animclip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, transform.localPosition.y, _dropDuration, destination.y));
                break;
            case GridManager.DropDirection.Right:
                animclip.SetCurve("", typeof(Transform), "localPosition.x", new AnimationCurve(
                    new Keyframe(0, transform.localPosition.x),
                    new Keyframe(_dropDuration - 0.2f, destination.x + 0.1f),
                    new Keyframe(_dropDuration - 0.1f, destination.x - 0.1f),
                    new Keyframe(_dropDuration, destination.x)));
                animclip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, transform.localPosition.y, _dropDuration, destination.y));
                break;
            default:
                break;
        }
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = _dropDuration;
        animEvent.functionName = "OnMoveToDone";
        animclip.AddEvent(animEvent);

        _animation.AddClip(animclip, "MoveTo");
        _animation.Play("MoveTo");
        Destroy(animclip, _dropDuration);

        GameManager.Instance.AnimationCount++;

        GridManager.Instance.MovedGridsList.Add(this);
    }

    public void RemoveAndFlyTo(GameObject target)
    {
        if (!_animation || !IsChainGrid() || IsRemoving)
            return;
        if (_animation.isPlaying)
        {
            _animation.Stop();
            GameManager.Instance.AnimationCount--;
        }

        _spriteRenderer.sortingOrder += 99; // Always on top

        if (Color <= GridColor.Coin)
            _targetFlyTo = PlayerManager.Instance.Player;
        else
            _targetFlyTo = target;
        Vector3 destination = _targetFlyTo.transform.position;
        AnimationClip animclip = new AnimationClip();
#if UNITY_5
        animclip.legacy = true;
#endif
        float distance = (destination - transform.position).magnitude;
        float scaleOneDuration = 0.15f;
        float moveDuration = Mathf.Sqrt(distance) * 0.08f + scaleOneDuration;
        
        // Scale big first then fly to target and scale small
        animclip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.EaseInOut(scaleOneDuration, transform.localPosition.x, moveDuration, destination.x));
        animclip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(scaleOneDuration, transform.localPosition.y, moveDuration, destination.y));
        animclip.SetCurve("", typeof(Transform), "localScale.x", new AnimationCurve(
            new Keyframe(0, transform.localScale.x),
            new Keyframe(scaleOneDuration, transform.localScale.x * 1.8f),
            new Keyframe(moveDuration, transform.localScale.x * 0.5f)));
        animclip.SetCurve("", typeof(Transform), "localScale.y", new AnimationCurve(
            new Keyframe(0, transform.localScale.y),
            new Keyframe(scaleOneDuration, transform.localScale.y * 1.8f),
            new Keyframe(moveDuration, transform.localScale.y * 0.5f)));

        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = moveDuration * 0.5f;
        animEvent.functionName = "OnRemoveAndFlyToAnimationDone";
        animclip.AddEvent(animEvent);

        animEvent = new AnimationEvent();
        animEvent.time = moveDuration;
        animEvent.functionName = "OnRemoveAndFlyToDone";
        animclip.AddEvent(animEvent);

        _animation.AddClip(animclip, "RemoveAndFlyTo");
        _animation.Play("RemoveAndFlyTo");
        Destroy(animclip, moveDuration);

        IsRemoving = true;

        GameManager.Instance.AnimationCount++;
    }

    public void Glow()
    {
        if (!_animation)
            return;
        _animation.Play("GridGlow");
    }

    public void StopGlow()
    {
        if (!_animation)
            return;
        _animation["GridGlow"].time = 0;
        _animation.Sample();
        _animation.Stop("GridGlow");
    }

    public void MoveToAndDestory(Vector3 destination)
    {
        MoveToAndDestory(destination, _moveDuration);
    }

    public void MoveToAndDestory(Vector3 destination, float moveDuration)
    {
        if (!_animation || !IsChainGrid() || IsRemoving)
            return;
        if (_animation.isPlaying)
            return;
        AnimationClip animclip = new AnimationClip();
#if UNITY_5
        animclip.legacy = true;
#endif

        animclip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, transform.localPosition.x, moveDuration, destination.x));
        animclip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.Linear(0, transform.localPosition.y, moveDuration, destination.y));
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = moveDuration;
        animEvent.functionName = "OnMoveToAndDestoryDone";
        animclip.AddEvent(animEvent);

        _animation.AddClip(animclip, "MoveToAndDestory");
        _animation.Play("MoveToAndDestory");
        Destroy(animclip, moveDuration);

        GameManager.Instance.AnimationCount++;
    }

    public void ScaleToAndRemove(float scaleX, float scaleY, float duration)
    {
        if (!_animation || !IsChainGrid() || IsRemoving)
            return;
        if (_animation.isPlaying)
            return;

        AnimationClip animclip = new AnimationClip();
#if UNITY_5
        animclip.legacy = true;
#endif
        animclip.SetCurve("", typeof(Transform), "localScale.x", AnimationCurve.Linear(0, transform.localScale.x, duration, scaleX));
        animclip.SetCurve("", typeof(Transform), "localScale.y", AnimationCurve.Linear(0, transform.localScale.y, duration, scaleY));
        AnimationEvent animEvent = new AnimationEvent();
        animEvent.time = duration;
        animEvent.functionName = "OnMoveToAndDestoryDone";
        animclip.AddEvent(animEvent);

        _animation.AddClip(animclip, "ScaleToAndRemove");
        _animation.Play("ScaleToAndRemove");
        Destroy(animclip, duration);

        GameManager.Instance.AnimationCount++;
    }

    void OnShowDone()
    {
        GameManager.Instance.AnimationCount--;
    }

    void OnMoveToDone()
    {
        GameManager.Instance.AnimationCount--;
    }

    void OnRemoveDone()
    {
        _animation.Stop();
        Destroy(gameObject);
        GameManager.Instance.AnimationCount--;
        GridManager.Instance.IsHintGlowing = false;
    }

    void OnGlowDone()
    {
        GridManager.Instance.IsHintGlowing = false;
    }

    void OnMoveToAndDestoryDone()
    {
        _animation.Stop();
        Destroy(gameObject);
        GameManager.Instance.AnimationCount--;
    }

    void OnRemoveAndFlyToAnimationDone()
    {
        GameManager.Instance.AnimationCount--;
        GridManager.Instance.IsHintGlowing = false;
    }

    void OnRemoveAndFlyToDone()
    {
        // Attack temp properties of enemy & player
        AddTempProperties();
        _animation.Stop();
        Destroy(gameObject);
    }

    // Assistant Functions
    public void SetGridColor(GridColor gridColor)
    {
        if (Type == GridType.Magic)
        {
            Color = GridColor.Magic;
            _spriteRenderer.sprite = null;
        }
        else
        {
            Color = gridColor;
            _spriteRenderer.sprite = GridManager.Instance.GridItemSprites[(int)Color];
        }
        name = Type + "_" + Color;
    }

    public void SetColor(Color color)
    {
        _spriteRenderer.color = color;
    }

    public bool IsChainGrid()
    {
        return Type > GridType.ChainCheckMin && Type < GridType.ChainCheckMax;
    }

    public bool IsSpecialGrid()
    {
        return Type > GridType.Normal && Type < GridType.ChainCheckMax;
    }

    public void Hide()
    {
        Color tempColor = _spriteRenderer.color;
        tempColor.a = 0;
        _spriteRenderer.color = tempColor;
    } 

    void AddTempProperties()
    {
        PlayerPropertie playerPropertie = PlayerManager.Instance.GetPlayerPropertie();
        switch (Color)
        {
            case GridColor.Coin:
                playerPropertie.AddPropertie(PropertiesEnum.Gold);
                break;
            case GridColor.Heart:
                playerPropertie.AddTempNumber(PropertiesEnum.RestoreHP);
                break;
            case GridColor.Earth:
                _targetFlyTo.GetComponent<EnemyPropertie>().AddTempNumber(playerPropertie.GetPropertie(PropertiesEnum.Earth));
                break;
            case GridColor.Water:
                _targetFlyTo.GetComponent<EnemyPropertie>().AddTempNumber(playerPropertie.GetPropertie(PropertiesEnum.Water));
                break;
            case GridColor.Fire:
                _targetFlyTo.GetComponent<EnemyPropertie>().AddTempNumber(playerPropertie.GetPropertie(PropertiesEnum.Fire));
                break;
            case GridColor.Wind:
                _targetFlyTo.GetComponent<EnemyPropertie>().AddTempNumber(playerPropertie.GetPropertie(PropertiesEnum.Wind));
                break;
            default:
                break;
        }
    }
}
