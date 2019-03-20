using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileHolderChanger : MonoBehaviour {

    [SerializeField]
    GameObject[] objectToChangeTo;

    public void ChangeMyObject(int index) {
        if (index == 0) { return; }

        GameObject newObject = Instantiate(objectToChangeTo[index], this.transform);
    }
}
