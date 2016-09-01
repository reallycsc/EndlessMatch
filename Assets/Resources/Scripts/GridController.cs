using UnityEngine;
using System.Collections;

public class GridController : MonoBehaviour {
    private GameObject _touchedObject;
    private GridManager _gridManager;
    private GameManager _gameManager;
    
    // Use this for initialization
    void Start ()
    {
        _gridManager = GridManager.Instance;
        _gameManager = GameManager.Instance;
    }
	
	// Update is called once per frame
	void Update ()
	{
	    if (_gameManager.CoroutineCount > 0)
	        return;
        if (_gameManager.GetGameStage() != GameManager.GameStage.SwapStage)
	        return;
	    if (_gameManager.AnimationCount > 0)
	        return;

        GridTouchProcess();
    }

    void GridTouchProcess()
    {
        if (Input.GetMouseButtonDown(0)) // Get 1st touched object
        {
            if (_touchedObject == null)
                _touchedObject = GetTouchedObject();

            if (_touchedObject != null)
            {
                if (_touchedObject.CompareTag("Grid"))
                    _touchedObject.GetComponent<Grid>().SetColor(Color.white);
                else
                    _touchedObject = null;
            }
        }
        else if (Input.GetMouseButton(0)) // Get second touched object
        {
            do
            {
                if (_touchedObject == null)
                    break;

                GameObject swapObject = GetSwapObject();
                if (swapObject == null || !swapObject.CompareTag("Grid"))
                    break;

                Grid grid1 = _touchedObject.GetComponent<Grid>();
                Grid grid2 = swapObject.GetComponent<Grid>();
                // Check if is special grid
                if (_gridManager.SpecialGridProcess(grid1, grid2))
                    break;
                if (_gridManager.SpecialGridProcess(grid2, grid1))
                    break;

                _gridManager.SwapGrids(grid1, grid2);
                // Check if any 3 chain
                if (_gridManager.CheckChain(grid1) >= 1 || _gridManager.CheckChain(grid2) >= 1)
                {
                    // Play move animation
                    grid1.MoveTo(swapObject.transform.localPosition);
                    grid2.MoveTo(_touchedObject.transform.localPosition);
                    _gridManager.IsSwapDone = true;
                    _gridManager.IsCheckNeeded = true;
                    _gridManager.StopHintGlowing();
                }
                else // swap back
                {
                    _gridManager.SwapGrids(grid1, grid2);
                    grid1.MoveToAndMoveBack(swapObject.transform.localPosition);
                    grid2.MoveToAndMoveBack(_touchedObject.transform.localPosition);
                }

                _touchedObject.GetComponent<Grid>().SetColor(_gridManager.DefaultGridSpriteColor);
                _touchedObject = null;

            } while (false);
        }
        else if (Input.GetMouseButtonUp(0)) // reset
        {
            if (_touchedObject != null)
            {
                _touchedObject.GetComponent<Grid>().SetColor(_gridManager.DefaultGridSpriteColor);
                _touchedObject = null;
            }
        }
    }

    // assist functions
    GameObject GetTouchedObject()
    {
        Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D overlapPoint = Physics2D.OverlapPoint(touchPos);
        if (overlapPoint != null)
            return overlapPoint.gameObject;
        return null;
    }

    GameObject GetSwapObject()
    {
        Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 distance = touchPos - (Vector2)_touchedObject.transform.position;
        Vector2 gridSizeHalf = _touchedObject.GetComponentInChildren<SpriteRenderer>().bounds.size / 2;
        float absDistanceX = Mathf.Abs(distance.x);
        float absDistanceY = Mathf.Abs(distance.y);
        if (absDistanceX > gridSizeHalf.x || absDistanceY > gridSizeHalf.y)
        {
            Grid touchedGrid = _touchedObject.GetComponent<Grid>();
            int x = (int)touchedGrid.MapPosition.x;
            int y = (int)touchedGrid.MapPosition.y;
            if (absDistanceX < absDistanceY)
                if (distance.y >= 0) // Up
                {
                    _gridManager.DropDir = GridManager.DropDirection.Up;
                    y++;
                }
                else // Down
                {
                    _gridManager.DropDir = GridManager.DropDirection.Down;
                    y--;
                }
            else if (distance.x >= 0) // Right
            {
                _gridManager.DropDir = GridManager.DropDirection.Right;
                x++;
            }
            else // Left
            {
                _gridManager.DropDir = GridManager.DropDirection.Left;
                x--;
            }

            return _gridManager.GetGridObject(x, y);
        }
        return null;
    }

    public void ResetTouchObject()
    {
        _touchedObject = null;
    }
}

