using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour {

    public enum DamageType {
        piercing,
        bludgeoning,
        slashing, 
        psychic
    }

    [HideInInspector]
    public string weaponName;
    [HideInInspector]
    public string holderTag;
    [HideInInspector]
    public bool onCooldown;

    [SerializeField]
    WeaponBlueprint blueprint;
    [SerializeField]
    protected RuntimeAnimatorController myAnimator;
    [SerializeField]
    protected string[] myAnimations;

    protected int damage;
    protected float cooldown;
    protected float durability;
    protected DamageType damageType;


    public void InitObject() {
        GetCharacteristicsFromBlueprint();
    }

    public abstract string GetAttackName();

    public RuntimeAnimatorController GetAnimator() {
        return myAnimator;
    }

    protected void GetCharacteristicsFromBlueprint() {
        weaponName = blueprint.wName.ToLower();
        damage = blueprint.damage;
        cooldown = blueprint.cooldownTime;
        durability = blueprint.durability;
        damageType = blueprint.type;

    }

    protected void OnEnable() {
        InitObject();
    }
}
