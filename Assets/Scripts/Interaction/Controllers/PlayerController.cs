using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    public static PlayerController instance;

    Rigidbody rb;

    public bool control;
    public bool restrictMovement;

    public GameObject visor;

    [Header("Camera")]
    public CameraController mainCamera;

    [Header("Movement")]
    public bool enableMovement;
    public MovementState moveState;
    public float walkSpeed;
    float moveSpeed;
    float desiredMoveSpeed;
    float lastDesiredMoveSpeed;

    [Header("Jumping")]
    public bool enableJump;
    public float jumpForce;
    public float jumpCoodown;
    public float airMultiplier;
    public int maxJumps;
    bool canJump;
    int jumpCount;
    bool exitJump;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public LayerMask nonPlayerMask;
    float playerHeight;
    bool grounded;

    [Header("Crouching")]
    public bool enableCrouch;
    public float crouchSpeed;
    public float crouchYScale;
    public bool crouchToggle;
    float startYScale;
    bool crouching;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    public float slopeDownForce;
    RaycastHit slopeHit;

    [Header("WallRunning")]
    public bool enableWallRun;
    public float wallRunSpeed;
    public float maxWallRunTime;
    public float wallCheckDistance;
    public float minJumpHeight;
    public float wallJumpForwardForce;
    public float wallJumpUpForce;
    public float wallJumpAwayForce;
    public float wallRunCamFOV;
    public float wallRunCamTilt;
    public float wallRunCamFOVTransition;
    public float wallRunCamTiltTransition;
    float wallRunTimer;
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    bool wallLeft;
    bool wallRight;
    bool wallRunning;
    bool wallRan;

    [Header("Climbing")]
    public bool enableClimbing;
    public float climbSpeed;
    public float climbForce;
    public float maxClimbingTime;
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    public float exitWallCooldown;
    bool exitingWall;
    float wallLookAngle;
    bool climbing;
    RaycastHit frontWallHit;
    bool wallFront;
    float climbingTime;

    [Header("Climb Jump")]
    public bool enableClimbJump;
    public float climbJumpForce;
    public float climbJumpBackForce;
    public int climbJumps;
    public float minWallNormalAngleChange;
    int climbJumpsLeft;
    Transform lastWall;
    Vector3 lastWallNormal;

    [Header("Ledge Grab")]
    public bool enableLedgeGrab;
    public float ledgeDetectLength;
    public float ledgeDetectRadius;
    public LayerMask ledgeMask;
    public float moveToLedgeSpeed;
    public float maxLedgeGrabDistance;
    public float minTimeOnLedge;
    float timeOnLedge;
    Transform currentLedge;
    Transform lastLedge;
    RaycastHit ledgeHit;
    RaycastHit currentHit;
    bool ledgeGrabbed;

    [Header("Ledge Jump")]
    public bool enableLedgeJump;
    public float ledgeJumpForwardForce;
    public float ledgeJumpUpwardForce;
    public float exitLedgeTime;
    bool exitingLedge;
    float exitLedgeTimer;

    float horizontalInput, verticalInput;
    Vector3 moveDirection;
    bool unlimitedSpeed;
    bool freezeSpeed;

    [Header("Physics")]
    public bool simulateGravity;
    public float regularGravity;
    public float downForceGravity;
    public float downForceSpeedThreshold;
    public bool gravityEnabled;

    [Header("Abilities")]
    public Ability ability1;

    bool moveAbility;

    [Header("Weapon")]
    public GameObject weaponHolder;
    Weapon weapon1;

    [Header("Keybinds")]
    public KeyCode bind_jump = KeyCode.Space;
    public KeyCode bind_crouch = KeyCode.LeftControl;
    public KeyCode bind_ability1 = KeyCode.LeftShift;
    public KeyCode bind_primaryFire = KeyCode.Mouse0;
    public KeyCode bind_secondaryFire = KeyCode.Mouse1;
    public KeyCode bind_tertiaryFire = KeyCode.Mouse2;
    public KeyCode bind_reload = KeyCode.R;
    public KeyCode bind_swapWeapon = KeyCode.Q;
    public KeyCode bind_kill = KeyCode.Backspace;

    public enum MovementState
    {
        Unlimited,
        Freeze,
        Walking,
        Crouching,
        WallRunning,
        Climbing,
        LedgeHolding,
        Ability,
        Air
    }

    public bool Grounded { get { return grounded; } }
    public bool IsWallrunning { get { return wallRunning; } }
    public bool MovementOverride { get; set; }

    private void Start()
    {
        if (!IsOwner) return;

        visor.SetActive(false);

        if (PlayerController.instance == null)
            PlayerController.instance = this;
        else
            Destroy(gameObject);

        control = true;
        mainCamera = Camera.main.GetComponent<CameraController>();
        weapon1 = weaponHolder.GetComponent<Weapon>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
        playerHeight = GetComponent<CapsuleCollider>().height;
        canJump = true;
        exitingWall = false;
        gravityEnabled = true;
        InitializeAbilities();

        //TODO: spawn at random for now
        GetComponent<SpawnHandler>().SpawnAtRandom();
    }

    private void InitializeAbilities()
    {
        if(weapon1 != null)
        {
            weapon1.Activated = true;
            weapon1.primaryInput = bind_primaryFire;
            weapon1.secondaryInput = bind_secondaryFire;
            weapon1.tertiaryInput = bind_tertiaryFire;
            weapon1.reloadInput = bind_reload;
            weapon1.swapInput = bind_swapWeapon;
        }

        if (ability1 != null)
        {
            ability1.Activated = true;
            ability1.input = bind_ability1;
        }
    }

    private void Update()
    {
        if (!IsOwner || !control) return;

        bool lastGrounded = grounded;
        Vector3 lastVelocity = new(rb.velocity.x, 0f, rb.velocity.z);

        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);
        bool slopeCheck = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.5f, groundMask);

        if (slopeCheck && !grounded)
            rb.AddForce(Vector3.down * 50f, ForceMode.Impulse);

        //detection
        WallCheck();
        ClimbWallCheck();
        LedgeDetection();
        SpeedControl();
        StateHandler();
        GetInput();

        //apply drag
        if (grounded)
            rb.drag = GamePhysics.KineticFriction;
        else
            rb.drag = 0f;

        if (grounded && !lastGrounded)
            rb.velocity = lastVelocity;

        //reset jumps
        if (grounded)
            jumpCount = 0;

        wallRunning = moveState == MovementState.WallRunning;

        if (wallRunning)
            wallRan = true;
        else if (mainCamera.transform.localRotation.z != 0)
            mainCamera.ResetTilt(wallRunCamTiltTransition);

        if (wallRan && grounded)
            mainCamera.ResetFOV(wallRunCamFOVTransition);

        mainCamera.speed = (Mathf.Max(rb.velocity.magnitude, walkSpeed) - walkSpeed) * 2f;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (enableMovement && moveState != MovementState.Ability)
            MovePlayer();
        SpecialGravity();
    }

    #region Input Handling

    private void GetInput()
    {
        if (Input.GetKeyDown(bind_kill))
            GetComponent<HealthManager>().Damage(1000);

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        //jumping
        if(canJump)
        {
            if (enableJump && Input.GetKeyDown(bind_jump) && jumpCount < maxJumps - 1 && !wallRunning && !climbing && !ledgeGrabbed)
            {
                canJump = false;
                Jump();

                Invoke(nameof(ResetJump), jumpCoodown);
            } 
            else if (enableWallRun && Input.GetKeyUp(bind_jump) && wallRunning)
            {
                canJump = false;
                WallJump();

                Invoke(nameof(ResetJump), jumpCoodown);
            }
            else if(enableLedgeJump && Input.GetKeyDown(bind_jump) && ledgeGrabbed && !exitingWall)
            {
                canJump = false;
                LedgeJump();

                Invoke(nameof(ResetJump), jumpCoodown);
            }
            else if(enableClimbJump && Input.GetKeyUp(bind_jump) && wallFront && climbJumpsLeft > 0 && !exitingWall)
            {
                canJump = false;
                ClimbJump();

                Invoke(nameof(ResetJump), jumpCoodown);
            }
        }

        //crouching
        if (enableCrouch)
        {
            if (crouchToggle)
            {
                if (Input.GetKeyDown(bind_crouch))
                {
                    if (!crouching)
                    {
                        StartCrouch();
                    }
                    else if (CheckStanding())
                    {
                        StopCrouch();
                    }
                }
            }
            else
            {
                if (Input.GetKeyDown(bind_crouch))
                {
                    StartCrouch();
                }
                else if (Input.GetKeyUp(bind_crouch) && CheckStanding())
                {
                    StopCrouch();
                }
            }
        }
    }

    private void StateHandler()
    {
        //freeze movement
        if (freezeSpeed)
        {
            moveState = MovementState.Freeze;
            rb.velocity = Vector3.zero;
        }
        //uncap speed
        else if (unlimitedSpeed)
        {
            moveState = MovementState.Unlimited;
            moveSpeed = 1000f;
        }
        //climbing
        else if (climbing && !exitingWall)
        {
            moveState = MovementState.Climbing;
            desiredMoveSpeed = climbSpeed;
        }
        //wall running
        else if(enableWallRun && (wallLeft || wallRight) && verticalInput > 0 && Input.GetKey(bind_jump) && IsAboveHeight(minJumpHeight) && CheckWallPeel() && !crouching)
        {
            if (!wallRunning)
            {
                mainCamera.FOV(wallRunCamFOV, wallRunCamFOVTransition);
                mainCamera.Tilt(wallRight ? wallRunCamTilt : -wallRunCamTilt, wallRunCamTiltTransition);
            }

            moveState = MovementState.WallRunning;
            desiredMoveSpeed = wallRunSpeed;
            BeginWallRun();
        }
        //movement ability
        else if (moveAbility)
        {
            moveState = MovementState.Ability;
        }
        //crouching
        else if (enableCrouch && crouching && grounded)  
        {
            moveState = MovementState.Crouching;
            desiredMoveSpeed = crouchSpeed;
        }
        //walking
        else if (grounded)   
        {
            moveState = MovementState.Walking;
            desiredMoveSpeed = walkSpeed;
        }
        //air
        else
            moveState = MovementState.Air;

        //climbing
        if (enableClimbing && wallFront && wallLookAngle < maxWallLookAngle && Input.GetKey(bind_jump) && verticalInput > 0 && !exitingWall)
        {
            if (!climbing && climbingTime > 0) StartClimbing();

            if (climbingTime > 0) climbingTime -= Time.deltaTime;
            if (climbingTime < 0) StopClimbing();
        }
        else
            StopClimbing();

        //ledge holding
        if(enableLedgeGrab && ledgeGrabbed && !exitingWall)
        {
            FreezeOnLedge();
            timeOnLedge += Time.deltaTime;
            if (timeOnLedge > minTimeOnLedge && (horizontalInput != 0 || verticalInput != 0))
                ExitLedgeHold();
        }

        if (enableLedgeGrab && ledgeGrabbed && climbing)
            StopClimbing();

        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 5f && desiredMoveSpeed < float.MaxValue && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SpeedLerp());
        }
        else
            moveSpeed = desiredMoveSpeed;

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SpeedLerp()
    {
        float t = 0;
        float dif = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while(t < dif)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, t / dif);
            t += Time.deltaTime;
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    #endregion

    #region Movement

    private void MovePlayer()
    {
        if (exitingWall || restrictMovement) return;

        //get movement direction
        moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (OnSlope() && !exitJump)
        {
            rb.AddForce(GetDirectionOnSlope(moveDirection.normalized) * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * slopeDownForce * 10f, ForceMode.Force);
        }
        else if (wallRunning && !exitJump)
        {
            rb.AddForce(GetWallMovementVector(moveDirection.normalized) * wallRunSpeed * 10f, ForceMode.Force);
            if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
                rb.AddForce(-GetWallNormal() * 50f, ForceMode.Force);
            rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
        }
        else if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        if (climbing && !exitJump)
            ClimbMovement();
    }

    private void SpecialGravity()
    {
        if (simulateGravity && gravityEnabled && !OnSlope() && !wallRunning)
        {
            if (!grounded && rb.velocity.y < downForceSpeedThreshold)
                rb.AddForce(Vector3.down * downForceGravity, ForceMode.Acceleration);
            else
                rb.AddForce(Vector3.down * regularGravity, ForceMode.Acceleration);
        }
        else
        {
            rb.useGravity = gravityEnabled && !OnSlope() && !wallRunning;
        }
    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitJump)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    #endregion

    #region Jump

    public void Jump()
    {
        exitJump = true;

        //reset y velocity
        rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        jumpCount++;
    }

    private void WallJump()
    {
        exitJump = true;

        rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 force = transform.up * wallJumpUpForce + GetWallNormal() * wallJumpAwayForce + mainCamera.transform.forward * wallJumpForwardForce;
        rb.AddForce(force, ForceMode.Impulse);
    }

    private void ClimbJump()
    {
        exitJump = true;
        ExitWall();

        Vector3 force = transform.up * climbJumpForce + frontWallHit.normal * climbJumpBackForce;
        rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(force, ForceMode.Impulse);

        climbJumpsLeft--;
    }

    private void LedgeJump()
    {
        if (!enableLedgeJump) return;

        exitJump = true;
        ExitLedgeHold();
        ExitWall();
        Invoke(nameof(DelayedLedgeJumpForce), 0.05f);
    }

    private void DelayedLedgeJumpForce()
    {
        Vector3 force = transform.up * ledgeJumpUpwardForce + mainCamera.transform.forward * ledgeJumpForwardForce;
        
        if(Vector3.Dot(force, currentHit.normal) < 0)
            force = Vector3.ProjectOnPlane(mainCamera.transform.forward, currentHit.normal).normalized * ledgeJumpForwardForce + transform.up * ledgeJumpUpwardForce;

        rb.velocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);
    }

    public void ResetJump()
    {
        canJump = true;
        exitJump = false;
    }

    #endregion

    #region Crouching

    public void StartCrouch()
    {
        transform.localScale = new(transform.localScale.x, crouchYScale, transform.localScale.z);
        if (grounded)
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
        crouching = true;
    }

    public void StopCrouch()
    {
        transform.localScale = new(transform.localScale.x, startYScale, transform.localScale.z);
        crouching = false;
    }

    public bool CheckStanding()
    {
        return !Physics.Raycast(transform.position, transform.up, 1.5f, nonPlayerMask);
    }

    #endregion

    #region Slope Handling

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            return GetSlopeAngle() < maxSlopeAngle && GetSlopeAngle() != 0;
        }

        return false;
    }

    public float GetSlopeAngle()
    {
        return Vector3.Angle(Vector3.up, slopeHit.normal);
    }

    public Vector3 GetDirectionOnSlope(Vector3 vector)
    {
        return Vector3.ProjectOnPlane(vector, slopeHit.normal).normalized;
    }

    #endregion

    #region Abilities

    public void StartMovementAbility(float abilitySpeed = -1f, bool gradualDelta = false)
    {
        moveAbility = true;
        if (abilitySpeed > -1f)
        {
            if (gradualDelta)
                desiredMoveSpeed = abilitySpeed;
            else
                moveSpeed = abilitySpeed;
        }
        else
        {
            if (gradualDelta)
                desiredMoveSpeed = float.MaxValue;
            else
                moveSpeed = float.MaxValue;
        }
    }

    public void StopMovementAbility()
    {
        moveAbility = false;
    }

    public void SetMoveSpeed(float newSpeed)
    {
        desiredMoveSpeed = newSpeed;
    }

    #endregion

    #region Wall Running

    private void WallCheck()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, groundMask);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, groundMask);

        if (wallRight && Vector3.Dot(rightWallHit.normal, Vector3.up) != 0)
            wallRight = false;
        if (wallLeft && Vector3.Dot(leftWallHit.normal, Vector3.up) != 0)
            wallLeft = false;
    }

    private bool IsAboveHeight(float height)
    {
        return !Physics.Raycast(transform.position, Vector3.down, height, groundMask);
    }

    private Vector3 GetWallMovementVector(Vector3 moveVector)
    {
        Vector3 direction = GetWallNormal();
        direction = Vector3.Cross(direction, Vector3.up);
        direction = Vector3.Project(moveVector, direction);

        return direction.normalized;
    }

    private Vector3 GetWallNormal()
    {
        return wallRight ? rightWallHit.normal : leftWallHit.normal;
    }

    private bool CheckWallPeel()
    {
        return wallRight ? horizontalInput >= 0 : horizontalInput <= 0;
    }

    private void BeginWallRun()
    {
        if(!wallRunning)
            rb.velocity = Vector3.Project(rb.velocity, GetWallMovementVector(rb.velocity));
    }

    #endregion

    #region Climbing

    private void ClimbWallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, transform.forward, out frontWallHit, detectionLength, groundMask);
        wallLookAngle = Vector3.Angle(transform.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if((wallFront && newWall) || grounded || ledgeGrabbed)
        {
            climbingTime = maxClimbingTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private void StartClimbing()
    {
        climbing = true;

        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
    }

    private void StopClimbing()
    {
        climbing = false;
    }

    private void ClimbMovement()
    {
        rb.velocity = new(rb.velocity.x, climbForce, rb.velocity.z);
    }

    private void ExitWall()
    {
        exitingWall = true;
        Invoke(nameof(ExitWallReset), exitWallCooldown);
    }

    private void ExitWallReset()
    {
        exitingWall = false;
    }

    #endregion

    #region Ledge Climbing

    private void LedgeDetection()
    {
        if (exitingWall || wallRunning || !enableLedgeGrab) return;

        bool ledgeDetected = Physics.SphereCast(mainCamera.transform.position, ledgeDetectRadius, mainCamera.transform.forward, out ledgeHit, ledgeDetectLength, ledgeMask);
        if (!ledgeDetected) return;

        RaycastHit losCheck;
        Physics.Raycast(transform.position, (ledgeHit.point - transform.position).normalized, out losCheck, Vector3.Distance(transform.position, ledgeHit.point));
        
        if(losCheck.transform != ledgeHit.transform)
        {
            ledgeDetected = false;
            return;
        }

        //float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.transform.position);
        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.point);

        if (ledgeHit.transform == lastLedge) return;

        if (distanceToLedge < maxLedgeGrabDistance && !ledgeGrabbed)
        {
            currentHit = ledgeHit;
            EnterLedgeHold();
        }
    }

    private void FreezeOnLedge()
    {
        if (!enableLedgeGrab) return;

        gravityEnabled = false;
        //Vector3 dir = currentLedge.position - transform.position;
        Vector3 dir = currentHit.point - transform.position;
        //float dist = Vector3.Distance(transform.position, currentLedge.position);
        float dist = Vector3.Distance(transform.position, currentHit.point);

        if(dist > 1f)
        {
            if (rb.velocity.magnitude < moveToLedgeSpeed) 
                rb.AddForce(dir.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime, ForceMode.Force);
        }
        else
        {
            if (!freezeSpeed) freezeSpeed = true;
            if (unlimitedSpeed) unlimitedSpeed = false;
        }

        if (dist > maxLedgeGrabDistance)
        {
            Debug.Log("Exiting | Distance: " + dist + " | Ledge: " + currentHit.transform.name);
            ExitLedgeHold();
        }
    }

    private void EnterLedgeHold()
    {
        if (!enableLedgeGrab) return;

        ledgeGrabbed = true;
        restrictMovement = true;
        unlimitedSpeed = true;
        currentLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        gravityEnabled = false;
        rb.velocity = Vector3.zero;
    }

    private void ExitLedgeHold()
    {
        if (!enableLedgeGrab) return;
        ledgeGrabbed = false;
        timeOnLedge = 0f;
        restrictMovement = false;
        freezeSpeed = false;
        gravityEnabled = true;

        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        if (!enableLedgeGrab) return;

        lastLedge = null;
    }

    #endregion
}
