using UnityEngine;
using System.Collections;

public class AttackFlyItem : MonoBehaviour
{
    public GameObject Source;
    public GameObject Target;
    public float FlySpeed = 0.4f;

    private Vector3 _direction;
	// Use this for initialization
    void Awake()
    {
        //GameManager.Instance.AnimationCount++;
    }
	void Start ()
	{
	    _direction = (Target.transform.position - transform.position).normalized;
        float towardAngle = -Mathf.Rad2Deg *
                                    Mathf.Atan((transform.position.x - Target.transform.position.x) /
                                               (transform.position.y - Target.transform.position.y));
        if (transform.position.y - Target.transform.position.y < 0)
            towardAngle += 180;
        transform.eulerAngles = new Vector3(0, 0, towardAngle);
	}
	
	// Update is called once per frame
	void Update ()
	{
	    FlySpeed += Time.deltaTime * 5;
        transform.Translate(FlySpeed * _direction, Space.World);
    }

    // OnTriggerEnter2D is called whenever this object overlaps woth a trigger collider
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Enemy") && !transform.parent.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyControl>().BeAttacked();
            Destroy(gameObject);
            //GameManager.Instance.AnimationCount--;
        }
        else if (other.gameObject.CompareTag("Player") && !transform.parent.CompareTag("Player"))
        {
            other.GetComponent<PlayerControl>().BeAttacked(Source, _direction);
            Destroy(gameObject);
            //GameManager.Instance.AnimationCount--;
        }
    }

}
