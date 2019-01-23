using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Room {

    public Vector2 gridPos;
    public int type; //Can be changed and expanded later
    //public bool doorTop, doorBot, doorLeft, doorRight;
    public bool isMainRoom, isAccesibleFromMainRoom = false;
    [SerializeField]
    public Room connectedDownRoom, connectedUpRoom, connectedRightRoom, connectedLeftRoom;

    public Room(Vector2 _gridPos, int _type) {
        gridPos = _gridPos;
        type = _type;

    }

    public int DoorCount {
        get {
            int doorCount = 0;

            if (connectedDownRoom != null) {
                doorCount++;
            }
            if (connectedUpRoom != null) {
                doorCount++;
            }
            if (connectedRightRoom != null) {
                doorCount++;
            }
            if (connectedLeftRoom != null) {
                doorCount++;
            }

            return doorCount;
        }
    }
}

