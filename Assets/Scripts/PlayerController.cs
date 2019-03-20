using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    [SerializeField]
    Cinemachine.CinemachineVirtualCamera camera;

    [SerializeField]
    float stopTime = 1f;
    [SerializeField]
    int acceleration = 50;
    [SerializeField]
    int jumpForce = 30;
    [SerializeField]
    int keepInAirForce = 20;
    [SerializeField]
    int maxVelocity = 100;
    [SerializeField]
    float maxJumpTime = 0.5f;

    Collider2D mycollider;

    //Flags
    bool playerMovingH; //Is the player moving Horizontally?
    bool stopLeftInput, stopRightInput; //Should we stop horizontal input?
    bool grounded; //Is on the ground?
    bool jumping; //Is the player Jumping?
    bool canJumpHigher; //Can the player go higher by keeping the jump button pressed?
    //bool stopping; //Has the player changed directions quickly?

    Rigidbody2D rb2d;

    private void Start() {
        rb2d = GetComponent<Rigidbody2D>();
        mycollider = GetComponent<Collider2D>();
        gameObject.SetActive(false);
        camera.gameObject.SetActive(false);
        LevelGenerator.OnFinished += SetPosition;

        playerMovingH = false;
        stopRightInput = false;
        stopLeftInput = false;
    }

    public void SetPosition () {
        Vector3 newPosition = GameObject.FindWithTag("Start Point").transform.position;

        transform.position = newPosition;
        gameObject.SetActive(true);
    
        camera.gameObject.SetActive(true);
    }

    public void Move(float h, float v) {
        Vector2 movement = new Vector2(h, 0).normalized;

        if (v > 0) {

        }

        if (movement.x != 0 && !playerMovingH) {
            //Debug.LogWarning("STOP STOPPING");
            playerMovingH = true;
            StopCoroutine("StopHorizontal");
        }

        if (movement.x == 0 && playerMovingH) {
            playerMovingH = false;
            //Debug.LogError("START STOPPING");
            StopCoroutine("StopHorizontal");
            StartCoroutine("StopHorizontal");
            return;
        } else if (movement.x == 0 && !playerMovingH) {
            return;
        }

        CheckIfHittingwall();

        if (stopLeftInput && movement.x < 0) {
            movement = Vector2.zero;
        }
        if (stopRightInput && movement.x > 0) {
            movement = Vector2.zero;
        }

        StopContraryDirection(movement.x);

        rb2d.AddForce(movement * (acceleration / Time.deltaTime), ForceMode2D.Force);
        if (Mathf.Abs(rb2d.velocity.x) > maxVelocity) {
            //Debug.Log("MAX: " + rb2d.velocity);
            rb2d.velocity = new Vector2(maxVelocity * (Mathf.Sign(rb2d.velocity.x)), rb2d.velocity.y);
        }
    }

    public void Jump(bool jumpButtonPressed) {
        CheckGrounded();

        if (grounded) {
            jumping = false;
        }

        if (!jumpButtonPressed && !grounded && jumping) {
            canJumpHigher = false;
            StopCoroutine("StopVertical");
        }

        if (jumpButtonPressed && grounded && !jumping) {
            Debug.Log("JUMP");
            grounded = false;
            rb2d.AddForce(Vector2.up * (jumpForce), ForceMode2D.Force);
            jumping = true;
            canJumpHigher = true;

            StopCoroutine("StopVertical");
            StartCoroutine("StopVertical");

        } else if (jumpButtonPressed && canJumpHigher) {
            Debug.Log("JUMP MORE");
            rb2d.AddForce(Vector2.up * (keepInAirForce), ForceMode2D.Force);
        }
    }

    IEnumerator StopHorizontal() {
        Vector2 stopVelocity;
        float startTime = Time.time;
        float stopLenght = Mathf.Abs(rb2d.velocity.x);

        //Debug.Log(stopLenght);

        while (rb2d.velocity.x != 0) {
            float timeToStop = (Time.time - startTime) * stopTime;
            float fracStop = timeToStop / stopLenght;

            //Debug.Log(timeToStop + " | " + fracStop);

            stopVelocity = new Vector2(0, rb2d.velocity.y);
            rb2d.velocity = Vector2.Lerp(rb2d.velocity, stopVelocity, fracStop);

            yield return new WaitForFixedUpdate();
        }
        canJumpHigher = false;

        yield break;
    }

    void StopContraryDirection(float playerDirection) {
        if (Mathf.Sign(rb2d.velocity.x) != Mathf.Sign(playerDirection)) {
            rb2d.velocity = new Vector2(0, rb2d.velocity.y);
        }
    }

    IEnumerator StopVertical() {
        float timeUntilStopJumping = 0f;

        while (timeUntilStopJumping < maxJumpTime) {
            timeUntilStopJumping += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        canJumpHigher = false;

        yield break;
    }

    void CheckIfHittingwall() {
        RaycastHit2D[] righthits = new RaycastHit2D[] {
            Physics2D.Raycast(transform.position, Vector2.right, mycollider.bounds.extents.x + 0.1f, 1 << LayerMask.NameToLayer("Obstacle")),
            Physics2D.Raycast(transform.position + (Vector3.up * mycollider.bounds.extents.y), Vector2.right, mycollider.bounds.extents.x + 0.1f, 1 << LayerMask.NameToLayer("Obstacle")),
            Physics2D.Raycast(transform.position + (Vector3.down * mycollider.bounds.extents.y), Vector2.right, mycollider.bounds.extents.x + 0.1f, 1 << LayerMask.NameToLayer("Obstacle"))
        };
        RaycastHit2D[] leftHits = new RaycastHit2D[] {
        Physics2D.Raycast(transform.position, Vector2.left, mycollider.bounds.extents.x + 0.1f, 1 << LayerMask.NameToLayer("Obstacle")),
        Physics2D.Raycast(transform.position + (Vector3.up * mycollider.bounds.extents.y), Vector2.left, mycollider.bounds.extents.x + 0.1f, 1 << LayerMask.NameToLayer("Obstacle")),
        Physics2D.Raycast(transform.position + (Vector3.down * mycollider.bounds.extents.y), Vector2.left, mycollider.bounds.extents.x + 0.1f, 1 << LayerMask.NameToLayer("Obstacle"))
        };

        foreach (RaycastHit2D rightHit in righthits) {
            if (rightHit.collider != null) {
                stopRightInput = true;
                stopLeftInput = false;
                return;
            }
        }
        foreach (RaycastHit2D leftHit in leftHits) {
            if (leftHit.collider != null) {
                stopRightInput = false;
                stopLeftInput = true;
                return;
            }
        }
        
        stopRightInput = false;
        stopLeftInput = false;
        
    }

    void CheckGrounded() {
        RaycastHit2D downHit = Physics2D.Raycast(transform.position, Vector2.down, mycollider.bounds.extents.y + 0.1f, 1 << LayerMask.NameToLayer("Obstacle"));

        if (downHit.collider != null) {
            grounded = true;
        } else {
            grounded = false;
        }
    }
}
