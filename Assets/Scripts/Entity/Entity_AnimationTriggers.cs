using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;
    private Enity_Combat entityCombat;

    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<Enity_Combat>();
    }

    private void CurrentStateTrigger()
    {
        entity.CurrentStateAnimationTrigger();
    }

    private void AttackTrigger()
    {
        // Phải gọi hàm PerformAttack thông qua Reflection vì nó là protected
        var performAttackMethod = typeof(Enity_Combat).GetMethod("PerformAttack", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (performAttackMethod != null)
        {
            performAttackMethod.Invoke(entityCombat, null);
        }
    }
}
