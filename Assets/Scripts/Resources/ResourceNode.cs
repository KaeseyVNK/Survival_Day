// File: Assets/Scripts/Resources/ResourceNode.cs
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class ResourceNode : MonoBehaviour, IDamageable
{
    // Make this field private, it should only be set through Initialize
    private ResourceNodeData resourceData;

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
        float randomX;
        float randomY;
        Vector3 randomOffset;

        int amountToDrop = Random.Range(resourceData.minDropAmount, resourceData.maxDropAmount + 1);
        for (int i = 0; i < amountToDrop; i++)
        {
            randomX = Random.Range(-0.5f, 0.5f);
            randomY = Random.Range(-0.5f, 0.5f);
            randomOffset = new Vector3(transform.position.x + randomX, transform.position.y + randomY, 0);

            Instantiate(resourceData.dropPrefab, randomOffset, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}