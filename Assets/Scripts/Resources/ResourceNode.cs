// File: Assets/Scripts/Resources/ResourceNode.cs
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class ResourceNode : MonoBehaviour, IDamageable
{
    // Make this field private, it should only be set through Initialize
    private ResourceNodeData resourceData;

    public GameObject dropParent;

    [Header("Item Drop Prefab")]
    [Tooltip("Assign the generic prefab for dropped items here.")]
    public GameObject dropItemPrefab;

    public string uniqueId; // To identify this specific node

    protected FallingBox fallingBox;

    private CapsuleCollider2D cap;
    private BoxCollider2D box;

    [SerializeField]  private float currentHealth;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        fallingBox = GetComponent<FallingBox>();  
        cap = GetComponent<CapsuleCollider2D>();
        box = GetComponent<BoxCollider2D>();

        // Automatically find the container for dropped items
        if (dropParent == null)
        {
            dropParent = GameObject.Find("Recources");
        }
    }

    private void Start()
    {
        
        if (resourceData.isTriggerable)
        {
            cap.enabled = false;
            box.isTrigger = true;
        }
        else
        {

        }
    }

    public void Initialize(ResourceNodeData data)
    {
        resourceData = data;
        
        // Setup logic moved here
        currentHealth = resourceData.maxHealth;
        sr.sprite = resourceData.sprite;
        gameObject.name = resourceData.resourceName;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }

    public void ChangeStatus()
    {
        fallingBox.ChangeStatus();
    }

    private void OnDestroyed()
    {
        // Register this node as destroyed before removing it
        if (!string.IsNullOrEmpty(uniqueId) && WorldStateManager.instance != null)
        {
            WorldStateManager.instance.AddDestroyedResource(uniqueId);
        }

        if (resourceData.itemToDrop == null || dropItemPrefab == null)
        {
            Debug.LogWarning($"Resource node '{gameObject.name}' is missing itemToDrop data or dropItemPrefab.");
            Destroy(gameObject);
            return;
        }

        int amountToDrop = Random.Range(resourceData.minDropAmount, resourceData.maxDropAmount + 1);
        for (int i = 0; i < amountToDrop; i++)
        {
            float randomX = Random.Range(-0.5f, 0.5f);
            float randomY = Random.Range(-0.5f, 0.5f);
            Vector3 randomOffset = new Vector3(transform.position.x + randomX, transform.position.y + randomY, 0);

            GameObject newDrop = Instantiate(dropItemPrefab, randomOffset, Quaternion.identity, dropParent.transform);
            newDrop.GetComponent<DropItem>().dropItemData = resourceData.itemToDrop;
        }

        Destroy(gameObject);
    }
}