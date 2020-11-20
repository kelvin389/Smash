using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public CapsuleCollider col;

    public float dashSpeed;
    public float runSpeed;
    public float dashLength;
    public float friction;

    public bool tapJump;
    public float mass;
    public float fullJumpForce;
    public float shortJumpForce;
    public float fastFallMultiplier;
    public int maxAirJumps;
    public float jumpSquatLength;

    public float airDodgeForce;
    public float airDodgeLength;
    public float airDodgeStoppingSpeed;

    private float dashStartTime = 0f;

    private float airDodgeStartTime = 0f;

    private int airJumps = 0;
    private float jumpSquatStart = 0f;
    private bool inJumpSquat = false;
    private bool grounded = false;
    private bool fastFalling = false;
    private bool bufferedJump = false;

    private bool airDodging = false;
    private bool helpless = false;

    private PlayerControls controls;
    private Vector2 input = Vector2.zero;
    private float jumpForce;

    void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.LeftStick.performed += ctx => {
            if (airDodging) return; // can input while freefalling so we dont call Actionable()

            input = ctx.ReadValue<Vector2>();
            if (input.x != 0) HorizontalInputHandler();
            if (input.y != 0) VerticalInputHandler();
        };

        controls.Gameplay.LeftStick.canceled += ctx => input = Vector2.zero;

        controls.Gameplay.Jump.started += ctx => { // when jump button is pressed, full hop is prepped
            if (!Actionable()) return;

            JumpInputHandler();
            jumpForce = fullJumpForce;
        };
        controls.Gameplay.Jump.canceled += ctx => jumpForce = shortJumpForce; // if jump button is released in time, short hop force will be loaded

        controls.Gameplay.Shield.started += ctx => {
            if (!Actionable()) return;

            ShieldInputHandler();
        };
    }

    void OnEnable()
    {
        controls.Gameplay.Enable();
    }
    void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    void Start()
    {
        col.material.dynamicFriction = friction;
    }

    void Update()
    {
        if (Time.time - jumpSquatStart > jumpSquatLength && bufferedJump) // not >= b/c you are airborne the frame AFTER jumpsquat is over
        {
            TryJump();
            bufferedJump = false;
        }

        // Debug.Log(rb.velocity);
        // col.material.dynamicFriction = friction;
        if (airDodging)
        {                
            if (Time.time - airDodgeStartTime >= airDodgeLength || grounded)
            {
                StopAirDodge();
            }
            else
            {
                AirDodgeMove();
                return; // do not allow further input during an airdodge
            }
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

    private bool Actionable()
    {
        return !airDodging && !helpless;
    }

    private void HorizontalInputHandler()
    {
        float speed = dashSpeed;

        // set dashStartTime whenever a movement key is pushed
        // if (Input.GetButtonDown("Horizontal") && (input.x < 0 || input.x > 0))
        // {
            dashStartTime = Time.time;
        // }

        // if we have been moving in one direction for more than dashLength, then we should be at running speed
        if (Time.time - dashStartTime >= dashLength)
        {
            speed = runSpeed;
        }

        // read current velocity
        Vector3 vel = rb.velocity;
        // modify x velocity
        vel.x = input.x * speed;

        // set velocity
        rb.velocity = vel;
    }

    private void VerticalInputHandler()
    {
        // if (Input.GetButtonDown("Vertical"))
        // {
            if (input.y > 0 && tapJump) // up
            {
                JumpInputHandler();
            }
            if (input.y < 0) // down
            {
                if (isFalling())
                {
                    fastFalling = true;
                }
            }
        // }
    }

    private void AirDodgeMove()
    {
        rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * airDodgeStoppingSpeed);
    }

    private void ShieldInputHandler()
    {
        if (grounded && !inJumpSquat)
        {
            // shield
        }
        else
        {
            // if tech
            // else if l-cancel

            // else
            TryAirDodge();
        }
    }

    private void JumpInputHandler()
    {
        if (grounded)
        {
            // dont allow for overwriting jump inputs
            if (!bufferedJump)
            {
                jumpSquatStart = Time.time;
                inJumpSquat = true;
                bufferedJump = true;
            }
        }
        else
        {
            TryJump();
        }
    }

    private bool CanJump()
    {
        return airJumps > 0 || grounded;
    }

    private void TryJump()
    {
        if (!CanJump()) return;

        // no longer in jump squat because we will jump this frame. buffered jump should be reset in parent function
        inJumpSquat = false;

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

    private bool isFalling()
    {
        return rb.velocity.y <= 0 && !grounded;
    }

    private void TryAirDodge()
    {
        rb.useGravity = false; // disable gravity
        input *= airDodgeForce; // multiply input by force
        rb.velocity = input; // overwrite current velocity with airdodge velocity

        airDodgeStartTime = Time.time;
        airDodging = true;

        // in case of wave dash
        bufferedJump = false;
        inJumpSquat = false;
    }

    private void StopAirDodge()
    {
        rb.useGravity = true;
        airDodging = false;
        helpless = true;
    }

    // TEMPORARY
    // TEMPORARY
    // TEMPORARY
    void OnCollisionEnter(Collision collision)
    {
        TouchingGround(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        TouchingGround(collision);
    }

    private void TouchingGround(Collision collision)
    {
        if (collision.gameObject.name == "floor")
        {
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
     void OnCollisionExit(Collision collision)
     {
         if (collision.gameObject.name == "floor")
         {
             grounded = false;
         }
     }

}
