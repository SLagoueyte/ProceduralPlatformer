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
    int extraExitSpace, extraVertRange, extraHorizRange;

    public void SetConnectedRoom(Room _connected) {
        connectedExit = _connected;
    }

    /// <summary>
    /// Searchs for another exit on the connected exit room that it's the contrary of this one.
    /// </summary>
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

    public void SetExtraExitSpace(int extraSpace, int verticalDifference, int horizontalDifference) {
        extraExitSpace += extraSpace;
        extraVertRange += verticalDifference;
        extraHorizRange += horizontalDifference;
    }

    public void StrechToExtraSpace() {
        if (extraExitSpace != 0) {
            transform.localScale += new Vector3(extraExitSpace, 0, 0);
        }

        if (extraVertRange != 0) {
            transform.localPosition += new Vector3(0, extraVertRange * RoomInstance.CommonTileSize, 0);
        }

        if (extraHorizRange != 0) {
            transform.localPosition += new Vector3(extraHorizRange * RoomInstance.CommonTileSize, 0, 0);
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
