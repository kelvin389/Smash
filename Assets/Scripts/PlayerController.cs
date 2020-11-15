using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public float dashSpeed;
    public float runSpeed;
    public float dashLength;

    public float mass;
    public float jumpForce;
    public float fastFallMultiplier;
    public int maxAirJumps;

    public float airDodgeForce;
    public float airDodgeLength;
    public float airDodgeSpeed;

    private float dashStartTime = 0f;

    private float airDodgeStartTime = 0f;

    private int airJumps = 0;
    private bool grounded = false;
    private bool fastFalling = false;

    private bool airDodging = false;
    private bool helpless = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (airDodging)
        {
            if (Time.time - airDodgeStartTime >= airDodgeLength || grounded)
            {
                helpless = true;
                rb.useGravity = true;
                airDodging = false;
            }
            else
            {
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime / airDodgeSpeed);
                return;
            }
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float speed = dashSpeed; // assume we are dashing by default

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (grounded)
            {
                // shield
            }
            else
            {
                AirDodge(input);
                return;
            }
        }

        // set dashStartTime whenever a movement key is pushed
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A))
        {
            dashStartTime = Time.time;
        }
        // if we have been moving in one direction for more than dashLength, then we should be at running speed
        if (Time.time - dashStartTime >= dashLength)
        {
            speed = runSpeed;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            TryJump();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (!grounded && isFalling())
            {
                fastFalling = true;
            }
        }

        MoveX(input.x, speed);
    }

    private void FixedUpdate()
    {
        if (rb.useGravity)
        {
            // if fast falling, use multiplier, otherwise use base mass
            rb.mass = fastFalling ? mass * fastFallMultiplier : mass;
            rb.AddForce(Physics.gravity * rb.mass * rb.mass);
        }
    }

    private void MoveX(float axis, float speed)
    {
        // read current velocity
        Vector3 vel = rb.velocity;
        // modify x velocity
        vel.x = axis * speed;

        // set velocity
        rb.velocity = vel;
    }

    private void TryJump()
    {
        if (!CanJump()) return;

        // if try jump while airborne, subtract from number of airjumps
        if (!grounded) airJumps -= 1;

        // adjust so that jump height is consistent regardless of gravity
        float adjustedJumpForce = jumpForce * rb.mass;
        // read current velocity
        Vector3 vel = rb.velocity;
        // modify y velocity
        vel.y = adjustedJumpForce;

        // set velocity
        rb.velocity = vel;
    }

    private bool CanJump()
    {
        return airJumps > 0 || grounded;
    }

    private bool isFalling()
    {
        return rb.velocity.y <= 0;
    }

    private void AirDodge(Vector2 direction)
    {
        rb.useGravity = false; // disable gravity
        direction *= airDodgeForce;
        rb.velocity = direction;

        airDodgeStartTime = Time.time;
        airDodging = true;
    }

    void OnCollisionEnter(Collision theCollision)
    {
        if (theCollision.gameObject.name == "floor")
        {
            grounded = true;
            airJumps = maxAirJumps; // restore air jumps when landing
            fastFalling = false;

            helpless = false;
        }
    }
     
     //consider when character is jumping .. it will exit collision.
     void OnCollisionExit(Collision theCollision)
     {
         if (theCollision.gameObject.name == "floor")
         {
             grounded = false;
         }
     }

}
