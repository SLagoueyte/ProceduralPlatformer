using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour {

    #region instances
    //----------------------------------------------------------------
    // Singleton code
    //----------------------------------------------------------------
    // s_Instance is used to cache the instance found in the scene so we don't have to look it up every time.
    private static EnemySpawnManager s_Instance = null;

    // This defines a static instance property that attempts to find the object in the scene and
    // returns it to the caller.
    public static EnemySpawnManager instance {
        get {
            if (s_Instance == null) {
                s_Instance = FindObjectOfType(typeof(EnemySpawnManager)) as EnemySpawnManager;
            }

            return s_Instance;
        }
    }

    /// <summary>
    /// Indicates whether a EnemySpawnManager object exists in the
    /// current scene.
    /// </summary>
    /// <returns>True if a EnemySpawnManager exists, false otherwise.</returns>
    public static bool Exists() {
        return s_Instance != null;
    }
    //----------------------------------------------------------------
    // End Singleton code
    //----------------------------------------------------------------
    #endregion

    [SerializeField]
    GameObject[] normalEnemies;

    List<Transform> possibleSpawnPoints = new List<Transform>();

    public void AddSpawnPoint(Transform point) {
        possibleSpawnPoints.Add(point);
    }

    public void StartSpawn() {
        //Temp

        foreach (Transform spawnPoint in possibleSpawnPoints) {
            int enemyType = Random.Range(-1, normalEnemies.Length);
            if (enemyType >= 0) {
                GameObject enemy = Instantiate(normalEnemies[enemyType], spawnPoint);

            }
        }
    }
}
