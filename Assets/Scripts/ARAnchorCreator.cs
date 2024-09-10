using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

public class SpawnGameObjectOnTap : MonoBehaviour
{
    [SerializeField] private GameObject gameObjectToSpawn;
    private ARRaycastManager arRaycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    public SpawnAtCoords spawner;

    void Awake()
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
    }

    void OnEnable()
    {
        EnhancedTouch.EnhancedTouchSupport.Enable();
        EnhancedTouch.Touch.onFingerDown += SpawnGameObject;
    }

    void OnDisable()
    {
        EnhancedTouch.Touch.onFingerDown -= SpawnGameObject;
        EnhancedTouch.EnhancedTouchSupport.Disable();
    }

    void SpawnGameObject(EnhancedTouch.Finger finger)
    {
        if (!EventSystem.current.IsPointerOverGameObject(finger.index))
        {
            if (arRaycastManager.Raycast(finger.screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                Instantiate(gameObjectToSpawn, hitPose.position, hitPose.rotation);
                spawner.SpawnThing();
            }
        }
    }
}