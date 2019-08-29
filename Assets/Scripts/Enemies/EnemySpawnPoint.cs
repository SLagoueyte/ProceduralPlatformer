using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour {

    private void OnEnable() {
        EnemySpawnManager.instance.AddSpawnPoint(transform);
    }
}
