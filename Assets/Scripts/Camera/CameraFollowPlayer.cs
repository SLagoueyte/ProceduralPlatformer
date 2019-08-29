using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour {

    [SerializeField]
    float normalFollowSpeed = 0.0001f;
    [SerializeField]
    float extraFollowSpeed = 10;
    [SerializeField]
    float lastFollowSpeed = 20;

    [Space(5)]

    [SerializeField]
    float startFollowDistance = 10;
    [SerializeField]
    float lastFollowDistance = 20;


    float followSpeed;
    Camera thisCamera;
    PlayerController player;
    Sprite bakcground;

    bool followPlayer;
    float xLeft, xRight, yUp, yDown;
    float toCenterDistanceX, toCenterDistanceY;

    private void Start() {
        thisCamera = GetComponent<Camera>();

        followPlayer = false;
    }

    private void OnEnable() {
        PlayerController.OnPlayerChangedRooms += Roomchanged;
    }

    private void OnDisable() {
        PlayerController.OnPlayerChangedRooms -= Roomchanged;
    }

    public void StartFollowing (PlayerController follow) {
        followPlayer = true;
        player = follow;
        GoToPlace(player.transform.position);
        Roomchanged();
    }

    void GoToPlace(Vector2 finalPlace) {
        transform.position = new Vector3(finalPlace.x, finalPlace.y, -10);
    }

    public void Roomchanged() {
        if (player == null) {
            return;
        }

        bakcground = player.GetCurrentRoomInstance.GetComponent<SpriteRenderer>().sprite;
        
        xLeft = player.GetCurrentRoomInstance.transform.position.x - bakcground.bounds.max.x;
        xRight = player.GetCurrentRoomInstance.transform.position.x + bakcground.bounds.max.x;
        yUp = player.GetCurrentRoomInstance.transform.position.y + bakcground.bounds.max.y;
        yDown = player.GetCurrentRoomInstance.transform.position.y - bakcground.bounds.max.y;


        Vector3 cameraRightUp = thisCamera.ViewportToWorldPoint(new Vector3(1, 1, thisCamera.nearClipPlane));

        toCenterDistanceX = Mathf.Abs(cameraRightUp.x - transform.position.x);
        toCenterDistanceY = Mathf.Abs(cameraRightUp.y - transform.position.y);
    }

    private void FixedUpdate() {
        if (!followPlayer) {
            return;
        }

        float distance = Vector2.Distance(player.transform.position, transform.position);

        if (distance > lastFollowDistance) {
            if (followSpeed > lastFollowSpeed) {
                followSpeed -= Time.deltaTime * 2f;
            } else if (followSpeed < lastFollowSpeed) {
                followSpeed += Time.deltaTime * 2f;
                if (followSpeed >= lastFollowSpeed) {
                    followSpeed = lastFollowSpeed;
                }
            }
        } else if (distance > startFollowDistance) {
            if (followSpeed > extraFollowSpeed) {
                followSpeed -= Time.deltaTime;
            } else if (followSpeed < extraFollowSpeed) {
                followSpeed += Time.deltaTime;
                if (followSpeed >= extraFollowSpeed) {
                    followSpeed = extraFollowSpeed;
                }
            }
        } else if (distance > 0.01f) {
            if (followSpeed > normalFollowSpeed) {
                followSpeed -= Time.deltaTime * 0.5f;
            } else if (followSpeed < normalFollowSpeed) {
                followSpeed += Time.deltaTime * 0.5f;
                if (followSpeed >= normalFollowSpeed) {
                    followSpeed = normalFollowSpeed;
                }
            }

        }

        Vector3 newPosition;

        newPosition = Vector2.Lerp(transform.position, player.transform.position, followSpeed * Time.deltaTime);
        newPosition = new Vector3(newPosition.x, newPosition.y, -10);

        int endOfRoom = isEndOfRoom(newPosition);

        switch (endOfRoom) {
            case 0:
            case -1:
                transform.position = newPosition;
                break;

            case 1:
                transform.position = new Vector3(xRight - toCenterDistanceX, newPosition.y, newPosition.z);
                break;

            case 2:
                transform.position = new Vector3(xLeft + toCenterDistanceX, newPosition.y, newPosition.z);
                break;

            case 3:
                transform.position = new Vector3((xRight + xLeft) * 0.5f, newPosition.y, newPosition.z);
                break;

            case 4:
                transform.position = new Vector3(newPosition.x, yUp - toCenterDistanceY, newPosition.z);
                break;

            case 5:
                transform.position = new Vector3(xRight - toCenterDistanceX, yUp - toCenterDistanceY, newPosition.z);
                break;

            case 6:
                transform.position = new Vector3(xLeft + toCenterDistanceX, yUp - toCenterDistanceY, newPosition.z);
                break;

            case 7:
                transform.position = new Vector3((xRight + xLeft) * 0.5f, yUp - toCenterDistanceY, newPosition.z);
                break;

            case 8:
                transform.position = new Vector3(newPosition.x, yDown + toCenterDistanceY, newPosition.z);
                break;

            case 9:
                transform.position = new Vector3(xRight - toCenterDistanceX, yDown + toCenterDistanceY, newPosition.z);
                break;

            case 10:
                transform.position = new Vector3(xLeft + toCenterDistanceX, yDown + toCenterDistanceY, newPosition.z);
                break;

            case 11:
                transform.position = new Vector3((xRight + xLeft) * 0.5f, yDown + toCenterDistanceY, newPosition.z);
                break;

            case 12:
                transform.position = new Vector3(newPosition.x, (yUp + yDown) * 0.5f, newPosition.z);
                break;

            case 13:
                transform.position = new Vector3(xRight - toCenterDistanceX, (yUp + yDown) * 0.5f, newPosition.z);
                break;

            case 14:
                transform.position = new Vector3(xLeft + toCenterDistanceX, (yUp + yDown) * 0.5f, newPosition.z);
                break;

            case 15:
                transform.position = new Vector3((xRight + xLeft) * 0.5f, (yUp + yDown) * 0.5f, newPosition.z);
                break;
        }

        //transform.position = new Vector3(
        //    Mathf.Clamp(newPosition.x, xLeft + toCenterDistanceX, xRight - toCenterDistanceX),
        //    Mathf.Clamp(newPosition.y, yDown + toCenterDistanceY, yUp - toCenterDistanceY),
        //    newPosition.z);
    }

    int isEndOfRoom(Vector3 nextPosition) {
        if (bakcground == null) {
            bakcground = player.GetCurrentRoomInstance.GetComponent<SpriteRenderer>().sprite;
            return -1;
        }

        int returnInt = 0;


        if (nextPosition.x + toCenterDistanceX > xRight) {
            returnInt += 1;
        }

        if (nextPosition.x - toCenterDistanceX < xLeft) {
            returnInt += 2;
        }

        if (nextPosition.y + toCenterDistanceY > yUp) {
            returnInt += 4;
        }

        if (nextPosition.y - toCenterDistanceY < yDown) {
            returnInt += 8;
        }

        return returnInt;
    }

    IEnumerator FollowTo(Vector2 finalPlace, float strenght) {
        float startTime = Time.time;
        float distance = Vector2.Distance(finalPlace, transform.position);

        while (Vector2.Distance(finalPlace, transform.position) > 0.01f) {
            float timeToFollow = (Time.time - startTime) * strenght;
            float fracPlace = timeToFollow / distance;

            transform.position = Vector2.Lerp(transform.position, finalPlace, fracPlace);
            transform.position = new Vector3(transform.position.x, transform.position.y, -10);

            yield return new WaitForFixedUpdate();
        }

        yield break;
    }
}
