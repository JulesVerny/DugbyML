using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SmartAgent : Agent
{
    public enum PlayerState { Idle, Running, PickingUpBall, TacklingPlayer, RunningWithBall, PlantingBall, KickingBall };
    // ===================================================
    // public bool OwnPlayer;
    public GameObject TheOpPlayer;
    public GameObject TheOpGoalArea;
    public GameObject TheBall;
    public GameObject TheGameManager;

    private float RunSpeed = 2.0f;
    private float gravity = -9.8f;
    private float PlayerRotationRate = 150.0f;
    bool CurrentlyGrounded;

    private bool IsAbleToPickup;
    private bool IsAbleToKick;
    private bool IsAbleToTackle;

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

    private int EpisodeStepCount; 
    private int MaxStepsPerEpisodes = 5000;
    private bool FirstBallPickup = false;
    private bool FirstTackle = false;

    private int GameDifficulty = 1;     // Just Debug Game difficulty settings 

    // ==========================================================================================================================
    public override void Initialize()
    {
        ThePlayerCurrentState = PlayerState.Idle;

        if (GetComponent<CharacterController>() != null) TheCharController = GetComponent<CharacterController>();
        else Debug.Log("*** ERROR: Player Cannot Get Its Character Controller");

        if (GetComponent<Animator>() != null) ThePlayerAnimator = GetComponent<Animator>();
        else Debug.Log("*** ERROR: Player Could Not Get Its Animator");

        float LessonDifficultyLevelF = Academy.Instance.EnvironmentParameters.GetWithDefault("difficultylevel", 1.0f);
        GameDifficulty = (int)Mathf.CeilToInt(LessonDifficultyLevelF); 

        //OnEpisodeBegin();

    } // Initialize
    // ======================================================================================================================
    public override void OnEpisodeBegin()
    {
        CurrentlyHasBall = false;
        OppoCurrentlyHasBall = false;
        EpisodeStepCount = 0;
        FirstBallPickup = false;
        FirstTackle = false;

        IsAbleToPickup = false;
        IsAbleToKick = false;
        IsAbleToTackle = false; 

        Vector3 RandomStartPosition = Vector3.zero;
        GameDifficulty = 1; 
        float LessonDifficultyLevelF = Academy.Instance.EnvironmentParameters.GetWithDefault("difficultylevel", 1.0f);
        GameDifficulty = (int)Mathf.CeilToInt(LessonDifficultyLevelF);

        TheGameManager.SendMessage("UpdateLessonDisplay", GameDifficulty);

        //  Smart Player Initial Positions 
        RandomStartPosition.x = 14.5f;
        if(GameDifficulty==1) RandomStartPosition.x = TheOpGoalArea.transform.position.x - Random.Range(14.0f, 15.0f);
        if (GameDifficulty == 2) RandomStartPosition.x = TheOpGoalArea.transform.position.x - Random.Range(14.5f, 15.5f);
        if (GameDifficulty == 3) RandomStartPosition.x = TheOpGoalArea.transform.position.x - Random.Range(15.0f, 16.0f);
        if (GameDifficulty == 4) RandomStartPosition.x = TheOpGoalArea.transform.position.x - Random.Range(15.0f, 17.0f);
        if (GameDifficulty == 5) RandomStartPosition.x = TheOpGoalArea.transform.position.x - Random.Range(16.0f, 17.5f);

        RandomStartPosition.y = TheOpGoalArea.transform.position.y - 0.5f;
        RandomStartPosition.z = TheOpGoalArea.transform.position.z + Random.Range(-1.0f, 1.0f);
        if (GameDifficulty == 2) RandomStartPosition.z = TheOpGoalArea.transform.position.z + Random.Range(-1.5f, 1.5f);
        if (GameDifficulty == 3) RandomStartPosition.z = TheOpGoalArea.transform.position.z + Random.Range(-2.0f, 2.0f);
        if (GameDifficulty >= 4) RandomStartPosition.z = TheOpGoalArea.transform.position.z + Random.Range(-2.5f, 2.5f);

        TheCharController.enabled = false;
        transform.position = new Vector3(RandomStartPosition.x, RandomStartPosition.y, RandomStartPosition.z);
        transform.rotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
        TheCharController.enabled = true;

        SetPlayerIdleState();

        // Reste the Ball Position 
        TheBall.SendMessage("ResetBallPosition");

        // Now Reset the Oppo Player  with the Current Game Difficulty
        TheOpPlayer.SendMessage("ResetPlayerEpisode",GameDifficulty); 

    } // OnEpisodeBegin
    // =======================================================================
    public override void CollectObservations(VectorSensor sensor)
    {      
        // float:  Direction Euler:Y 
        // float:  OppoGoal.x - Player.x
        // float:  OppoGoal.z - Player.z
        // float:  Ball.x - Player.x
        // float:  Ball.z - Player.z
        // float:  OppoPlayer.x - Player.x
        // float:  OppoPlayer.z - Player.z  
        // 7x Directional/Postional floats
        sensor.AddObservation(transform.rotation.y);        // In lieu of Player Rotation
        sensor.AddObservation((TheOpGoalArea.transform.position.x - transform.position.x) / 12.0f);     // OppoGoal.x - Player.x
        sensor.AddObservation((TheOpGoalArea.transform.position.z - transform.position.z) / 4.5f);      // OppoGoal.z - Player.z
        sensor.AddObservation((TheBall.transform.position.x - transform.position.x) / 12.0f);           // Ball.x - Player.x
        sensor.AddObservation((TheBall.transform.position.z - transform.position.z) / 4.5f);            // Ball.z - Player.z
        sensor.AddObservation((TheOpPlayer.transform.position.x - transform.position.x) / 12.0f);       // OppoPlayer.x - Player.x
        sensor.AddObservation((TheOpPlayer.transform.position.z - transform.position.z) / 4.5f);        // OppoPlayer.z - Player.z

        // Enumerating the Discrete States:  (One Hot Encoded)
        // float:  Own Has Ball  (No: -1.0, Yes:1.0)
        // float:  Oppo Has Ball (No: -1.0, Yes: 1.0)
        // float:  AbleToPickUp (No: -1.0, Yes:1.0)
        // float:  AbleToKickBall (No: -1.0, Yes:1.0)
        // float:  AbleToTackle (No: -1.0, Yes:1.0) 
        // 5x Status float Observations 
        float OwnHasBallF = -1.0f;
        if (CurrentlyHasBall) OwnHasBallF = 1.0f;
        sensor.AddObservation(OwnHasBallF);             //  Own Has Ball  (No: -1.0, Yes:1.0)

        float OppoHasBallF = -1.0f;
        if (OppoCurrentlyHasBall) OppoHasBallF = 1.0f;
        sensor.AddObservation(OppoHasBallF);             //  Oppo Has Ball  (No: -1.0, Yes:1.0)

        float AbleToPickupF = -1.0f;
        if (IsAbleToPickup) AbleToPickupF = 1.0f; 
        sensor.AddObservation(AbleToPickupF);

        float AbleToKickF = -1.0f;
        if (IsAbleToKick) AbleToKickF = 1.0f;
        sensor.AddObservation(AbleToKickF);

        float AbleToTackleF = -1.0f;
        if (IsAbleToTackle) AbleToTackleF = 1.0f;
        sensor.AddObservation(AbleToTackleF);

        // So a total of 12 floats  Observations 

    } //CollectObservations
      // ======================================================================================================================
    float NormalisedAngle(Vector3 Origin, Vector3 Destination)
    {
        // Returns the Angle Direction from Origin position to Destination Position Vectors : Normalised between (-1.0f and +1.0f)
        // 
        float returnAngle = 0.0f;
        float DeltaX = Destination.x - Origin.x;
        float DeltaZ = Destination.z - Origin.z;
        float AngleRad = Mathf.Atan2(DeltaZ, DeltaX) * Mathf.Rad2Deg;
        returnAngle = AngleRad / 180.0f;

        return returnAngle;
    } // NormalisedAngle
    // =================================================================================================================================
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // ========================================
        // Initial (Fixed Update) Player Processing)    In Lieue of Fixed Update Processing 
        // Get  Distance to the Other Player
        DistanceToOtherPlayer = Vector3.Distance(transform.position, TheOpPlayer.transform.position);
        EpisodeStepCount = EpisodeStepCount + 1;
        if (EpisodeStepCount > MaxStepsPerEpisodes)
        {
            // Excessive Episode Step Count - Terminate the Episode
            SetReward(-1.0f);
            TheGameManager.SendMessage("UpdateNarrativeString", "");
            RequestEndEpisode();
        }  // 

        // ==================================================
        // Check Player and Ball Update Interactions
        if (CurrentlyHasBall)
        {
            // Ball Held Position is Player (Feet) Position + 0.2m Forward direction and 1.0m high 
            Vector3 BallHeldPosition = transform.position + transform.forward * 0.3f + new Vector3(0.0f, 0.9f, 0.0f);
            TheBall.SendMessage("UpdateHeldPosition", BallHeldPosition);

            // Reward Picking up the Ball, at least for the first Time
            if (FirstBallPickup == false)
            {
                AddReward(0.2f);
                FirstBallPickup = true;
            }
            // Reward Move Penalty (With Ball)
            AddReward(-0.5f / MaxStepsPerEpisodes);   // Half Move Penalty if Have the Ball

        }   // Player Has Ball
        else
        {
            // Capture Distance to the Ball  - 
            DistanceToTheBall = Vector3.Distance(transform.position, TheBall.transform.position);
            
            // Reward Move Penalty Fn of Oppo Player Has Ball
            if(OppoCurrentlyHasBall) AddReward(-2.0f / MaxStepsPerEpisodes);   // Move Penalty
            else AddReward(-1.0f / MaxStepsPerEpisodes);
        }  // Player Does not Have Ball
        // ======================================

        // Actions Branches:
        // Branch 0: Motion: discreteActionsOut[0] = 0:NOOP, 1: Foward, 2: Rotate Left, 3: Rotate Right, 4: Pickup, 5: Kick, 6: Tackle 

        // Process Rotations Motion Actions First
        if (actionBuffers.DiscreteActions[1] == 1) transform.Rotate(new Vector3(0.0f, PlayerRotationRate * Time.deltaTime, 0.0f), Space.Self);  // Rotate Negative Action
        if (actionBuffers.DiscreteActions[1] == 2) transform.Rotate(new Vector3(0.0f, -PlayerRotationRate * Time.deltaTime, 0.0f), Space.Self);     // Rotate Positive Action

        // Player Movement Control
        DeltaLocalMovement = Vector3.zero;

        // Check If Forward Motion being Requested 
        if (actionBuffers.DiscreteActions[0] == 1)
        {
            // Need to Set Running If Still Only Idle
            if (ThePlayerCurrentState == PlayerState.Idle) SetPlayerRunningState();
            if ((ThePlayerCurrentState == PlayerState.Running) || (ThePlayerCurrentState == PlayerState.RunningWithBall)) DeltaLocalMovement = transform.forward * RunSpeed;
        }
        // Perform the Character Delta Movement
        PerformDeltaMovement(DeltaLocalMovement);

        // Check Able to Pickup
        CheckAbleToPickup();

        // Check Able to Kick The Ball
        CheckandAbleToPerformKick();

        // Check If ABle to Tackle
        CheckandAbleToTackle(); 

        // Now Process Game Actions
        if ((actionBuffers.DiscreteActions[2] == 1) && (IsAbleToPickup))
        {
            ThePlayerCurrentState = PlayerState.PickingUpBall;
            SetPlayerPickingUpTheBallState();          // Pickup Action
        } // Pickup Operation

        if ((actionBuffers.DiscreteActions[2] == 2) && (IsAbleToKick))
        {
            // Kick Action
            ThePlayerCurrentState = PlayerState.KickingBall;
            SetPlayerKickingBallState();
        } // Kick Operation

        if ((actionBuffers.DiscreteActions[2] == 3) && (IsAbleToTackle))
        {
            ThePlayerCurrentState = PlayerState.TacklingPlayer;
            SetPlayerTacklingPlayerState();

            // Add some reward for that first tackle
            if(FirstTackle==false)
            {
                AddReward(0.2f);
                FirstTackle = true; 
            }
        }  // Tackle Operation
        // ===================================
        // Now perfom the Rest of the (Fixed Update) Player Processing and Game Progress Checks
        //
        //  Check Whether TheBall within opposite GoalPost Area if neither Player currently has the Ball
        if ((!CurrentlyHasBall) && (!OppoCurrentlyHasBall)) CheckBallBetweenGoalPosts();

        // ====================================
        // Check Auto Plant if Running With Ball in Oppo Goal Area
        if ((ThePlayerCurrentState == PlayerState.RunningWithBall) && (CurrentlyHasBall))
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
        if ((CurrentAnnimationName == "Pickup") && (ThePlayerCurrentState == PlayerState.PickingUpBall))
        {
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.5f)
            {
                CurrentlyHasBall = true;
                TheOpPlayer.SendMessage("OppoBallStatusUpdate", true);
                TheBall.SendMessage("Taken");
            }
            // Only Change Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
               SetPlayerRunningWithBallState();
               TheGameManager.SendMessage("UpdateNarrativeString", "Blue Now Has the Ball");
            }  // 100 % Complte

        }  // Pickup Annimation Progress Check 
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
                TheOpPlayer.SendMessage("OppoBallStatusUpdate", false);

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
                if (CurrentlyHasBall) TheBall.SendMessage("ApplyChestKick", transform.forward);
                else TheBall.SendMessage("ApplyGroundKick", transform.forward);
                CurrentlyHasBall = false;
                TheOpPlayer.SendMessage("OppoBallStatusUpdate", false);

                TheGameManager.SendMessage("UpdateNarrativeString", "Blue has Kicked the Ball");
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
                TheOpPlayer.SendMessage("TakeTackle");
                TheGameManager.SendMessage("UpdateNarrativeString", "Great Tackle");
            }
            // Only Change Player State when Annimation 100% Complete
            if (ThePlayerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1.0f)
            {
                // After the Tackle the Own Player Stands around, awaiting Next Instruction
                SetPlayerIdleState();
            }
        }  // Tackling Player Animation progress 
        // ====================================

    } // OnActionReceived  (In lieue of Fixed Update) 
    // =======================================================================
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Capture and Perform the Manual User Controls
        // Branch 0: Motion: discreteActionsOut[0] = 0:NOOP, 1: Foward, 2: Rotate Left, 3: Rotate Right, 4: Pickup, 5: Kick, 6: Tackle, 
        // Branch 1: Play  : discreteActionsOut[1] = 0:NOOP, 
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;      // NOOP, Forward Action
        discreteActionsOut[1] = 0;      // NOOP Rotate Left, Rotate Right Actions
        discreteActionsOut[2] = 0;      // NOOP Pickup, Kick, Tackle Actions

        // Keyboard Motion Actions 
        if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[0] = 1;       // Move Forward
        if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[1] = 1;    // Rotate Left
        if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[1] = 2;     // Rotate Right

        // Keyboard Play Actions 
        if (Input.GetKey(KeyCode.P) && !CurrentlyHasBall) discreteActionsOut[2] = 1;    // Pickup Action
        if (Input.GetKey(KeyCode.K)) discreteActionsOut[2] = 2;                          // Kick Action 
        if (Input.GetKey(KeyCode.T)) discreteActionsOut[2] = 3;                         // Tackle Action
   
        // ===========================================

    } // Heuristic
    // ==============================================================================================
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
     // ====================================================================================================
    public void OppoBallStatusUpdate(bool OppoHasBall)
    {
        OppoCurrentlyHasBall = OppoHasBall;
        if (OppoCurrentlyHasBall)
        {
            // Need to Ensure Both Players Cannot Have the Ball 
            CurrentlyHasBall = false;
            if (ThePlayerCurrentState == PlayerState.RunningWithBall) SetPlayerRunningState();
            if (ThePlayerCurrentState == PlayerState.PickingUpBall) SetPlayerRunningState();
            if (ThePlayerCurrentState == PlayerState.PlantingBall) SetPlayerRunningState();
            if (ThePlayerCurrentState == PlayerState.KickingBall) SetPlayerRunningState();
        }
    } // OppoBallStatusUpdate
    // =======================================================================================================
    public void TakeTackle()
    {
        // Player Has been tackled - Assume always Loses Ball, In Behind Direction
        if (CurrentlyHasBall)
        {
            CurrentlyHasBall = false;
            TheOpPlayer.SendMessage("OppoBallStatusUpdate", false);
            Vector3 BallLostPosition = transform.position - 3.5f * transform.forward + 2.0f * transform.up + 0.5f * transform.right;   // Instruct Ball to be lost behind Player
            TheBall.SendMessage("PlayerLostBall", BallLostPosition);       // 

            // Assume will want to Keep on Running - without Ball
            SetPlayerRunningState();
        }
    }  // TakeTackle
    // ======================================================================================================================
    void CheckandAbleToTackle()
    {
        IsAbleToTackle = false;
        // Tackle Requestd Check Other Player Has Ball 
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((ThePlayerCurrentState == PlayerState.Running) && (CurrentAnnimationName == "Run"))
        {
            if ((!CurrentlyHasBall) && (OppoCurrentlyHasBall))
            {
                // Check Oppo Has the Ball
                if (DistanceToOtherPlayer < TackleDistance)
                {
                    IsAbleToTackle = true;           
                }
            }  // Check Oppo Has the Ball
        } // Only Pickup Will Running
    } // CheckandPerformTackle()
    // =============================================================
    void CheckAbleToPickup()
    {
        IsAbleToPickup = false;
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
                    if (Vector3.Dot(transform.forward, DirectionToBall) > 0.5f)
                    {
                        IsAbleToPickup = true; 
                    }
                }
            } // neither Has the Ball
        } // Only Pickup Will Running
    } // CheckandPerformBallPickup
    // ====================================================================
    void CheckandAbleToPerformKick()
    {
        IsAbleToKick = false;
        CurrentAnnimationName = ThePlayerAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if ((CurrentAnnimationName == "RunWith") || (CurrentAnnimationName == "Run"))
        {
            // Requesting Kicking the Ball
            if ((DistanceToTheBall < PickupBallThreshold) && (!OppoCurrentlyHasBall))
            {
                if (CurrentlyHasBall)
                {
                    // Kicking from Chest
                    IsAbleToKick = true; 
                }
                else
                {
                    // Kicking From Ground, so Make sure Ball in Front
                    Vector3 DirectionToBall = (TheBall.transform.position - transform.position).normalized;
                    float PlayerBallDirectionDot = Vector3.Dot(transform.forward, DirectionToBall);
                    if (PlayerBallDirectionDot > 0.5f)
                    {
                        IsAbleToKick = true;
                    }
                }
            }  // Distance Check
        }
    } // CheckandPerformKick()
    // ======================================================================================================================
    void CheckBallPlantedinGoalArea()
    {
        Vector3 OppoGoalDifference = TheOpGoalArea.transform.position - TheBall.transform.position;
        float DeltaXPitch = OppoGoalDifference.x;

        if (Mathf.Abs(DeltaXPitch) < 2.5f)
        {
            // Ball is within the Oppos Goal Are so Has Scored
            TheGameManager.SendMessage("IncrementBlueScoreTry");
            TheGameManager.SendMessage("UpdateNarrativeString", "Blue has Scored a Try !!");
            Debug.Log(" Own Player Scored Try: " + EpisodeStepCount.ToString());

            // Positive Reward Player
            AddReward(2.0f); 
           
            RequestEndEpisode();
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
            // Ball is within the Oppos Goal Area, so the current Player has Scored
            
            TheGameManager.SendMessage("IncrementBlueScoreGoal");
            TheGameManager.SendMessage("UpdateNarrativeString", "Blue has Scored a Goal !!");
            Debug.Log(" Own Player Scored Goal: " + EpisodeStepCount.ToString());
            // Positive Reward Player
            AddReward(1.0f);
           
            RequestEndEpisode();
        } // Delta X Next to Goal Posts, and Z Within Goal Posts
    } // CheckBallBetweenGoalPosts
    // ===================================================================================================================
    void OnCollisionEnter(Collision theCollision)
    {
        if (theCollision.gameObject.tag == "Floor") CurrentlyGrounded = true;

    }  // OnCollisionEnter

    //consider when character is jumping .. it will exit collision.
    void OnCollisionExit(Collision theCollision)
    {
        if (theCollision.gameObject.name == "floor") CurrentlyGrounded = false;
    } // OnCollisionExit
      // ==============================================================================================
    public void OppoJustScored()
    {
        // The Oppos Player Has Requested End of Episode (Perhaps they juts scored) 
        AddReward(-2.0f);

        RequestEndEpisode();
    } // OppoReqEndEpisode
    // =========================================================================================================
    public void BallRequestedEndEpisode()
    {
        Debug.Log(" Ball Out Of Play: " + EpisodeStepCount.ToString());
        // Negative Reward Player
        AddReward(-0.2f);

        RequestEndEpisode();

    } // BallRequestedEndEpisode
    // =============================================================================================
    void RequestEndEpisode()
    {
        // End of the Episode
        TheGameManager.SendMessage("EndOfEpisode");   

        // Now Call for the End of Episode
        EndEpisode();

    }  // RequestEndEpisode
    // ======================================================================================================================
    //
    // =========================  Annimation Control States =========================================
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
        ThePlayerCurrentState = PlayerState.KickingBall;
        ThePlayerAnimator.SetBool("IsRunning", false);
        ThePlayerAnimator.SetBool("PlayerHasBall", true);

        ThePlayerAnimator.SetBool("IsPickingUp", false);
        ThePlayerAnimator.SetBool("IsTackling", false);
        ThePlayerAnimator.SetBool("IsKicking", true);
        ThePlayerAnimator.SetBool("IsPlanting", false);
    } // SetPlayerKickingBallState
    // ==================================================
    // ======================================================================================================================
}
