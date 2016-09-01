using UnityEngine;
using System.Collections;

public class MagicEffect : MonoBehaviour
{
    private float _length;

	// Use this for initialization
	void Awake ()
	{
	    _length = GetComponent<SpriteRenderer>().bounds.size.y;
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnAnimationDone()
    {
        Destroy(transform.parent.gameObject);
    }

    public void SetDestination(Vector3 destination)
    {
        Vector3 direction = destination - transform.parent.transform.position;
        float scaleY = direction.magnitude/_length*1.1f;
        transform.parent.localScale = new Vector3(1, scaleY, 1);
        float angle = Vector3.Angle(transform.up, direction);
        if (direction.x > 0)
            angle = -angle;
        transform.parent.eulerAngles = new Vector3(0, 0, angle);
    }
}
