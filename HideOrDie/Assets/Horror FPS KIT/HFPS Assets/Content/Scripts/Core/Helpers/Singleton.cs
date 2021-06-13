using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static bool IsInitialized
    {
        get
        {
            return instance != null;
        }
    }

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                MonoBehaviour[] instances = FindObjectsOfType<T>();

                if (instances.Length > 0)
                {
                    instance = (T)instances[0];
                }

                if (instances.Length > 1)
                {
                    Debug.LogError("[Singleton] Something went really wrong - there should never be more than 1 singleton!");
                    return instance;
                }

                if (instance == null)
                {
                    //Debug.LogError("[Singleton] Something went really wrong - specified Singleton does not found!");
                    return default;
                }
            }

            return instance;
        }
    }
}