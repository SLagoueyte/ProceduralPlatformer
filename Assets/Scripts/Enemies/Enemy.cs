using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    [SerializeField]
    protected float knockbackForce = 1;
    [SerializeField]
    protected int damageWhenTouched;
    [SerializeField]
    protected int maxLifePoints;
    protected int currentLifePoints;

    Rigidbody2D rb2d;

    private void Start() {
        currentLifePoints = maxLifePoints;
        rb2d = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(int damage, Vector2 hitPosition) {
        currentLifePoints -= damage;
        Knockback(hitPosition);
        Debug.Log("Enemy life: " + currentLifePoints);

        if (currentLifePoints <= 0) {
            Die();
        }
    }

    bool LeftRightZ;
    float EyeScanZ;
    float ViewDistance = 50;

    private void Update() {
        //Code from  jeremysmartcs 
        if (LeftRightZ) {
            if (EyeScanZ < 30) {
                EyeScanZ += 100 * Time.deltaTime;
            } else {
                LeftRightZ = false;
            }
        } else {
            if (EyeScanZ > -30) {
                EyeScanZ -= 100 * Time.deltaTime;
            } else {
                LeftRightZ = true;
            }
        }
        transform.Find("MEyes").transform.localEulerAngles = new Vector3(0, 0, EyeScanZ);


        int layerMask =~ (LayerMask.GetMask("Enemy", "Default") );

        RaycastHit2D hit = Physics2D.Raycast(transform.Find("MEyes").position, transform.Find("MEyes").transform.right * ViewDistance, ViewDistance, layerMask);

        Debug.DrawRay(transform.Find("MEyes").position, transform.Find("MEyes").transform.right * ViewDistance);

        if (hit.collider != null) {
            if (hit.collider.CompareTag("Player")) {
                //Seeing Player
            }

        }
    }

    void Knockback(Vector2 hit) {
        Vector2 direction;

        if (hit.x <= transform.position.x) {
            direction = Vector2.right;
        } else {
            direction = Vector2.left;
        }

        rb2d.AddForce(Vector2.up * knockbackForce * 0.5f, ForceMode2D.Impulse);
        rb2d.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
    }

    void Die() {
        Destroy(this.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            PlayerController player = collision.GetComponent<PlayerController>();
            player.ReceiveDamage(damageWhenTouched, transform.position);
        }
    }
}
