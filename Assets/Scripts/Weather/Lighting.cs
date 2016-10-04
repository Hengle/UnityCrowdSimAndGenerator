using UnityEngine;
using System.Collections;

public class Lighting : MonoBehaviour
{
    public Material mat;
    private Light[] lights;
    private Light mainLight;

    // Use this for initialization
    public void SetSampleSceneLighting()
    {
        float valX = 0;
        float valY = 0;
        WeatherConditions wc = GetComponent<WeatherConditions>();
        lights = FindObjectsOfType<Light>();
        mainLight = GetComponent<WeatherConditions>().MainLight.GetComponent<Light>();

        if (wc.Time == 3)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Sample3_Estate")
            {
                mainLight.intensity = 0f;
                mat.SetColor("_EmissionColor", Color.white);
                SetActiveLights(true);
                valX = mainLight.transform.rotation.x;
                valY = mainLight.transform.rotation.y;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Sample2_Square")
            {
                mainLight.intensity = 0f;
                mainLight.color = new Color(231, 229, 219);
                valX = -4;
                valY = -45.0f;
                SetActiveLights(true);
            }
            else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Sample1_Crossroad")
            {
                valX = mainLight.transform.eulerAngles.x;
                valY = 45.0f;
            }
            mainLight.transform.rotation = Quaternion.Euler(valX, valY, mainLight.transform.rotation.z);
        }
        else
        {
            mat.SetColor("_EmissionColor", Color.black);
            SetActiveLights(false);
        }
    }
    void SetActiveLights(bool truefalse)
    {
        foreach (Light light in lights)
        {
            if (light.type != LightType.Directional)
            {
                light.gameObject.SetActive(truefalse);
                light.shadows = LightShadows.Hard;
            }
        }

    }
}
