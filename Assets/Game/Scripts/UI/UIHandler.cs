using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    
    public TextMeshProUGUI steps;
    public TextMeshProUGUI episodes;
    public TextMeshProUGUI success;
    public TextMeshProUGUI failure;


    private int step_count;
    private int episode_count;
    private int success_count;
    private int failure_count;
    
    public void UpdateSteps(int amount)
    {
        step_count = amount;
        steps.text = step_count.ToString();
    }

    public void UpdateEpisodes(int amount)
    {
        episode_count++;
        episodes.text = episode_count.ToString();
    }


    public void UpdateSuccess(int amount)
    {
        success_count += amount;
        success.text = success_count.ToString();
       
    }


    public void UpdateFailure(int amount, bool isTimed)
    {
        failure_count += amount;
        failure.text += amount.ToString();
        failure.color = isTimed ? Color.red : Color.yellow;
    }

}
