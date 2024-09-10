using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SpawnAtCoords : MonoBehaviour
{
    public GameObject objectToInstantiate; // GameObject to instantiate
    public Vector3 spawnPosition; // Desired spawn position
	private ARAnchor arAnchor; // Reference to the AR anchor

    public void SpawnThing()
    {
        // Instantiate the object at the specified position
        Instantiate(objectToInstantiate, spawnPosition, Quaternion.identity);
    }

public void SpawnRelative()
    {
        // Attempt to find an AR anchor
        ARAnchorManager anchorManager = FindObjectOfType<ARAnchorManager>();
        if (anchorManager != null)
        {
            ARAnchor[] anchors = anchorManager.GetComponentsInChildren<ARAnchor>();
            if (anchors.Length > 0)
            {
                // Found an AR anchor, use the first one
                arAnchor = anchors[0];
            }
            else
            {
                Debug.LogWarning("No AR anchor found!");
            }
        }
        else
        {
            Debug.LogWarning("ARAnchorManager not found!");
        }

        // If AR anchor found, instantiate object relative to its position and rotation
        if (arAnchor != null)
        {
            // Get the position and rotation of the AR anchor
            Vector3 anchorPosition = arAnchor.transform.position;
            Quaternion anchorRotation = arAnchor.transform.rotation;

            // Calculate the spawn position relative to the anchor
            Vector3 spawnPosition = anchorPosition + (anchorRotation * Vector3.forward * 0.5f);

            // Instantiate the object at the calculated position
            Instantiate(objectToInstantiate, spawnPosition, Quaternion.identity);
        }
    }
    
}
