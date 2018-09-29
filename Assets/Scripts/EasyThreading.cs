using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyThreading : MonoBehaviour {
    public static EasyThreading Instance;

    private Queue<Action> Tasks = new Queue<Action>();

    public static void Dispatch(Action f) {
        Instance.InternalDispatch(f);
    }

    public static void EnsureInstance() {
        if (Instance == null) {
            GameObject go = new GameObject("EasyThreading");
            go.AddComponent<EasyThreading>();
        }
    }

    public void Awake() {
        Instance = this;
    }

    public void InternalDispatch(Action f) {
        lock (Tasks) {
            Tasks.Enqueue(f);
        }
    }
	
	public void Update () {
		lock (Tasks) {
            while (Tasks.Count > 0) {
                var f = Tasks.Dequeue();
                f();
            }
        }
	}
}
