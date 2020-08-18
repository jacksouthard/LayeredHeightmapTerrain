using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T> {
    public static T instance {
        get {
            if (cachedInstance == null) {
                cachedInstance = FindObjectOfType<T>();
            }

            return cachedInstance;
        }
    }

    static T cachedInstance;
}
