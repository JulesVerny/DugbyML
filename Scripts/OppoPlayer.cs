using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OppoPlayer : MonoBehaviour
{
    // =========================================================================
    public enum PlayerState {Idle, Running,PickingUpBall,TacklingPlayer,RunningWithBall,PlantingBall,KickingBall};

    //public bool OwnPlayer;
    public GameObject TheOpSmartPlayer;
    public GameObject TheOpGoalArea;
    public GameObject TheBall;
    public GameObject TheGameManager; 
    private int DifficultyLevel;
    private bool OppoFirstHandle; 

    //private float WalkSpeed = 1.0f;
    private float RunSpeed = 2.0f;
    private float gravity = -9.8f;
    //private float PlayerRotationRate = 150.0f;   // Not uswed by Oppo
    bool CurrentlyGrounded;

    private Vector3 DeltaLocalMovement;
    private PlayerState ThePlayerCurrentState; 

    private bool CurrentlyHasBall = false;
    private bool OppoCurrentlyHasBall = false; 

    private float DistanceToTheBall;
    private float PickupBallThreshold = 0.9f;
    private float DistanceToOtherPlayer;
    private float TackleDistance = 0.8f; 

    private CharacterController TheCharController;
    private Animator ThePlayerAnimator;
    private string CurrentAnnimationName;
    private int NumberSteps; 

    // =========================================================================
    void Start()
    {
        ThePlayerCurrentState = PlayerState.Idle;

        if (GetComponent<CharacterController>() != null) TheCharController = GetComponent<CharacterController>();
        else Debug.Log("*** ERROR: Player Cannot Get Its Character Controller");

        if (GetComponent<Animator>() != null) ThePlayerAnimator = GetComponent<Animator>();
        else Debug.Log("*** ERROR: Player Could Not Get Its Animator");

        ResetPlayerEpisode(1);

    }  // Start
    // =========================================================================
    public void ResetPlayerEpisode(int RequestedDifficultyLevel)
    {
        CurrentlyHasBall = false;
        OppoCurrentlyHasBall = false;
        OppoFirstHandle = true;
        NumberSteps = 0;
        DifficultyLevel = RequestedDifficultyLevel; 
        Vector3 RandomStartPosition = Vector3.zero;

        // Oppo Player Initial Starting Postions
        if(DifficultyLevel<=2) RandomStartPosition.x = TheOpGoalArea.transform.position.x + Random.Range(15.0f, 17.5f);
        else RandomStartPosition.x = TheOpGoalArea.transform.position.x + Random.Range(14.0f, 16.0f);

        RandomStartPosition.y = TheOpGoalArea.transform.position.y - 0.5f;
        RandomStartPosition.z = TheOpGoalArea.transform.position.z + Random.Range(-2.0f, 2.0f);
        TheCharController.enabled = false;
        transform.position = new Vector3(RandomStartPosition.x, RandomStartPosition.y, RandomStartPosition.z);
        transform.rotation = Quaternion.Euler(0.0f, -90.0f, 0.0f);
        TheCharController.enabled = true;

        // Opposition Player
        if (DifficultyLevel == 1) SetPlayerIdleState();
        if(DifficultyLevel > 1) SetPlayerRunningState();

    }  // ResetPlayerEpisode
    // ============================================================================
    public void OppoBallStatusUpdate(bool OppoHasBall)
    {
        OppoCurrentlyHasBall = OppoHasBall;
        if (OppoCurrentlyHasBall)
        {
            // Need to Ensure Both Players Cannot Have the Ball 
            CurrentlyHasBall = false;
            if (ThePlayerCurrentState == PlayerState.RunningWithBall)  SetPlayerRunningState();
            if (ThePlayerCurrentState == PlayerState.PickingUpBall) SetPlayerRunningState();
            if (ThePlayerCurrentState == PlayerState.PlantingBall) SetPlayerRunningState();
            if (ThePlayerCurrentState == PlayerState.KickingBall) SetPlayerRunningState();

        }
    } // OppoBallStatusUpdate
    // ============================================================================
    // UI Updates
    void Update()
    {
       // No User Controls for Oppo Player

    } // Update
    // ================================================================
    void CheckandPerformTackle()
    {
        // Tackle Requestd Check Other Player Has Ball 
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((ThePlayerCurrentState == PlayerState.Running) && (CurrentAnnimationName == "Run"))
        {
            if ((!CurrentlyHasBall) && (OppoCurrentlyHasBall))
            {
                // Check Oppo Has the Ball
                if (DistanceToOtherPlayer < TackleDistance)
                {
                    // It is a legitmate Tackle
                    ThePlayerCurrentState = PlayerState.TacklingPlayer;
                    SetPlayerTacklingPlayerState();
                }
            }  // Check Oppo Has the Ball
        } // Only Pickup Will Running
    } // CheckandPerformTackle()
    // =============================================================
    void CheckandPerformBallPickup()
    {
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((ThePlayerCurrentState == PlayerState.Running) && (CurrentAnnimationName == "Run"))      
        {
            // Check thta Neither Player Has the Ball
            if ((!OppoCurrentlyHasBall) && (!CurrentlyHasBall))
            {
                // Requesting Picking Up the Ball  - Check within   PickupBallThreshold 
                if ((DistanceToTheBall < PickupBallThreshold))
                {
                    // Take Ball if its In Front of Player
                    Vector3 DirectionToBall = (TheBall.transform.position - transform.position).normalized;
                    if (Vector3.Dot(transform.forward, DirectionToBall) > 0.6f)
                    {
                        ThePlayerCurrentState = PlayerState.PickingUpBall;
                        SetPlayerPickingUpTheBallState();
                    }
                }
            } // neither Has the Ball
        } // Only Pickup Will Running
    } // CheckandPerformBallPickup
    // ====================================================================
    void CheckandPerformKick()
    {
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnnimationName == "RunWith") || (CurrentAnnimationName == "Run"))
        {
            // Requesting Kicking the Ball
            if ((DistanceToTheBall < PickupBallThreshold) && (!OppoCurrentlyHasBall))
            {
                if (CurrentlyHasBall)
                {
                    // Kicking from Chest
                    ThePlayerCurrentState = PlayerState.KickingBall;
                    SetPlayerKickingBallState();
                    //Debug.Log("Player Kick Ball:Chest");
                }
                else
                {
                    // Kicking From Ground, so Make sure Ball in Front
                    Vector3 DirectionToBall = (TheBall.transform.position - transform.position).normalized;
                    float PlayerBallDirectionDot = Vector3.Dot(transform.forward, DirectionToBall);

                    if (PlayerBallDirectionDot > 0.6f)
                    {
                        ThePlayerCurrentState = PlayerState.KickingBall;
                        SetPlayerKickingBallState();
                        //Debug.Log("Player Kick Ball:Ground");
                    }
                    // fn to Kick Ball  - Move to later in Animation 
                }
            }  // Distance Check
        }
    } // CheckandPerformKick()
    // ============================================================================
    float NormalisedAngle(Vector3 Origin, Vector3 Destination)
    {
        float returnAngle = 0.0f;
        float DeltaX = Destination.x - Origin.x;
        float DeltaZ = Destination.z - Origin.z;
        float AngleRad = Mathf.Atan2(DeltaZ, DeltaX)* Mathf.Rad2Deg;
        returnAngle = AngleRad/180.0f;

        return returnAngle;
    } // NormalisedAngle
    // ==========================================================================
    void FixedUpdate()
    {
        // Get  Distance to the Other Player
        DistanceToOtherPlayer = Vector3.Distance(transform.position, TheOpSmartPlayer.transform.position);
        NumberSteps = NumberSteps + 1;

        // ================================
        // Check Player and Ball Update Interactions
        if (CurrentlyHasBall)
        {
            // Ball Held Position is Player (Feet) Position + 0.2m Forward direction and 1.0m high 
            Vector3 BallHeldPosition = transform.position + transform.forward * 0.3f + new Vector3(0.0f, 0.9f, 0.0f);
            TheBall.SendMessage("UpdateHeldPosition", BallHeldPosition); 

        }   // Player Has Ball
        else
        {
            // Set Distance to the Ball  - 
            DistanceToTheBall = Vector3.Distance(transform.position, TheBall.transform.position);

        }  // Player Does not Have Ball
        // ===============================
        

        // ===========================================================================================   
        // Level of Opposition Play fn (OppoLevel) 
        // Regardless Towards the Ball if some way away from Ball 
        if((DistanceToTheBall > PickupBallThreshold) && (!CurrentlyHasBall))
        {
            Vector3 GroundedBallPosition = new Vector3(TheBall.transform.position.x, 0.2f, TheBall.transform.position.z);
            transform.LookAt(GroundedBallPosition, Vector3.up);
        }
        // ===================================
        // Oppo Movement Controls 
        DeltaLocalMovement = Vector3.zero;
        // Oppo 1: Don't Move at all
        // Oppo 2: Move Towards Ball 
        if (DifficultyLevel > 1)
        {
            // Speed Adjustments 
            DeltaLocalMovement = Vector3.zero;
            if(DifficultyLevel <= 2) DeltaLocalMovement = transform.forward * 0.2f * RunSpeed;
            if((DifficultyLevel > 2) && (DifficultyLevel <= 3)) DeltaLocalMovement = transform.forward * 0.25f * RunSpeed;
            if ((DifficultyLevel > 3) && (DifficultyLevel < 5)) DeltaLocalMovement = transform.forward * 0.35f * RunSpeed;
            if (DifficultyLevel >= 5) DeltaLocalMovement = transform.forward * 0.45f * RunSpeed;

        } // Oppo Level 2 and above

        //  Perfom Oppo Movement Alwyas if Level 3, or if Level2 and not yet Handled 
        if ((DifficultyLevel > 2) || ((DifficultyLevel > 1) && (OppoFirstHandle))) PerformDeltaMovement(DeltaLocalMovement);    // Perform Movement       

        // ================================
        // Oppo 2: Move Towards Ball, Take Ball, but note at Oppo2: Will Not move after
        if (DifficultyLevel > 1)
        {
            // Check and Perfom Ball Pickup 
            CheckandPerformBallPickup();

            // Turn Towards Oppostion Goal if Currently Have The Ball
            if (CurrentlyHasBall)
            {
                transform.LookAt(TheOpGoalArea.transform.position, Vector3.up);
            }  
        } // Oppo Level 3 Face and Run Towards Oppo Goal 
        // ===================================
        // Oppo 4: Full Capabiltiy  
        if (DifficultyLevel > 3)
        {
            CheckandPerformTackle();    // Has abiltiy to Tackle Blue player
            // ** A Random choice, whether to Kick or run leave and Run for Try 
            float RandomKick = Random.Range(0.0f, 100.0f); 
        
            // Repeat Sanity Kick Ball Check 
            if ((DistanceToTheBall < PickupBallThreshold) && (!OppoCurrentlyHasBall) && (RandomKick>50.0f)) 
            {
                // Only Kick From Outside Oppo Goal Area 
                Vector3 OppoGoalDifference = TheOpGoalArea.transform.position - TheBall.transform.position;
                float DeltaXPitch = OppoGoalDifference.x;
                if (Mathf.Abs(DeltaXPitch) > 4.0f)    // Outside Goal Area
                {
                    // Check Sensible Kick Direction, Ball and Oppo Goal in Simimar Direciton 
                    Vector3 BallDirection = (TheBall.transform.position - transform.position).normalized;
                    Vector3 GoalDirection = (TheOpGoalArea.transform.position - transform.position).normalized;
                    float GoalandBallAlignment = Vector3.Dot(BallDirection, GoalDirection);

                    if ((GoalandBallAlignment > 0.6f) && (DistanceToOtherPlayer < TackleDistance * 3.0f))
                    {
                        Debug.Log("Red Oppo Decided to Kick");
                        CheckandPerformKick();
                    }
                } // Only Kick From Outside Gaol Area 
            }  // sanity Kick Ball Check
        } // (OppoLevel > 5)
        // ===================================

        // Now Check Whether TheBall within opposite GoalPost Area if neither Player currently has the Ball
        if ((!CurrentlyHasBall)&&(!OppoCurrentlyHasBall)) CheckBallBetweenGoalPosts();
        // =====================================================
        
        // Check Auto Plant if Running With Ball in Oppo Goal Area
        if ((ThePlayerCurrentState == PlayerState.RunningWithBall)  && (CurrentlyHasBall))
        {
            // Check if in Oppo Goal Area
            Vector3 OppoGoalDifference = TheOpGoalArea.transform.position - TheBall.transform.position;
            float DeltaXPitch = OppoGoalDifference.x;
            if (Mathf.Abs(DeltaXPitch) < 2.0f)
            {
                // Player With Ball in Oppo Gaol Area
                // Need to check that Player currently Running and has Fully completed any Picking Up, Kicking Tackling, Annimation States
                CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                if (CurrentAnnimationName == "RunWith")
                {
                    ThePlayerCurrentState = PlayerState.PlantingBall;
                    SetPlayerPlantingBallState();
                }
            }  // Within Oppo Goal Area
        }  // Running With Ball
        // ====================================================
        // Check Picking Up Annimation Progress
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnnimationName=="Pickup") && (ThePlayerCurrentState== PlayerState.PickingUpBall))
        {
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.5f)
            {
                CurrentlyHasBall = true;
                TheOpSmartPlayer.SendMessage("OppoBallStatusUpdate", true);
                TheBall.SendMessage("Taken");

                OppoFirstHandle = false;
            }
            // Only Change Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >=1.0f)
            {
                SetPlayerRunningWithBallState();

                TheGameManager.SendMessage("UpdateNarrativeString", "Red Now Has the Ball");

            }  // 100 % Complte

        }  // Pickup Animation Progres Check 
        // =================================
        // Check Planting Ball Annimation Progress
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnnimationName == "PlantBall") && (ThePlayerCurrentState == PlayerState.PlantingBall))
        {
            // Plant the Ball at 10% Annimaiton 
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.1f)
            {
                Vector3 BallPlantedPosition = transform.position + transform.forward * 0.5f + new Vector3(0.0f, 0.2f, 0.0f);
                TheBall.SendMessage("PlantBall", BallPlantedPosition);
                CurrentlyHasBall = false;
                TheOpSmartPlayer.SendMessage("OppoBallStatusUpdate", false);

            }  // 10% Through Plant Annimation
            // Only Check New Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
            {
                // Now Check if Scored - x Distance within Oppo Goal
                CheckBallPlantedinGoalArea();
            }  // Plant Annimation Completed

        }  // Planting Ball Animation Progres Check 
        // =================================
        // Check Kicking Annimation Progress
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnnimationName == "Kick") && (ThePlayerCurrentState == PlayerState.KickingBall))
        {
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.6f)
            {
               
                if(CurrentlyHasBall) TheBall.SendMessage("ApplyChestKick", transform.forward);      
                else TheBall.SendMessage("ApplyGroundKick", transform.forward);
                CurrentlyHasBall = false;
                TheOpSmartPlayer.SendMessage("OppoBallStatusUpdate", false);

                TheGameManager.SendMessage("UpdateNarrativeString", "Red has Kicked the Ball");
            }
            // Only Change Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
            {
                SetPlayerRunningState();
            }

        }  // Ball Kicking Animation Progres Check 
        // =================================
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnnimationName == "Tackle") && (ThePlayerCurrentState == PlayerState.TacklingPlayer))
        {
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.75f)
            {
                TheOpSmartPlayer.SendMessage("TakeTackle");
                TheGameManager.SendMessage("UpdateNarrativeString", "Great Tackle");
            }
            // Only Change Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
            {
                // After the Tackle the Oppo Keeps running (Moviong) without Ball to find Ball Again
                SetPlayerRunningState(); 
            }
        }  // Tackling Player Animation progress 
        // ====================================

    } // Fixed Update
      // ====================================================================================================
    void PerformDeltaMovement(Vector3 TheDeltaMovement)
    {
        // May need a better Grounded Function, Ray cast Down Height Calculation
        TheDeltaMovement.y = 0.0f;    // ** Try to Avoid Sky Walking !
        if (!CurrentlyGrounded)
        {
            TheDeltaMovement.y = 100.0f * gravity;
        }
        TheDeltaMovement = TheDeltaMovement * Time.deltaTime;
        // Now Perform the actual Character Contoller Movement    
        TheCharController.Move(TheDeltaMovement);

    }// PerformDeltaMovement
    // =========================================================================================================
    public void TakeTackle()
    {
        // Player Has been tackled - Assume always Loses Ball, In Behind Direction
        if (CurrentlyHasBall)
        {
            CurrentlyHasBall = false;
            TheOpSmartPlayer.SendMessage("OppoBallStatusUpdate", false);
            Vector3 BallLostPosition = transform.position - 3.5f * transform.forward + 2.0f*transform.up + 0.5f * transform.right;   // Instruct Ball to be lost behind Player
            TheBall.SendMessage("PlayerLostBall", BallLostPosition);       // 

            // Assume will want to Keep on Running - without Ball
            SetPlayerRunningState();
        }
    }  // TakeTackle
    // =========================================================================================================
    void CheckBallPlantedinGoalArea()
    {
        Vector3 OppoGoalDifference = TheOpGoalArea.transform.position - TheBall.transform.position;
        float DeltaXPitch = OppoGoalDifference.x;
        
        if (Mathf.Abs(DeltaXPitch) < 2.5f)
        {
            
            TheGameManager.SendMessage("IncrementRedScoreTry");
            TheGameManager.SendMessage("UpdateNarrativeString", "Red has Scored a Try");
         
            Debug.Log(" Red Player Scored a Try ! ");
            TheOpSmartPlayer.SendMessage("OppoJustScored");
        }
        else
        {
            // Not Planted in Goal Area so Simply set back Running
            SetPlayerRunningState();
        }
    } // CheckPossedBallinGoalArea
    // ==========================================================================================================
    void CheckBallBetweenGoalPosts()
    {
        Vector3 OppoGoalDifference = TheOpGoalArea.transform.position - TheBall.transform.position;
        float DeltaXPitch = OppoGoalDifference.x;
        float DeltaZPitch = OppoGoalDifference.z; 

        if ((Mathf.Abs(DeltaXPitch) < 0.4f) && (Mathf.Abs(DeltaZPitch) < 1.8f))
        {
        
            TheGameManager.SendMessage("IncrementRedScoreGoal");
            TheGameManager.SendMessage("UpdateNarrativeString", "Red has Scored a Goal");
            
            Debug.Log(" Red Player Scored Goal !");

            TheOpSmartPlayer.SendMessage("OppoJustScored");
        } // Delta X Next to Goal Posts, and Z Within Goal Posts

    } // CheckBallBetweenGoalPosts
    // ===========================================================================================================
    void OnCollisionEnter(Collision theCollision)
    {
        if (theCollision.gameObject.tag == "Floor") CurrentlyGrounded = true;

    }  // OnCollisionEnter

    //consider when character is jumping .. it will exit collision.
    void OnCollisionExit(Collision theCollision)
    {
        if (theCollision.gameObject.name == "floor") CurrentlyGrounded = false;
    } // OnCollisionExit
    // ===============================================================
    // public enum PlayerState {Idle, Running,PickingUpBall,TacklingPlayer,RunningWithBall, PlantingBall,KickingBall};
    // ====================================================
    void SetPlayerIdleState()
    {
        ThePlayerCurrentState = PlayerState.Idle;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("PlayerHasBall", false);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", false);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerIdleState
    // ==================================================
    void SetPlayerRunningState()
    {
        ThePlayerCurrentState = PlayerState.Running;
        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("PlayerHasBall", false);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", false);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerRunningState
      // ==================================================
    void SetPlayerPickingUpTheBallState()
    {
        ThePlayerCurrentState = PlayerState.PickingUpBall;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("PlayerHasBall", false);

        ThePlayerAnimator.SetBool("IsPickingUp", true);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", false);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerPickingUpTheBallState
    // ==================================================
    void SetPlayerTacklingPlayerState()
    {
        ThePlayerCurrentState = PlayerState.TacklingPlayer;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("PlayerHasBall", false);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", true);
        ThePlayerAnimator.SetBool("IsKicking", false);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerTacklingPlayerState
    // ==================================================
    void SetPlayerRunningWithBallState()
    {
        ThePlayerCurrentState = PlayerState.RunningWithBall;
        ThePlayerAnimator.SetBool("IsRunning", true);
        ThePlayerAnimator.SetBool("PlayerHasBall", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", false);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerRunningWithBallState
    // ==================================================
    void SetPlayerPlantingBallState()
    {
        ThePlayerCurrentState = PlayerState.PlantingBall;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("PlayerHasBall", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", false);
        ThePlayerAnimator.SetBool("IsPlanting", true);
    } // SetPlayerPlantingBallState

    // ==================================================
    void SetPlayerKickingBallState()
    {

        // Debug
       // CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
       // Debug.Log("Player Kick Set True, With Current Annimation:" + CurrentAnnimationName); 

        ThePlayerCurrentState = PlayerState.KickingBall;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("PlayerHasBall", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", true);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerKickingBallState
    // ==================================================
    
}
