using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBlueprint : ScriptableObject {

    public string wName;
    public int damage;
    public float cooldownTime;
    public float durability;
    public Weapon.DamageType type;
}
