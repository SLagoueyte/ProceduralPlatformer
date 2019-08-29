using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour {

    #region instances
    //----------------------------------------------------------------
    // Singleton code
    //----------------------------------------------------------------
    // s_Instance is used to cache the instance found in the scene so we don't have to look it up every time.
    private static GameController s_Instance = null;

    // This defines a static instance property that attempts to find the object in the scene and
    // returns it to the caller.
    public static GameController instance {
        get {
            if (s_Instance == null) {
                s_Instance = FindObjectOfType(typeof(GameController)) as GameController;
            }

            return s_Instance;
        }
    }

    /// <summary>
    /// Indicates whether a GameController object exists in the
    /// current scene.
    /// </summary>
    /// <returns>True if a GameController exists, false otherwise.</returns>
    public static bool Exists() {
        return s_Instance != null;
    }
    //----------------------------------------------------------------
    // End Singleton code
    //----------------------------------------------------------------
    #endregion

    [SerializeField]
    PlayerController player; //We'll need to either store the player or find him every time
    [SerializeField]
    CameraFollowPlayer cameraFollow;

    RoomInstance[] roomInstances;
    Room[,] rooms;


    //General Flags
    bool hasLevelFinishedLoading;

    public PlayerController GetPlayerController {
        get {
            return player;
        }
    }

    private void Start() {
        DontDestroyOnLoad(this.gameObject);
        hasLevelFinishedLoading = false;

        if (player == null) {
            player = FindObjectOfType<PlayerController>();
        }

        if (cameraFollow == null) {
            cameraFollow = FindObjectOfType<CameraFollowPlayer>();
        }
    }

    public void LevelFinishedLoad(Room[,] currentRooms) {
        hasLevelFinishedLoading = true;

        EnemySpawnManager.instance.StartSpawn();

        rooms = currentRooms;
        player.SetStartPosition(FindStartRoom());
        cameraFollow.StartFollowing(player);
    }

    public void SetRoomInstances(RoomInstance[] currentRoomInstances) {
        roomInstances = currentRoomInstances;
    }

    Room FindStartRoom() {
        foreach (Room room in rooms) {
            if (room != null && room.isMainRoom) {
                return room;
            }
        }
        return null;
    }

    public RoomInstance GetRoomInstanceByRoomGridPos (Vector2 roomGridPos) {
        foreach (RoomInstance roomInstance in roomInstances) {
            if (roomInstance.gridPos == roomGridPos) {
                return roomInstance;
            }
        }
        return null;
    }

}
