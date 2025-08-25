using System.Collections.Generic;
using UnityEngine;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager instance;

    private HashSet<string> destroyedResourceIds = new HashSet<string>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep this manager across scene loads
        }
    }

    public void AddDestroyedResource(string id)
    {
        if (!destroyedResourceIds.Contains(id))
        {
            destroyedResourceIds.Add(id);
        }
    }

    public bool IsResourceDestroyed(string id)
    {
        return destroyedResourceIds.Contains(id);
    }
}
