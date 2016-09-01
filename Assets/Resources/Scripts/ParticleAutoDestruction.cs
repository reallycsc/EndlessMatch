using UnityEngine;
using System.Collections;

public class ParticleAutoDestruction : MonoBehaviour
{
    private ParticleSystem[] _particleSystems;

	// Use this for initialization
	void Start ()
	{
	    _particleSystems = GetComponentsInChildren<ParticleSystem>();
	}
	
	// Update is called once per frame
	void Update ()
	{
	    bool isAllStopped = true;

	    foreach (var ps in _particleSystems)
	        if (!ps.isStopped)
	            isAllStopped = false;

	    if (isAllStopped)
	        Destroy(gameObject);
	}
}
