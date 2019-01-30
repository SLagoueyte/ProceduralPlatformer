using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheetAssigner : MonoBehaviour {

    [SerializeField]
    Texture2D[] sheetsNormal;
    [SerializeField]
    Texture2D[] sheetsStart;

    [SerializeField]
    GameObject roomObj;

    Vector2 roomDimensions;

    [SerializeField]
    Vector2 gutterSize = new Vector2(9, 4); //How large it's the gap between rooms

    public Vector2 GutterSize {
        get {
            return gutterSize;
        }
    }

    public Vector2 RoomDimensions {
        get {
            return roomDimensions;
        }
    }

    public void Assign(Room[,] rooms) {
        Texture2D[] selectedSheet;
        List<RoomInstance> roomInstances = new List<RoomInstance>();

        foreach (Room room in rooms) {
            if (room == null) {
                continue;
            }

            int index;

            if (room.type == 0) {
                index = Mathf.RoundToInt(Random.value * (sheetsNormal.Length - 1));
                selectedSheet = sheetsNormal;
            } else {
                index = Mathf.RoundToInt(Random.value * (sheetsStart.Length - 1));
                selectedSheet = sheetsStart;
            }

            RoomInstance myRoom = Instantiate(roomObj, Vector3.zero, Quaternion.identity).GetComponent<RoomInstance>();
            myRoom.gameObject.name = "Room at " + room.gridPos;
            myRoom.Setup(selectedSheet[index],
                room.gridPos,
                room.type,
                room,

                room.connectedUpRoom,
                room.connectedDownRoom,
                room.connectedLeftRoom,
                room.connectedRightRoom);

            if (myRoom.RoomSizeInTiles.x > myRoom.RoomSizeInTiles.y) {
                roomDimensions = new Vector2(myRoom.TileSize * myRoom.RoomSizeInTiles.x, myRoom.TileSize * myRoom.RoomSizeInTiles.x);
            } else {
                roomDimensions = new Vector2(myRoom.TileSize * myRoom.RoomSizeInTiles.y, myRoom.TileSize * myRoom.RoomSizeInTiles.y);
            }

            Vector2 offset = (gutterSize * myRoom.TileSize);

            Vector3 pos = new Vector3(room.gridPos.x * (roomDimensions.x + offset.x), room.gridPos.y * (roomDimensions.y + offset.y), 0);

            myRoom.gameObject.transform.position = pos;
            roomInstances.Add(myRoom);
        }

        foreach (RoomInstance roomInstance in roomInstances) {
            roomInstance.ConnectDoors();
        }

    }
}
