using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlacementManager : MonoBehaviour
{
    public GameObject mejaPrefab;

    // MathL1 - sambungkan GameManager di Inspector
    public ML1GameManager ml1GameManager;
    
    // MathL2 - sambungkan ML2GameManager di Inspector (biarkan kosong jika MathL1)
    public ML2GameManager ml2GameManager;

    // Literacy - sambungkan LiteracyGameManager di Inspector (biarkan kosong jika bukan literacy)
    public LL1GameManager ll1GameManager;

    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private bool isPlaced = false;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (isPlaced) return;

        Vector2 inputPosition;
        if (!TryGetTapPosition(out inputPosition)) return;

        if (raycastManager.Raycast(inputPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            // biar meja tidak miring
            pose.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);

            GameObject meja = Instantiate(mejaPrefab, pose.position, pose.rotation);
            meja.SetActive(true); // Pastikan meja aktif meskipun prefab aslinya mati
            Debug.Log("Meja di-spawn");

            isPlaced = true;

            // MathL1: pakai GameManager
            if (ml1GameManager != null)
            {
                ml1GameManager.SetMeja(meja);
                ml1GameManager.StartGame();
            }
            // MathL2: pakai ML2GameManager
            else if (ml2GameManager != null)
            {
                // Cari DropZone & AppleBasket di dalam meja yang baru di-spawn
                // (Ini penting! Referensi runtime, bukan dari prefab statis)
                DropZone spawnedDropZone = meja.GetComponentInChildren<DropZone>(true);
                AppleBasket spawnedBasket = meja.GetComponentInChildren<AppleBasket>(true);

                if (spawnedDropZone != null)
                {
                    ml2GameManager.dropZone = spawnedDropZone;
                    // Set ketinggian permukaan meja untuk semua apel yang akan di-drag
                    DraggableApple.TableSurfaceY = spawnedDropZone.transform.position.y;
                    Debug.Log($"TableSurfaceY diset ke: {DraggableApple.TableSurfaceY}");
                }
                else
                    Debug.LogWarning("PlacementManager: DropZone tidak ditemukan di dalam meja yang di-spawn!");

                if (spawnedBasket != null)
                    ml2GameManager.appleBasket = spawnedBasket;
                else
                    Debug.LogWarning("PlacementManager: AppleBasket tidak ditemukan di dalam meja yang di-spawn!");


                ml2GameManager.StartGame();
            }
            // Literacy: pakai LiteracyGameManager
            else if (ll1GameManager != null)
            {
                Transform sourceArea = meja.transform.Find("SourceArea");
                Transform slotArea   = meja.transform.Find("SlotArea");

                if (sourceArea != null)
                {
                    ll1GameManager.sourceAreaCenter = sourceArea;
                    // Gunakan posisi Y SourceArea yang sudah diatur di atas permukaan meja
                    DraggableCard.TableSurfaceY = sourceArea.position.y;
                    Debug.Log($"TableSurfaceY (Literacy) = {DraggableCard.TableSurfaceY}");
                }
                else
                    Debug.LogWarning("PlacementManager: 'SourceArea' tidak ditemukan di meja!");

                if (slotArea != null)
                    ll1GameManager.slotAreaCenter = slotArea;
                else
                    Debug.LogWarning("PlacementManager: 'SlotArea' tidak ditemukan di meja!");

                ll1GameManager.StartGame();
            }
        }
    }

    private bool TryGetTapPosition(out Vector2 position)
    {
#if UNITY_EDITOR
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = default;
            return false;
        }

        position = Mouse.current.position.ReadValue();
        return true;
#else
        if (Touchscreen.current == null)
        {
            position = default;
            return false;
        }

        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.wasPressedThisFrame)
        {
            position = default;
            return false;
        }

        position = touch.position.ReadValue();
        return true;
#endif
    }
}