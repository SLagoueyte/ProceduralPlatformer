using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    [SerializeField]
    Cinemachine.CinemachineVirtualCamera camera;

    [SerializeField]
    int velocity = 50;

    private void Start() {
        gameObject.SetActive(false);
        camera.gameObject.SetActive(false);
        LevelGenerator.OnFinished += SetPosition;
    }

    public void SetPosition () {
        Vector3 newPosition = GameObject.FindWithTag("Start Point").transform.position;

        transform.position = newPosition;
        gameObject.SetActive(true);
    
        camera.gameObject.SetActive(true);
    }

    public void Move(float h, float v) {
        Vector2 movement = new Vector2(h, v).normalized;
        transform.Translate(movement * velocity * Time.deltaTime);
    }
}
