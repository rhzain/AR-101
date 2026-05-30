using UnityEngine;

public class ProgressResetButton : MonoBehaviour
{
    [Header("Progress Target")]
    public string subject = "Math";
    public int totalLevels = 3;

    [Header("Optional")]
    public LevelSelectMenu levelSelectMenu;

    public void ResetProgress()
    {
        LevelProgress.ResetCurrentProfileProgress(subject, totalLevels);

        if (levelSelectMenu != null)
            levelSelectMenu.Refresh();
    }
}
