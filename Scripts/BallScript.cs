using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallScript : MonoBehaviour
{
    // =======================================================================
    public GameObject BlueGoalArea;
    public GameObject RedGoalArea;
    public GameObject TheGameManager;
    public GameObject TheBluePlayer; 

    private bool CurrentlyBeingHeld;
    private Rigidbody BallRigidBody;
    private SphereCollider TheSphereCollider;
   
    // Start is called before the first frame update
    void Start()
    {
        CurrentlyBeingHeld = false;
        BallRigidBody = GetComponent<Rigidbody>();
        TheSphereCollider = GetComponent<SphereCollider>();

        ResetBallPosition();

    } // Start 
    // =======================================================================
    public void ResetBallPosition()
    {
        Vector3 BallResetPosition = Vector3.zero;
        BallResetPosition.x = 0.5f*(BlueGoalArea.transform.position.x + RedGoalArea.transform.position.x);
        BallResetPosition.y = BlueGoalArea.transform.position.y;
        BallResetPosition.z = 0.5f * (BlueGoalArea.transform.position.z + RedGoalArea.transform.position.z);
        
        TheSphereCollider.enabled = true;
        BallRigidBody.isKinematic = false;
        BallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities
        BallRigidBody.angularVelocity = Vector3.zero;

        transform.position = BallResetPosition;

        // Debug.Log("Ball Has Been reset: " + transform.position.x.ToString());
    }
    // =======================================================================
    void Update()
    {
        // None  
    } // Update
   
    // ===============================================================
    void FixedUpdate()
    {
        // Check Ball out of Bounds (e.g. Kicked Out )
        if (transform.position.y < BlueGoalArea.transform.position.y -5.0f)
        {
            TheGameManager.SendMessage("UpdateNarrativeString", "Ball Kicked off Pitch !");
            TheBluePlayer.SendMessage("BallRequestedEndEpisode");
        } // Check Out of Bounds

    } // Update
    // =========================================================================
    public void UpdateHeldPosition(Vector3 HeldPosition)
    {
        if (CurrentlyBeingHeld)
        {
            transform.position = HeldPosition;
        }
    } // UpdateHeldPosition
    // =========================================================================
    public void ApplyChestKick(Vector3 KickDirection)
    {
        CurrentlyBeingHeld = false;
        BallRigidBody.isKinematic = false;
        // Add some elevation
        KickDirection.y = 0.75f;
        BallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Befor Kick
        BallRigidBody.angularVelocity = Vector3.zero;
   
        BallRigidBody.AddForce(KickDirection * 4.5f, ForceMode.Impulse);
        
        TheSphereCollider.enabled = true;

    } // ApplyChestKick
      // =========================================================================
    public void ApplyGroundKick(Vector3 KickDirection)
    {
        CurrentlyBeingHeld = false;
        BallRigidBody.isKinematic = false;
        // Add some elevation
        KickDirection.y = 0.4f;
        BallRigidBody.velocity = Vector3.zero;  // Clear out any Residual Velocities Before Kick
        BallRigidBody.angularVelocity = Vector3.zero;

        BallRigidBody.AddForce(KickDirection * 3.25f, ForceMode.Impulse);
        TheSphereCollider.enabled = true;

    } // ApplyKick
    // =========================================================================
    public void Taken()
    {
        CurrentlyBeingHeld = true;
        BallRigidBody.isKinematic = true;
        TheSphereCollider.enabled = false;
    }
    // =========================================================================
    public void PlayerLostBall(Vector3 LostPosition)
    {
        CurrentlyBeingHeld = false;
        transform.position = LostPosition;
        BallRigidBody.isKinematic = false;
        TheSphereCollider.enabled = true;
    } // LostBall
    // =========================================================================
    public void PlantBall(Vector3 PlantPosition)
    {
        transform.position = PlantPosition; 
        CurrentlyBeingHeld = false;
        BallRigidBody.isKinematic = false;
        TheSphereCollider.enabled = true;
    } // LostBall
    // =========================================================================
}
