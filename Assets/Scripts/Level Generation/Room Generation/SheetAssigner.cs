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

    [SerializeField]
    Vector2 gutterSize = new Vector2(9, 4); //How large it's the gap between rooms

    public Vector2 GutterSize {
        get {
            return gutterSize;
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

            roomInstances.Add(myRoom);
        }

        GameController.instance.SetRoomInstances(roomInstances.ToArray());
        RePositionRoomInstances(roomInstances);
    }

    void RePositionRoomInstances(List<RoomInstance> instances) {
        Vector2 biggestDimension = -Vector2.one;
        Vector2 roomDimensions;
        Vector2 offset = Vector2.zero;

        foreach (RoomInstance instance in instances) {
            if (instance.RoomSizeInTiles.x > instance.RoomSizeInTiles.y) {
                roomDimensions = new Vector2(instance.TileSize * instance.RoomSizeInTiles.x, instance.TileSize * instance.RoomSizeInTiles.x);
            } else {
                roomDimensions = new Vector2(instance.TileSize * instance.RoomSizeInTiles.y, instance.TileSize * instance.RoomSizeInTiles.y);
            }

            if (roomDimensions.x > biggestDimension.x) {
                biggestDimension.x = roomDimensions.x;
            }

            if (roomDimensions.y > biggestDimension.y) {
                biggestDimension.y = roomDimensions.y;
            }
        }

        foreach (RoomInstance instance in instances) {

            offset = (gutterSize * instance.TileSize);

            Vector3 pos = new Vector3(instance.gridPos.x * (biggestDimension.x + offset.x), instance.gridPos.y * (biggestDimension.y + offset.y), 0);
            //Debug.LogFormat("<color=red>Room at {0}, with dimesions {1}, has an offset of {2}, with new position of {3}</color>", instance.gridPos, biggestDimension, offset, pos);

            instance.gameObject.transform.position = pos;

            instance.ConnectDoors();
        }
    }
}
