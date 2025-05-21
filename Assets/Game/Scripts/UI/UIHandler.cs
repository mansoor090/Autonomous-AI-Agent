using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    public LevelGenerator levelGenerator;
    public TextMeshProUGUI myPort;
    public TextMeshProUGUI steps;
    public TextMeshProUGUI episodes;
    public TextMeshProUGUI success;
    public TextMeshProUGUI failure;
    public TextMeshProUGUI failureReason;
    public TextMeshProUGUI rate;

	[Header("Level Switcher")]
	public Button nextButton;
    public Button previousButton;
	public TextMeshProUGUI levelInfo;

    private int currentLevel = 0;
    private const string LEVEL_PREFIX = "Level_";
    private const int MAX_LEVELS = 100;

    private int step_count;
    private int episode_count;
    private int success_count;
    private int failure_count;

   	void Start()
    {
        nextButton.onClick.AddListener(NextLevel);
        previousButton.onClick.AddListener(PreviousLevel);
    }
    
    public void UpdatePort(int port)
    {
        myPort.text = "Port: " + port.ToString();
    }
    public void UpdateSteps(int amount)
    {
        step_count += amount;
        steps.text = "Step: " +step_count.ToString();
    }

    public void UpdateEpisodes(int amount)
    {
        episode_count++;
        episodes.text = "Total: " + episode_count.ToString();
    }


    public void UpdateSuccess(int amount)
    {
        success_count += amount;
        success.text = "Win: " + success_count.ToString();
        UpdateRate();
    }


    public void UpdateFailure(int amount, bool isTimed)
    {
        failure_count += amount;
        failure.text = "Lost: " + failure_count.ToString();
        failure.color = isTimed ? Color.red : Color.yellow;
        UpdateRate();
    } 
    
    public void UpdateFailure(string msg)
    {
        failureReason.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        failureReason.text = "Env Reset Due to:" + msg;
    }

    private void UpdateRate()
    {
        if(success_count > 0 && failure_count > 0)
            rate.text = "Ratio: " + (((float)success_count / (float)(success_count + failure_count)) * 100).ToString("F1") + "%";
    
    }

    void NextLevel()
    {
        currentLevel++;
        string levelName = "Level"+currentLevel;
        levelGenerator.LoadLevel(levelName, levelInfo);
    }

    void PreviousLevel()
    {
        if (currentLevel > 0)
        {
            currentLevel--;
            string levelName = "Level"+currentLevel;
            levelGenerator.LoadLevel(levelName, levelInfo);
        }
    }

}
