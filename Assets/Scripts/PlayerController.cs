using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public CapsuleCollider col;

    public float dashSpeed;
    public float runSpeed;
    public float dashLength;
    public float friction;

    public float mass;
    public float jumpForce;
    public float fastFallMultiplier;
    public int maxAirJumps;

    public float airDodgeForce;
    public float airDodgeLength;
    public float airDodgeStoppingSpeed;

    private float dashStartTime = 0f;

    private float airDodgeStartTime = 0f;

    private int airJumps = 0;
    private bool inJumpSquat = false;
    private bool grounded = false;
    private bool fastFalling = false;

    private bool airDodging = false;
    private bool helpless = false;

    // Start is called before the first frame update
    void Start()
    {
        col.material.dynamicFriction = friction;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(rb.velocity);
        col.material.dynamicFriction = friction;
        if (airDodging)
        {                
            AirDodgeMove();
            return; // do not allow further input during an airdodge
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (input.x != 0)
            HorizontalInputHandler(input.x);
        if (input.y != 0)
            VerticalInputHandler(input.y);

        if (!helpless)
        {
            ShieldInputHandler(input);
        }

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

    private void HorizontalInputHandler(float xInput)
    {
        float speed = dashSpeed;

        // set dashStartTime whenever a movement key is pushed
        if (Input.GetButtonDown("Horizontal") && (xInput < 0 || xInput > 0))
        {
            dashStartTime = Time.time;
        }

        // if we have been moving in one direction for more than dashLength, then we should be at running speed
        if (Time.time - dashStartTime >= dashLength)
        {
            speed = runSpeed;
        }

        // read current velocity
        Vector3 vel = rb.velocity;
        // modify x velocity
        vel.x = xInput * speed;

        // set velocity
        rb.velocity = vel;
    }

    private void VerticalInputHandler(float yInput)
    {
        if (Input.GetButtonDown("Vertical"))
        {
            if (yInput > 0) // up
            {
                TryJump();
            }
            if (yInput < 0) // down
            {
                if (isFalling())
                {
                    fastFalling = true;
                }
            }
        }
    }

    private void AirDodgeMove()
    {
        if (Time.time - airDodgeStartTime >= airDodgeLength || grounded)
        {
            StopAirDodge();
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * airDodgeStoppingSpeed);
        }
    }

    private void ShieldInputHandler(Vector2 input)
    {
        if (Input.GetButtonDown("Fire3"))
        {
            if (grounded)
            {
                // shield
            }
            else
            {
                // if tech
                // else if l-cancel

                // else
                AirDodge(input);
            }
        }
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
        return rb.velocity.y <= 0 && !grounded;
    }

    private void AirDodge(Vector2 input)
    {
        rb.useGravity = false; // disable gravity
        input *= airDodgeForce; // multiply input by force
        rb.velocity = input; // overwrite current velocity with airdodge velocity

        airDodgeStartTime = Time.time;
        airDodging = true;
    }

    private void StopAirDodge()
    {
        rb.useGravity = true;
        airDodging = false;
        helpless = true;
    }

    void OnCollisionEnter(Collision theCollision)
    {
        if (theCollision.gameObject.name == "floor")
        {
            Debug.Log("A");
            // wavedash
            if (airDodging) 
            {
                StopAirDodge();
            }
            grounded = true;
            airJumps = maxAirJumps; // restore air jumps when landing
            fastFalling = false;

            helpless = false; // also overrides helplessnes from stoping airdodge
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
