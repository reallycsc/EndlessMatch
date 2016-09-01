using UnityEngine;
using System.Collections;

[System.Serializable]

public class Bezier : System.Object
{

    public Vector3 P0;
    public Vector3 P1;
    public Vector3 P2;
    public Vector3 P3;

    public float Ti = 0f;

    private Vector3 _b0 = Vector3.zero;
    private Vector3 _b1 = Vector3.zero;
    private Vector3 _b2 = Vector3.zero;
    private Vector3 _b3 = Vector3.zero;

    private float _ax;
    private float _ay;
    private float _az;

    private float _bx;
    private float _by;
    private float _bz;

    private float _cx;
    private float _cy;
    private float _cz;

    // Init function v0 = 1st point, v1 = handle of the 1st point , v2 = handle of the 2nd point, v3 = 2nd point
    public Bezier(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        P0 = v0;
        P1 = v1;
        P2 = v2;
        P3 = v3;
    }

    // 0.0 >= t <= 1.0
    public Vector3 GetPointAtTime(float t)
    {
        CheckConstant();

        float t2 = t * t;
        float t3 = t * t * t;
        float x = _ax * t3 + _bx * t2 + _cx * t + P0.x;
        float y = _ay * t3 + _by * t2 + _cy * t + P0.y;
        float z = _az * t3 + _bz * t2 + _cz * t + P0.z;

        return new Vector3(x, y, z);
    }

    private void SetConstant()
    {
        _cx = 3f * (P1.x - P0.x);
        _bx = 3f * (P2.x - P1.x) - _cx;
        _ax = P3.x - P0.x - _cx - _bx;
        _cy = 3f * (P1.y - P0.y);
        _by = 3f * (P2.y - P1.y) - _cy;
        _ay = P3.y - P0.y - _cy - _by;
        _cz = 3f * (P1.z - P0.z);
        _bz = 3f * (P2.z - P1.z) - _cz;
        _az = P3.z - P0.z - _cz - _bz;
    }

    // Check if P0, P1, P2 or P3 have changed
    private void CheckConstant()
    {
        if (P0 != _b0 || P1 != _b1 || P2 != _b2 || P3 != _b3)
        {
            SetConstant();

            _b0 = P0;
            _b1 = P1;
            _b2 = P2;
            _b3 = P3;
        }
    }
}
