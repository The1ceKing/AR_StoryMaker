using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class LightingControlTool
{
    private GameObject directionalLightObject;

    public void SetDirectionalLightMorning()
    {
        if (directionalLightObject == null)
            FindDirectionalLight();

        if (directionalLightObject != null)
        {
            Light directionalLight = directionalLightObject.GetComponent<Light>();
            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
                directionalLight.intensity = 1f;
                directionalLight.color = new Color(1.0f, 1.0f, 1.0f);
            }
        }
    }

    public void SetDirectionalLightMidday()
    {
        if (directionalLightObject == null)
            FindDirectionalLight();

        if (directionalLightObject != null)
        {
            Light directionalLight = directionalLightObject.GetComponent<Light>();
            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                directionalLight.intensity = 1f;
                directionalLight.color = new Color(1.0f, 1.0f, 1.0f);
            }
        }
    }

    public void SetDirectionalLightEvening()
    {
        if (directionalLightObject == null)
            FindDirectionalLight();

        if (directionalLightObject != null)
        {
            Light directionalLight = directionalLightObject.GetComponent<Light>();
            if (directionalLight != null)
            {
                directionalLight.transform.rotation = Quaternion.Euler(130f, 30f, 0f);
                directionalLight.intensity = 0.5f;
                directionalLight.color = new Color(1.0f, 1.0f, 1.0f);
            }
        }
    }

    public void SetDirectionalLightNight()
    {
        if (directionalLightObject == null)
            FindDirectionalLight();

        if (directionalLightObject != null)
        {
            Light directionalLight = directionalLightObject.GetComponent<Light>();
            if (directionalLight != null)
            {
                directionalLight.intensity = 0.2f;
                directionalLight.color = new Color(0.0f, 0.1f, 0.2f);
            }
        }
    }

    private void FindDirectionalLight()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            Light lightComponent = obj.GetComponent<Light>();
            if (lightComponent != null && lightComponent.type == LightType.Directional)
            {
                directionalLightObject = obj;
                break;
            }
        }
    }
}
