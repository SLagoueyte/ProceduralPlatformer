using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsManager : MonoBehaviour {

    #region instances
    //----------------------------------------------------------------
    // Singleton code
    //----------------------------------------------------------------
    // s_Instance is used to cache the instance found in the scene so we don't have to look it up every time.
    private static WeaponsManager s_Instance = null;

    // This defines a static instance property that attempts to find the object in the scene and
    // returns it to the caller.
    public static WeaponsManager instance {
        get {
            if (s_Instance == null) {
                s_Instance = FindObjectOfType(typeof(WeaponsManager)) as WeaponsManager;
            }

            return s_Instance;
        }
    }

    /// <summary>
    /// Indicates whether a WeaponsManager object exists in the
    /// current scene.
    /// </summary>
    /// <returns>True if a WeaponsManager exists, false otherwise.</returns>
    public static bool Exists() {
        return s_Instance != null;
    }
    //----------------------------------------------------------------
    // End Singleton code
    //----------------------------------------------------------------
    #endregion

    [SerializeField]
    GameObject[] prefabs;
    [SerializeField]
    int poolsSize = 10;

    Dictionary<string, List<GameObject>> objectsPool = new Dictionary<string, List<GameObject>>();

    List<GameObject> weaponsBeingUsed = new List<GameObject>();

    public void Start() {
        SetPool();   
    }

    public Weapon GetWeaponByName (string wName, Transform wHolder) {

        string searchName = wName.ToLower();

        if (objectsPool.ContainsKey(searchName)) {
            Weapon toGive = GetFirstAvailableInPool(objectsPool[searchName]).GetComponent<Weapon>();
            if (toGive != null) {
                toGive.gameObject.SetActive(true);
                weaponsBeingUsed.Add(toGive.gameObject);
                objectsPool[searchName].RemoveAt(objectsPool[searchName].IndexOf(toGive.gameObject));

                toGive.transform.parent = wHolder;
                toGive.transform.localScale = Vector2.one;
                toGive.transform.localPosition = Vector2.zero;
            }

            return toGive;
        }

        Debug.LogErrorFormat("Weapon Name Given '{0}' not found", wName);
        return null;
    }

    GameObject GetFirstAvailableInPool(List<GameObject> pool) {
        foreach (GameObject weapon in pool) {
            if (!weapon.activeInHierarchy) {
                return weapon;
            }
        }

        return null;
    }



    void SetPool() {
        for (int i = 0; i < prefabs.Length; i++) {
            GameObject prefab = prefabs[i];

            Weapon newWeapon = prefab.GetComponent<Weapon>();
            newWeapon.InitObject();

            objectsPool.Add(
                newWeapon.weaponName, 
                CreateObjects(prefab, CreateNewObject(newWeapon.weaponName).transform, poolsSize));
        }
    } 

    GameObject CreateNewObject (string objectName) {

        GameObject nObj = new GameObject();
        nObj.transform.parent = transform;
        nObj.transform.position = new Vector3(9999, 9999, -9999);
        nObj.name = objectName;
        return nObj;
    }

    List<GameObject> CreateObjects (GameObject original, Transform parent, int quantity) {

        List<GameObject> gameObjects = new List<GameObject>();

        for (int i = 0; i < quantity; i++) {
            gameObjects.Insert(0, Instantiate(original, parent, false));
            gameObjects[0].SetActive(false);
            gameObjects[0].name = original.name;
        }

        return gameObjects;
    }
}
