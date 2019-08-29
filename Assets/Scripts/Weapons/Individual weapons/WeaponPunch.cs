using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPunch : Weapon {

    int currentPunch;

    private void Awake() {
        currentPunch = 1;
    }

    public override string GetAttackName() {
        if (onCooldown)
            return "-1";

        if (!onCooldown) {
            onCooldown = true;
            StopAllCoroutines();
            StartCoroutine("WaitForCooldown");
            StartCoroutine("RestartWhenNotPunching");
        }



        if (currentPunch == 1) {
            currentPunch = 2;
            return myAnimations[0];
        } else if (currentPunch == 2) {
            currentPunch = 1;
            return myAnimations[1];
        } else {

            return "How did we manage to get here?";
        }
    }

    IEnumerator WaitForCooldown() {
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }

    IEnumerator RestartWhenNotPunching() {
        yield return new WaitForSeconds(cooldown * 2);
        currentPunch = 1;
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag(holderTag)) {
            return;
        }

        if (collision.CompareTag("Enemy")) {

            Enemy hittedEnemy = collision.GetComponent<Enemy>();
            hittedEnemy.TakeDamage(damage, transform.position);
            
        } else if (collision.CompareTag("Player")) {

        }
    }

}
