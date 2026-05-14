using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    public TMP_InputField InputNama;
    public TMP_Dropdown InputKelas;

    public void LoadHome()
    {
        string nama = InputNama.text.Trim();
        int kelasIndex = InputKelas.value;

        // VALIDASI
        if (nama == "")
        {
            Debug.Log("Nama kosong!");
            return;
        }

        if (kelasIndex == 0)
        {
            Debug.Log("Kelas belum dipilih!");
            return;
        }

        // SIMPAN DATA
        PlayerPrefs.SetString("Nama", nama);
        PlayerPrefs.SetInt("Kelas", kelasIndex);
        PlayerPrefs.Save();

        // PINDAH SCENE
        SceneManager.LoadScene("HomeScene");
    }
}