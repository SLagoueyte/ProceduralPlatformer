using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGenerator : MonoBehaviour {

    enum StartPosition {
        top, bottom, right, left,
        topRight, topLeft, bottomRight, bottomLeft,
        center
    }

    [SerializeField]
    StartPosition startPosition = StartPosition.left;
    [SerializeField]
    Vector2 worldSize = new Vector2(4, 4);
    [SerializeField]
    int numberOfRooms = 20, connectionToMainRoomCycle = 4;
    [SerializeField]
    float randomCompareStart = 0.2f, randomCompareEnd = 0.01f, randomDoorVanishment = 0.1f; //Magic numbers


    Room[,] rooms;
    List<Vector2> takenPositions = new List<Vector2>();

    int gridSizeX, gridSizeY;

    public GameObject roomWhiteObj;


    public delegate void FinishedCreatingMap();
    public static event FinishedCreatingMap OnFinished;

    private void Start() {
        if (numberOfRooms > ((worldSize.x * 2) * (worldSize.y * 2)) * 0.5f) {
            numberOfRooms = Mathf.RoundToInt(((worldSize.x * 2) * (worldSize.y * 2)) * 0.5f);
        }

        gridSizeX = Mathf.RoundToInt(worldSize.x);
        gridSizeY = Mathf.RoundToInt(worldSize.y);

        CreateRooms();
        SetRoomDoors();
        StartCoroutine(RandomizeRoomDoors());
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            Restart();
        }
    }

    private void Restart() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    Room SetStartRoom() {
        switch (startPosition) {
            default:
                return null;

            case StartPosition.bottom:
                return rooms[gridSizeX, 0] = new Room(new Vector2(0, -gridSizeY), 1);
            case StartPosition.bottomLeft:
                return rooms[0, 0] = new Room(new Vector2(-gridSizeX, -gridSizeY), 1);
            case StartPosition.bottomRight:
                return rooms[rooms.GetLength(0) - 1, 0] = new Room(new Vector2(gridSizeX - 1, -gridSizeY), 1);
            case StartPosition.center:
                return rooms[gridSizeX, gridSizeY] = new Room(new Vector2(0, 0), 1);
            case StartPosition.left:
                return rooms[0, gridSizeY] = new Room(new Vector2(-gridSizeX, 0), 1);
            case StartPosition.right:
                return rooms[rooms.GetLength(0) - 1, gridSizeY] = new Room(new Vector2(gridSizeX - 1, 0), 1);
            case StartPosition.top:
                return rooms[gridSizeX, rooms.GetLength(1) - 1] = new Room(new Vector2(0, gridSizeY - 1), 1);
            case StartPosition.topLeft:
                return rooms[0, rooms.GetLength(1) - 1] = new Room(new Vector2(-gridSizeX, gridSizeY - 1), 1);
            case StartPosition.topRight:
                return rooms[rooms.GetLength(0) - 1, rooms.GetLength(1) - 1] = new Room(new Vector2(gridSizeX - 1, gridSizeY - 1), 1);
        }   
    }

    /// <summary>
    /// Creates a first-main room and starts creating rooms from there.
    /// </summary>
    void CreateRooms() {
        //Setup
        rooms = new Room[gridSizeX * 2, gridSizeY * 2];

        Room startRoom = SetStartRoom();
        startRoom.isMainRoom = true;
        startRoom.isAccesibleFromMainRoom = true;
        Debug.Log(startRoom.gridPos);

        takenPositions.Insert(0, startRoom.gridPos);
        Vector2 checkPos = startRoom.gridPos;

        float randomCompare = 0.2f;

        //Add rooms
        for (int i = 0; i < numberOfRooms - 1; i++) {
            float randomPerc = ((float)i) / ((float)numberOfRooms - 1);
            randomCompare = Mathf.Lerp(randomCompareStart, randomCompareEnd, randomPerc);

            //Grab new position
            checkPos = NewPosition();

            //Test new position
            if (NumberOfNeighbours(checkPos, takenPositions) > 1 && Random.value > randomCompare) {
                int iterations = 0;
                do {
                    checkPos = SelectiveNewPosition();
                    iterations++;
                } while (NumberOfNeighbours(checkPos, takenPositions) > 1 && iterations <= 100);
                if (iterations >= 50) {
                    Debug.LogError("Error: " + checkPos + " Could not be created with fewer neighbours than: " + NumberOfNeighbours(checkPos, takenPositions));
                }
            }

            //Finalize position
            rooms[(int)checkPos.x + gridSizeX, (int)checkPos.y + gridSizeY] = new Room(checkPos, 0); //Can change the type of room here
            takenPositions.Insert(0, checkPos);
        }
    }

    /// <summary>
    /// If a room has a neighbour, makes a door across them.
    /// </summary>
    void SetRoomDoors() {
        for (int x = 0; x < (gridSizeX * 2); x++) {
            for (int y = 0; y < (gridSizeY * 2); y++) {
                if (rooms[x, y] == null) {
                    continue;
                }

                //Vector2 gridPosition = new Vector2(x, y);

                if (y - 1 < 0) { //check bellow
                    rooms[x, y].connectedDownRoom = null;
                } else {
                    rooms[x, y].connectedDownRoom = rooms[x, y - 1] ?? null;
                }
                if (y + 1 >= gridSizeY * 2) { //check above
                    rooms[x, y].connectedUpRoom = null;
                } else {
                    rooms[x, y].connectedUpRoom = rooms[x, y + 1] ?? null;
                }
                if (x - 1 < 0) { //check left
                    rooms[x, y].connectedLeftRoom = null;
                } else {
                    rooms[x, y].connectedLeftRoom = rooms[x - 1, y] ?? null;
                }
                if (x + 1 >= gridSizeX * 2) { //check right
                    rooms[x, y].connectedRightRoom = null;
                } else {
                    rooms[x, y].connectedRightRoom = rooms[x + 1, y] ?? null;
                }
            }
        }
    }

    /// <summary>
    /// Randomly deletes doors
    /// </summary>
    /// <returns></returns>
    IEnumerator RandomizeRoomDoors() {
        for (int x = 0; x < (gridSizeX * 2); x++) {
            yield return new WaitForEndOfFrame();
            for (int y = 0; y < (gridSizeY * 2); y++) {

                if (rooms[x, y] == null || rooms[x, y].DoorCount <= 1) {
                    continue;
                }

                float r = Random.value;
                while (r < randomDoorVanishment && rooms[x, y].DoorCount > 1) {
                    r = Random.value;
                    int r2 = Random.Range(0, 5);
                    switch (r2) {
                        default:
                            break;

                        case 0: //All
                            if (rooms[x, y].connectedUpRoom != null && (rooms[x, y + 1] != null)) {
                                if (rooms[x, y + 1].DoorCount > 1 && rooms[x, y].DoorCount > 1) {
                                    rooms[x, y + 1].connectedDownRoom = null;
                                    rooms[x, y].connectedUpRoom = null;
                                }
                            }
                            if (rooms[x, y].connectedRightRoom != null && (rooms[x + 1, y] != null)) {
                                if (rooms[x + 1, y].DoorCount > 1 && rooms[x, y].DoorCount > 1) {
                                    rooms[x + 1, y].connectedLeftRoom = null;
                                    rooms[x, y].connectedRightRoom = null;
                                }
                            }
                            if (rooms[x, y].connectedDownRoom != null && (rooms[x, y - 1] != null)) {
                                if (rooms[x, y - 1].DoorCount > 1 && rooms[x, y].DoorCount > 1) {
                                    rooms[x, y - 1].connectedUpRoom = null;
                                    rooms[x, y].connectedDownRoom = null;
                                }
                            }
                            if (rooms[x, y].connectedLeftRoom != null && (rooms[x - 1, y] != null)) {
                                if (rooms[x - 1, y].DoorCount > 1 && rooms[x, y].DoorCount > 1) {
                                    rooms[x - 1, y].connectedRightRoom = null;
                                    rooms[x, y].connectedLeftRoom = null;
                                }
                            }
                            break;

                        case 1: //up
                            if (rooms[x, y].connectedUpRoom != null && (rooms[x, y + 1] != null)) {
                                if (rooms[x, y + 1].DoorCount > 1) {
                                    rooms[x, y + 1].connectedDownRoom = null;
                                    rooms[x, y].connectedUpRoom = null;
                                }
                            }
                            break;

                        case 2: //right
                            if (rooms[x, y].connectedRightRoom != null && (rooms[x + 1, y] != null)) {
                                if (rooms[x + 1, y].DoorCount > 1) {
                                    rooms[x + 1, y].connectedLeftRoom = null;
                                    rooms[x, y].connectedRightRoom = null;
                                }
                            }
                            break;

                        case 3: //down
                            if (rooms[x, y].connectedDownRoom != null && (rooms[x, y - 1] != null)) {
                                if (rooms[x, y - 1].DoorCount > 1) {
                                    rooms[x, y - 1].connectedUpRoom = null;
                                    rooms[x, y].connectedDownRoom = null;
                                }
                            }
                            break;

                        case 4: //left
                            if (rooms[x, y].connectedLeftRoom != null && (rooms[x - 1, y] != null)) {
                                if (rooms[x - 1, y].DoorCount > 1) {
                                    rooms[x - 1, y].connectedRightRoom = null;
                                    rooms[x, y].connectedLeftRoom = null;
                                }
                            }
                            break;
                    }
                }
                //Ends while
            }
        }
        //Ends Room cycle

        StartCoroutine(CheckConnectedToMainRoom());
        yield break;
    }

    /// <summary>
    /// Does several cycles to check if every room is connected to the main room on any level.
    /// </summary>
    /// <returns></returns>
    IEnumerator CheckConnectedToMainRoom() {
        bool isAccesibleYet;

        for (int i = 0; i < connectionToMainRoomCycle; i++) {
            yield return new WaitForEndOfFrame();
            for (int x = 0; x < (gridSizeX * 2); x++) {
                yield return new WaitForEndOfFrame();
                for (int y = 0; y < (gridSizeY * 2); y++) {

                    if (rooms[x, y] == null) {
                        continue;
                    }

                    isAccesibleYet = false;
                    int debug = 0;

                    if (rooms[x, y].isAccesibleFromMainRoom) {
                        continue;
                    } else {
                        if (rooms[x, y].connectedDownRoom != null && rooms[x, y].connectedDownRoom.isAccesibleFromMainRoom) {
                            isAccesibleYet = true;
                            debug = 1;
                        }
                        if (rooms[x, y].connectedLeftRoom != null && rooms[x, y].connectedLeftRoom.isAccesibleFromMainRoom) {
                            isAccesibleYet = true;
                            debug = 2;
                        }
                        if (rooms[x, y].connectedRightRoom != null && rooms[x, y].connectedRightRoom.isAccesibleFromMainRoom) {
                            isAccesibleYet = true;
                            debug = 3;
                        }
                        if (rooms[x, y].connectedUpRoom != null && rooms[x, y].connectedUpRoom.isAccesibleFromMainRoom) {
                            isAccesibleYet = true;
                            debug = 4;
                        }
                    }

                    if (isAccesibleYet == false) {
                        if (y - 1 >= 0 && !isAccesibleYet) { //check bellow
                            if (rooms[x, y - 1] != null && rooms[x, y].connectedDownRoom == null) {
                                rooms[x, y].connectedDownRoom = rooms[x, y - 1].isAccesibleFromMainRoom ? rooms[x, y - 1] : null;

                                if (rooms[x, y].connectedDownRoom != null) {
                                    rooms[x, y - 1].connectedUpRoom = rooms[x, y];
                                    isAccesibleYet = true;
                                    debug = 5;
                                }
                            }
                        } 
                        if (y + 1 < gridSizeY * 2 && !isAccesibleYet) { //check above
                            if (rooms[x, y + 1] != null && rooms[x, y].connectedUpRoom == null) {
                                rooms[x, y].connectedUpRoom = rooms[x, y + 1].isAccesibleFromMainRoom ? rooms[x, y + 1] : null;

                                if (rooms[x, y].connectedUpRoom != null) {
                                    rooms[x, y + 1].connectedDownRoom = rooms[x, y];
                                    isAccesibleYet = true;
                                    debug = 6;
                                }
                            }
                        } 
                        if (x - 1 >= 0 && !isAccesibleYet) { //check left
                            if (rooms[x - 1, y] != null && rooms[x, y].connectedLeftRoom == null) {
                                rooms[x, y].connectedLeftRoom = rooms[x - 1, y].isAccesibleFromMainRoom ? rooms[x - 1, y] : null;

                                if (rooms[x, y].connectedLeftRoom != null) {
                                    rooms[x - 1, y].connectedRightRoom = rooms[x, y];
                                    isAccesibleYet = true;
                                    debug = 7;
                                }
                            }
                        } 
                        if (x + 1 < gridSizeX * 2 && !isAccesibleYet) { //check right
                            if (rooms[x + 1, y] != null && rooms[x, y].connectedRightRoom == null) {
                                rooms[x, y].connectedRightRoom = rooms[x + 1, y].isAccesibleFromMainRoom ? rooms[x + 1, y] : null;

                                if (rooms[x, y].connectedRightRoom != null) {
                                    rooms[x + 1, y].connectedLeftRoom = rooms[x, y];
                                    isAccesibleYet = true;
                                    debug = 8;
                                }
                            }
                        } 
                    }

                    rooms[x, y].isAccesibleFromMainRoom = isAccesibleYet;
                    //if (isAccesibleYet) {
                    //    Debug.LogFormat("Room {0} is accesible because {1}", rooms[x, y].gridPos.ToString(), debug);
                    //}
                }
            }
        }

        foreach (Room room in rooms) {
            if (room == null) {
                continue;
            }
            if (!room.isAccesibleFromMainRoom) {
                StartCoroutine(CheckConnectedToMainRoom());
                Debug.LogWarning("Warning: Repeating main room door putting process");
                yield break;
            }
        }

        DrawMap();
        GetComponent<SheetAssigner>().Assign(rooms);


        GameController.instance.LevelFinishedLoad(rooms);
        yield break;
    }

    void DrawMap() {
        Sprite roomSprite = roomWhiteObj.GetComponent<SpriteRenderer>().sprite;
        foreach (Room room in rooms) {
            if (room == null) {
                continue;
            }
            Vector2 drawPos = room.gridPos;
            drawPos.x *= roomSprite.rect.width; //sprite size?
            drawPos.y *= roomSprite.rect.height;
            MapSpriteSelector mapper = Instantiate(roomWhiteObj, drawPos, Quaternion.identity).GetComponent<MapSpriteSelector>();
            mapper.Init(room);
        }
    }

    Vector2 NewPosition() {
        int x = 0, y = 0;
        int count = 0;
        Vector2 checkingPos = Vector2.zero;
        do {
            int index = Mathf.RoundToInt(Random.value * (takenPositions.Count - 1));
            x = (int)takenPositions[index].x;
            y = (int)takenPositions[index].y;
            bool UpDown = (Random.value < 0.5f);
            bool positive = (Random.value < 0.5f);

            count++;

            if (UpDown) {
                if (positive) {
                    y += 1;
                } else {
                    y -= 1;
                }
            } else {
                if (positive) {
                    x += 1;
                } else {
                    x -= 1;
                }
            }
            checkingPos = new Vector2(x, y);

            if (count > 1000) {
                break;
            }
        } while (takenPositions.Contains(checkingPos) || x >= gridSizeX || x < -gridSizeX || y >= gridSizeY || y < -gridSizeY );

        if (count >= 1000 && takenPositions.Contains(checkingPos)) {
            Debug.LogError("Error: Could not find new position");
        }

        return checkingPos;
    }

    Vector2 SelectiveNewPosition() {
        int index = 0, inc = 0, safeGuard = 0;
        int x = 0, y = 0;
        Vector2 checkingPos = Vector2.zero;
        do {
            inc = 0;
            safeGuard++;
            do {
                index = Mathf.RoundToInt(Random.value * (takenPositions.Count - 1));
                inc++;
            } while (NumberOfNeighbours(takenPositions[index], takenPositions) > 1 && inc < 100);

            x = (int)takenPositions[index].x;
            y = (int)takenPositions[index].y;
            bool UpDown = (Random.value < 0.5f);
            bool positive = (Random.value < 0.5f);

            if (UpDown) {
                if (positive) {
                    y += 1;
                } else {
                    y -= 1;
                }
            } else {
                if (positive) {
                    x += 1;
                } else {
                    x -= 1;
                }
            }
            checkingPos = new Vector2(x, y);

            if (safeGuard > 1000) {
                break;
            }
        } while (takenPositions.Contains(checkingPos) || x >= gridSizeX || x < -gridSizeX || y >= gridSizeY || y < -gridSizeY);
        if (inc >= 100) {
            Debug.LogError("Error: could not find position with only one neighbour");
        }
        if (safeGuard >= 1000 && takenPositions.Contains(checkingPos)) {
            Debug.LogError("Error: could not find position selectively");
        }
        return checkingPos;
    }

    int NumberOfNeighbours(Vector2 checkingPos, List<Vector2> usedPositions) {
        int rent = 0;
        if (usedPositions.Contains(checkingPos + Vector2.right)) {
            rent++;
        }
        if (usedPositions.Contains(checkingPos + Vector2.down)) {
            rent++;
        }
        if (usedPositions.Contains(checkingPos + Vector2.left)) {
            rent++;
        }
        if (usedPositions.Contains(checkingPos + Vector2.up)) {
            rent++;
        }
        return rent;
    }
}
