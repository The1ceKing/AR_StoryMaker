using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARController : MonoBehaviour
{
       public ARRaycastManager raycastManager;
       public GameObject myObject;
   
       private void Update()
       {
           if (Input.touchCount>0 && Input.GetTouch(0).phase==TouchPhase.Began)
           {
               List<ARRaycastHit> touches = new List<ARRaycastHit>();
   
               raycastManager.Raycast(Input.GetTouch(0).position, touches, UnityEngine.XR.ARSubsystems.TrackableType.Planes);
   
               if (touches.Count > 0)
               {
                   GameObject.Instantiate(myObject, touches[0].pose.position, touches[0].pose.rotation);
               }
           }
       }
}
