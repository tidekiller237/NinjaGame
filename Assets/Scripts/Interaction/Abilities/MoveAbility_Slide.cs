using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAbility_Slide : Ability
{
    PlayerController pc;
    Rigidbody rb;
    Transform tf;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideSpeed;
    public float slideYScale;
    public float slopeMaxSpeed;
    float slideTimer;
    float startYScale;

    float horizontalInput, verticalInput;
    bool sliding;

    private void Start()
    {
        //set up parent parameters
        Type = AbilityType.Movement;

        //initialize variables
        pc = PlayerController.instance;
        rb = pc.GetComponent<Rigidbody>();
        tf = pc.transform;
        startYScale = tf.localScale.y;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(input) && (horizontalInput != 0 || verticalInput != 0))
            StartSlide();

        if (Input.GetKeyUp(input) && sliding)
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (sliding)
            SlideMovement();
    }

    private void StartSlide()
    {
        pc.StartCrouch();
        pc.StartMovementAbility(slideSpeed);
        sliding = true;
        slideTimer = maxSlideTime;
    }

    private void StopSlide()
    {
        pc.StopMovementAbility();
        pc.StopCrouch();
        sliding = false;
    }

    private void SlideMovement()
    {
        //get movement direction
        Vector3 inputDirection = tf.forward * verticalInput + tf.right * horizontalInput;

        if (!pc.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideSpeed * 10f, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        }
        else
        {
            rb.AddForce(pc.GetDirectionOnSlope(inputDirection.normalized) * slideSpeed * 10f, ForceMode.Force);
            pc.SetMoveSpeed(slopeMaxSpeed * pc.GetSlopeAngle());
        }

        if (slideTimer <= 0)
            StopSlide();
    }
}
