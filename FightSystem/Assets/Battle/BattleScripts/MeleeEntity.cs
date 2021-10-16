using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEntity : BaseEntity
{
    public void Update()
    {
        if(!GameManager.Instance.IsGameStarted)
        {
            base.WalkToStart();
            return;
        }
        if (!HasEnemy)
        {
            FindTarget();
        }

        if(IsInRange && !moving)
        {
            //In range for attack!
            if(canAttack)
            {
                Attack();
                currentTarget.TakeDamage(baseDamage);
            }
        }
        else
        {
            GetInRange();
        }
    }
}
