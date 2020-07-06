using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
	private readonly static Queue<Action> _executionQueue = new Queue<Action> ();

	public void Update ()
	{
		while (_executionQueue.Count > 0) {
			Action nextAction = _executionQueue.Dequeue ();
			if (nextAction != null) {
				nextAction.Invoke ();
			}
		}
	}

	public void Enqueue (IEnumerator action)
	{
		lock (_executionQueue) {
			_executionQueue.Enqueue (() => {
				StartCoroutine (action);
			});
		}
	}

	private static UnityMainThreadDispatcher _instance = null;

	public static bool Exists ()
	{
		return _instance != null;
	}

	public static UnityMainThreadDispatcher Instance ()
	{
		if (!Exists ()) {
			throw new Exception ("UnityMainThreadDispacer could not find the UnityMainThreadDispacher object. Please ensure you have added the mainthreadexcutor prefab to your scene. ");
		}
		return _instance;
	}

	void Awake ()
	{
		if (_instance == null) {
			_instance = this;
			DontDestroyOnLoad (this.gameObject);
		}
	}

	void OnDestroy ()
	{
		_instance = null;
	}
}
