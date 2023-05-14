using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    public int Team { get; private set; }

    public Rigidbody rb;
    public StatusEffects statusEffects;

    public bool Control { get; private set; }
    public bool restrictMovement;

    public GameObject[] firstPersonViewObjects;
    public GameObject[] thirdPersonViewObjects;

    public GameObject model;

    [Header("Game Management")]
    public string characterName;

    [Header("Camera")]
    public CameraController mainCamera;
    public Camera handCam;

    [Header("Movement")]
    public bool enableMovement;
    public MovementState moveState;
    float walkSpeed;
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
    public float crouchSpeedMultiplier;
    public float crouchYScale;
    public bool crouchToggle;
    public bool crouching;
    float startYScale;
    float crouchSpeed;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    public float slopeDownForce;
    RaycastHit slopeHit;

    [Header("Dash")]
    public GameObject dashTrail;
    public bool enableDash;
    public float dashCooldown;
    public float dashSpeedMultiplier;
    public float dashDuration;
    public bool canDash;
    bool dashReset;
    bool dashing;
    float dashSpeed;
    Vector3 dashInput;

    public UnityEvent OnDash = new UnityEvent();

    public float DashCooldownTime { get; private set; }

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
    public float minWallRunChangeDistance;
    public float minWallRunChangeAngle;
    float wallRunTimer;
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    RaycastHit lastWallRan;
    bool wallLeft;
    bool wallRight;
    bool wallRunning;
    bool wallRan;
    bool wallRunExit;

    [Header("Climbing")]
    public bool enableClimbing;
    public float climbSpeed;
    public float climbForce;
    public float maxClimbingTime;
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    public float exitWallCooldown;
    public float climbExitDetectionLength;
    bool exitingWall;
    float wallLookAngle;
    bool climbing;
    bool climbStopping;
    RaycastHit frontWallHit;
    bool wallFront;
    float climbingTime;
    Collider climbLedgeCollider;

    [Header("Climb Jump")]
    public bool enableClimbJump;
    public float climbJumpForce;
    public float climbJumpBackForce;
    public int climbJumps;
    public float minWallNormalAngleChange;
    public float climbStopDelay;
    int climbJumpsLeft;
    Transform lastWall;
    Vector3 lastWallNormal;

    [Header("Ledge Climb")]
    public bool enableLedgeClimb;
    public float ledgeClimbSpeed;
    public float ledgeClimbCheckOffset;
    public float ledgeClimbMinDistance;
    bool ledgeClimbing;
    RaycastHit ledgeClimbHit;
    Vector3 ledgeClimbUp;
    Vector3 ledgeClimbForward;
    bool ledgeClimbUpCheck;
    bool ledgeClimbForwardCheck;

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
    [HideInInspector] public bool gravityEnabled;
    [HideInInspector] public bool dragEnabled;
    [HideInInspector] public bool speedLimitEnabled;

    [HideInInspector] public bool hasControl;

    [Header("Weapon")]
    public GameObject weaponHolder;
    Weapon weapon;
    public Weapon Weapon { get { return weapon; } }

    [Header("Class")]
    public GameObject classHolder;
    Class playerClass;
    public Class PlayerClass { get { return playerClass; } }

    [Header("Abilities")]
    public GameObject abilityHolder;
    Ability ability;
    public Ability Ability { get { return ability; } }
    public bool moveAbility;

    public enum MovementState
    {
        Unlimited,
        Freeze,
        Walking,
        Crouching,
        WallRunning,
        Climbing,
        LedgeClimbing,
        LedgeHolding,
        Ability,
        Air
    }

    public bool Grounded { get { return grounded; } }
    public bool IsWallrunning { get { return wallRunning; } }
    public bool MovementOverride { get; set; }

    private void Awake()
    {
        mainCamera = Camera.main.GetComponent<CameraController>();
        weapon = weaponHolder.GetComponent<Weapon>();
        playerClass = classHolder.GetComponent<Class>();
        ability = abilityHolder.GetComponent<Ability>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        statusEffects = GetComponent<StatusEffects>();
    }

    private void Start()
    {
        if (!IsOwner) return;

        walkSpeed = GameManager.Instance.playerWalkSpeed;
        crouchSpeed = walkSpeed * crouchSpeedMultiplier;
        climbSpeed = walkSpeed;
        climbForce = climbSpeed * 2;
        wallRunSpeed = walkSpeed;
        dashSpeed = walkSpeed * dashSpeedMultiplier;

        for (int i = 0; i < firstPersonViewObjects.Length; i++)
            firstPersonViewObjects[i].SetActive(true);

        for (int i = 0; i < thirdPersonViewObjects.Length; i++)
            thirdPersonViewObjects[i].SetActive(false);

        gameObject.layer = LayerMask.NameToLayer("LocalPlayer");
        SetHitboxLayer("LocalPlayer", model, 15);
        GetComponent<CapsuleCollider>().enabled = true;

        Control = true;
        startYScale = transform.localScale.y;
        playerHeight = GetComponent<CapsuleCollider>().height;
        canJump = true;
        canDash = true;
        exitingWall = false;
        wallRunExit = false;
        DashCooldownTime = dashCooldown;
        InitializeAbilities();
        InitializeStatusEffects();
        ReturnControl();

        //TODO: spawn at random for now
        GetComponent<SpawnHandler>().SpawnAtRandom();
    }

    private void InitializeAbilities()
    {
        if(weapon != null)
        {
            weapon.Activated = true;
            weapon.controller = this;
        }

        if(playerClass != null)
        {
            playerClass.Activated = true;
            playerClass.controller = this;

            if (ability != null)
            {
                ability.Activated = true;
                ability.Controller = this;
                ability.PlayerClass = playerClass;
                ability.PlayerWeapon = weapon;
            }
        }
    }

    private void InitializeStatusEffects()
    {
        statusEffects.OnRoot.AddListener(() => {
            rb.velocity = new(0f, rb.velocity.y, 0f);
        });

        statusEffects.OnHover.AddListener(() => { rb.velocity = Vector3.zero; });
    }

    private void SetHitboxLayer(string layerName, GameObject obj, int maxRecursion)
    {
        if (obj == null || maxRecursion == 0)
            return;

        obj.layer = LayerMask.NameToLayer(layerName);
        maxRecursion -= 1;

        foreach (Transform child in obj.transform)
            SetHitboxLayer(layerName, child.gameObject, maxRecursion);
    }

    private void Update()
    {
        if (!IsOwner) return;

        handCam.transform.parent.position = mainCamera.transform.position;
        handCam.transform.parent.rotation = mainCamera.transform.rotation;

        ControlHandler();
        if (!Control) return;

        #region Debugging (Delete)

        if (Input.GetKeyDown(KeyCode.J))
            statusEffects.Stun(5f);

        if (Input.GetKeyDown(KeyCode.K))
            statusEffects.Root(5f);

        if (Input.GetKeyDown(KeyCode.L))
            statusEffects.Hover(5f);

        #endregion

        bool lastGrounded = grounded;
        Vector3 lastVelocity = new(rb.velocity.x, 0f, rb.velocity.z);

        //ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundMask);

        //detection
        WallCheck();
        ClimbWallCheck();
        LedgeDetection();
        GetInput();
        StateHandler();

        //apply drag
        if (dragEnabled && grounded && !dashing && !statusEffects.slippery)
            rb.drag = GamePhysics.KineticFriction;
        else
            rb.drag = 0f;

        if (grounded && !lastGrounded)
            rb.velocity = lastVelocity;

        //reset jumps
        if (grounded || wallRunning || climbing)
            jumpCount = 0;

        //wallrunning movement state
        wallRunning = moveState == MovementState.WallRunning;

        //reset wallrun camera tilt
        if (wallRunning)
        {
            wallRan = true;
            UpdateLastWallRan();
        }
        else if (mainCamera.transform.localRotation.z != 0)
            mainCamera.ResetTilt(wallRunCamTiltTransition);

        //stop the wall running, only if you ran on the wall
        if (wallRan && !wallRunExit && !wallRunning)
            StopWallRun();

        //reset the wall ran variable
        if (wallRan && grounded)
        {
            mainCamera.ResetFOV(wallRunCamFOVTransition);
            wallRan = false;
        }

        //end exiting a wallrun
        if (wallRunExit && grounded)
            wallRunExit = false;

        //visual for dash
        if(DashCooldownTime < dashCooldown)
        {
            DashCooldownTime += Time.deltaTime;
            DashCooldownTime = Mathf.Min(DashCooldownTime, dashCooldown);
        }

        //reset dash
        if (!canDash && dashReset && (grounded || wallRunning || climbing))
            canDash = true;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (enableMovement && moveState != MovementState.Ability)
        {
            if(!dashing)
                MovePlayer();
            else
                Dash();

            SpeedControl();
        }

        SpecialGravity();
    }

    #region Teams

    [ServerRpc]
    public void SetTeamServerRpc(int team)
    {
        SetTeamClientRpc(team);
    }

    [ClientRpc]
    public void SetTeamClientRpc(int team)
    {
        SetTeam(team);
    }

    public void SetTeam(int team)
    {
        if(team == 1)
        {
            Team = team;
            gameObject.tag = GameManager.tag_team1;
        }
        else if(team == 2)
        {
            Team = team;
            gameObject.tag = GameManager.tag_team2;
        }
        else if(team == -1)
        {
            Team = team;
            gameObject.tag = GameManager.tag_spectator;
        }
    }

    #endregion

    #region Input Handling

    private void GetInput()
    {
        if (restrictMovement || statusEffects.stunned)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            return;
        }

        if (Input.GetKeyDown(GameManager.bind_kill))
            KillBindServerRpc(OwnerClientId, NetworkObjectId);
        
        //crouching
        if (enableCrouch)
        {
            if (crouchToggle)
            {
                if (Input.GetKeyDown(GameManager.bind_crouch))
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
                if (Input.GetKeyDown(GameManager.bind_crouch))
                {
                    StartCrouch();
                }
                else if (Input.GetKeyUp(GameManager.bind_crouch) && CheckStanding())
                {
                    StopCrouch();
                }
            }
        }

        if(statusEffects.rooted)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (statusEffects.hovered) return;

        //jumping
        if(canJump)
        {
            if (enableJump && Input.GetKeyDown(GameManager.bind_jump) && jumpCount < maxJumps - 1 && !wallRunning && (!climbing || climbStopping) && !ledgeGrabbed)
            {
                canJump = false;
                Jump();

                Invoke(nameof(ResetJump), jumpCoodown);

                if (climbStopping)
                    SecondClimbStop();
            } 
            else if (enableWallRun && Input.GetKeyDown(GameManager.bind_jump) && wallRunning)
            {
                canJump = false;
                WallJump();

                Invoke(nameof(ResetJump), jumpCoodown);
            }
            else if(enableLedgeJump && Input.GetKeyDown(GameManager.bind_jump) && ledgeGrabbed && !exitingWall)
            {
                canJump = false;
                LedgeJump();

                Invoke(nameof(ResetJump), jumpCoodown);
            }
            else if(enableClimbJump && Input.GetKeyDown(GameManager.bind_jump) && wallFront && !exitingWall)
            {
                canJump = false;
                ClimbJump();

                Invoke(nameof(ResetJump), jumpCoodown);
            }
        }

        //dashing
        if (enableDash && canDash)
        {
            if (Input.GetKeyDown(GameManager.bind_abilityShift) && !crouching)
                StartDash();
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
        //ledge climbing
        else if(ledgeClimbing && !exitingWall && !crouching)
        {
            moveState = MovementState.LedgeClimbing;
            moveSpeed = ledgeClimbSpeed;
        }
        //climbing
        else if (climbing && !climbStopping && !exitingWall && !crouching)
        {
            moveState = MovementState.Climbing;
            desiredMoveSpeed = climbSpeed;
        }
        //wall running
        else if(enableWallRun && (wallLeft || wallRight) && verticalInput > 0 && IsAboveHeight(minJumpHeight) && CheckWallPeel() && !crouching && !exitingWall && !climbStopping && !crouching)
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
        if (enableClimbing && wallFront && wallLookAngle < maxWallLookAngle && verticalInput > 0 && !exitingWall && !climbStopping && !crouching)
        {
            if (!climbing && climbingTime > 0) StartClimbing();

            if (climbingTime > 0) climbingTime -= Time.deltaTime;
            if (climbingTime < 0) StopClimbing();
        }
        else if ((!climbStopping && climbing) || crouching)
        {
            StopClimbing();
        }

        if (climbing && climbStopping && ClimbStopGroundCheck())
        {
            rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
            SecondClimbStop();
        }

        //ledge holding
        if (enableLedgeGrab && ledgeGrabbed && !exitingWall)
        {
            FreezeOnLedge();
            timeOnLedge += Time.deltaTime;
            if (timeOnLedge > minTimeOnLedge && (horizontalInput != 0 || verticalInput != 0))
                ExitLedgeHold();
        }

        if (enableLedgeGrab && ledgeGrabbed && climbing)
            StopClimbing();

        //carrying momentum
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

    private void ControlHandler()
    {
        if (GameManager.Instance.sceneState == GameManager.SceneState.InGame && GetComponent<HealthManager>().IsAlive)
            SetControl(true);
        else
            SetControl(false);
    }

    private void SetControl(bool value)
    {
        Control = value;
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

            //if (rb.velocity.y > 0)
            //    rb.AddForce(Vector3.down * slopeDownForce * 10f, ForceMode.Force);
            rb.AddForce(slopeHit.normal * -slopeDownForce * 10f, ForceMode.Force);
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

        if (climbing && !climbStopping && !exitJump)
            ClimbMovement();
    }

    private void Dash()
    {
        rb.AddForce(dashInput.normalized * dashSpeedMultiplier * 100f, ForceMode.Force);
    }

    private void SpecialGravity()
    {
        if (statusEffects.hovered) return;

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
        if (!speedLimitEnabled) return;

        if ((OnSlope() || climbing || dashing) && !exitJump)
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
        if (crouching)
            StopCrouch();

        exitJump = true;

        //reset y velocity
        rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        jumpCount++;
    }

    private void WallJump()
    {
        exitJump = true;
        float forwardValue = wallJumpForwardForce;
        if(Vector3.Dot(mainCamera.transform.forward, (wallRight) ? rightWallHit.normal : leftWallHit.normal) < 0f)
            forwardValue = 0f;

        StopWallRun();

        rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 force = transform.up * wallJumpUpForce + GetWallNormal() * wallJumpAwayForce + mainCamera.transform.forward * forwardValue;
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
        GetComponent<CapsuleCollider>().height = playerHeight * (2 / 3);
        if (grounded)
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);
        crouching = true;
    }

    public void StopCrouch()
    {
        GetComponent<CapsuleCollider>().height = playerHeight;
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
        if (statusEffects.hovered) return false;

        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f, groundMask))
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

    public void TakeControl(bool movement, bool gravity = true, bool drag = true, bool speedLimiter = true)
    {
        hasControl = false;
        restrictMovement = !movement;
        gravityEnabled = gravity;
        dragEnabled = drag;
        speedLimitEnabled = speedLimiter;
    }

    public void ReturnControl()
    {
        TakeControl(true, true, true, true);
        hasControl = true;
    }

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

        if (wallRight && Vector3.Dot(rightWallHit.normal, Vector3.up) > 0.01f)
            wallRight = false;
        if (wallLeft && Vector3.Dot(leftWallHit.normal, Vector3.up) > 0.01f)
            wallLeft = false;

        if (wallRunExit && (wallRight || wallLeft))
        {
            bool newWallByAngle = Mathf.Abs(Vector3.Angle(lastWallRan.normal, ((wallRight) ? rightWallHit.normal : leftWallHit.normal))) >= minWallRunChangeAngle;

            bool newWallByTransform = lastWallRan.collider.GetInstanceID() != ((wallRight) ? rightWallHit.collider.GetInstanceID() : leftWallHit.collider.GetInstanceID());

            bool newWallToRun = newWallByAngle || newWallByTransform;

            if (!newWallToRun)
            {
                wallRight = false;
                wallLeft = false;
            }
            else
            {
                wallRunExit = false;
            }
        }
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
        if (statusEffects.hovered) return;

        if(!wallRunning)
            rb.velocity = Vector3.Project(rb.velocity, GetWallMovementVector(rb.velocity));
    }

    private void UpdateLastWallRan()
    {
        lastWallRan = (wallLeft) ? leftWallHit : rightWallHit;
    }

    private void StopWallRun()
    {
        wallRunExit = true;
    }

    #endregion

    #region Climbing

    private void ClimbWallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, transform.forward, out frontWallHit, detectionLength, groundMask);
        wallLookAngle = Vector3.Angle(transform.forward, -frontWallHit.normal);

        //bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;
        bool newWall = Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if(newWall || grounded || ledgeGrabbed)
        {
            climbingTime = maxClimbingTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private void StartClimbing()
    {
        if (statusEffects.hovered) return;

        climbing = true;

        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
    }

    private void StopClimbing()
    {
        climbStopping = true;
        climbLedgeCollider = frontWallHit.collider;
        Invoke(nameof(SecondClimbStop), climbStopDelay);
    }

    private void SecondClimbStop()
    {
        climbing = false;
        climbStopping = false;
    }

    private bool ClimbStopGroundCheck()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, (playerHeight * 0.5f) + climbExitDetectionLength, groundMask);
        return hit.collider != null && hit.collider == climbLedgeCollider;
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

    #region Dashing

    public void StartDash()
    {
        OnDash.Invoke();

        dashing = true;
        canDash = false;
        dashReset = false;
        unlimitedSpeed = true;
        moveSpeed = dashSpeed;

        if (grounded)
        {
            dashInput = transform.right * horizontalInput + transform.forward * verticalInput;
            dashInput.Normalize();

            if (dashInput.magnitude == 0)
                dashInput = transform.forward;
        }
        else
            dashInput = mainCamera.transform.forward;

        gravityEnabled = false;
        rb.drag = 0;
        rb.velocity = Vector3.zero;

        Invoke(nameof(EndDash), dashDuration);
    }

    public void EndDash()
    {
        dashing = false;
        rb.velocity = Vector3.zero;
        rb.drag = GamePhysics.KineticFriction;
        unlimitedSpeed = false;
        gravityEnabled = true;

        DashCooldownTime = 0f;
        Invoke(nameof(ResetDash), dashCooldown);
    }

    public void ResetDash()
    {
        dashReset = true;
    }

    #endregion

    #region Ledge Climbing

    private void LedgeDetection()
    {
        if (exitingWall || !enableLedgeClimb || !climbing) return;

        //raycast from above and in front of the player
        Vector3 playerClimbPosition = transform.position + transform.forward * ledgeClimbCheckOffset;
        Vector3 rayStartPoint = playerClimbPosition + Vector3.up * (playerHeight / 2);
        bool ledgeDetected = Physics.Raycast(rayStartPoint, Vector3.down, out ledgeClimbHit, Vector3.Distance(rayStartPoint, playerClimbPosition), groundMask);
        Debug.DrawRay(rayStartPoint, Vector3.down, Color.red, 10f);

        if (!ledgeDetected) return;

        ledgeClimbUp = Vector3.Project((ledgeClimbHit.point + Vector3.up * playerHeight / 2) - transform.position, Vector3.up) + transform.position;
        ledgeClimbForward = Vector3.Project(ledgeClimbHit.point - transform.position, transform.forward) + transform.position;

        if(!ledgeClimbing)
            StartLedgeClimb();

        //bool ledgeDetected = Physics.SphereCast(mainCamera.transform.position, ledgeDetectRadius, mainCamera.transform.forward, out ledgeHit, ledgeDetectLength, ledgeMask);
        //if (!ledgeDetected) return;
        //
        //RaycastHit losCheck;
        //Physics.Raycast(transform.position, (ledgeHit.point - transform.position).normalized, out losCheck, Vector3.Distance(transform.position, ledgeHit.point));
        //
        //if(losCheck.transform != ledgeHit.transform)
        //{
        //    ledgeDetected = false;
        //    return;
        //}
        ////float distanceToLedge = Vector3.Distanzce(transform.position, ledgeHit.transform.position);
        //float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.point);
        //
        //if (ledgeHit.transform == lastLedge) return;
        //
        //if (distanceToLedge < maxLedgeGrabDistance && !ledgeGrabbed)
        //{
        //    currentHit = ledgeHit;
        //    EnterLedgeHold();
        //}
    }

    private float GetLedgeHitAngle()
    {
        return Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(-frontWallHit.normal, Vector3.up));
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

    private void StartLedgeClimb()
    {
        ledgeClimbing = true;
        gravityEnabled = false;
        ledgeClimbForwardCheck = false;
        ledgeClimbUpCheck = true;
    }

    private void ClimbLedge()
    {
        if (!enableLedgeClimb || !ledgeClimbing || exitingWall) return;

        //up
        Vector3 dir = ledgeClimbUp - transform.position;
        float dist = Vector3.Distance(transform.position, ledgeClimbUp);

        if (ledgeClimbUpCheck && dist > ledgeClimbMinDistance)
        {
            if (rb.velocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(dir.normalized * moveToLedgeSpeed * Time.deltaTime, ForceMode.Force);
            return;
        }
        ledgeClimbUpCheck = false;

        rb.velocity = new(rb.velocity.x, 0f, rb.velocity.z);

        //forward
        dir = ledgeClimbForward - transform.position;
        dist = Vector3.Distance(transform.position, ledgeClimbForward);

        if(ledgeClimbForwardCheck && dist > ledgeClimbMinDistance)
        {
            if (rb.velocity.magnitude < moveToLedgeSpeed)
                rb.AddForce(dir.normalized * moveToLedgeSpeed * Time.deltaTime, ForceMode.Force);
            return;
        }
        ledgeClimbForwardCheck = false;

        rb.velocity = Vector3.zero;

        //destination reached
        StopLedgeClimb();
    }

    private void StopLedgeClimb()
    {
        ledgeClimbing = false;
        gravityEnabled = true;
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

    #region Network

    [ServerRpc]
    private void KillBindServerRpc(ulong clientId, ulong objectId)
    {
        NetworkObject obj = GetNetworkObject(objectId);
        if (obj != null && obj.OwnerClientId == clientId)
            obj.GetComponent<HealthManager>().Damage(1000);
    }

    #endregion

    #region Weapon

    //public void TriggerMelee1()
    //{
    //    PrimaryMeleeTrigger1.Invoke();
    //}
    //
    //public void TriggerMelee2()
    //{
    //    PrimaryMeleeTrigger2.Invoke();
    //}
    //
    //public void TriggerRanged1()
    //{
    //    PrimaryRangedTrigger1.Invoke();
    //}
    //
    //public void TriggerRanged2()
    //{
    //    PrimaryRangedTrigger2.Invoke();
    //}

    #endregion
}
