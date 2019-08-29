using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    //Necesary Components
    [SerializeField]
    WeaponHolder weaponHolder;

    //Player characteristics
    [SerializeField]
    float stopTime = 1f;
    [SerializeField]
    int acceleration = 50;
    [SerializeField]
    int maxVelocity = 100;

    [Space(5)]

    [SerializeField]
    float jumpForce = 30;
    [SerializeField]
    float keepInAirForce = 20;
    [SerializeField]
    float doubleJumpForce = 30;
    [SerializeField]
    float maxJumpTime = 0.5f;

    [Space(5)]

    [SerializeField]
    float fallForce = 10;
    [SerializeField]
    float fallMax = 28;

    [Space(5)]
    [SerializeField]
    int maxLife = 5;
    [SerializeField]
    float invincibilityTime = 2;

    [Space(5)]
    [SerializeField]
    float knockbackForce = 10;
    [SerializeField]
    float knockbackTime = 0.5f;

    Collider2D mycollider;

    //Flags
    bool stopPlayer; //Stop all Input
    bool playerMovingH; //Is the player moving Horizontally?
    bool stopLeftInput, stopRightInput; //Should we stop horizontal input?
    bool grounded; //Is on the ground?
    bool jumping; //Is the player Jumping?
    bool canJumpHigher; //Can the player go higher by keeping the jump button pressed?
    bool jumpAgain; //Can he make a second jump?
    bool hasWastedJumps; //Has the player jumped twice?
    bool onKnockback; //Is the player on knockback?
    bool hasBeenHit; //Has the player just been hit?

    //Useful stuff
    Rigidbody2D rb2d;
    Room currentRoom;
    RoomInstance currentRoomInstance;

    //Holders
    float gravityScale;

    int currentLife;

    public RoomInstance GetCurrentRoomInstance {
        get {
            return currentRoomInstance;
        }
    }

    public delegate void PlayerChanges();
    public static event PlayerChanges OnPlayerChangedRooms;

    private void Start() {
        rb2d = GetComponent<Rigidbody2D>();
        mycollider = GetComponent<Collider2D>();
        gameObject.SetActive(false);

        currentRoom = null;
        stopPlayer = true;

        playerMovingH = false;
        stopRightInput = false;
        stopLeftInput = false;
        jumping = false;
        jumpAgain = false;
        hasWastedJumps = false;

        currentLife = maxLife;

        gravityScale = rb2d.gravityScale;
    }

    public void SetStartPosition (Room startRoom) {
        weaponHolder.Initialize();

        Vector3 newPosition = GameObject.FindWithTag("Start Point").transform.position;
        ChangePlayerRoom(startRoom);

        transform.position = newPosition;
        gameObject.SetActive(true);

        stopPlayer = false;
    }

    public void ChangePlayerRoom(Room newRoom) {
        currentRoom = newRoom;
        Debug.Log("Player is now at Room " + currentRoom.gridPos);

        currentRoomInstance = GameController.instance.GetRoomInstanceByRoomGridPos(currentRoom.gridPos);
        if (currentRoomInstance == null) {
            Debug.LogError("Player currently in non-existent room Instance");
        }

        OnPlayerChangedRooms();
    }

    public void Move(float h, float v) {
        if (stopPlayer || onKnockback)
            return;

        ChangePlayerDirection(h);

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
            rb2d.velocity *= new Vector2(0, 1);
        }
        if (stopRightInput && movement.x > 0) {
            movement = Vector2.zero;
            rb2d.velocity *= new Vector2(0, 1);
        }

        StopContraryDirection(movement.x);

        rb2d.AddForce(movement * (acceleration / Time.deltaTime), ForceMode2D.Force);
        if (Mathf.Abs(rb2d.velocity.x) > maxVelocity) {
            //Debug.Log("MAX: " + rb2d.velocity);
            rb2d.velocity = new Vector2(maxVelocity * (Mathf.Sign(rb2d.velocity.x)), rb2d.velocity.y);
        }
    }

    public void Jump(bool jumpButtonPressed) {
        if (stopPlayer || onKnockback)
            return;

        CheckGrounded();
        Fall();

        if (grounded) {
            jumping = false;
            jumpAgain = false;
            hasWastedJumps = false;
        }

        if (!jumpButtonPressed && !grounded && jumping) {
            canJumpHigher = false;
            StopCoroutine("StopVertical");
        }

        if (jumpButtonPressed && grounded && !jumping) {
            grounded = false;
            rb2d.AddForce(Vector2.up * (jumpForce / Time.fixedDeltaTime), ForceMode2D.Force);
            jumping = true;
            canJumpHigher = true;

            StopCoroutine("StopVertical");
            StartCoroutine("StopVertical");

        } else if (jumpButtonPressed && canJumpHigher) {
            rb2d.AddForce(Vector2.up * (keepInAirForce / Time.fixedDeltaTime), ForceMode2D.Force); //If the player keeps pressng jump, he gets higher
        }

        if (!canJumpHigher && !grounded) {
            if (!jumpButtonPressed) {
                jumpAgain = true;
            }
            if (jumpButtonPressed) {
                DoubleJump();
            }
        }
    }

    public void DoubleJump() {
        if (stopPlayer)
            return;

        if (hasWastedJumps || !jumpAgain) { return; }

        ResetGravityScale();

        jumping = true;
        rb2d.velocity = new Vector2(rb2d.velocity.x, 0);
        rb2d.AddForce(Vector2.up * (doubleJumpForce / Time.fixedDeltaTime), ForceMode2D.Impulse);
        hasWastedJumps = true;
    }

    //Initiates the animation of current Weapon
    public void Attack(bool attacking) {
        if (stopPlayer || onKnockback)
            return;

        if (weaponHolder.IsAttacking) {
            return;
        }

        if (attacking) {
            if (!weaponHolder.IsAttacking) {
                weaponHolder.StartAttack();
            } 
        }
    }

    public void ReceiveDamage(int damage, Vector3 hitPosition) {
        if (hasBeenHit)
            return;

        Knockback(hitPosition);
        StartCoroutine("KnockbackTimeCount");

        StartCoroutine("MakeInvincible");

        currentLife -= damage;
        Debug.Log("DAMAGED! for " + damage);

        if (currentLife <= 0) {
            LostAllLife();
        }
    }



    void LostAllLife() {
        Debug.Log("DEAD!");
    }

    void Knockback(Vector3 hitPosition) {
        Vector2 direction;

        if (hitPosition.x <= transform.position.x) {
            direction = Vector2.right;
        } else {
            direction = Vector2.left;
        }

        rb2d.drag = 0;
        rb2d.velocity = Vector2.zero;
        rb2d.angularVelocity = 0;
        StopCoroutine("StopVertical");
        StopCoroutine("StopHorizontal");


        rb2d.AddForce(Vector2.up * knockbackForce * 0.5f, ForceMode2D.Impulse);
        rb2d.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }

    void Fall() {

        if (grounded) {
            ResetGravityScale();
            return;
        }
        if (jumping || !grounded) {
            rb2d.gravityScale += fallForce * Time.fixedDeltaTime;

            if (rb2d.gravityScale >= fallMax) {
                rb2d.gravityScale = fallMax;
            }
        }
    }

    void ResetGravityScale() {
        if (rb2d.gravityScale != gravityScale) {
            rb2d.gravityScale = gravityScale;
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
        RaycastHit2D[] downHits = new RaycastHit2D[] {
            Physics2D.Raycast(transform.position, Vector2.down, mycollider.bounds.extents.y + 0.1f, 1 << LayerMask.NameToLayer("Obstacle")),
            Physics2D.Raycast(transform.position + (Vector3.right * mycollider.bounds.extents.x), Vector2.down, mycollider.bounds.extents.y + 0.1f, 1 << LayerMask.NameToLayer("Obstacle")),
            Physics2D.Raycast(transform.position + (Vector3.left * mycollider.bounds.extents.x), Vector2.down, mycollider.bounds.extents.y + 0.1f, 1 << LayerMask.NameToLayer("Obstacle"))
        };

        int detectedFloor = 0;

        foreach (RaycastHit2D downHit in downHits) {
            if (downHit.collider != null) {
                detectedFloor++;
            } 
        }

        if (detectedFloor == 0) {
            grounded = false;
            return;
        }
        //if (detectedFloor == 1 && grounded == true) {
        //    grounded = true;
        //} else if (detectedFloor == 1 && grounded == false) {
        //    grounded = false;
        //} 
        //else 
        
        if (detectedFloor >= 1) {
            grounded = true;
        }
    }

    void ChangePlayerDirection(float h) {
        if (weaponHolder.IsAttacking)
            return;

        if (h > 0) {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        } else if (h < 0) {
            transform.rotation = Quaternion.Euler(0, 180, 0);

        }
    }

    IEnumerator MakeInvincible() {
        hasBeenHit = true;
        yield return new WaitForSeconds(invincibilityTime);
        hasBeenHit = false;
    }

    IEnumerator KnockbackTimeCount() {
        onKnockback = true;
        yield return new WaitForSeconds(knockbackTime);
        onKnockback = false;
    }
}
