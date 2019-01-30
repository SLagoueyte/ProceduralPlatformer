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
    int commonTileSize = 16;

    float tileSize; //Has to be some factor from the roomSizeInTiles numbers

    Vector2 roomSizeInTiles; //The size of the small texture

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
            if (tileWidth == tileHeight) {
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

                    default:
                        tiles[x, y] = new Tile(spawnPos, x, y);
                        break;
                }

                tiles[x,y].myGameObject = Instantiate(mapping.prefab, spawnPos, Quaternion.identity);
                tiles[x, y].myGameObject.transform.parent = this.transform;
            } else {
                //print(mapping.color + " , " + pixelColor);
            }
        }
    }

    void CalculateTileClusters() {
        for (int x = 0; x < tiles.GetLength(0); x++) {
            for (int y = 0; y < tiles.GetLength(1); y++) {
                if (tiles[x,y] == null || (tiles[x, y].myType == Tile.Type.none)) {
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
              
                if (tiles[checkPosX + h, checkPosY + v] != null) {
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

    void CalculateNodes() {
        Dictionary<Vector2, List<Tile>> nodesdictionary;
        List<Vector2> positions;

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

    void ConnectNodes(Dictionary<Vector2, List<Tile>> dictionary, List<Vector2> positions, GameObject gObject) {
        List<Vector2> edges = new List<Vector2>();
        HashSet<Vector2> checkedVectors = new HashSet<Vector2>();

        Vector2 lastChange = -Vector2.one;

        EdgeCollider2D edgeCollider = gObject.AddComponent<EdgeCollider2D>();

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
                            foundPartner = CheckTileNodes(dictionary[checkedPosition], dictionary[checkedPosition + direction]);
                            if (foundPartner) {
                                edges.Add(checkedPosition);
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

        return corners.ToArray();
    }

    bool CheckTileNodes(List<Tile> tiles, List<Tile> otherTiles) {
        int sharedSquares = 0;
        foreach (Tile tile in tiles) {
            foreach (Tile otherTile in otherTiles) {
                if (tile == otherTile) {
                    sharedSquares++;
                }
            }
        }
        return sharedSquares < 2;
    }

    Vector3 PositionFromTileGrid(int x, int y) {
        Vector3 ret;
        //Vector3 offset = new Vector3((-roomSizeInTiles.x + 1) * tileSize,
        //                            (roomSizeInTiles.y/4) * tileSize - (tileSize/4), 0);
        //ret = new Vector3(tileSize * (float)x, -tileSize * (float)y, 0) + offset + transform.position;

        Vector3 offset = new Vector3(-roomSizeInTiles.x * tileSize * 0.5f + tileSize * 0.5f, -roomSizeInTiles.y * tileSize * 0.5f + tileSize * 0.5f, 0);
        ret = new Vector3(tileSize * (float)x, tileSize * (float)y, 0) + offset + transform.position;
        return ret;
    }

    void PlaceDoor(Vector3 spawnPos, Room doorExit, GameObject doorSpawn, Quaternion spawnRot) {
        if (doorExit != null) {
            GameObject door = Instantiate(doorSpawn, spawnPos, spawnRot) as GameObject;
            door.transform.parent = transform;
            doorTriggers.Insert(0, door.GetComponent<DoorTrigger>());

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
        }
    }
}
