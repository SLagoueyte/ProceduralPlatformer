using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSpriteSelector : MonoBehaviour {

    [SerializeField]
    Sprite spUp, spDown, spLeft, spRight,
        spUpDown, spRightLeft, spUpRight, spUpLeft, spDownRight, spDownLeft,
        spUpLeftDown, spLeftDownRight, spDownRightUp, spRightUpLeft,
        spUpRightDownLeft;

    public int doorFlag = 0; //up +1, right +2, down +4, left +8

    public int type; //0 normal, 1 start

    public Color normalColor, enterColor;

    Color mainColor;
    SpriteRenderer rend;
    [SerializeField]
    Room myRoom;

    public void Init(Room _room) {
        myRoom = _room;

        type = myRoom.type;

        if (myRoom.connectedUpRoom != null) {
            doorFlag += 1;
        }
        if (myRoom.connectedRightRoom != null) {
            doorFlag += 2;
        }
        if (myRoom.connectedDownRoom != null) {
            doorFlag += 4;
        }
        if (myRoom.connectedLeftRoom != null) {
            doorFlag += 8;
        }

        //Debug.LogFormat("Room {0}, up {1}, right {2}, down {3}, left {4}", myRoom.gridPos,
        //    myRoom.connectedUpRoom != null ? myRoom.connectedUpRoom.gridPos.ToString() : "null",
        //    myRoom.connectedRightRoom != null ? myRoom.connectedRightRoom.gridPos.ToString() : "null",
        //    myRoom.connectedDownRoom != null ? myRoom.connectedDownRoom.gridPos.ToString() : "null",
        //    myRoom.connectedLeftRoom != null ? myRoom.connectedLeftRoom.gridPos.ToString() : "null");

        rend = GetComponent<SpriteRenderer>();
        mainColor = normalColor;
        PickSprite();
        PickColor();
    }

    void PickSprite() {
        switch (doorFlag) {
            case 0:
                Debug.LogError("Room at " + myRoom.gridPos.ToString() + " has no exits");
                break;
            case 1:
                rend.sprite = spUp;
                break;
            case 2:
                rend.sprite = spRight;
                break;
            case 3:
                rend.sprite = spUpRight;
                break;
            case 4:
                rend.sprite = spDown;
                break;
            case 5:
                rend.sprite = spUpDown;
                break;
            case 6:
                rend.sprite = spDownRight;
                break;
            case 7:
                rend.sprite = spDownRightUp;
                break;
            case 8:
                rend.sprite = spLeft;
                break;
            case 9:
                rend.sprite = spUpLeft;
                break;
            case 10:
                rend.sprite = spRightLeft;
                break;
            case 11:
                rend.sprite = spRightUpLeft;
                break;
            case 12:
                rend.sprite = spDownLeft;
                break;
            case 13:
                rend.sprite = spUpLeftDown;
                break;
            case 14:
                rend.sprite = spLeftDownRight;
                break;
            case 15:
                rend.sprite = spUpRightDownLeft;
                break;
        }
    }

    void PickColor() {
        if (type == 0) {
            mainColor = normalColor;
        } else {
            mainColor = enterColor;
        }
        rend.color = mainColor;
    }

}
