using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.InputSystem;


namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        //audio
        public AudioClip jumpAudio;
        public AudioSource audioSource;

        //movement
        Vector2 move;
        public float maxSpeed = 7;
        

        //non latching movement control boolean
        public bool controlEnabled = true;

        //jumping
        public float jumpTakeOffSpeed = 7;
        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        bool jump;

        //core player
        public Collider2D collider2d;
        SpriteRenderer spriteRenderer;

        //animationneed to add back when we put animation in
        //internal Animator animator;

        //platformer variables
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        //input actions set up in the editor
        private InputAction m_MoveAction;
        private InputAction m_JumpAction;
        private InputAction Latch;

        //latching variables
        [Header("Swing Settings")]
        public float swingSpeed = 160f;          // Rotation speed multiplier
        public float gravityPull = 25f;         // Force pulling toward center
        public float swingDrag = 0.98f; // Slowdown factor
        public float inputMultiplier = 2.2f;
        public float verticalLaunchMultiplier = 1.3f;
        public float horizontalLaunchMultiplier = 0.5f;
        public float swingRadius = 1.5f; // Default value

        public LayerMask wallLayer;
        public LayerMask collisionLayers; // Set this in Inspector to include Ground and Obstacles

        private bool isLatched;
        private Vector2 latchPoint;             // Pivot to rotate around
        private float swingMomentum;            // Track swing force
        private float currentAngle;             // Current rotation angle
        private Rigidbody2D rb;
        
        public Collider2D topCollider;
        public Collider2D checkCollider;

        void Awake()
        {
            //shift latch input action
            Latch = InputSystem.actions.FindAction("Player/Latch");
            Latch.Enable();

            // Auto-configure if you forget to set it
            if (wallLayer.value == 0)
                wallLayer = LayerMask.GetMask("Wall");

            // Corrected: Properly set collisionLayers (Ground and Obstacles)
            if (collisionLayers.value == 0)
                collisionLayers = LayerMask.GetMask("Ground") | LayerMask.GetMask("Obstacles");

            //get components
            rb = GetComponent<Rigidbody2D>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            topCollider = transform.Find("ColliderTop").GetComponent<Collider2D>();
            checkCollider = transform.Find("CollisionDetector").GetComponent<Collider2D>();


            //need to add back when we do animation
            //animator = GetComponent<Animator>();

            //normal movement input actions
            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");

            m_MoveAction.Enable();
            m_JumpAction.Enable();
        }

        protected override void Update()
        {

            if (Latch.WasPressedThisFrame() && !isLatched)
            {
                TryLatchToWall();
            }
            else if (Latch.WasReleasedThisFrame() && isLatched)
            {
                ReleaseFromWall();
            }


            if (controlEnabled)
            {
                move.x = m_MoveAction.ReadValue<Vector2>().x;

                if (jumpState == JumpState.Grounded && m_JumpAction.WasPressedThisFrame())
                    jumpState = JumpState.PrepareToJump;
                else if (m_JumpAction.WasReleasedThisFrame())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }

            UpdateJumpState();
            base.Update();
        }

        protected override void FixedUpdate()
        {
            if (isLatched)
            {
                HandleSwingPhysics();
            }
            else
            {
                base.FixedUpdate();
            }
        }

        private void ReleaseFromWall()
        {
            if (!isLatched) return;

            // 1. Calculate vertical component (more controlled)
            float verticalMomentum = 0f;

            Debug.Log($"Swing - Angle: {currentAngle}°, Momentum: {swingMomentum:F2}, Latched: {isLatched}");

            if (Mathf.Abs(currentAngle) > 10f)
            {

                // Diminishing returns curve (square root for smooth falloff)
                float angleRatio = Mathf.Pow(Mathf.Abs(currentAngle) / 90f, 0.75f); // Softer curve
                
                Debug.Log($"Ratio: {angleRatio}°");
                verticalMomentum = angleRatio * jumpTakeOffSpeed * verticalLaunchMultiplier;

            }

            // 2. Horizontal momentum (your existing formula)
            float horizontalMomentum = swingMomentum * horizontalLaunchMultiplier;

            // 3. Apply velocities
            velocity = new Vector2(horizontalMomentum, verticalMomentum);
     

            // 4. Reset all states
            isLatched = false;
            rb.gravityScale = 1f;
            swingMomentum = 0f;
            currentAngle = 0f;
            transform.rotation = Quaternion.identity;

            // 4. Force physics update
            Physics2D.SyncTransforms();
        }

        private void TryLatchToWall()
        {
            if (topCollider.IsTouchingLayers(wallLayer))
            {
                // 1. Set latch point
                latchPoint = topCollider.bounds.center;
                swingRadius = Vector2.Distance(latchPoint, transform.position);

                // 2. Initialize physics
                currentAngle = 0f; // 0° = straight down
                swingMomentum = 0f;

                // 3. Force initial position
                ForceSwingPosition();

                // 4. Physics setup
                rb.linearVelocity = Vector2.zero;
                rb.gravityScale = 0f;
                isLatched = true;
            }
        }

        private void ForceSwingPosition()
        {
            // Calculate exact target position
            Vector2 targetPos = latchPoint + new Vector2(
                Mathf.Sin(currentAngle * Mathf.Deg2Rad),
                -Mathf.Cos(currentAngle * Mathf.Deg2Rad)
            ) * swingRadius;

            // Directly set position (bypassing physics smoothing)
            rb.transform.position = targetPos;
            rb.linearVelocity = Vector2.zero;

            // Smooth rotation
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }

        private void HandleSwingPhysics()
        {
            if (checkCollider.IsTouchingLayers(collisionLayers))
            {
                swingMomentum *= 0f;
                // Improved (frame-rate independent):
                currentAngle = Mathf.MoveTowards(currentAngle, 0f, 90f * Time.fixedDeltaTime);
                
            }
            else
            {
                // 1. Get input
                float moveInput = m_MoveAction.ReadValue<Vector2>().x * inputMultiplier;

                // 2. Apply swing force
                if (Mathf.Abs(moveInput) > 0.1f)
                {
                    swingMomentum += moveInput * swingSpeed * Time.fixedDeltaTime;
                }

                // 3. Apply pendulum physics
                swingMomentum += (-currentAngle * gravityPull * Time.fixedDeltaTime);
            }
               
            swingMomentum *= swingDrag;
            currentAngle += swingMomentum * Time.fixedDeltaTime;


            // 4. Update position
            ForceSwingPosition();
        }


        protected override void ComputeVelocity()
        {
            // Handle normal movement
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y *= model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            if (IsGrounded)
            {
                //need to reset when we have animation
                //animator.SetBool("grounded", IsGrounded);
                //animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);
                targetVelocity = move * maxSpeed;
            }
            else
            {
                //animator.SetBool("grounded", IsGrounded);
                //animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed * 2);
                targetVelocity = move * maxSpeed * 2;
            }
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}