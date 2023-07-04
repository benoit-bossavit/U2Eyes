using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using SimpleJSON;

public class LightingController : MonoBehaviour {

    public LightMeter PhotoMeter;
    public bool manuallyRandomizeLighting = false;  // Can manually randomize lighting in editor

    List<Cubemap> envTexs = new List<Cubemap>(); 	// Set of HDR environments used for lighting
    protected int _nbSkyDebug;
    private Light directionalLight;					// Directional light for hard shadows

	public ReflectionProbe ReflectionProbe { get; private set; }        // Reflection probe for better eye-reflections

    public int envTexSwitchFrequency = 300;			// How often to switch environments
    private int lightingChangeTicks = 0;            // The number of times the lighting has been changed

    protected Color defaultLightColor = new Color(236f / 255f, 248f / 255f, 1f);

    protected XMLScene sceneParameters;
    public bool Initialised { get; protected set; }

    void Start () {

		// load all HDR environments
		foreach (Cubemap c in Resources.LoadAll ("Skies", typeof(Cubemap)))
			envTexs.Add(c);
        // load all HDR environments
        _nbSkyDebug = 0;
        foreach (Cubemap c in Resources.LoadAll("SkiesDebug", typeof(Cubemap)))
        {
            _nbSkyDebug++;
            envTexs.Add(c);
        }
        // initialize game objects
        directionalLight = GameObject.Find ("directional_light").GetComponent<Light> ();
		ReflectionProbe = GameObject.Find ("reflection_probe").GetComponent<ReflectionProbe>();

        sceneParameters = new XMLScene();      

        Initialised = false;
	}

    protected void setDefaultPh()
    {
        sceneParameters.PhMin = 0f;
        sceneParameters.PhMax = 1f;
    }

    void Update()
    {
        if (!Initialised)
            return;

        if (manuallyRandomizeLighting) RandomizeLighting();  
    }

	public void RandomizeLighting ()
    {        
        // If enough frames have passed, switch the environment texture
        if (lightingChangeTicks % envTexSwitchFrequency == 0) {
            sceneParameters.SkyboxId = Random.Range (0, envTexs.Count-_nbSkyDebug);
            sceneParameters.SkyboxExposure = Random.Range(1.0f, 1.2f);
            sceneParameters.SkyboxRotation = Random.Range(0, 360);
		}
        lightingChangeTicks++;

        // randomize light color
        sceneParameters.LightColor = new HSBColor(defaultLightColor);
        sceneParameters.LightColor.h = Random.value;

        // randomize light direction and intensity        
        sceneParameters.LightDirection = new Vector3(Random.Range(-50, 50), 180 + Random.Range(-90, 90), 0);
        /* this is the old code.. */
        //Vector3 lightDirection = -SyntheseyesUtils.RandomVec(-10, 90, -90, 90);
        //directionalLight.transform.LookAt(directionalLight.transform.position + lightDirection);

        sceneParameters.LightIntensity = Random.Range(0.6f, 1.2f);

        // randomly vary environment intensity
        sceneParameters.AmbientIntensity = Random.Range(0.8f, 1.2f);

        setDefaultPh();

        initialiseShaders();
    }

    public void SetPosition(Vector3 pos)
    {
        ReflectionProbe.transform.position = pos;
    }

    protected void initialiseShaders()
    {

        PhotoMeter.PhMin = sceneParameters.PhMin;
        PhotoMeter.PhMax = sceneParameters.PhMax;

        RenderSettings.skybox.SetTexture("_Tex", envTexs[sceneParameters.SkyboxId]);
        RenderSettings.skybox.SetFloat("_Exposure", sceneParameters.SkyboxExposure);
        RenderSettings.skybox.SetFloat("_Rotation", sceneParameters.SkyboxRotation);

        directionalLight.color = sceneParameters.LightColor.ToColor();
        directionalLight.transform.rotation = Quaternion.Euler(sceneParameters.LightDirection);
        directionalLight.intensity = sceneParameters.LightIntensity;

        RenderSettings.ambientIntensity = sceneParameters.AmbientIntensity;

        DynamicGI.UpdateEnvironment();
        
        StartCoroutine(processScene());
        
    }

    protected IEnumerator processScene()
    {
        while (!DynamicGI.isConverged)
            yield return new WaitForEndOfFrame();

        Initialised = true;
    }

     public void LoadSceneFromFile(string file)
    {
        var serializer = new XmlSerializer(typeof(XMLScene));
        var stream = new FileStream(file, FileMode.Open);
        sceneParameters = serializer.Deserialize(stream) as XMLScene;
        stream.Close();

        if (sceneParameters.PhMin == sceneParameters.PhMax) //was not in the config file
            setDefaultPh();

        initialiseShaders();
    }

    public void SaveSceneToFile(string file)
    {
        sceneParameters.PhMin = PhotoMeter.PhMin;
        sceneParameters.PhMax = PhotoMeter.PhMax;

        var serializer = new XmlSerializer(typeof(XMLScene));
        var stream = new FileStream(file, FileMode.Create);
        serializer.Serialize(stream, sceneParameters);
        stream.Close();
    }
}
