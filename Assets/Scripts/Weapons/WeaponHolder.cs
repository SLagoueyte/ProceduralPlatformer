using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHolder : MonoBehaviour {

    Animator myAnimator;
    Weapon currentWeapon;

    public void Initialize() {
        myAnimator = GetComponent<Animator>();

        ChangeWeapon("fist");
    }

    //Is the weapon attacking right now
    public bool IsAttacking {
        get {
            return !myAnimator.GetCurrentAnimatorStateInfo(0).IsName("null");
        }
    }

    public void ChangeWeapon(string weaponName) {
        currentWeapon = WeaponsManager.instance.GetWeaponByName(weaponName, transform);

        currentWeapon.holderTag = transform.parent.tag;
        myAnimator.runtimeAnimatorController = currentWeapon.GetAnimator();

    }

    public void StartAttack() {
        if (currentWeapon != null && !currentWeapon.onCooldown) {
            Debug.Log("Attack with " + currentWeapon.weaponName);

            string attackName = currentWeapon.GetAttackName();
            myAnimator.Play(attackName);
        }
        
    }
}
