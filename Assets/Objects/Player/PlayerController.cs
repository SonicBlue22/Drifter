﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyStuff;
using MyStuff.GeometryObjects;

[RequireComponent(typeof(PlayerClass))]
public class PlayerController : MonoBehaviour {
    #region Variables
    [Header("Ground Movement")]
    public float walkingSpeed = 10.0f;
    public float jumpingSpeed = 10.0f;
    public float jumpCooldown = 0.5f;
    public float jumpCoolLeft { get; private set; }


    [Header("Wall Movement")]
    public float slideAcceleration = 1.0f;
    public float walljumpSpeed = 10.0f;
    public float walljumpCooldown = 0.5f;
    

    [Header("Air Movement")]
    public float horizontalForce = 5.0f;
    public float verticalForce = 5.0f;
    public float friction = 0.01f;

    [Header("Rope Movement")]
    public float retractRate = 0.1f;

    [Header("Other")]
    public float boxError = 1.01f;

    public float groundError = 0.02f;
    public bool onGround { get; private set; }
    public float groundTime { get; private set; }

    public float wallError = 0.02f;
    public bool onWall { get; private set; }
    public float wallTime { get; private set; }

    public bool inAir => (!onWall) && (!onGround);
    public float airTime { get; private set; }

    public int flashModulus = 2;
    public int flashThreshold = 0;

    /// <summary>True = Right, False = Left</summary>
    public bool facing { get; private set; }

    [HideInInspector] public Vector2 size;

    public class InputSummary { public float horizontal, vertical; }
    [HideInInspector] public InputSummary inputs = new InputSummary();

    PlayerClass player;
    new Rigidbody2D rigidbody => player.rigidbody;
    BoxCollider2D boxCollider => player.collider;
    Rope rope => player.rope;
    SpriteRenderer spriteRenderer => player.renderer;
    LineRenderer lineRenderer => player.line;
    Healthbar healthbar => player.healthbar;

    float Width => boxCollider.size.x * transform.localScale.x;
    float Height => boxCollider.size.y * transform.localScale.y;
    #endregion

    #region Update
    public void Start() {
        MyFunctions.AddCameraListener(this);
        player = GetComponent<PlayerClass>();
        size = boxCollider.size * transform.lossyScale;
    }

    public RaycastHit2D aim;
    public void OnCameraReady() {
        var position = (Vector2)transform.position;
        var mouse = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        #region Aiming
        var aimDirection = mouse - position;
        aim = Physics2D.Raycast(position, aimDirection);
        if(aim) {
            lineRenderer.SetPositions(new Vector3[] { transform.position, aim.point });

            var fire1 = Input.GetButtonDown("Fire1");
            if(fire1) rope.Connect(aim.point);

        } else {
            lineRenderer.SetPositions(new Vector3[] { transform.position, aimDirection.normalized * 100000 });
        }

        var fire2 = Input.GetButtonDown("Fire2");
        if(fire2) rope.Disconnect();

        var fire3 = Input.GetButtonDown("Fire3");
        if(fire3) { }
        #endregion
    }

    public void Update() {
        var newColor = spriteRenderer.color;
        if(healthbar.invincible) newColor.a = Time.frameCount % flashModulus > flashThreshold ? 0f : 1f;
        else newColor.a = 1f;
        spriteRenderer.color = newColor;
    }

