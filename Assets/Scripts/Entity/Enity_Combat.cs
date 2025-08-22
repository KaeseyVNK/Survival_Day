
using System.Linq;
using UnityEngine;

public class Enity_Combat : MonoBehaviour
{

    public Collider2D[] targetColliders; 

    [Header("Attack Settings")]
    [SerializeField] private float damage = 10f;

    [Header("Attack Detection - Top Down")]
    [SerializeField] Transform attackCheck;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] LayerMask whatIsTree;
    [SerializeField] bool includeTriggers = true; // thêm

    private void Awake(){

    }


    protected Collider2D[] GetDetectedColliders(LayerMask whatToDetect)
    {
        var all = Physics2D.OverlapCircleAll(attackCheck.position, attackRange, whatToDetect);
        targetColliders = all
            .Where(c => c is CapsuleCollider2D && (includeTriggers || c.isTrigger == true))
            .ToArray();
        return targetColliders;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackCheck.position, attackRange);
    }

    public void PerformAttack()
    {

        foreach (var target in GetDetectedColliders(whatIsTree))
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            ResourceNode resource = target.GetComponent<ResourceNode>();

            if (damageable != null)
            {
                resource.ChangeStatus();
                damageable.TakeDamage(damage);
            }
        }
        
    }

}
