using UnityEngine;

public static class LevelProgress
{
    public const int FirstLevel = 1;

    private static string ProfileKey()
    {
        string nama = PlayerPrefs.GetString("Nama", "Player").Trim().ToLowerInvariant();
        int kelas = PlayerPrefs.GetInt("Kelas", 0);

        if (string.IsNullOrEmpty(nama))
            nama = "player";

        nama = nama.Replace(" ", "_");
        return $"{nama}_kelas_{kelas}";
    }

    private static string PassKey(string subject, int levelNumber)
    {
        return $"Progress_{ProfileKey()}_{subject}_Level_{levelNumber}_Passed";
    }

    private static string BestScoreKey(string subject, int levelNumber)
    {
        return $"Progress_{ProfileKey()}_{subject}_Level_{levelNumber}_BestScore";
    }

    public static bool IsLevelUnlocked(string subject, int levelNumber)
    {
        if (levelNumber <= FirstLevel)
            return true;

        return IsLevelPassed(subject, levelNumber - 1);
    }

    public static bool IsLevelPassed(string subject, int levelNumber)
    {
        return PlayerPrefs.GetInt(PassKey(subject, levelNumber), 0) == 1;
    }

    public static int GetBestScore(string subject, int levelNumber)
    {
        return PlayerPrefs.GetInt(BestScoreKey(subject, levelNumber), 0);
    }

    public static int CountPassedLevels(string subject, int totalLevels)
    {
        int count = 0;

        for (int level = FirstLevel; level <= totalLevels; level++)
        {
            if (IsLevelPassed(subject, level))
                count++;
        }

        return count;
    }

    public static void SaveResult(string subject, int levelNumber, int correctAnswers, int minimumCorrectToPass)
    {
        int bestScore = GetBestScore(subject, levelNumber);
        if (correctAnswers > bestScore)
            PlayerPrefs.SetInt(BestScoreKey(subject, levelNumber), correctAnswers);

        if (correctAnswers >= minimumCorrectToPass)
            PlayerPrefs.SetInt(PassKey(subject, levelNumber), 1);

        PlayerPrefs.Save();
    }

    public static void ResetCurrentProfileProgress(string subject, int totalLevels)
    {
        for (int level = FirstLevel; level <= totalLevels; level++)
        {
            PlayerPrefs.DeleteKey(PassKey(subject, level));
            PlayerPrefs.DeleteKey(BestScoreKey(subject, level));
        }

        PlayerPrefs.Save();
    }
}