    public void FixedUpdate() {
        jumpCoolLeft -= Time.fixedDeltaTime;
        var position = (Vector2)transform.position;

        #region Surface Detection
        // Ground detection //
        RaycastHit2D groundHit = BoxCast(Direction.Down);

        if(groundHit) groundTime += Time.fixedDeltaTime;
        else groundTime = 0f;
        onGround = groundHit && groundTime > 0.1f;

        GameObject ground = null;
        if(groundHit) ground = groundHit.collider.gameObject;


        // Wall Detection //   todo: do not run this if onGround
        RaycastHit2D wallHit;
        var leftHit = BoxCast(Direction.Left);
        var rightHit = BoxCast(Direction.Right);
        if(leftHit ^ rightHit)
            wallHit = leftHit ? leftHit : rightHit;
        else if(leftHit && rightHit)
            wallHit = leftHit.distance < rightHit.distance ? leftHit : rightHit;
        else wallHit = leftHit; //arbitrary. neither are true. compiler wants it to be something, though.

        if(wallHit) wallTime += Time.fixedDeltaTime;
        else wallTime = 0f;
        onWall = wallHit && wallTime > 0.1f;

        GameObject wall = null;
        if(wallHit) wall = wallHit.collider.gameObject;
        #endregion

        #region Movement
        // Movement options when on the ground //
        if(onGround) {
            float horizontalInput = Input.GetAxis("Horizontal");
            inputs.horizontal = horizontalInput;
            float verticalInput = Input.GetAxis("Vertical");
            inputs.vertical = verticalInput;
            float jumpInput = Input.GetAxis("Jump");

            //Facing
            if(horizontalInput != 0f)
                facing = horizontalInput > 0f;

            //Jumping
            float jumpingForce = jumpInput * jumpingSpeed;
            if(jumpCoolLeft > 0) jumpingForce = 0f;
            if(jumpingForce > 0) jumpCoolLeft = jumpCooldown;
            rigidbody.AddForce(Vector2.up * jumpingForce, ForceMode2D.Impulse);

            //Walking
            float walkingForce = horizontalInput * walkingSpeed;
            if(GoodWalkingSpeedOn(ground, Geometry.LinearDirection(walkingForce, Axis.Horizontal)))
                rigidbody.AddForce(Vector2.right * (walkingForce - RelativeX(ground)), ForceMode2D.Impulse);
        }

        // Movement options when sliding on a wall //
        else if(onWall) {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            inputs.horizontal = horizontalInput;
            float verticalInput = Input.GetAxis("Vertical");
            inputs.vertical = verticalInput;
            float jumpInput = Input.GetAxis("Jump");

            //Facing
            facing = wallHit == leftHit;

            //Sliding
            float slideForce = verticalInput * slideAcceleration;
            rigidbody.AddForce(Vector2.up * slideForce);

            //Jumping
            float walljumpForce = jumpInput * walljumpSpeed;
            if(jumpCoolLeft > 0) walljumpForce = 0f;
            if(walljumpForce > 0) jumpCoolLeft = walljumpCooldown;
            if(facing) rigidbody.AddForce(Vector2.right.Rotate(Mathf.Deg2Rad * 60f) * walljumpForce, ForceMode2D.Impulse);
            else rigidbody.AddForce(Vector2.right.Rotate(Mathf.Deg2Rad * 120f) * walljumpForce, ForceMode2D.Impulse);

        }

        // Movement options when in midair //
        else {
            float horizontalInput = Input.GetAxis("Horizontal");
            inputs.horizontal = horizontalInput;
            float verticalInput = Input.GetAxis("Vertical");
            inputs.vertical = verticalInput;

            //Magic
            Vector2 inputForce = new Vector2(0, 0);
            inputForce.x =  horizontalInput * horizontalForce;
            inputForce.y =    verticalInput * verticalForce;
            rigidbody.AddForce(inputForce);

            //'Friction'
            rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, Vector2.zero, friction);

        }
        #endregion

        #region Rope
        if(rope.Attached) {
            float retractInput = Input.GetAxis("Jump");
            rope.ShortenRope(retractInput * retractRate);
        }
        #endregion

    }

    public void OnDamage() {
    }

    public void OnHealed() {

    }

    public void OnDeath() {

    }

    #endregion

    #region Methods

    private bool GoodWalkingSpeedOn(GameObject ground, Direction direction) {
        float relativeX = RelativeX(ground);

        if(direction == Direction.Left)
            return relativeX > -walkingSpeed;
        else return relativeX < walkingSpeed;
    }

    private float RelativeX(GameObject ground) {
        float relativeX = 0f;

        if(ground != null) {
            Rigidbody2D groundRigidbody = ground.GetComponent<Rigidbody2D>();
            if(groundRigidbody != null)
                relativeX = rigidbody.velocity.x - groundRigidbody.velocity.x;
        }

        return relativeX;
    }

    /// <summary>
    /// A method specifically for the player that detects if they are on the ground or a wall. Used for walking and wall jumping respectively.
    /// </summary>
    /// <param name="direction">Which surface are you trying to detect?</param>
    private RaycastHit2D BoxCast(Direction direction) {
        float angle = transform.rotation.eulerAngles.z;

        float distance = 0f;
        if(direction == Direction.Right || direction == Direction.Left)
            distance = wallError;
        else distance = groundError;
        
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, size * boxError, Angle.DegDirection(direction), Geometry.VectorDirection(direction), distance);
        //if(hit) ExtendedDebug.BoxCast2D(transform.position, hit, Color.red);
        return hit;
    }

    #endregion
}
