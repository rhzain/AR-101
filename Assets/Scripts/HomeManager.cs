using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HomeManager : MonoBehaviour
{
    public TMP_Text nameText;
    public Image profileImage;
    public Sprite[] profileSprites;

    void Start()
    {
        // AMBIL NAMA
        string nama = PlayerPrefs.GetString("Nama", "Player");

        // SET NAMA
        nameText.text = nama;

        // RANDOM FOTO
        if (profileSprites != null && profileSprites.Length > 0)
        {
            int index = Random.Range(0, profileSprites.Length);
            profileImage.sprite = profileSprites[index];
        }
    }
}