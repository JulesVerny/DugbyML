using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DugbyGameManager : MonoBehaviour
{
    // ========================================================
    public Text ScoreTextDisplayObject;
    public Text NarrativeTextDisplay;
    public Text LessonTextDisplay; 
    public GameObject BluePlayer;
    public GameObject RedPlayer;
    public GameObject TheBall;

    private string CurrentScoreText;
    private int BlueScore;
    private int RedScore;
    public static int EpisodeCount; 
    // =========================================================
    void Start()
    {
        BlueScore = 0;
        RedScore = 0;
        NarrativeTextDisplay.text = "Have a Great Game";
        EpisodeCount = 0; 
        UpdateScoreDisplay();

    }
    // ========================================================
    public void EndOfEpisode()
    {
        EpisodeCount = EpisodeCount + 1; 
        
    } // EndEpisode
    // ========================================================
    public void IncrementRedScoreGoal()
    {
        RedScore = RedScore+3;
        UpdateScoreDisplay();
    }
    // ========================================================
    public void IncrementBlueScoreGoal()
    {
        BlueScore = BlueScore + 3;
        UpdateScoreDisplay();
    }
    // ========================================================
    public void IncrementRedScoreTry()
    {
        RedScore = RedScore + 5;
        UpdateScoreDisplay();
    }
    // ========================================================
    public void IncrementBlueScoreTry()
    {
        BlueScore = BlueScore + 5;
        UpdateScoreDisplay();
    }
    // ========================================================
    public void UpdateNarrativeString(string RevisedDisplayString)
    {
        NarrativeTextDisplay.text = RevisedDisplayString;
    }
    // ========================================================
    void UpdateScoreDisplay()
    {
        CurrentScoreText = "Blue: " + BlueScore.ToString() + "  Red: " + RedScore.ToString();
        ScoreTextDisplayObject.text = CurrentScoreText;
    }
    // ========================================================
    public void UpdateLessonDisplay(int LessonIndex)
    {
        LessonTextDisplay.text = "Lesson: " + LessonIndex.ToString(); 
    }  // UpdateLessonDisplay
    
}
