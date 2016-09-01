using UnityEngine;
using System.Collections;

public class BezierLine : MonoBehaviour
{
    public GameObject StartObject = null;
    public GameObject EndObject = null;
    public float CurveRatio = 2;

    private LineRenderer _lineRenderer;
    private Vector3 _startObjPos;
    private Vector3 _endObjPos;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.SetVertexCount(100);
        _lineRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (StartObject && EndObject)
            if (_startObjPos != StartObject.transform.position || _endObjPos != EndObject.transform.position)
                DrawLine();
    }

    public void DrawLine()
    {
        _startObjPos = StartObject.transform.position;
        _endObjPos = EndObject.transform.position;
        Vector3 start = new Vector3(_startObjPos.x, _startObjPos.y, 0);
        Vector3 end = new Vector3(_endObjPos.x, _endObjPos.y, 0);
        Vector3 sub = end - start;
        float curveParam = CurveRatio / sub.magnitude;
        Vector3 mid1 = start + sub * 0.25f + Vector3.Cross(sub, Vector3.forward) * curveParam;
        Vector3 mid2 = start + sub * 0.75f + Vector3.Cross(sub, Vector3.forward) * curveParam;

        Bezier myBezier = new Bezier(start, mid1, mid2, end);
        for (int i = 1; i <= 100; i++)
        {
            Vector3 vec = myBezier.GetPointAtTime((float)(i * 0.01));
            _lineRenderer.SetPosition(i - 1, vec);
        }
    }

    public void HideLine()
    {
        _lineRenderer.enabled = false;
    }

    public void ShowLine()
    {
        _lineRenderer.enabled = true;
    }
}
