using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomInstance : MonoBehaviour {

    [SerializeField]
    SpriteRenderer mySpr;

    public Texture2D tex;
    public Sprite[] backgrounds;

    [HideInInspector]
    public Vector2 gridPos;

    [SerializeField]
    int type;

    [HideInInspector]
    public Room connectedDownRoom, connectedUpRoom, connectedRightRoom, connectedLeftRoom;

    [SerializeField]
    GameObject doorU, doorD, doorL, doorR, doorWall;

    [SerializeField]
    ColorToGameObject[] mappings;

    Room myRoom;
    [HideInInspector]
    public List<DoorTrigger> doorTriggers = new List<DoorTrigger>();

    [SerializeField]
    static int commonTileSize = 16;

    float tileSize; //Has to be some factor from the roomSizeInTiles numbers

    Vector2 roomSizeInTiles; //The size of the read/write texture

    Tile[,] tiles;
    List<List<Tile>> tileClustersList = new List<List<Tile>>();
    int recheckX, recheckY;


    class Tile {
        public enum Type {
            wall, door, obst, none
        }

        public Type myType = Type.none;
        public bool alreadychecked = false;
        public int posX;
        public int posY;
        public Vector2 realPosition;
        public GameObject myGameObject;

        public Tile(Vector2 _realPosition, int _posX, int _posY, Type _type = Type.none) {
            myType = _type;
            realPosition = _realPosition;
            posX = _posX;
            posY = _posY;
        }
    }

    public void Setup(Texture2D _tex, Vector2 _gridPos, int _type, Room _myRoom,
        Room _doorTop, Room _doorBot, Room _doorLeft, Room _doorRight) {
        tex = _tex;
        gridPos = _gridPos;
        type = _type;
        myRoom = _myRoom;
        myRoom.myInstance = this;

        connectedUpRoom = _doorTop;
        connectedDownRoom = _doorBot;
        connectedLeftRoom = _doorLeft;
        connectedRightRoom = _doorRight;

        roomSizeInTiles = new Vector2(
            tex.width, //Deberia ser 17
            tex.height); //Deberia ser 9

        float tileWidth;
        float tileHeight;
        tileSize = -1;

        foreach (Sprite background in backgrounds) {
            tileWidth = background.texture.width / roomSizeInTiles.x;
            tileHeight = background.texture.height / roomSizeInTiles.y;
            if (tileWidth == tileHeight) { //If the background has a tile that's the same width and height as the one we need, we pick that one as our background.
                tileSize = tileWidth;
                mySpr.sprite = background;
                break;
            }
        }
        if (tileSize == -1) {
            Debug.LogError("Error: There's no background that matches the texture \"" + tex.name + "\"");
            Debug.Break();
        }
        if (tileSize != commonTileSize) {
            Debug.LogError("Error: Background \"" + mySpr.sprite.name + "\" it's not on a x" + commonTileSize + " scale with tiles. Errors will occur");
        }

        tiles = new Tile[tex.width, tex.height];


        MakeDoors();
        GenerateRoomTiles();
    }

    public static int CommonTileSize {
        get {
            return commonTileSize;
        }
    }

    public float TileSize {
        get {
            return tileSize;
        }
    } 

    public Vector2 RoomSizeInTiles {
        get {
            return roomSizeInTiles;
        }
    }

    void MakeDoors() {
        //Vector3 spawnPos = transform.position + Vector3.up * (roomSizeInTiles.y / 4 * tileSize) - Vector3.up * (tileSize/4); //Debe sar 64
        Vector3 spawnPos = transform.position + Vector3.up * (roomSizeInTiles.y * tileSize * 0.5f) - Vector3.up * (tileSize * 0.5f);
        Quaternion rot = Quaternion.Euler(0, 0, 0);
        PlaceDoor(spawnPos, connectedUpRoom, doorU, rot);

        spawnPos = transform.position + Vector3.down * (roomSizeInTiles.y * tileSize * 0.5f) - Vector3.down * (tileSize * 0.5f);
        rot = Quaternion.Euler(0, 0, 180);
        PlaceDoor(spawnPos, connectedDownRoom, doorD, rot);

        //spawnPos = transform.position + Vector3.right * (roomSizeInTiles.x * tileSize) - Vector3.right * (tileSize); //Debe ser 128
        spawnPos = transform.position + Vector3.right * (roomSizeInTiles.x * tileSize * 0.5f) - Vector3.right * (tileSize * 0.5f);
        rot = Quaternion.Euler(0, 0, 270);
        PlaceDoor(spawnPos, connectedRightRoom, doorR, rot);

        spawnPos = transform.position + Vector3.left * (roomSizeInTiles.x * tileSize * 0.5f) - Vector3.left * (tileSize * 0.5f);
        rot = Quaternion.Euler(0, 0, 90);
        PlaceDoor(spawnPos, connectedLeftRoom, doorL, rot);
    }

    void GenerateRoomTiles() {
        for (int x = 0; x < tex.width; x++) {
            for (int y = 0; y < tex.height; y++) {
                GenerateTile(x, y);
            }
        }

        CalculateTileClusters();
        CalculateNodes();
    }
    
    void GenerateTile(int x, int y) {
        Color pixelColor = tex.GetPixel(x, y);
        if (pixelColor.a == 0) {
            return;
        }
        foreach (ColorToGameObject mapping in mappings) {
            if (mapping.color.Equals(pixelColor)) {
                Vector3 spawnPos = PositionFromTileGrid(x, y);
                switch (mapping.type) {
                    case "wall":
                        tiles[x, y] = new Tile(spawnPos, x, y, Tile.Type.wall);
                        break;

                    case "obst":
                        tiles[x, y] = new Tile(spawnPos, x, y, Tile.Type.obst);
                        break;

                    case "door":
                        Vector2Int nearestDoorTrigger = AreDoorsNear(x, y);
                        if (nearestDoorTrigger != new Vector2Int(tiles.GetLength(0) + 1, tiles.GetLength(1) + 1)) {
                            DoorTrigger doorTriggerObject = tiles[nearestDoorTrigger.x, nearestDoorTrigger.y].myGameObject.GetComponent<DoorTrigger>();

                            tiles[x, y] = new Tile(spawnPos, x, y, Tile.Type.door);
                            doorTriggerObject.SetExtraExitSpace(1, 
                                (nearestDoorTrigger.y > y) ? 1 : (nearestDoorTrigger.y < y) ? -1 : 0, 
                                (nearestDoorTrigger.x > x) ? 1 : (nearestDoorTrigger.x < x) ? -1 : 0);
                                
                        } else {
                            tiles[x, y] = new Tile(spawnPos, x, y, Tile.Type.wall);
                        }
                        break;

                    default:
                        tiles[x, y] = new Tile(spawnPos, x, y);
                        break;
                }

                tiles[x, y].myGameObject = Instantiate(mapping.prefab, spawnPos, Quaternion.identity);
                tiles[x, y].myGameObject.transform.parent = this.transform;

                if (mapping.type == "door") {
                    if (tiles[x,y].myType == Tile.Type.wall) {
                        tiles[x, y].myGameObject.GetComponent<TileHolderChanger>().ChangeMyObject(1);
                    }
                }

                return;
            } else {
                //print(" MAP: " + mapping.color);
            }
        } 

        //print(pixelColor);
    }

    Vector2Int AreDoorsNear(int x, int y) {
        if (x + 1 < tiles.GetLength(0)) {
            if (tiles[x + 1, y] != null && tiles[x + 1, y].myType == Tile.Type.door && tiles[x + 1, y].myGameObject != null) {
                return new Vector2Int(x + 1, y);
            }
        }

        if (x - 1 >= 0) {
            if (tiles[x - 1, y] != null && tiles[x - 1, y].myType == Tile.Type.door && tiles[x - 1, y].myGameObject != null) {
                return new Vector2Int(x - 1, y);
            }
        }

        if (y + 1 < tiles.GetLength(1)) {
            if (tiles[x, y + 1] != null && tiles[x, y + 1].myType == Tile.Type.door && tiles[x, y + 1].myGameObject != null) {
                return new Vector2Int(x, y + 1);
            }
        }

        if (y - 1 >= 0) {
            if (tiles[x, y - 1] != null && tiles[x, y - 1].myType == Tile.Type.door && tiles[x, y - 1].myGameObject != null) {
                return new Vector2Int(x, y - 1);
            }
        }

        return new Vector2Int(tiles.GetLength(0) + 1, tiles.GetLength(1) + 1);
    }

    /// <summary>
    /// Checks for tiles that are next to each other
    /// </summary>
    void CalculateTileClusters() {
        for (int x = 0; x < tiles.GetLength(0); x++) {
            for (int y = 0; y < tiles.GetLength(1); y++) {
                if (tiles[x,y] == null || (tiles[x, y].myType == Tile.Type.none) || (tiles[x, y].myType == Tile.Type.door)) {
                    continue;
                }

                if (!tiles[x, y].alreadychecked) {
                    GameObject newParent = new GameObject("Cluster");
                    newParent.transform.position = transform.position;
                    newParent.transform.parent = this.transform;

                    tileClustersList.Insert(0, new List<Tile>());
                    tiles[x, y].alreadychecked = true;
                    tiles[x, y].myGameObject.transform.parent = newParent.transform;
                    tileClustersList[0].Add(tiles[x, y]);
                    recheckX = x;
                    recheckY = y;

                    FollowTile(tiles[x, y], x, y, newParent, false);
                }
            }
        }
    }

    /// <summary>
    /// Checks for any tile that is either vertica or horizontal to the original tiles, 
    /// and continues on that path until it finds a repeat and no other unchecked adyacent tile.
    /// </summary>
    /// <param name="tile"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="parent"></param>
    /// <param name="reCheck"></param>
    void FollowTile(Tile tile, int x, int y, GameObject parent, bool reCheck) {
        Tile checkingTile = tile;
        int checkPosX = x;
        int checkPosY = y;

        int otherLikeMeCount = 0;

        bool foundTile = false;

        for (int h = -1; h < 2; h++) {
            for (int v = -1; v < 2; v++) {
                if (checkPosX + h < 0 || checkPosX + h >= tiles.GetLength(0) || checkPosY + v < 0 || checkPosY + v >= tiles.GetLength(1) ||
                    (h == 0 && v == 0)) {
                    continue;
                }
              
                if (tiles[checkPosX + h, checkPosY + v] != null && tiles[checkPosX + h, checkPosY + v].myType != Tile.Type.door) {
                    if (!tiles[checkPosX + h, checkPosY + v].alreadychecked) {
                        if (tiles[checkPosX + h, checkPosY + v].myType == checkingTile.myType) {
                            foundTile = true;

                            if (checkingTile == tiles[recheckX, recheckY]) {
                                reCheck = false;
                            }

                            checkingTile = tiles[checkPosX + h, checkPosY + v];
                            checkingTile.alreadychecked = true;
                            tileClustersList[0].Add(checkingTile);
                            checkingTile.myGameObject.transform.parent = parent.transform;

                            FollowTile(checkingTile, checkPosX + h, checkPosY + v, parent, reCheck);
                            return;
                        }
                    } else if (tiles[checkPosX + h, checkPosY + v].myType == checkingTile.myType) {
                        otherLikeMeCount++;
                        if (otherLikeMeCount > 1) {
                            if (tileClustersList[0].Count < 2) {
                                foreach (List<Tile> tilecluster in tileClustersList) {
                                    if (tilecluster.Contains(tiles[checkPosX + h, checkPosY + v])) {
                                        Destroy(tileClustersList[0][0].myGameObject.transform.parent.gameObject);

                                        tileClustersList.RemoveAt(0);
                                        tilecluster.Add(checkingTile);
                                        checkingTile.myGameObject.transform.parent = tilecluster[0].myGameObject.transform.parent;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (!foundTile && !reCheck) {
            FollowTile(tiles[recheckX, recheckY], recheckX, recheckY, parent, true);
        }
    }

    /// <summary>
    /// Calculates the four vertices of each tile and adds them to a dictionary.
    /// </summary>
    void CalculateNodes() {
        Dictionary<Vector2, List<Tile>> nodesdictionary;
        List<Vector2> positions;

        //if (myRoom.DoorCount == 1) {
        //    Debug.LogError("NEW ROOM | " + gridPos);
        //}
        foreach (List<Tile> tileCluster in tileClustersList) {
            nodesdictionary = new Dictionary<Vector2, List<Tile>>();
            positions = new List<Vector2>();

            foreach (Tile tile in tileCluster) {
                Vector2[] nodes = new Vector2[4];

                nodes[0] = tile.realPosition + (Vector2.up * tileSize * 0.5f) + (Vector2.left * tileSize * 0.5f);
                nodes[1] = tile.realPosition + (Vector2.up * tileSize * 0.5f) + (Vector2.right * tileSize * 0.5f);
                nodes[2] = tile.realPosition + (Vector2.down * tileSize * 0.5f) + (Vector2.right * tileSize * 0.5f);
                nodes[3] = tile.realPosition + (Vector2.down * tileSize * 0.5f) + (Vector2.left * tileSize * 0.5f);

                foreach (Vector2 node in nodes) {
                    if (!nodesdictionary.ContainsKey(node)) {
                        nodesdictionary.Add(node, new List<Tile>());
                        positions.Add(node);
                    }          
                    nodesdictionary[node].Add(tile);
                }
            }

            ConnectNodes(nodesdictionary, positions, tileCluster[0].myGameObject.transform.parent.gameObject);
        }
    }

    /// <summary>
    /// Checks for nodes that are connected to each other, in order to put a collider between them. 
    /// Tries to only search for edges.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <param name="positions"></param>
    /// <param name="gObject"></param>
    void ConnectNodes(Dictionary<Vector2, List<Tile>> dictionary, List<Vector2> positions, GameObject gObject) {
        List<Vector2> edges = new List<Vector2>();
        HashSet<Vector2> checkedVectors = new HashSet<Vector2>();

        Vector2 lastChange = -Vector2.one;

        EdgeCollider2D edgeCollider = gObject.AddComponent<EdgeCollider2D>();
        edgeCollider.tag = doorWall.tag;
        gObject.layer = doorWall.layer;

        //if (myRoom.DoorCount == 1) {
        //    Debug.LogWarning("New cluster");
        //}

        foreach (Vector2 position in positions) {
            if (dictionary[position].Count == 0 || dictionary[position].Count >= 4) {
                continue;
            }
            if (checkedVectors.Contains(position)) {
                continue;
            }

            Vector2 checkedPosition = position;
            bool foundPartner;
            do {            
                Vector2 direction = Vector2.zero;
                foundPartner = false;

                for (float x = -tileSize; x <= tileSize; x += TileSize) {
                    for (float y = -tileSize; y <= tileSize; y += tileSize) {
                        if ((x == 0 && y == 0) || (x != 0 && y != 0)) {
                            continue;
                        }

                        direction = new Vector2(x, y);

                        if (checkedVectors.Contains(checkedPosition + direction)) {
                                continue;
                        }

                        if (dictionary.ContainsKey(checkedPosition + direction) && foundPartner == false) {
                            if (myRoom.DoorCount == 1) {
                                //Debug.Log("Checked Position: " + checkedPosition + ", Other: " + checkedPosition + direction);
                            }
                            foundPartner = SameSquareSharing(dictionary[checkedPosition], dictionary[checkedPosition + direction]);

                            
                            if (foundPartner) {
                                edges.Add(checkedPosition);
                                if (myRoom.DoorCount == 1) {
                                    //Debug.LogWarning(edges[edges.Count - 1]);
                                }
                                checkedVectors.Add(checkedPosition);
                                break;
                            }
                        }
                    }
                    if (foundPartner) {
                        break;
                    }
                }
                if (foundPartner) {
                    checkedPosition = checkedPosition + direction;
                    lastChange = checkedPosition;
                }
            } while (!checkedVectors.Contains(checkedPosition) && foundPartner);
        }
        if (!edges.Contains(lastChange) && lastChange != -Vector2.one) {
            edges.Add(lastChange);
        }
        edges.Add(edges[0]);

        edgeCollider.points = PolishEdges(edges.ToArray());
    }

    /// <summary>
    /// Checks for sudden changes in the direction of the edges, so the collider that will be created has less points.
    /// </summary>
    /// <param name="edges"></param>
    /// <returns></returns>
    Vector2[] PolishEdges(Vector2[] edges) {

        List<Vector2> corners = new List<Vector2>();

        float initialX = 0, initialY = 0;
        float lastX = 0, lastY = 0;
        bool staticX = false, changeX = false;

        for (int i = 0; i < edges.Length; i++) {

            if (i == 0) {
                initialX = edges[i].x;
                initialY = edges[i].y;
                continue;
            }

            if (i == 1) {
                if (initialX == edges[i].x) {
                    staticX = true;
                    changeX = false;
                }
                if (initialY == edges[i].y) {
                    staticX = false;
                    changeX = true;
                }
            }

            if (lastX == edges[i].x) {
                staticX = true;
            }
            if (lastY == edges[i].y) {
                staticX = false;
            }

            if (staticX == changeX) {
                changeX = !staticX;
                corners.Add(edges[i - 1]);
            }

            lastX = edges[i].x;
            lastY = edges[i].y;
        }

        if (lastX == edges[1].x) {
            staticX = true;
        }
        if (lastY == edges[1].y) {
            staticX = false;
        }

        if (staticX == changeX) {
            //Debug.Log("H: " + lastX);
            //Debug.Log("V: " + lastY);
            changeX = !staticX;
            corners.Add(edges[0]);
        }

        corners.Add(corners[0]);

        //if (myRoom.DoorCount == 1) {
        //    Debug.LogWarning("New cornes");
        //}
        //if (myRoom.DoorCount == 1) {
        //    foreach (Vector2 corner in corners) {
        //        Debug.Log(corner);
        //    }
        //}

        return corners.ToArray();
    }

    /// <summary>
    /// Returns true if less than 2 squares share the same node
    /// </summary>
    /// <param name="tiles"></param>
    /// <param name="otherTiles"></param>
    /// <returns></returns>
    bool SameSquareSharing(List<Tile> tiles, List<Tile> otherTiles) {
        int sharedSquares = 0;
        foreach (Tile tile in tiles) {
            foreach (Tile otherTile in otherTiles) {
                if (tile == otherTile) {
                    sharedSquares++;
                }
            }
        }
        //if (myRoom.DoorCount == 1) {
        //    if (sharedSquares < 1) {
        //        Debug.Log(sharedSquares);
        //    }
        //}
        return sharedSquares == 1;
    }

    /// <summary>
    /// Gets the world position equivalent to the grid position of each tile.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    Vector3 PositionFromTileGrid(int x, int y) {
        Vector3 ret;
        //Vector3 offset = new Vector3((-roomSizeInTiles.x + 1) * tileSize,
        //                            (roomSizeInTiles.y/4) * tileSize - (tileSize/4), 0);
        //ret = new Vector3(tileSize * (float)x, -tileSize * (float)y, 0) + offset + transform.position;

        Vector3 offset = new Vector3(-roomSizeInTiles.x * tileSize * 0.5f + tileSize * 0.5f, -roomSizeInTiles.y * tileSize * 0.5f + tileSize * 0.5f, 0);
        ret = new Vector3(tileSize * (float)x, tileSize * (float)y, 0) + offset + transform.position;
        return ret;
    }

    /// <summary>
    /// Places door objects if there are rooms connected to this one.
    /// </summary>
    /// <param name="spawnPos"></param>
    /// <param name="doorExit"></param>
    /// <param name="doorSpawn"></param>
    /// <param name="spawnRot"></param>
    void PlaceDoor(Vector3 spawnPos, Room doorExit, GameObject doorSpawn, Quaternion spawnRot) {
        if (doorExit != null) {
            GameObject door = Instantiate(doorSpawn, spawnPos, spawnRot) as GameObject;
            door.transform.parent = transform;
            doorTriggers.Insert(0, door.GetComponent<DoorTrigger>());

            int xTile = 0, yTile = 0;

            if (spawnPos.y > 0) {
                xTile = tiles.GetLength(0) / 2;
                yTile = tiles.GetLength(1) - 1;
                tiles[tiles.GetLength(0) / 2, tiles.GetLength(1) - 1] = new Tile(spawnPos, tiles.GetLength(0) / 2, tiles.GetLength(1) - 1, Tile.Type.door);
            } else if (spawnPos.y < 0) {
                xTile = tiles.GetLength(0) / 2;
                yTile = 0;
                tiles[tiles.GetLength(0) / 2, 0] = new Tile(spawnPos, tiles.GetLength(0) / 2, 0, Tile.Type.door);
            } else if (spawnPos.x < 0) {
                xTile = 0;
                yTile = tiles.GetLength(1) / 2;
                tiles[0, tiles.GetLength(1) / 2] = new Tile(spawnPos, 0, tiles.GetLength(1) / 2, Tile.Type.door);
            } else if (spawnPos.x > 0) {
                xTile = tiles.GetLength(0) - 1;
                yTile = tiles.GetLength(1) / 2;
                tiles[tiles.GetLength(0) - 1, tiles.GetLength(1) / 2] = new Tile(spawnPos, tiles.GetLength(0) - 1, tiles.GetLength(1) / 2, Tile.Type.door);
            }
            tiles[xTile, yTile] = new Tile(spawnPos, xTile, yTile, Tile.Type.door);
            tiles[xTile, yTile].myGameObject = door;


            doorTriggers[0].SetConnectedRoom(doorExit);
            if (doorExit == connectedDownRoom) {
                doorTriggers[0].myDirection = DoorTrigger.Direction.down;
                return;
            }
            if (doorExit == connectedLeftRoom) {
                doorTriggers[0].myDirection = DoorTrigger.Direction.left;
                return;
            }
            if (doorExit == connectedRightRoom) {
                doorTriggers[0].myDirection = DoorTrigger.Direction.right;
                return;
            }
            if (doorExit == connectedUpRoom) {
                doorTriggers[0].myDirection = DoorTrigger.Direction.up;
                return;
            }

        } else {
            if (spawnPos.y > 0) {
                tiles[tiles.GetLength(0) / 2, tiles.GetLength(1) - 1] = new Tile(spawnPos, tiles.GetLength(0) / 2, tiles.GetLength(1) - 1, Tile.Type.wall);
                tiles[tiles.GetLength(0) / 2, tiles.GetLength(1) - 1].myGameObject = Instantiate(doorWall, spawnPos, Quaternion.identity);
                tiles[tiles.GetLength(0) / 2, tiles.GetLength(1) - 1].myGameObject.transform.parent = transform;
            } else if (spawnPos.y < 0) {
                tiles[tiles.GetLength(0) / 2, 0] = new Tile(spawnPos, tiles.GetLength(0) / 2, 0, Tile.Type.wall);
                tiles[tiles.GetLength(0) / 2, 0].myGameObject = Instantiate(doorWall, spawnPos, Quaternion.identity);
                tiles[tiles.GetLength(0) / 2, 0].myGameObject.transform.parent = transform;
            } else if (spawnPos.x < 0) {
                tiles[0, tiles.GetLength(1) / 2] = new Tile(spawnPos, 0, tiles.GetLength(1) / 2, Tile.Type.wall);
                tiles[0, tiles.GetLength(1) / 2].myGameObject = Instantiate(doorWall, spawnPos, Quaternion.identity);
                tiles[0, tiles.GetLength(1) / 2].myGameObject.transform.parent = transform;
            } else if (spawnPos.x > 0) {
                tiles[tiles.GetLength(0) - 1, tiles.GetLength(1) / 2] = new Tile(spawnPos, tiles.GetLength(0) - 1, tiles.GetLength(1) / 2, Tile.Type.wall);
                tiles[tiles.GetLength(0) - 1, tiles.GetLength(1) / 2].myGameObject = Instantiate(doorWall, spawnPos, Quaternion.identity);
                tiles[tiles.GetLength(0) - 1, tiles.GetLength(1) / 2].myGameObject.transform.parent = transform;
            }
        }
    }

    public void ConnectDoors() {
        foreach (DoorTrigger doorTrigger in doorTriggers) {
            doorTrigger.SetExit();
            doorTrigger.StrechToExtraSpace();
        }
    }
}
