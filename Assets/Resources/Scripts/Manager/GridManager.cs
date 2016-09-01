using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    #region Singleton
    private static GridManager _mInstance;
    public static GridManager Instance
    {
        get { return _mInstance;}
    }
    #endregion

    public Sprite[] GridItemSprites;
    public Color DefaultGridSpriteColor = new Color(0.8f, 0.8f, 0.8f, 1);
    public float HintDelayTime = 1.0f;

    [HideInInspector] public GridController GridController;
    [HideInInspector] public enum DropDirection
    {
        Up,
        Right,
        Down,
        Left,
    }
    [HideInInspector] public DropDirection DropDir = DropDirection.Down;

    [HideInInspector] public bool IsCheckNeeded;
    [HideInInspector] public bool IsSwapDone;
    [HideInInspector] public bool IsDropNeeded;
    [HideInInspector] public bool IsHintGlowing;
    [HideInInspector] public List<Grid> MovedGridsList;

    private GameObject _gridPrefab;
    private GameObject _verticalPrefab;
    private GameObject _horizantalPrefab;
    private GameObject _bombPrefab;
    private GameObject _magicPrefab;
    private GameObject _verticalEffectPrefab;
    private GameObject _horizantalEffectPrefab;
    private GameObject _bombEffectPrefab;
    private GameObject _magicEffectPrefab;
    private GameObject _lightballEffectPrefab;
    private Transform _effectTransform;

    private Grid[,] _gridsMap;
    private List<Grid> _possibleChainGrids;
    private float _gridWidth;
    private float _gridHeight;
    private int _colNumberHalf;
    private int _rowNumberHalf;
    private int _colNumber;
    private int _rowNumber;
    private Color _gridSpriteColor;
    private float _addHintTime;
    private Grid _maxPossibleGrid;

    // Use this for initialization
    void Awake ()
    {
        _mInstance = this;
        MovedGridsList = new List<Grid>();
        _possibleChainGrids = new List<Grid>();
        _gridPrefab = Resources.Load("Prefabs/Grid", typeof(GameObject)) as GameObject;
        _verticalPrefab = Resources.Load("Prefabs/Vertical", typeof(GameObject)) as GameObject;
        _horizantalPrefab = Resources.Load("Prefabs/Horizontal", typeof(GameObject)) as GameObject;
        _bombPrefab = Resources.Load("Prefabs/Bomb", typeof(GameObject)) as GameObject;
        _magicPrefab = Resources.Load("Prefabs/Magic", typeof(GameObject)) as GameObject;

        _verticalEffectPrefab = Resources.Load("Prefabs/Effects/VerticalEffect", typeof(GameObject)) as GameObject;
        _horizantalEffectPrefab = Resources.Load("Prefabs/Effects/HorizontalEffect", typeof(GameObject)) as GameObject;
        _bombEffectPrefab = Resources.Load("Prefabs/Effects/BombEffect", typeof(GameObject)) as GameObject;
        _magicEffectPrefab = Resources.Load("Prefabs/Effects/MagicEffect", typeof(GameObject)) as GameObject;
        _lightballEffectPrefab = Resources.Load("Prefabs/Effects/LightBallEffect", typeof(GameObject)) as GameObject;

        _effectTransform = transform.FindChild("EffectNode");

        _gridSpriteColor = DefaultGridSpriteColor;
    }

    void Start()
    {
        GridController = GetComponent<GridController>();

        CreateGrids();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.CoroutineCount > 0 || GameManager.Instance.AnimationCount > 0 ||
            GameManager.Instance.GetGameStage() != GameManager.GameStage.SwapStage)
            return;

        // Check and remove grids
        CheckAndRemoveGrids();

        if (GameManager.Instance.CoroutineCount > 0 || GameManager.Instance.AnimationCount > 0)
            return;

        // Drop grids
        if (IsDropNeeded)
        {
            DropAllGrids();
            return;
        }

        // Check if any swap can chain
        if (_maxPossibleGrid == null)
        {
            _maxPossibleGrid = CheckAllPossibleChains();
            if (_maxPossibleGrid == null) // no possible chains, then check if any special grid can be used
            {
                MarkAllPossibleSpeicalGrids();
                if (_possibleChainGrids.Count == 0)
                {
                    // no special grid can be uesd, need to create new grids
                    StartCoroutine(CreateNewGrids());
                    return;
                }
            }
        }

        // Hint glowing for player
        HintGlowing();

        // Goto next stage
        if (!IsCheckNeeded && !IsDropNeeded && IsSwapDone)
        {
            IsSwapDone = false;
            GameManager.Instance.ChangeStageTo(GameManager.GameStage.PlayerStage);
        }
    }

    // Grid create function
    void CreateGrids()
    {
        // get viewport size
        Vector2 bottomLeft = GameManager.Instance.ViewportBottomLeft;
        Vector2 topRight = GameManager.Instance.ViewportTopRight;
        float sceneWidth = topRight.x - bottomLeft.x;
        float sceneHeight = topRight.y - bottomLeft.y;
        // get grid size
        GameObject tmpObj = Instantiate(_gridPrefab);
        _gridWidth = tmpObj.transform.localScale.x;
        _gridHeight = tmpObj.transform.localScale.y;
        Destroy(tmpObj);
        // calculate grids row & col numbers
        _colNumberHalf = Mathf.FloorToInt((sceneWidth - _gridWidth) / _gridWidth * 0.5f);
        _rowNumberHalf = Mathf.FloorToInt((sceneHeight - _gridHeight) / _gridHeight * 0.5f);
        /////////////////////////////////////DEBUG
        //_colNumberHalf = 2;
        //_rowNumberHalf = 2;
        /////////////////////////////////////DEBUG
        _colNumber = 2 * _colNumberHalf + 1;
        _rowNumber = 2 * _rowNumberHalf + 1;

        // create grids
        _gridsMap = new Grid[_colNumber, _rowNumber];
        GameObject gridBackgroundPrefab = Resources.Load("Prefabs/GridBackground", typeof (GameObject)) as GameObject;
        Transform backTransform = transform.FindChild("BackgroundNode");
        for (int x = 0; x < _colNumber; x++)
        {
            for (int y = 0; y < _rowNumber; y++)
            {
                // background
                tmpObj = Instantiate(gridBackgroundPrefab);
                tmpObj.transform.SetParent(backTransform);
                tmpObj.transform.localPosition = GetPosition(x, y);
                // grid
                _gridsMap[x, y] = InstantiateGrid(x, y);
                _gridsMap[x, y].Hide();
            }
        }

        // repeatly check if have any chains
        while (CheckAllChains() >= 3)
            if (RemoveAllChainsNoAnimation())
                DropAllGridsDown(false);

        _gridsMap[_colNumberHalf, _rowNumberHalf] = InstantiatePlayerGrid(_colNumberHalf, _rowNumberHalf);
        _gridsMap[_colNumberHalf, _rowNumber - 1] = InstantiateEnemyGrid(_colNumberHalf, _rowNumber - 1);

        StartCoroutine(ShowGrids());
    }

    IEnumerator CreateNewGrids()
    {
        GameManager.Instance.CoroutineCount++;
        // Suck all grid into middle
        Vector3 playerPos = PlayerManager.Instance.PlayerPosition;
        for (int x = 0; x < _colNumber; x++)
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid.IsChainGrid() && !grid.IsSpecialGrid()) // Speical grid stays
                {
                    grid.MoveToAndDestory(playerPos);
                    _gridsMap[x, y] = null;
                }
            }

        yield return new WaitForSeconds(0.2f);

        // create new grids
        _gridSpriteColor = DefaultGridSpriteColor;
        for (int x = 0; x < _colNumber; x++)
            for (int y = 0; y < _rowNumber; y++)
                if (_gridsMap[x, y] == null)
                {
                    _gridsMap[x, y] = InstantiateGrid(x, y);
                    _gridsMap[x, y].Hide();
                }

        // Show all grids
        yield return StartCoroutine(ShowGrids());

        IsCheckNeeded = true;
        DropDir = DropDirection.Down;

        GameManager.Instance.CoroutineCount--;
    }

    IEnumerator ShowGrids()
    {
        GameManager.Instance.CoroutineCount++;
        for (int i = 1, iMax = Mathf.Max(_rowNumberHalf, _colNumberHalf); i <= iMax; i++)
        {
            yield return new WaitForSeconds(0.1f);
            int xMin = _colNumberHalf - i;
            int xMax = _colNumberHalf + i;
            int yMin = _rowNumberHalf - i;
            int yMax = _rowNumberHalf + i;
            int xMinClamp = Mathf.Max(xMin, 0);
            int xMaxClamp = Mathf.Min(xMax, _colNumber - 1);
            int yMinClamp = Mathf.Max(yMin, 0);
            int yMaxClamp = Mathf.Min(yMax, _rowNumber - 1);
            // Up
            if (yMax < _rowNumber)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                {
                    Grid grid = _gridsMap[j, yMax];
                    if (grid != null && grid.IsChainGrid())
                        grid.Show();
                }
            // Right
            if (xMax < _colNumber)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                {
                    Grid grid = _gridsMap[xMax, j];
                    if (grid != null && grid.IsChainGrid())
                        grid.Show();
                }
            // Down
            if (yMin >= 0)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                {
                    Grid grid = _gridsMap[j, yMin];
                    if (grid != null && grid.IsChainGrid())
                        grid.Show();
                }
            // Left
            if (xMin >= 0)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                {
                    Grid grid = _gridsMap[xMin, j];
                    if (grid != null && grid.IsChainGrid())
                        grid.Show();
                }
        }
        GameManager.Instance.CoroutineCount--;
    }

    Grid InstantiateGrid(int x, int y)
    {
        GameObject tmpObj = Instantiate(_gridPrefab);
        tmpObj.transform.SetParent(transform);
        tmpObj.transform.localPosition = GetPosition(x, y);
        Grid grid = tmpObj.GetComponent<Grid>();
        grid.SetColor(_gridSpriteColor);
        grid.MapPosition = new Vector2(x, y);
        return grid;
    }

    Grid InstantiateGrid(int x, int y, Vector2 position, Vector2 destination, DropDirection dropDirection)
    {
        GameObject tmpObj = Instantiate(_gridPrefab);
        tmpObj.transform.SetParent(transform);
        tmpObj.transform.localPosition = position;
        Grid grid = tmpObj.GetComponent<Grid>();
        grid.SetColor(_gridSpriteColor);
        grid.MapPosition = new Vector2(x, y);
        grid.DropTo(destination, dropDirection);
        return grid;
    }

    Grid InstantiateSpecialGrid(int x, int y, Grid.GridColor color, Grid.GridType type)
    {
        GameObject tmpObj;
        switch (type)
        {
            case Grid.GridType.Vertical:
                tmpObj = Instantiate(_verticalPrefab);
                break;
            case Grid.GridType.Horizontal:
                tmpObj = Instantiate(_horizantalPrefab);
                break;
            case Grid.GridType.Bomb:
                tmpObj = Instantiate(_bombPrefab);
                break;
            case Grid.GridType.Magic:
                tmpObj = Instantiate(_magicPrefab);
                break;
            default:
                tmpObj = Instantiate(_gridPrefab);
                break;
        }
        tmpObj.transform.SetParent(transform);
        tmpObj.transform.localPosition = GetPosition(x, y);
        Grid grid = tmpObj.GetComponent<Grid>();
        grid.Type = type;
        if (type == Grid.GridType.Magic)
            grid.SetGridColor(Grid.GridColor.Magic);
        else
            grid.SetGridColor(color);
        grid.SetColor(_gridSpriteColor);
        grid.MapPosition = new Vector2(x, y);

        // Player effect
        //tmpObj = Instantiate(_lightballEffectPrefab);
        //tmpObj.transform.SetParent(_effectTransform);
        //tmpObj.transform.localPosition = grid.transform.position;
        //tmpObj.GetComponent<ParticleSystem>().Play();

        return grid;
    }

    Grid InstantiatePlayerGrid(int x, int y)
    {
        if (_gridsMap[x, y] != null)
        {
            Destroy(_gridsMap[x, y].gameObject);
            _gridsMap[x, y] = null;
        }
        Grid grid = PlayerManager.Instance.InstantiatePlayer(GetPosition(x, y)).GetComponent<Grid>();
        grid.MapPosition = new Vector2(x, y);
        return grid;
    }

    Grid InstantiateEnemyGrid(int x, int y)
    {
        if (_gridsMap[x, y] != null)
        {
            Destroy(_gridsMap[x, y].gameObject);
            _gridsMap[x, y] = null;
        }
        Grid grid = EnemyManager.Instance.InstantiateEnemy(GetPosition(x, y)).GetComponent<Grid>();
        grid.MapPosition = new Vector2(x, y);
        return grid;
    }

    Grid InstantiateEnemyGrid(int x, int y, Vector2 position, Vector2 destination, DropDirection dropDirection)
    {
        if (_gridsMap[x, y] != null)
        {
            Destroy(_gridsMap[x, y].gameObject);
            _gridsMap[x, y] = null;
        }
        Grid grid = EnemyManager.Instance.InstantiateEnemy(position).GetComponent<Grid>();
        grid.MapPosition = new Vector2(x, y);
        grid.DropTo(destination, dropDirection);
        return grid;
    }

    // Grid Controller functions
    public GameObject GetGridObject(int x, int y)
    {
        if (x >= 0 && x < _colNumber && y >= 0 && y < _rowNumber)
            return _gridsMap[x, y].gameObject;
        return null;
    }

    public bool SpecialGridProcess(Grid grid1, Grid grid2)
    {
        int x = (int)grid1.MapPosition.x;
        int y = (int)grid1.MapPosition.y;
        bool isRemoved = false;
        if (grid1.Type == Grid.GridType.Magic)
        {
            grid1.RemoveAndFlyTo(PlayerManager.Instance.GetPlayerControl().GetTarget());
            _gridsMap[x, y] = null;
            StartCoroutine(RemoveMagicGrids(x, y, grid2));
            isRemoved = true;
        }
        else if (grid1.Type == Grid.GridType.Horizontal)
        {
            if (grid2.Type == Grid.GridType.Horizontal || 
                grid2.Type == Grid.GridType.Vertical || 
                grid2.Type == Grid.GridType.Bomb)
            {
                StartCoroutine(RemoveHorizantalWithSpeical(grid1, grid2));
                isRemoved = true;
            }
        }
        else if (grid1.Type == Grid.GridType.Vertical)
        {
            if (grid2.Type == Grid.GridType.Horizontal ||
                grid2.Type == Grid.GridType.Vertical ||
                grid2.Type == Grid.GridType.Bomb)
            {
                StartCoroutine(RemoveVerticalWithSpeical(grid1, grid2));
                isRemoved = true;
            }
        }
        else if (grid1.Type == Grid.GridType.Bomb)
        {
            if (grid2.Type == Grid.GridType.Horizontal)
            {
                StartCoroutine(RemoveHorizantalWithSpeical(grid2, grid1));
                isRemoved = true;
            }
            else if (grid2.Type == Grid.GridType.Horizontal)
            {
                StartCoroutine(RemoveVerticalWithSpeical(grid2, grid1));
                isRemoved = true;
            }
            else if (grid2.Type == Grid.GridType.Bomb)
            {
                StartCoroutine(RemoveTwoBombGrids(grid1, grid2));
                isRemoved = true;
            }
        }
        if (isRemoved)
        {
            IsDropNeeded = true;
            StopHintGlowing();
        }
        return isRemoved;
    }

    public void SwapGrids(Grid grid1, Grid grid2)
    {
        Vector2 tmpPos = grid1.MapPosition;
        grid1.MapPosition = grid2.MapPosition;
        grid2.MapPosition = tmpPos;

        int x1 = (int) grid1.MapPosition.x;
        int y1 = (int) grid1.MapPosition.y;
        int x2 = (int)grid2.MapPosition.x;
        int y2 = (int)grid2.MapPosition.y;

        _gridsMap[x1, y1] = grid1;
        _gridsMap[x2, y2] = grid2;
    }

    void CheckAndRemoveGrids()
    {
        if (IsCheckNeeded)
        {
            int chains = CheckAllChains();
            if (chains >= 3)
                RemoveAllChians();
            IsCheckNeeded = false;
            if (!IsDropNeeded) // Already checked and no drop grid, restart the hint time
            {
                _addHintTime = 0;
                _maxPossibleGrid = null;
            }
        }
    }

    void DropAllGrids()
    {
        switch (DropDir)
        {
            case DropDirection.Down:
                DropAllGridsDown(true);
                break;
            case DropDirection.Up:
                DropAllGridsUp();
                break;
            case DropDirection.Left:
                DropAllGridsLeft();
                break;
            case DropDirection.Right:
                DropAllGridsRight();
                break;
            default:
                DropAllGridsDown(true);
                break;
        }
        IsCheckNeeded = true;
        IsDropNeeded = false;
    }

    void HintGlowing()
    {
        if (!IsHintGlowing)
        {
            _addHintTime += Time.deltaTime;
            if (_addHintTime >= HintDelayTime)
            {
                _addHintTime = 0;
                if (_maxPossibleGrid)
                    MarkPossibleChain(_maxPossibleGrid);
                if (_possibleChainGrids.Count > 0)
                {
                    IsHintGlowing = true;
                    foreach (Grid grid in _possibleChainGrids)
                        grid.Glow();
                }
            }
        }
    }

    void RemoveAllChians()
    {
        IsDropNeeded = false;
        List<Grid> specialGridsList = new List<Grid>();
        GameObject target = PlayerManager.Instance.GetPlayerControl().GetTarget();
        for (int x = 0; x < _colNumber; x++)
        {
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid && grid.IsChainGrid() && (grid.IsChainH || grid.IsChainV))
                {
                    if (grid.NeedToBeType != Grid.GridType.Normal)
                    {
                        _gridsMap[x, y] = InstantiateSpecialGrid((int)grid.MapPosition.x, (int)grid.MapPosition.y,
                            grid.Color, grid.NeedToBeType);
                        specialGridsList.Add(_gridsMap[x, y]);
                    }
                    else
                        _gridsMap[x, y] = null;
                    grid.RemoveAndFlyTo(target);
                    // Process special grid
                    if (grid.IsSpecialGrid())
                        StartCoroutine(RemoveSpecialGrid(grid));
                    IsDropNeeded = true;
                }
            }
        }
        specialGridsList.Clear();
    }

    bool RemoveAllChainsNoAnimation()
    {
        bool isRemoved = false;
        for (int x = 0; x < _colNumber; x++)
        {
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid && (grid.IsChainH || grid.IsChainV))
                {
                    isRemoved = true;
                    Destroy(grid.gameObject);
                    _gridsMap[x, y] = null;
                }
            }
        }
        return isRemoved;
    }

    void DropAllGridsDown(bool isAnimation)
    {
        for (int x = 0; x < _colNumber; x++)
        {
            int needFillRow = 0;
            int needFillRowWithUnMovable = 0;
            int filledRow = 0;
            // drop all Grids in this column
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid == null)
                    needFillRow++;
                else if (!grid.IsMovable) // Jump un-movable grid
                {
                    needFillRowWithUnMovable = needFillRow;
                    if (needFillRowWithUnMovable > 0)
                        needFillRow++; // Fill one more row because need to jump un-movable grid
                }
                else
                {
                    if (needFillRow > 0)
                    {
                        if (filledRow > 0 && filledRow == needFillRowWithUnMovable)
                            // Sub one fill row because already filled all row before un-movable grid
                            needFillRow--;

                        int newY = y - needFillRow;
                        _gridsMap[x, newY] = grid;
                        _gridsMap[x, y] = null;
                        grid.MapPosition = new Vector2(x, newY);
                        Vector2 newPosition = GetPosition(x, newY);
                        if (isAnimation)
                            grid.DropTo(newPosition, DropDirection.Down);
                        else
                            grid.transform.localPosition = newPosition;
                        grid.IsChainH = false;
                        grid.IsChainV = false;

                        if (needFillRowWithUnMovable > 0)
                            filledRow++;
                    }
                }
            }
            // create new Grids and drop
            for (int y = _rowNumber - needFillRow; y < _rowNumber; y++)
            {
                if (filledRow > 0 && filledRow == needFillRowWithUnMovable)
                // Jump one fill row because already filled all row before un-movable grid
                {
                    filledRow++;
                    continue;
                }
                if (isAnimation)
                {
                    Vector2 destination = GetPosition(x, y);
                    Vector2 position = destination + Vector2.up * needFillRow * _gridHeight;
                    _gridsMap[x, y] = InstantiateGrid(x, y, position, destination, DropDirection.Down);
                }
                else
                {
                    _gridsMap[x, y] = InstantiateGrid(x, y);
                    _gridsMap[x, y].Hide();
                }
                if (needFillRowWithUnMovable > 0)
                    filledRow++;
            }
        }
    }

    void DropAllGridsUp()
    {
        for (int x = 0; x < _colNumber; x++)
        {
            int needFillRow = 0;
            int needFillRowWithUnMovable = 0;
            int filledRow = 0;
            // drop all Grids in this column
            for (int y = _rowNumber - 1; y >= 0; y--)
            {
                Grid grid = _gridsMap[x, y];
                if (grid == null)
                    needFillRow++;
                else if (!grid.IsMovable) // Jump un-movable grid
                {
                    needFillRowWithUnMovable = needFillRow;
                    if (needFillRowWithUnMovable > 0)
                        needFillRow++; // Fill one more row because need to jump un-movable grid
                }
                else
                {
                    if (needFillRow > 0)
                    {
                        if (filledRow > 0 && filledRow == needFillRowWithUnMovable) // Sub one fill row because already filled all row before un-movable grid
                            needFillRow--;

                        int newY = y + needFillRow;
                        _gridsMap[x, newY] = grid;
                        _gridsMap[x, y] = null;
                        grid.MapPosition = new Vector2(x, newY);
                        Vector2 newPosition = GetPosition(x, newY);
                        grid.DropTo(newPosition, DropDirection.Up);
                        grid.IsChainH = false;
                        grid.IsChainV = false;

                        if (needFillRowWithUnMovable > 0)
                            filledRow++;
                    }
                }
            }
            // create new Grids and drop
            for (int y = needFillRow - 1; y >= 0; y--)
            {
                if (filledRow > 0 && filledRow == needFillRowWithUnMovable) // Jump one fill row because already filled all row before un-movable grid
                {
                    filledRow++;
                    continue;
                }
                Vector2 destination = GetPosition(x, y);
                Vector2 position = destination + Vector2.down * needFillRow * _gridHeight;
                _gridsMap[x, y] = InstantiateGrid(x, y, position, destination, DropDirection.Up);
                if (needFillRowWithUnMovable > 0)
                    filledRow++;
            }
        }
    }

    void DropAllGridsLeft()
    {
        for (int y = 0; y < _rowNumber; y++)
        {
            int needFillCol = 0;
            int needFillColWithUnMovable = 0;
            int filledCol = 0;
            // drop all Grids in this column
            for (int x = 0; x < _colNumber; x++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid == null)
                    needFillCol++;
                else if (!grid.IsMovable) // Jump un-movable grid
                {
                    needFillColWithUnMovable = needFillCol;
                    if (needFillColWithUnMovable > 0)
                        needFillCol++; // Fill one more column because need to jump un-movable grid
                }
                else
                {
                    if (needFillCol > 0)
                    {
                        if (filledCol > 0 && filledCol == needFillColWithUnMovable) // Sub one fill column because already filled all column before un-movable grid
                            needFillCol--;

                        int newX = x - needFillCol;
                        _gridsMap[newX, y] = grid;
                        _gridsMap[x, y] = null;
                        grid.MapPosition = new Vector2(newX, y);
                        Vector2 newPosition = GetPosition(newX, y);
                        grid.DropTo(newPosition, DropDirection.Left);
                        grid.IsChainH = false;
                        grid.IsChainV = false;

                        if (needFillColWithUnMovable > 0)
                            filledCol++;
                    }
                }
            }
            // create new Grids and drop
            for (int x = _colNumber - needFillCol; x < _colNumber; x++)
            {
                if (filledCol > 0 && filledCol == needFillColWithUnMovable) // Jump one fill column because already filled all column before un-movable grid
                {
                    filledCol++;
                    continue;
                }
                Vector2 destination = GetPosition(x, y);
                Vector2 position = destination + Vector2.right * needFillCol * _gridWidth;
                _gridsMap[x, y] = InstantiateGrid(x, y, position, destination, DropDirection.Left);
                if (needFillColWithUnMovable > 0)
                    filledCol++;
            }
        }
    }

    void DropAllGridsRight()
    {
        for (int y = 0; y < _rowNumber; y++)
        {
            int needFillCol = 0;
            int needFillColWithUnMovable = 0;
            int filledCol = 0;
            // drop all Grids in this column
            for (int x = _colNumber - 1; x >= 0; x--)
            {
                Grid grid = _gridsMap[x, y];
                if (grid == null)
                    needFillCol++;
                else if (!grid.IsMovable) // Jump un-movable grid
                {
                    needFillColWithUnMovable = needFillCol;
                    if (needFillColWithUnMovable > 0)
                        needFillCol++; // Fill one more column because need to jump un-movable grid
                }
                else
                {
                    if (needFillCol > 0)
                    {
                        if (filledCol > 0 && filledCol == needFillColWithUnMovable) // Sub one fill column because already filled all column before un-movable grid
                            needFillCol--;

                        int newX = x + needFillCol;
                        _gridsMap[newX, y] = grid;
                        _gridsMap[x, y] = null;
                        grid.MapPosition = new Vector2(newX, y);
                        Vector2 newPosition = GetPosition(newX, y);
                        grid.DropTo(newPosition, DropDirection.Right);
                        grid.IsChainH = false;
                        grid.IsChainV = false;

                        if (needFillColWithUnMovable > 0)
                            filledCol++;
                    }
                }
            }
            // create new Grids and drop
            for (int x = needFillCol - 1; x >= 0; x--)
            {
                if (filledCol > 0 && filledCol == needFillColWithUnMovable) // Jump one fill column because already filled all column before un-movable grid
                {
                    filledCol++;
                    continue;
                }
                Vector2 destination = GetPosition(x, y);
                Vector2 position = destination + Vector2.left * needFillCol * _gridHeight;
                _gridsMap[x, y] = InstantiateGrid(x, y, position, destination, DropDirection.Right);
                if (needFillColWithUnMovable > 0)
                    filledCol++;
            }
        }
    }

    public IEnumerator RemoveSpecialGrid(Grid grid)
    {
        int x = (int) grid.MapPosition.x;
        int y = (int) grid.MapPosition.y;
        switch (grid.Type)
        {
            case Grid.GridType.Horizontal:
                yield return StartCoroutine(RemoveHorizantalGrids(x, y));
                break;
            case Grid.GridType.Vertical:
                yield return StartCoroutine(RemoveVerticalGrids(x, y));
                break;
            case Grid.GridType.Bomb:
                StartCoroutine(RemoveBombGrids(x, y, 1));
                break;
            case Grid.GridType.Magic:
                {
                    List<Grid> aroundGrids = new List<Grid>();
                    for (int newX = Mathf.Max(x - 1, 0); newX <= Mathf.Min(x + 1, _colNumber - 1); newX++)
                        for (int newY = Mathf.Max(y - 1, 0); newY <= Mathf.Min(y + 1, _rowNumber - 1); newY++)
                        {
                            if (newX == x && newY == y)
                                continue;
                            if (_gridsMap[newX, newY])
                                aroundGrids.Add(_gridsMap[newX, newY]);
                        }
                    if (aroundGrids.Count > 0)
                        // Randomly choose one around because be removed by other ways beside swap
                        yield return StartCoroutine(RemoveMagicGrids(x, y, aroundGrids[Random.Range(0, aroundGrids.Count)]));
                    aroundGrids.Clear();
                }  
                break;
            default:
                break;
        }
    }

    IEnumerator RemoveBombGrids(int x, int y, int range)
    {
        //print("In RemoveBombGrids");
        GameManager.Instance.CoroutineCount++;
        // Player effect animation here
        GameObject tmpObj = Instantiate(_bombEffectPrefab);
        tmpObj.transform.SetParent(_effectTransform);
        tmpObj.transform.position = GetPosition(x, y);
        tmpObj.GetComponent<BombEffect>().ScaleEnd *= range;
        // Remove around grids
        for (int i = 1; i <= range; i++)
        {
            int xMin = x - i;
            int xMax = x + i;
            int yMin = y - i;
            int yMax = y + i;
            int xMinClamp = Mathf.Max(xMin, 0);
            int xMaxClamp = Mathf.Min(x + i, _colNumber - 1);
            int yMinClamp = Mathf.Max(y - i, 0);
            int yMaxClamp = Mathf.Min(y + i, _rowNumber - 1);
            // Up
            if (yMax < _rowNumber)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                    RemoveGridForSpecialGrid(j, yMax);
            // Right
            if (xMax < _colNumber)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                    RemoveGridForSpecialGrid(xMax, j);
            // Down
            if (yMin >= 0)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                    RemoveGridForSpecialGrid(j, yMin);
            // Left
            if (xMin >= 0)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                    RemoveGridForSpecialGrid(xMin, j);
        }
        yield return new WaitForSeconds(0.15f);
        // Move outter range grids
        float moveDuration = 0.1f;
        float moveDistance = 0.5f;
        for (int i = range + 1; i <= range + 2; i++)
        {
            int xMin = x - i;
            int xMax = x + i;
            int yMin = y - i;
            int yMax = y + i;
            int xMinClamp = Mathf.Max(xMin, 0);
            int xMaxClamp = Mathf.Min(x + i, _colNumber - 1);
            int yMinClamp = Mathf.Max(y - i, 0);
            int yMaxClamp = Mathf.Min(y + i, _rowNumber - 1);
            float distance = moveDistance/(i - range);
            float duration = moveDuration/ (i - range);
            // Up
            if (yMax < _rowNumber)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                    if (_gridsMap[j, yMax])
                        _gridsMap[j, yMax].MoveToAndMoveBack(_gridsMap[j, yMax].transform.position + Vector3.up * distance, duration);
            // Right
            if (xMax < _colNumber)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                    if (_gridsMap[xMax, j])
                        _gridsMap[xMax, j].MoveToAndMoveBack(_gridsMap[xMax, j].transform.position + Vector3.right * distance, duration);
            // Down
            if (yMin >= 0)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                    if (_gridsMap[j, yMin])
                        _gridsMap[j, yMin].MoveToAndMoveBack(_gridsMap[j, yMin].transform.position + Vector3.down * distance, duration);
            // Left
            if (xMin >= 0)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                    if (_gridsMap[xMin, j])
                        _gridsMap[xMin, j].MoveToAndMoveBack(_gridsMap[xMin, j].transform.position + Vector3.left * distance, duration);
        }

        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveBombGrids");
    }

    IEnumerator RemoveHorizantalGrids(int x, int y)
    {
        //print("In RemoveHorizantalGrids");
        GameManager.Instance.CoroutineCount++;
        // Player effect animation here
        GameObject tmpObj = Instantiate(_horizantalEffectPrefab);
        tmpObj.transform.SetParent(_effectTransform);
        tmpObj.transform.position = GetPosition(x, y);
        // Remove horizantal grids
        for (int i = 1, maxCol = Mathf.Max(_colNumber - x, x + 1); i < maxCol; i++)
        {
            RemoveGridForSpecialGrid(x + i, y);
            RemoveGridForSpecialGrid(x - i, y);
            yield return new WaitForSeconds(0.03f);
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveHorizantalGrids");
    }

    IEnumerator RemoveVerticalGrids(int x, int y)
    {
        //print("In RemoveVerticalGrids");
        GameManager.Instance.CoroutineCount++;
        // Player effect animation here
        GameObject tmpObj = Instantiate(_verticalEffectPrefab);
        tmpObj.transform.SetParent(_effectTransform);
        tmpObj.transform.position = GetPosition(x, y);
        // Remove vertical grids
        for (int i = 1, maxRow = Mathf.Max(_rowNumber - y, y + 1); i < maxRow; i++)
        {
            RemoveGridForSpecialGrid(x, y + i);
            RemoveGridForSpecialGrid(x, y - i);
            yield return new WaitForSeconds(0.03f);
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveVerticalGrids");
    }

    IEnumerator RemoveMagicGrids(int x, int y, Grid otherGrid)
    {
        //print("In RemoveMagicGrids");
        GameManager.Instance.CoroutineCount++;
        switch (otherGrid.Type)
        {
            case Grid.GridType.Normal:
                {
                    List<Grid> needRemoveGrids = new List<Grid>();
                    // Player effect animation
                    for (int i = 0; i < _colNumber; i++)
                        for (int j = 0; j < _rowNumber; j++)
                        {
                            Grid gridTemp = _gridsMap[i, j];
                            if (gridTemp && gridTemp.IsChainGrid() && gridTemp.Color == otherGrid.Color)
                            {
                                // Player effect animation here
                                GameObject tmpObj = Instantiate(_magicEffectPrefab);
                                tmpObj.transform.SetParent(_effectTransform);
                                tmpObj.transform.position = GetPosition(x, y);
                                tmpObj.GetComponentInChildren<MagicEffect>().SetDestination(gridTemp.transform.position);
                                needRemoveGrids.Add(gridTemp);
                            }
                        }
                    yield return new WaitForSeconds(0.15f);
                    // Remove same color grids
                    foreach (var gridTemp in needRemoveGrids)
                        RemoveGridForSpecialGrid(gridTemp);
                    needRemoveGrids.Clear();
                    break;
                }
            case Grid.GridType.Bomb:
            case Grid.GridType.Horizontal:
            case Grid.GridType.Vertical:
                {
                    List<Grid> needRemoveGrids = new List<Grid>();
                    // Player effect animation
                    for (int i = 0; i < _colNumber; i++)
                        for (int j = 0; j < _rowNumber; j++)
                        {
                            Grid gridTemp = _gridsMap[i, j];
                            if (gridTemp && gridTemp.IsChainGrid() && gridTemp.Color == otherGrid.Color)
                            {
                                // Player effect animation here
                                GameObject tmpObj = Instantiate(_magicEffectPrefab);
                                tmpObj.transform.SetParent(_effectTransform);
                                tmpObj.transform.position = GetPosition(x, y);
                                tmpObj.GetComponentInChildren<MagicEffect>().SetDestination(gridTemp.transform.position);
                                needRemoveGrids.Add(gridTemp);
                            }
                        }
                    yield return new WaitForSeconds(0.15f);
                    // Change same color grids to special and remove
                    List<Grid> needRemoveSpeicalGrids = new List<Grid>();
                    foreach (var gridTemp in needRemoveGrids)
                        if (gridTemp)
                        {
                            int xTemp = (int) gridTemp.MapPosition.x;
                            int yTemp = (int) gridTemp.MapPosition.y;
                            Destroy(gridTemp.gameObject);
                            Grid gridTempNew = InstantiateSpecialGrid(xTemp, yTemp, gridTemp.Color, otherGrid.Type);
                            _gridsMap[xTemp, yTemp] = gridTempNew;
                            needRemoveSpeicalGrids.Add(gridTempNew);
                        }
                    needRemoveGrids.Clear();
                    yield return new WaitForSeconds(0.5f);
                    GameObject target = PlayerManager.Instance.GetPlayerControl().GetTarget();
                    foreach (var gridTemp in needRemoveSpeicalGrids)
                        if (gridTemp)
                        {
                            gridTemp.RemoveAndFlyTo(target);
                            StartCoroutine(RemoveSpecialGrid(gridTemp));
                            yield return new WaitForSeconds(0.2f);
                        }
                    needRemoveSpeicalGrids.Clear();
                    break;
                }
            case Grid.GridType.Magic:
                {
                    int otherX = (int)otherGrid.MapPosition.x;
                    int otherY = (int)otherGrid.MapPosition.y;
                    // Player effect animation with all grid
                    for (int i = 0; i < _colNumber; i++)
                        for (int j = 0; j < _rowNumber; j++)
                        {
                            if ((i == x && j == y) || (i == otherX && j == otherY))
                                continue;
                            Grid gridTemp = _gridsMap[i, j];
                            if (gridTemp && gridTemp.IsChainGrid())
                            {
                                // Player effect animation here
                                GameObject tmpObj = Instantiate(_magicEffectPrefab);
                                tmpObj.transform.SetParent(_effectTransform);
                                tmpObj.transform.position = GetPosition(x, y);
                                tmpObj.GetComponentInChildren<MagicEffect>()
                                    .SetDestination(gridTemp.transform.position);
                            }
                        }
                    yield return new WaitForSeconds(0.15f);
                    // Remove all grids
                    for (int i = 0; i < _colNumber; i++)
                        for (int j = 0; j < _rowNumber; j++)
                            RemoveGridForSpecialGrid(_gridsMap[i, j]);
                    break;
                }
            default:
                break;
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveMagicGrids");
    }

    IEnumerator RemoveCrossGrids(int x, int y)
    {
        // Create cross animation
        GameObject tmpObj1 = Instantiate(_horizantalEffectPrefab);
        tmpObj1.transform.SetParent(_effectTransform);
        tmpObj1.transform.position = GetPosition(x, y);
        GameObject tmpObj2 = Instantiate(_verticalEffectPrefab);
        tmpObj2.transform.SetParent(_effectTransform);
        tmpObj2.transform.position = GetPosition(x, y);
        // Remove horizantal grids
        for (int i = 1, maxCol = Mathf.Max(_colNumber - x, x + 1); i < maxCol; i++)
        {
            RemoveGridForSpecialGrid(x + i, y);
            RemoveGridForSpecialGrid(x - i, y);
            yield return new WaitForSeconds(0.03f);
        }
        // Remove vertical grids
        for (int i = 1, maxRow = Mathf.Max(_rowNumber - y, y + 1); i < maxRow; i++)
        {
            RemoveGridForSpecialGrid(x, y + i);
            RemoveGridForSpecialGrid(x, y - i);
            yield return new WaitForSeconds(0.03f);
        }
    }

    IEnumerator RemoveThreeHorizantalGrids(int x, int y)
    {
        //print("In RemoveThreeHorizantalGrids");
        GameManager.Instance.CoroutineCount++;
        GameObject target = PlayerManager.Instance.GetPlayerControl().GetTarget();
        // Player effect animation here and remove middle grid
        for (int newY = Mathf.Max(y - 1, 0); newY <= Mathf.Min(y + 1, _rowNumber-1); newY++)
        {
            GameObject tmpObj = Instantiate(_horizantalEffectPrefab);
            tmpObj.transform.SetParent(_effectTransform);
            tmpObj.transform.position = GetPosition(x, newY);
            Grid grid = _gridsMap[x, newY];
            if (grid)
            {
                grid.RemoveAndFlyTo(target);
                _gridsMap[x, newY] = null;
            }
        }
        // Remove three horizantal grids
        for (int i = 1, maxCol = Mathf.Max(_colNumber - x, x + 1); i < maxCol; i++)
        {
            for (int newY = Mathf.Max(y - 1, 0); newY <= Mathf.Min(y + 1, _rowNumber-1); newY++)
            {
                RemoveGridForSpecialGrid(x + i, newY);
                RemoveGridForSpecialGrid(x - i, newY);
            }
            yield return new WaitForSeconds(0.03f);
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveThreeHorizantalGrids");
    }

    IEnumerator RemoveThreeVerticalGrids(int x, int y)
    {
        //print("In RemoveThreeVerticalGrids");
        GameManager.Instance.CoroutineCount++;
        GameObject target = PlayerManager.Instance.GetPlayerControl().GetTarget();
        // Player effect animation here and remove middle grid
        for (int newX = Mathf.Max(x - 1, 0); newX <= Mathf.Min(x + 1, _colNumber-1); newX++)
        {
            GameObject tmpObj = Instantiate(_verticalEffectPrefab);
            tmpObj.transform.SetParent(_effectTransform);
            tmpObj.transform.position = GetPosition(newX, y);
            Grid grid = _gridsMap[newX, y];
            if (grid)
            {
                grid.RemoveAndFlyTo(target);
                _gridsMap[newX, y] = null;
            }
        }
        // Remove three horizantal grids
        for (int i = 1, maxRow = Mathf.Max(_rowNumber - y, y + 1); i < maxRow; i++)
        {
            for (int newX = Mathf.Max(x - 1, 0); newX <= Mathf.Min(x + 1, _colNumber-1); newX++)
            {
                RemoveGridForSpecialGrid(newX, y + i);
                RemoveGridForSpecialGrid(newX, y - i);
            }
            yield return new WaitForSeconds(0.03f);
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveThreeVerticalGrids");
    }

    IEnumerator RemoveHorizantalWithSpeical(Grid grid1, Grid grid2)
    {
        //print("In RemoveHorizantalWithSpeical");
        GameManager.Instance.CoroutineCount++;
        // Play move animation
        grid1.MoveTo(grid2.gameObject.transform.localPosition);
        grid2.MoveTo(grid1.gameObject.transform.localPosition);
        yield return new WaitForSeconds(0.3f);
        // Remove grid
        int x1 = (int)grid1.MapPosition.x;
        int y1 = (int)grid1.MapPosition.y;
        int x2 = (int)grid2.MapPosition.x;
        int y2 = (int)grid2.MapPosition.y;
        Vector3 midPos = (grid1.transform.position + grid2.transform.position) * 0.5f;
        switch (grid2.Type)
        {
            case Grid.GridType.Horizontal:
            case Grid.GridType.Vertical:
                {
                    grid1.MoveToAndDestory(midPos, 0.2f);
                    grid2.MoveToAndDestory(midPos, 0.2f);
                    _gridsMap[x1, y1] = null;
                    _gridsMap[x2, y2] = null;
                    yield return new WaitForSeconds(0.2f);
                    yield return StartCoroutine(RemoveCrossGrids(x2, y2));
                    break;
                }
            case Grid.GridType.Bomb:
                {
                    grid1.MoveToAndDestory(midPos, 0.2f);
                    grid2.MoveToAndDestory(midPos, 0.2f);
                    _gridsMap[x1, y1] = null;
                    _gridsMap[x2, y2] = null;
                    yield return new WaitForSeconds(0.2f);
                    Grid gridCopy = InstantiateSpecialGrid(x2, y2, grid1.Color, grid1.Type);
                    gridCopy.transform.position = midPos;
                    gridCopy.ScaleToAndRemove(grid1.transform.localScale.x * 3, grid1.transform.localScale.y * 3, 0.3f);
                    yield return new WaitForSeconds(0.3f);
                    yield return StartCoroutine(RemoveThreeHorizantalGrids(x2, y2));
                    break;
                }
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveHorizantalWithSpeical");
    }

    IEnumerator RemoveVerticalWithSpeical(Grid grid1, Grid grid2)
    {
        //print("In RemoveVerticalWithSpeical");
        GameManager.Instance.CoroutineCount++;
        // Play move animation
        grid1.MoveTo(grid2.gameObject.transform.localPosition);
        grid2.MoveTo(grid1.gameObject.transform.localPosition);
        yield return new WaitForSeconds(0.3f);
        // Remove grid
        int x1 = (int)grid1.MapPosition.x;
        int y1 = (int)grid1.MapPosition.y;
        int x2 = (int)grid2.MapPosition.x;
        int y2 = (int)grid2.MapPosition.y;
        Vector3 midPos = (grid1.transform.position + grid2.transform.position) * 0.5f;
        switch (grid2.Type)
        {
            case Grid.GridType.Horizontal:
            case Grid.GridType.Vertical:
                {
                    grid1.MoveToAndDestory(midPos, 0.2f);
                    grid2.MoveToAndDestory(midPos, 0.2f);
                    _gridsMap[x1, y1] = null;
                    _gridsMap[x2, y2] = null;
                    yield return new WaitForSeconds(0.2f);
                    yield return StartCoroutine(RemoveCrossGrids(x2, y2));
                    break;
                }
            case Grid.GridType.Bomb:
                {
                    grid1.MoveToAndDestory(midPos, 0.2f);
                    grid2.MoveToAndDestory(midPos, 0.2f);
                    _gridsMap[x1, y1] = null;
                    _gridsMap[x2, y2] = null;
                    yield return new WaitForSeconds(0.2f);
                    Grid gridCopy = InstantiateSpecialGrid(x2, y2, grid1.Color, grid1.Type);
                    gridCopy.transform.position = midPos;
                    gridCopy.ScaleToAndRemove(grid1.transform.localScale.x * 3, grid1.transform.localScale.y * 3, 0.3f);
                    yield return new WaitForSeconds(0.3f);
                    yield return StartCoroutine(RemoveThreeVerticalGrids(x2, y2));
                    break;
                }
        }
        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveVerticalWithSpeical");
    }

    IEnumerator RemoveTwoBombGrids(Grid grid1, Grid grid2)
    {
        //print("In RemoveTwoBombGrids");
        GameManager.Instance.CoroutineCount++;
        // Play move animation
        grid1.MoveTo(grid2.gameObject.transform.localPosition);
        grid2.MoveTo(grid1.gameObject.transform.localPosition);
        yield return new WaitForSeconds(0.3f);
        // Remove grids
        int x1 = (int)grid1.MapPosition.x;
        int y1 = (int)grid1.MapPosition.y;
        int x2 = (int)grid2.MapPosition.x;
        int y2 = (int)grid2.MapPosition.y;
        Vector3 midPos = (grid1.transform.position + grid2.transform.position) * 0.5f;
        grid1.MoveToAndDestory(midPos, 0.2f);
        grid2.MoveToAndDestory(midPos, 0.2f);
        _gridsMap[x1, y1] = null;
        _gridsMap[x2, y2] = null;
        yield return new WaitForSeconds(0.2f);
        Grid gridCopy = InstantiateSpecialGrid(x2, y2, grid1.Color, grid1.Type);
        gridCopy.transform.position = midPos;
        gridCopy.ScaleToAndRemove(grid1.transform.localScale.x * 3, grid1.transform.localScale.y * 3, 0.3f);
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(RemoveBombGrids(x2, y2, 2));

        GameManager.Instance.CoroutineCount--;
        //print("Out RemoveTwoBombGrids");
    }

    // Chain check functions
    int CheckAllChains()
    {
        int maxChainCount = 0;
        for (int x = 0; x < _colNumber; x++)
        {
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid.IsChainGrid())
                {
                    int chainCount = CheckChain(grid);
                    if (chainCount >= 3)
                        MarkChain(grid);
                    if (chainCount > maxChainCount)
                        maxChainCount = chainCount;
                }
            }
        }
        // mark special grid
        foreach (Grid grid in MovedGridsList)
            MarkSpeicalGrid(grid);
        MovedGridsList.Clear();

        return maxChainCount;
    }

    void MarkChain(Grid grid)
    {
        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        Grid.GridColor color = grid.Color;
        // Check horizontal
        if (!grid.IsChainH && x + 2 < _colNumber)
        {
            Grid grid1 = _gridsMap[++x, y];
            Grid grid2 = _gridsMap[++x, y];
            if (grid1.Color == color && grid2.Color == color)
            {
                grid.IsChainH = true;
                grid1.IsChainH = true;
                grid2.IsChainH = true;
                while (++x < _colNumber)
                {
                    grid2 = _gridsMap[x, y];
                    if (grid2.Color == color)
                        grid2.IsChainH = true;
                    else
                        break;
                }
            }
        }
        // Check vertical
        if (!grid.IsChainV && y + 2 < _rowNumber)
        {
            x = (int) grid.MapPosition.x;
            Grid grid1 = _gridsMap[x, ++y];
            Grid grid2 = _gridsMap[x, ++y];
            if (grid1.Color == color && grid2.Color == color)
            {
                grid.IsChainV = true;
                grid1.IsChainV = true;
                grid2.IsChainV = true;
                while (++y < _rowNumber)
                {
                    grid2 = _gridsMap[x, y];
                    if (grid2.Color == color)
                        grid2.IsChainV = true;
                    else
                        break;
                }
            }
        }
    }

    public int CheckChain(Grid grid)
    {
        int chainCountH = 1;
        int chainCountV = 1;
        int chainCount = 1;
        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        Grid.GridColor color = grid.Color;
        // Check horizontal
        int nextX = x;
        while (--nextX >= 0)
            if (_gridsMap[nextX, y] && _gridsMap[nextX, y].Color == color)
                chainCountH++;
            else
                break;
        nextX = x;
        while (++nextX < _colNumber)
            if (_gridsMap[nextX, y] && _gridsMap[nextX, y].Color == color)
                chainCountH++;
            else
                break;
        if (chainCountH >= 3)
            chainCount += chainCountH - 1;

        // Check vertical
        int nextY = y;
        while (--nextY >= 0)
            if (_gridsMap[x, nextY] && _gridsMap[x, nextY].Color == color)
                chainCountV++;
            else
                break;
        nextY = y;
        while (++nextY < _rowNumber)
            if (_gridsMap[x, nextY] && _gridsMap[x, nextY].Color == color)
                chainCountV++;
            else
                break;
        if (chainCountV >= 3)
            chainCount += chainCountV - 1;

        return chainCount;
    }

    int CheckPossibleChain(Grid grid, int originalX, int originalY, Grid.GridColor color)
    {
        int chainCountH = 1;
        int chainCountV = 1;
        int chainCount = 1;
        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        // Check horizontal
        int nextX = x;
        while (--nextX >= 0 && nextX != originalX)
            if (_gridsMap[nextX, y].Color == color)
                chainCountH++;
            else
                break;
        nextX = x;
        while (++nextX < _colNumber && nextX != originalX)
            if (_gridsMap[nextX, y].Color == color)
                chainCountH++;
            else
                break;
        if (chainCountH >= 3)
            chainCount += chainCountH - 1;
        // Check vertical
        int nextY = y;
        while (--nextY >= 0 && nextY != originalY)
            if (_gridsMap[x, nextY].Color == color)
                chainCountV++;
            else
                break;
        nextY = y;
        while (++nextY < _rowNumber && nextY != originalY)
            if (_gridsMap[x, nextY].Color == color)
                chainCountV++;
            else
                break;
        if (chainCountV >= 3)
            chainCount += chainCountV - 1;

        return chainCount;
    }

    Grid CheckAllPossibleChains()
    {
        int maxChainCount = 0;
        Grid maxPossibleGrid = null;
        for (int x = 0; x < _colNumber; x++)
        {
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid.IsChainGrid())
                {
                    int chainCount = CheckFourPossibleChain(grid);
                    if (chainCount >= 3 &&
                        ((chainCount == maxChainCount && Random.value >= 0.5f) || chainCount > maxChainCount)) // 50% probablity change to new grid if chainCount is same
                        {
                            maxChainCount = chainCount;
                            maxPossibleGrid = grid;
                        }
                }
            }
        }

        return maxPossibleGrid;
    }

    int CheckFourPossibleChain(Grid grid)
    {
        int maxChainCount = 0;
        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        Grid.GridColor color = grid.Color;
        // up
        if (y + 1 < _rowNumber)
        {
            Grid tempGrid = _gridsMap[x, y + 1];
            if (tempGrid.IsChainGrid())
                maxChainCount = Mathf.Max(CheckPossibleChain(tempGrid, x, y, color), maxChainCount);
        }
        // down
        if (y - 1 >= 0)
        {
            Grid tempGrid = _gridsMap[x, y - 1];
            if (tempGrid.IsChainGrid())
                maxChainCount = Mathf.Max(CheckPossibleChain(tempGrid, x, y, color), maxChainCount);
        }
        // left
        if (x + 1 < _colNumber)
        {
            Grid tempGrid = _gridsMap[x + 1, y];
            if (tempGrid.IsChainGrid())
                maxChainCount = Mathf.Max(CheckPossibleChain(tempGrid, x, y, color), maxChainCount);
        }
        // right
        if (x - 1 >= 0)
        {
            Grid tempGrid = _gridsMap[x - 1, y];
            if (tempGrid.IsChainGrid())
                maxChainCount = Mathf.Max(CheckPossibleChain(tempGrid, x, y, color), maxChainCount);
        }
        return maxChainCount;
    }

    void MarkPossibleChain(Grid grid)
    {
        int maxChainCount = 0;
        int chainCount;
        Grid maxPossibleTempGrid = null;
        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        Grid.GridColor color = grid.Color;
        // up
        if (y + 1 < _rowNumber)
        {
            Grid tempGrid = _gridsMap[x, y + 1];
            if (tempGrid.IsChainGrid())
            {
                chainCount = CheckPossibleChain(tempGrid, x, y, color);
                if (chainCount >= 3 && chainCount > maxChainCount)
                {
                    maxChainCount = chainCount;
                    maxPossibleTempGrid = tempGrid;
                }
            }
        }
        // down
        if (y - 1 >= 0)
        {
            Grid tempGrid = _gridsMap[x, y - 1];
            if (tempGrid.IsChainGrid())
            {
                chainCount = CheckPossibleChain(tempGrid, x, y, color);
                if (chainCount >= 3 && chainCount > maxChainCount)
                {
                    maxChainCount = chainCount;
                    maxPossibleTempGrid = tempGrid;
                }
            }
        }
        // left
        if (x + 1 < _colNumber)
        {
            Grid tempGrid = _gridsMap[x + 1, y];
            if (tempGrid.IsChainGrid())
            {
                chainCount = CheckPossibleChain(tempGrid, x, y, color);
                if (chainCount >= 3 && chainCount > maxChainCount)
                {
                    maxChainCount = chainCount;
                    maxPossibleTempGrid = tempGrid;
                }
            }
        }
        // right
        if (x - 1 >= 0)
        {
            Grid tempGrid = _gridsMap[x - 1, y];
            if (tempGrid.IsChainGrid())
            {
                chainCount = CheckPossibleChain(tempGrid, x, y, color);
                if (chainCount >= 3 && chainCount > maxChainCount)
                {
                    maxChainCount = chainCount;
                    maxPossibleTempGrid = tempGrid;
                }
            }
        }

        _possibleChainGrids.Clear();
        _possibleChainGrids.Add(grid);
        AddPossibleChain(maxPossibleTempGrid, x, y, color);
    }

    void AddPossibleChain(Grid grid, int originalX, int originalY, Grid.GridColor color)
    {
        if (grid == null)
            return;

        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        // Check horizontal
        int chainCount = 1;
        int nextX = x;
        while (--nextX >= 0 && nextX != originalX)
            if (_gridsMap[nextX, y].Color == color)
                chainCount++;
            else
                break;
        nextX = x;
        while (++nextX < _colNumber && nextX != originalX)
            if (_gridsMap[nextX, y].Color == color)
                chainCount++;
            else
                break;
        if (chainCount >= 3)
        {
            nextX = x;
            while (--nextX >= 0 && nextX != originalX)
            {
                Grid grid1 = _gridsMap[nextX, y];
                if (grid1.Color == color)
                    _possibleChainGrids.Add(grid1);
                else
                    break;
            }
            nextX = x;
            while (++nextX < _colNumber && nextX != originalX)
            {
                Grid grid1 = _gridsMap[nextX, y];
                if (grid1.Color == color)
                    _possibleChainGrids.Add(grid1);
                else
                    break;
            }
        }
        // Check vertical
        chainCount = 1;
        int nextY = y;
        while (--nextY >= 0 && nextY != originalY)
            if (_gridsMap[x, nextY].Color == color)
                chainCount++;
            else
                break;
        nextY = y;
        while (++nextY < _rowNumber && nextY != originalY)
            if (_gridsMap[x, nextY].Color == color)
                chainCount++;
            else
                break;
        if (chainCount >= 3)
        {
            nextY = y;
            while (--nextY >= 0 && nextY != originalY)
            {
                Grid grid1 = _gridsMap[x, nextY];
                if (grid1.Color == color)
                    _possibleChainGrids.Add(grid1);
                else
                    break;
            }
            nextY = y;
            while (++nextY < _rowNumber && nextY != originalY)
            {
                Grid grid1 = _gridsMap[x, nextY];
                if (grid1.Color == color)
                    _possibleChainGrids.Add(grid1);
                else
                    break;
            }
        }
    }

    void MarkAllPossibleSpeicalGrids()
    {
        _possibleChainGrids.Clear();
        for (int x = 0; x < _colNumber; x++)
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid.IsSpecialGrid())
                {
                    if (grid.Type == Grid.GridType.Magic)
                    {
                        _possibleChainGrids.Add(grid);
                        return;
                    }
                    // Find another special grid around
                    for (int newX = Mathf.Max(x - 1, 0); newX <= Mathf.Min(x + 1, _colNumber - 1); newX++)
                    {
                        if (newX == x)
                            continue;
                        Grid grid1 = _gridsMap[newX, y];
                        if (grid1 && grid1.IsSpecialGrid())
                        {
                            _possibleChainGrids.Add(grid);
                            _possibleChainGrids.Add(grid1);
                            return;
                        }
                    }
                    for (int newY = Mathf.Max(y - 1, 0); newY <= Mathf.Min(y + 1, _rowNumber - 1); newY++)
                    {
                        if (newY == y)
                            continue;
                        Grid grid1 = _gridsMap[x, newY];
                        if (grid1 && grid1.IsSpecialGrid())
                        {
                            _possibleChainGrids.Add(grid);
                            _possibleChainGrids.Add(grid1);
                            return;
                        }
                    }
                }
            }
    }

    void MarkSpeicalGrid(Grid grid)
    {
        int x = (int)grid.MapPosition.x;
        int y = (int)grid.MapPosition.y;
        // Check horizontal
        int chainCountH = 1;
        Grid.GridColor color = grid.Color;
        if (grid.IsChainH)
        {
            while (--x >= 0)
            {
                Grid grid1 = _gridsMap[x, y];
                if (grid1.NeedToBeType != Grid.GridType.Normal)
                    return;
                if (grid1.IsChainH && grid1.Color == color)
                    chainCountH++;
                else
                    break;
            }
            x = (int)grid.MapPosition.x;
            while (++x < _colNumber)
            {
                Grid grid1 = _gridsMap[x, y];
                if (grid1.NeedToBeType != Grid.GridType.Normal)
                    return;
                if (grid1.IsChainH && grid1.Color == color)
                    chainCountH++;
                else
                    break;
            }
        }

        x = (int)grid.MapPosition.x;
        // Check vertical
        int chainCountV = 1;
        if (grid.IsChainV)
        {
            while (--y >= 0)
            {
                Grid grid1 = _gridsMap[x, y];
                if (grid1.NeedToBeType != Grid.GridType.Normal)
                    return;
                if (grid1.IsChainV && grid1.Color == color)
                    chainCountV++;
                else
                    break;
            }
            y = (int)grid.MapPosition.y;
            while (++y < _rowNumber)
            {
                Grid grid1 = _gridsMap[x, y];
                if (grid1.NeedToBeType != Grid.GridType.Normal)
                    return;
                if (grid1.IsChainV && grid1.Color == color)
                    chainCountV++;
                else
                    break;
            }
        }

        // mark as special grid
        if (chainCountH >= 3 && chainCountV >= 3)
        {
            if (chainCountH + chainCountV == 6)
                grid.NeedToBeType = Grid.GridType.Bomb;
            else if (chainCountH + chainCountV == 7)
                grid.NeedToBeType = Grid.GridType.Bomb;
            else if (chainCountH + chainCountV == 8)
                grid.NeedToBeType = Grid.GridType.Magic;
        }
        else if (chainCountH == 4 && chainCountV < 3)
            grid.NeedToBeType = Grid.GridType.Vertical;
        else if (chainCountV == 4 && chainCountH < 3)
            grid.NeedToBeType = Grid.GridType.Horizontal;
        else if (chainCountH == 5 || chainCountV == 5)
            grid.NeedToBeType = Grid.GridType.Magic;
    }

    public void StopHintGlowing()
    {
        IsHintGlowing = false;
        foreach (Grid grid in _possibleChainGrids)
            grid.StopGlow();
    }

    // Battle assistance functions
    public GameObject GetNearestTargetInRange(int x, int y, int range, Grid.GridType type)
    {
        for (int i = 1; i <= range; i++)
        {
            int xMin = x - i;
            int xMax = x + i;
            int yMin = y - i;
            int yMax = y + i;
            int xMinClamp = Mathf.Max(xMin, 0);
            int xMaxClamp = Mathf.Min(xMax, _colNumber - 1);
            int yMinClamp = Mathf.Max(yMin, 0);
            int yMaxClamp = Mathf.Min(yMax, _rowNumber - 1);
            // Up
            if (yMax < _rowNumber)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                {
                    Grid grid = _gridsMap[j, yMax];
                    if (grid && grid.Type == type)
                        return grid.gameObject;
                }
            // Right
            if (xMax < _colNumber)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                {
                    Grid grid = _gridsMap[xMax, j];
                    if (grid && grid.Type == type)
                        return grid.gameObject;
                }
            // Down
            if (yMin >= 0)
                for (int j = xMinClamp; j <= xMaxClamp; j++)
                {
                    Grid grid = _gridsMap[j, yMin];
                    if (grid && grid.Type == type)
                        return grid.gameObject;
                }
            // Left
            if (xMin >= 0)
                for (int j = yMinClamp; j <= yMaxClamp; j++)
                {
                    Grid grid = _gridsMap[xMin, j];
                    if (grid && grid.Type == type)
                        return grid.gameObject;
                }
        }
        return null;
    }

    public void RemoveEnemyGrid(int x, int y)
    {
        _gridsMap[x, y] = null;
    }

    public void DropAllGridsDownAndCreateEnemy(int x)
    {
        int needFillRow = 0;
        int needFillRowWithUnMovable = 0;
        int filledRow = 0;
        // drop all Grids in this column
        for (int y = 0; y < _rowNumber; y++)
        {
            Grid grid = _gridsMap[x, y];
            if (grid == null)
                needFillRow++;
            else if (!grid.IsMovable) // Jump un-movable grid
            {
                needFillRowWithUnMovable = needFillRow;
                if (needFillRowWithUnMovable > 0)
                    needFillRow++;
            }
            else
            {
                if (needFillRow > 0)
                {
                    if (filledRow > 0 && filledRow == needFillRowWithUnMovable)
                        needFillRow--;

                    int newY = y - needFillRow;
                    _gridsMap[x, newY] = grid;
                    _gridsMap[x, y] = null;
                    grid.MapPosition = new Vector2(x, newY);
                    Vector2 newPosition = GetPosition(x, newY);
                    grid.DropTo(newPosition, DropDirection.Down);
                    grid.IsChainH = false;
                    grid.IsChainV = false;

                    if (needFillRowWithUnMovable > 0)
                        filledRow++;
                }
            }
        }
        // create new Grids and drop
        for (int y = _rowNumber - needFillRow; y < _rowNumber; y++)
        {
            Vector2 destination = GetPosition(x, y);
            Vector2 position = destination + Vector2.up * needFillRow * _gridHeight;
            _gridsMap[x, y] = InstantiateEnemyGrid(x, y, position, destination, DropDirection.Down);
        }
    }

    public void SetAllGridsColor(Color color)
    {
        if (_gridSpriteColor == color)
            return;
        _gridSpriteColor = color;
        for (int x = 0; x < _colNumber; x++)
        {
            for (int y = 0; y < _rowNumber; y++)
            {
                Grid grid = _gridsMap[x, y];
                if (grid == null)
                    break;
                if (grid.IsChainGrid())
                    grid.SetColor(_gridSpriteColor);
            }
        }
    }

    // Assist functions
    Vector2 GetPosition(int x, int y)
    {
        return new Vector2((x - _colNumberHalf) *_gridWidth, (y - _rowNumberHalf) *_gridHeight);
    }

    void RemoveGridForSpecialGrid(int x, int y)
    {
        if (x >= 0 && x < _colNumber && y >= 0 && y < _rowNumber)
        {
            Grid grid = _gridsMap[x, y];
            if (grid && grid.IsChainGrid() && !grid.IsRemoving && 
                grid.NeedToBeType == Grid.GridType.Normal) // If this grid is need to change to special grid then don't remove it by effect
            {
                grid.RemoveAndFlyTo(PlayerManager.Instance.GetPlayerControl().GetTarget());
                // Process special grid
                if (grid.IsSpecialGrid())
                    StartCoroutine(RemoveSpecialGrid(grid));
                _gridsMap[x, y] = null;
            }
        }
    }

    void RemoveGridForSpecialGrid(Grid grid)
    {
        if (grid && grid.IsChainGrid() && !grid.IsRemoving &&
            grid.NeedToBeType == Grid.GridType.Normal) // If this grid is need to change to special grid then don't remove it by effect
        {
            int x = (int)grid.MapPosition.x;
            int y = (int)grid.MapPosition.y;
            grid.RemoveAndFlyTo(PlayerManager.Instance.GetPlayerControl().GetTarget());
            // Process special grid
            if (grid.IsSpecialGrid())
                StartCoroutine(RemoveSpecialGrid(grid));
            _gridsMap[x, y] = null;
        }
    }
}
