using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorTrigger : MonoBehaviour {

    public enum Direction {
        up = 1,
        down = -1,
        left = 2,
        right = -2
    }

    [SerializeField]
    Transform mySpawnPoint;

    public Direction myDirection;

    Room connectedExit;
    Transform connectedExitPoint;

    public void SetConnectedRoom(Room _connected) {
        connectedExit = _connected;
    }

    public void SetExit() {
        DoorTrigger[] otherDoorTriggers = connectedExit.myInstance.doorTriggers.ToArray();
        foreach (DoorTrigger doorTrigger in otherDoorTriggers) {
            if ((int)doorTrigger.myDirection != -(int)myDirection) {
                continue;
            }

            connectedExitPoint = doorTrigger.GetSpawnPoint;
            return;
        }
    }

    public Transform GetSpawnPoint {
        get {
            return mySpawnPoint;
        }
    }

    public Transform GetConnectedExitPoint {
        get {
            return connectedExitPoint;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (connectedExit == null) {
            Debug.LogError("Error: Trying to use a door that has no exit");
            return;
        }

        if (collision.CompareTag("Player")) {
            collision.transform.position = connectedExitPoint.position;
        }
    }

}
