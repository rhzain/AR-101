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

    // MathL3 - sambungkan ML3GameManager di Inspector (biarkan kosong jika bukan MathL3)
    public MathLevel3.ML3GameManager ml3GameManager;

    // Literacy - sambungkan LiteracyGameManager di Inspector (biarkan kosong jika bukan literacy)
    public LL1GameManager ll1GameManager;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private bool isPlaced = false;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager   = GetComponent<ARPlaneManager>();
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
            meja.SetActive(true);
            Debug.Log("Meja di-spawn");

            isPlaced = true;

            // Hentikan plane detection dan sembunyikan semua plane
            StopPlaneDetection();

            // MathL1: pakai GameManager
            if (ml1GameManager != null)
            {
                ml1GameManager.SetMeja(meja);
                ml1GameManager.StartGame();
            }
            // MathL2: pakai ML2GameManager
            else if (ml2GameManager != null)
            {
                DropZone spawnedDropZone = meja.GetComponentInChildren<DropZone>(true);
                AppleBasket spawnedBasket = meja.GetComponentInChildren<AppleBasket>(true);

                if (spawnedDropZone != null)
                {
                    ml2GameManager.dropZone = spawnedDropZone;
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
            // MathL3: pakai ML3GameManager dan layout tiga slot
            else if (ml3GameManager != null)
            {
                MathLevel3.ML3TableLayout spawnedLayout = meja.GetComponent<MathLevel3.ML3TableLayout>();
                if (spawnedLayout == null)
                    spawnedLayout = meja.GetComponentInChildren<MathLevel3.ML3TableLayout>(true);

                if (spawnedLayout != null)
                {
                    ml3GameManager.tableLayout = spawnedLayout;
                    spawnedLayout.ApplyTableSurface();
                    Debug.Log("[PlacementManager] ML3TableLayout berhasil disambungkan ke ML3GameManager.");
                }
                else
                {
                    Debug.LogWarning("PlacementManager: ML3TableLayout tidak ditemukan di prefab layout yang di-spawn!");
                }

                ml3GameManager.StartGame();
            }
            // Literacy: pakai LiteracyGameManager
            else if (ll1GameManager != null)
            {
                Transform sourceArea = meja.transform.Find("SourceArea");
                Transform slotArea   = meja.transform.Find("SlotArea");

                if (sourceArea != null)
                {
                    ll1GameManager.sourceAreaCenter = sourceArea;
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

    /// <summary>
    /// Matikan ARPlaneManager agar tidak menscan area baru,
    /// dan sembunyikan semua visualisasi plane yang sudah terdeteksi.
    /// </summary>
    private void StopPlaneDetection()
    {
        if (planeManager == null) return;

        // Sembunyikan semua plane yang sudah terdeteksi
        foreach (ARPlane plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }

        // Matikan ARPlaneManager agar tidak menscan area baru
        planeManager.enabled = false;

        Debug.Log("[PlacementManager] Plane detection dimatikan.");
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
