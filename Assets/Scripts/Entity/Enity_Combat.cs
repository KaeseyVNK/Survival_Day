
using System.Linq;
using UnityEngine;

public class Enity_Combat : MonoBehaviour
{

    public Collider2D[] targetColliders; 

    [Header("Attack Settings")]
    [SerializeField] protected float damage = 10f;

    [Header("Attack Detection - Top Down")]
    [SerializeField] protected Transform attackCheck;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected LayerMask whatIsAttackable;
    [SerializeField] protected bool includeTriggers = true; // thêm


    private void Awake(){

    }


    protected Collider2D[] GetDetectedColliders(LayerMask whatToDetect)
    {
        var all = Physics2D.OverlapCircleAll(attackCheck.position, attackRange, whatToDetect);
        
        // Cập nhật để nó hoạt động với nhiều loại collider hơn, không chỉ Capsule
        targetColliders = all.Where(c => c is CapsuleCollider2D && (includeTriggers || c.isTrigger == true))
            .ToArray();
        return targetColliders;
    }

    protected virtual void OnDrawGizmos()
    {
        if(attackCheck == null) return;
        Gizmos.DrawWireSphere(attackCheck.position, attackRange);
    }

    protected virtual void PerformAttack()
    {
        foreach (var target in GetDetectedColliders(whatIsAttackable))
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Logic cũ để rung cây sẽ được chuyển xuống Player_Combat
        }
    }

}
