using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using SimpleJSON;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml.Serialization;

public class SynthesEyesServer : MonoBehaviour {
    
    public string OUTPUT_DIR = "output";

	public GameObject lightDirectionalObj;
    public RenderTexture FinalTargetTexture;
    public FaceManager faceManager;

    public GameObject eyeballObj;
	public GameObject eyeRegionObj;
	public GameObject eyeRegionSubdivObj;
	public GameObject eyeWetnessObj;
	public GameObject eyeWetnessSubdivObj;
	public GameObject eyeLashesObj;

    public GameObject eyeballObjLeft;
    public GameObject eyeRegionObjLeft;
    public GameObject eyeRegionSubdivObjLeft;
    public GameObject eyeWetnessObjLeft;
    public GameObject eyeWetnessSubdivObjLeft;
    public GameObject eyeLashesObjLeft;

    public GameObject noseRegionObj;

    private EyeballController eyeball;
	private EyeRegionController eyeRegion;
	private SubdivMesh eyeRegionSubdiv;
	private EyeWetnessController eyeWetness;
	private SubdivMesh eyeWetnessSubdiv;
	private DeformEyeLashes[] eyeLashes;

    private EyeballController eyeballLeft;
    private EyeRegionController eyeRegionLeft;
    private SubdivMesh eyeRegionSubdivLeft;
    private EyeWetnessController eyeWetnessLeft;
    private SubdivMesh eyeWetnessSubdivLeft;
    private DeformEyeLashes[] eyeLashesLeft;

    private NoseRegion noseRegion;

    private LightingController lightingController;

	// Render settings for randomization
	public float defaultCameraPitch = 0;
	public float defaultCameraYaw = 0;
	public float cameraPitchNoise = Mathf.Deg2Rad * 20;
	public float cameraYawNoise = Mathf.Deg2Rad * 40;
	public float defaultEyePitch = 0;
	public float defaultEyeYaw = 0;
	public float eyePitchNoise = 30;
	public float eyeYawNoise = 30;

    // frame index for saving
    private bool firstFrame = true;
    int framesSaved = 0;
    bool saveMetaData = false;
    protected bool saveAutomatically;
    protected string currentDir;
    protected int lastDirNb;

    List<GameObject> faceMetaData;
    

    protected bool _hasFaceModelChanged;
    protected Vector3 _faceOffset;

    protected bool _initialised;
    protected bool _enableUpdate;

    protected bool nextPose;
    protected bool diaporama;
    protected bool generatePOIImages;

    protected XMLPOI xmlPOI;

    protected GameObject eyeLeftObj;
    protected GameObject eyeRightObj;
    protected GameObject noseObj;

    public Vector3 FaceCenter { get; protected set; }

    //these are action that must be performed at the end of the frame
    protected int _saveEyesPOI;
    protected int _prepareNextPose;
    protected int _saveFrame;

    void Start () {
        
        // Initialise SynthesEyes Objects
        eyeRegion = eyeRegionObj.GetComponent<EyeRegionController> ();
		eyeball = eyeballObj.GetComponent<EyeballController> ();
		eyeRegionSubdiv = eyeRegionSubdivObj.GetComponent<SubdivMesh> ();
		eyeWetness = eyeWetnessObj.GetComponent<EyeWetnessController> ();
		eyeWetnessSubdiv = eyeWetnessSubdivObj.GetComponent<SubdivMesh> ();
		eyeLashes = eyeLashesObj.GetComponentsInChildren<DeformEyeLashes> (true);

        // Initialise SynthesEyesLeft Objects
        eyeRegionLeft = eyeRegionObjLeft.GetComponent<EyeRegionController>();
        eyeballLeft = eyeballObjLeft.GetComponent<EyeballController>();
        eyeRegionSubdivLeft = eyeRegionSubdivObjLeft.GetComponent<SubdivMesh>();
        eyeWetnessLeft = eyeWetnessObjLeft.GetComponent<EyeWetnessController>();
        eyeWetnessSubdivLeft = eyeWetnessSubdivObjLeft.GetComponent<SubdivMesh>();
        eyeLashesLeft = eyeLashesObjLeft.GetComponentsInChildren<DeformEyeLashes>(true);

        noseRegion = noseRegionObj.GetComponent<NoseRegion>();
        
        lightingController = GameObject.Find ("lighting_controller").GetComponent<LightingController> ();

        _hasFaceModelChanged = true;

        _initialised = false;

        if (!Directory.Exists(OUTPUT_DIR))
            Directory.CreateDirectory(OUTPUT_DIR);

        eyeLeftObj = GameObject.Find("EyeLeft");
        eyeRightObj = GameObject.Find("EyeRight");
        noseObj = GameObject.Find("NoseRegion");

        _faceOffset = -Vector3.one;
        _enableUpdate = false;

    }

    protected void initialise()
    {
        currentDir = "";
        
        bool userId = false;
        bool scene = false;
        bool headpose = false;
        bool camera = false;
        generatePOIImages = false;
        string[] arguments = Environment.GetCommandLineArgs();
        
        //arguments = ("eye.exe /i /u output/"+ dir+ "/userid.xml /c output/" + dir + "/camera.xml /e output/" + dir + "/scene.xml /h output/" + dir + "/headpose.xml").Split(' ');

        if (arguments.Length > 1) //no argument passed
        {
            for (int i = 1; i < arguments.Length; i += 2)
            {                
                if (arguments[i] == "/i") //draw POI img
                {
                    generatePOIImages = true;
                    i--;// because there are no second option
                }
                if (arguments[i] == "/e") //scene
                {
                    try
                    {
                        lightingController.LoadSceneFromFile(arguments[i + 1]);
                        scene = true;
                    }
                    catch (IOException e) { }
                }
                if (arguments[i] == "/c") //camera
                {
                    try
                    {
                        camera = true;
                        LoadCameraFromFile(arguments[i + 1]);
                    }
                    catch (IOException e) { }
                }
                else if (arguments[i] == "/h") //headpose
                {
                    try
                    {
                        faceManager.LoadHeadposeFromFile(arguments[i + 1]);
                        headpose = true;
                    }
                    catch (IOException e) { }
                }
                else if (arguments[i] == "/u") //face
                {
                    try
                    {
                        faceManager.LoadFaceFromFile(arguments[i + 1]);
                        userId = true;
                    }
                    catch (IOException e) { }
                }
            }
        }

        if (!scene) //scene
        {
            lightingController.RandomizeLighting();
        }

        if (!camera) //scene
        {
            //force window resolution
            Camera.main.targetTexture = null;
            Screen.SetResolution(Screen.width, Screen.height, FullScreenMode.FullScreenWindow);
            //StartCoroutine(WaitForScreenChange(new Vector2(Screen.width,Screen.height)));
        }

        if (!headpose) //headpose
        {
            faceManager.RandomizeHeadpose();
        }

        if (!userId) //face
        {            
            faceManager.RandomizeAppearance();
            _hasFaceModelChanged = true;
        }

        saveMetaData = false;
        
        _saveEyesPOI = -1;
        _prepareNextPose = -1;
        _saveFrame = -1;
        firstFrame = true;

        eyeLeftObj.SetActive(false);
        eyeRightObj.SetActive(false);
        noseObj.SetActive(false);

        _initialised = true;

        StartCoroutine(processInitialisation());
    }

    protected IEnumerator processInitialisation()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (firstFrame)
        {
            startSaving(); //automatically save iamges when loading from file
            firstFrame = false;
        }


    }

    protected void checkKeyboardInput()
    {
        if (Input.GetKeyDown("h"))
        {
            faceManager.RandomizeHeadpose();
        }

        if (Input.GetKeyDown("g"))
        {
            faceManager.RandomizeGaze();
        }

        if (Input.GetKeyDown("u"))
        {
            faceManager.RandomizeAppearance();
            _hasFaceModelChanged = true;
        }

        if (Input.GetKeyDown("e"))
        {
            lightingController.RandomizeLighting();
        }

        if (Input.GetKeyDown("s")) //save manually
        {
            startSaving();
        }
    }

    private void startSaving()
    {
        saveDetails();
        faceManager.MoveCamera = false;
        faceManager.ResetHeadposePosition();
        diaporama = true;
        nextPose = true;
        framesSaved = 0;
        xmlPOI = new XMLPOI();
    }


    protected void LoadCameraFromFile(string file)
    {
        var serializer = new XmlSerializer(typeof(XMLCamera));
        var stream = new FileStream(file, FileMode.Open);
        XMLCamera xmlCam = serializer.Deserialize(stream) as XMLCamera;
        stream.Close();

        if (xmlCam.Resolution.x > Screen.width || xmlCam.Resolution.y > Screen.height)
        {
            FinalTargetTexture.width = (int)xmlCam.Resolution.x;
            FinalTargetTexture.height = (int)xmlCam.Resolution.y;
            Camera.main.targetTexture = FinalTargetTexture;
        }
        else
        {            
            Screen.SetResolution((int)xmlCam.Resolution.x, (int)xmlCam.Resolution.y, FullScreenMode.FullScreenWindow);
            //StartCoroutine(WaitForScreenChange(xmlCam.Resolution));
        }

        Camera.main.nearClipPlane = xmlCam.Near;
        Camera.main.farClipPlane = xmlCam.Far;
        Camera.main.orthographicSize = xmlCam.OrthographicSize;
        Camera.main.orthographic = xmlCam.IsOrthographic;

        if (!Camera.main.orthographic)
        {

            Camera.main.fieldOfView = xmlCam.FieldOfView;

            Camera.main.usePhysicalProperties = xmlCam.IsPhysicalCamera;
            if (Camera.main.usePhysicalProperties)
            {
                Camera.main.focalLength = xmlCam.Focal;
                Camera.main.sensorSize = xmlCam.SensorSize;
                Camera.main.lensShift = xmlCam.LensShift;
                Camera.main.gateFit = xmlCam.GateFit;
            }
        }

        if (xmlCam.UseProjectionMatrix) //re-write projection matrix 
            Camera.main.projectionMatrix = xmlCam.ProjectionMatrix;
    }

    protected Vector2 optimisedResolution(Vector2 resolution)
    {
        if (resolution.x > Screen.currentResolution.width || resolution.y > Screen.currentResolution.height)
        {
            float ratio = resolution.x / resolution.y;
            float w = Screen.currentResolution.height * ratio;            
            return w > Screen.currentResolution.width ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height / ratio) : new Vector2(w,Screen.currentResolution.height);
        }

        return resolution;
    }

    protected void SaveCameraToFile(string file)
    {
        XMLCamera xmlCam = new XMLCamera();

        int width = Camera.main.targetTexture != null ? Camera.main.targetTexture.width : Screen.width;
        int height = Camera.main.targetTexture != null ? Camera.main.targetTexture.height: Screen.height;
        xmlCam.Resolution = new Vector2(width, height);

        xmlCam.Near = Camera.main.nearClipPlane;
        xmlCam.Far = Camera.main.farClipPlane;

        xmlCam.UseProjectionMatrix = false;
        xmlCam.ProjectionMatrix = Camera.main.projectionMatrix;

        xmlCam.IsOrthographic = Camera.main.orthographic;
        xmlCam.OrthographicSize = Camera.main.orthographicSize;
        xmlCam.FieldOfView = Camera.main.fieldOfView;

        xmlCam.IsPhysicalCamera = Camera.main.usePhysicalProperties;
        xmlCam.Focal = Camera.main.focalLength;
        xmlCam.SensorSize = Camera.main.sensorSize;
        xmlCam.LensShift = Camera.main.lensShift;
        xmlCam.GateFit = Camera.main.gateFit;


        var serializer = new XmlSerializer(typeof(XMLCamera));
        var stream = new FileStream(file, FileMode.Create);
        serializer.Serialize(stream, xmlCam);
        stream.Close();
    }

    IEnumerator AfterEndFrame()
    {
        yield return new WaitForEndOfFrame();

            if (_saveFrame > 0)
                _saveFrame--;
            else if (_saveFrame == 0)
            {
                saveFrame();
                _saveFrame = -1;
            }

            if (_saveEyesPOI > 0)
                _saveEyesPOI--;
            else if (_saveEyesPOI == 0)
            {
                saveEyesPOI();
                _saveEyesPOI = -1;
                _prepareNextPose = 2;
            }

            if (_prepareNextPose > 0)
                _prepareNextPose--;
            else if (_prepareNextPose == 0)
            {
                nextPose = true;
                _prepareNextPose = -1;
            }

        //reset the transformation of both face to center them 
        //we have to do this because of the CastOnEye function 
        //when mesh is calculated and it has to be centered
        UpdatePosition(-Vector3.one);
    }

    private void preUpdate()
    {
        //initialise face parameters
        if (!_initialised)
        {
            initialise();
            return;
        }

        //wait for lighting setting
        if (!lightingController.Initialised)
            return;

        //initialise faceOffset the first time and when face changes
        if (_faceOffset == -Vector3.one || _hasFaceModelChanged)
        {
            activateLeftEye(true);
            eyeRegionLeft.UpdateEyeRegion();
            eyeRegionSubdivLeft.Subdivide();
            _faceOffset = eyeRegionSubdivLeft.ComputeCenter();
            FaceCenter = -new Vector3(0, 1.1f * _faceOffset.x, 3.1f * _faceOffset.x);
            lightingController.SetPosition(FaceCenter + Vector3.forward * 3f);
        }

        activateFace(false);
        if (!faceManager.IsFaceReady)
           return;

        activateFace(true);

        //initialisation is done, we can update face
        _enableUpdate = true;

        //calculate rotation of right eyeball, taking into account the symetry of the face
        eyeball.UpdateEyeballSymetricalRotation(FaceCenter, _faceOffset, _hasFaceModelChanged);

        //prepare left eyeball rotation for eye calculation
        eyeballLeft.UpdateEyeballRotation(FaceCenter + _faceOffset, _hasFaceModelChanged);
    }
    
    private void LateUpdate()
    {
        if (!_enableUpdate)
            return;

        //since right eye rotation was calculated with eye symetry, we calculate now the final rotation
        eyeball.UpdateEyeballRotation(FaceCenter - _faceOffset, _hasFaceModelChanged);

        //reset for the next frame
        StartCoroutine(AfterEndFrame());

        if (!faceManager.IsFaceReady) //after a change of headPose
            _enableUpdate = false;
    }

    static int ll = 0;

    void Update () {

        preUpdate();

        if (!_enableUpdate)
            return;
       
        updateEyes();        

        //StartCoroutine(processFirstFrame());
        if (!firstFrame)
        {
            if (saveMetaData)
            {
                //metadata must be saved the frame after data are saved since we had a rendering phase with the spheres

                if (generatePOIImages)
                    _saveEyesPOI = 1;
                else
                    _prepareNextPose = 1;

                saveMetaData = false;                
            }

            if (diaporama && nextPose)
            {
                if (faceManager.NextHeadpose())
                {
                    _saveFrame = 10;                    
                }
                else
                {
                    diaporama = false;
                    faceManager.MoveCamera = true;
                    Debug.Log("Images saved!");
                }
                nextPose = false;
            }

            if (!diaporama) //allows input only when diaporama is finished
            {
                if (xmlPOI != null)
                {
                    var serializer = new XmlSerializer(typeof(XMLPOI));
                    var stream = new FileStream(currentDir + "poi_data.xml", FileMode.Create);
                    serializer.Serialize(stream, xmlPOI);
                    stream.Close();
                    Debug.Log("POI saved!");
                    xmlPOI = null;
                    Application.Quit();
                }
                checkKeyboardInput();
            }
        }        
    }
    
    protected void activateFace(bool activate)
    {
        activateLeftEye(activate);
        activateRightEye(activate);
        noseObj.SetActive(activate);
    }

    protected void activateRightEye(bool activate)
    {
        eyeRightObj.SetActive(activate);
        eyeball.gameObject.SetActive(activate);
    }

    protected void activateLeftEye(bool activate)
    {
        eyeLeftObj.SetActive(activate);
        eyeballLeft.gameObject.SetActive(activate);
    }
    
    protected void updateEyes()
    {
        activateFace(false);

        /* FIRST CALCULATE RIGHT EYE */
        activateLeftEye(false);
        activateRightEye(true);

        //calculate eye right
        eyeRegion.UpdateEyeRegion();
        eyeRegionSubdiv.Subdivide();
        eyeWetness.UpdateEyeWetness();
        eyeWetnessSubdiv.Subdivide();
        //remove eyeball because of collision for eyelash calculation
        eyeball.gameObject.SetActive(false);
        foreach (DeformEyeLashes eyeLash in eyeLashes)
            eyeLash.UpdateLashes(true);
        //reactivate eyeball
        eyeball.gameObject.SetActive(true);

        /* SECOND CALCULATE LEFT EYE */
        activateLeftEye(true);
        activateRightEye(false);

        //calculate eye left
        eyeRegionLeft.UpdateEyeRegion(); //check cast on eye here! + compare to original version!
        eyeRegionSubdivLeft.Subdivide();
        eyeWetnessLeft.UpdateEyeWetness();
        eyeWetnessSubdivLeft.Subdivide();

        //remove eyeball because of collision for eyelash calculation
        eyeballLeft.gameObject.SetActive(false);
        foreach (DeformEyeLashes eyeLashLeft in eyeLashesLeft)
             eyeLashLeft.UpdateLashes(false);
        //reactivate eyeball
        eyeballLeft.gameObject.SetActive(true);
        
        /* REACTIVATE RIGHT EYE */
        activateRightEye(true);       

        /* ACTIVATE NOSE */
        noseObj.SetActive(true);

        //update nose
        if (_hasFaceModelChanged)
            noseRegion.UpdateNose(_faceOffset);


        //apply faceOffset
        UpdatePosition(_faceOffset);

        _hasFaceModelChanged = false;
    }

  
    protected void UpdatePosition(Vector3 center)
    {
        if (center != -Vector3.one)
            faceManager.transform.position = FaceCenter;
        else
            faceManager.transform.position = Vector3.zero;            

        eyeRegion.UpdatePosition(center);        
        eyeRegionLeft.UpdatePosition(center);

        if (center == -Vector3.one)
            center = Vector3.zero;

        eyeball.transform.localPosition = -center;
        eyeballLeft.transform.localPosition = center;
    }
    

    private Color parseColor(JSONNode jN){
		return new Color (jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat, 1.0f);
	}
	

	private Vector3 parseVec(JSONNode jN){
		return new Vector3 (jN[0].AsFloat, jN[1].AsFloat, jN[2].AsFloat);
	}


    private void saveFrame()
    {
        framesSaved++;

        // Create a texture the size of the screen, RGB24 format
        int width = Camera.main.targetTexture != null ? Camera.main.targetTexture.width : Screen.width;
        int height = Camera.main.targetTexture != null ? Camera.main.targetTexture.height : Screen.height;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        RenderTexture.active = Camera.main.targetTexture;
        // Read screen contents into the texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] imgBytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(string.Format(currentDir + "{0}.png", framesSaved), imgBytes);

        UnityEngine.Object.Destroy(tex);

        faceMetaData = new List<GameObject>();
        saveEyeMetaData(true);
        saveEyeMetaData(false);
        xmlPOI.FinalPupilSize = faceManager.FinalPupilSize;
        xmlPOI.Phot = lightingController.PhotoMeter.Phot;
        saveMetaData = true;
    }

    protected Vector2 get2DPos(Vector2 pos)
    {
        int height = (Camera.main.targetTexture == null) ? Screen.height : FinalTargetTexture.height;
        
        return new Vector2(pos.x, height - pos.y);
    }
    protected void saveEyeMetaData(bool right)
    {
        Vector3 center = right ? -_faceOffset : _faceOffset;
        Vector3 mirror = Vector3.one;// right ? new Vector3(-1, 1, 1) : Vector3.one;
        EyeRegionController eye = right ? eyeRegion : eyeRegionLeft;
        EyeballController ball = right ? eyeball : eyeballLeft;

        Mesh meshEyeRegion = eye.transform.GetComponent<MeshFilter>().mesh;
        Mesh meshEyeBall = ball.transform.GetComponent<MeshFilter>().mesh;

        POIDef poi = new POIDef();
        
        float sphereScale = 0.1f;
        int i = 0;
        int half = EyeRegionTopology.interior_margin_idxs.Length / 2;
        Vector3 previousPoint = Vector3.zero;
        foreach (var idx in EyeRegionTopology.interior_margin_idxs)
        {
            Vector3 v_3d = eye.transform.localToWorldMatrix * Vector3.Scale(meshEyeRegion.vertices[idx],mirror);
            Vector3 newPos = v_3d + center + FaceCenter;       

            poi.InteriorMargin.Point3D.Add(-newPos);//change to face reference (X towards left eye; Y towards feet; Z towards back)
            poi.InteriorMargin.Point2D.Add(get2DPos(Camera.main.WorldToScreenPoint(newPos)));

            int mod = i % half;
            bool insertPoint = false;// mod > 1 && mod < half;

            if (insertPoint)
            {
                Vector3 pos = 0.5f * (newPos + previousPoint);
                //poi.InteriorMarginInterpolated.Point3D.Add(-pos);//change to face reference (X towards left eye; Y towards feet; Z towards back)
                //poi.InteriorMarginInterpolated.Point2D.Add(get2DPos(Camera.main.WorldToScreenPoint(pos)));
            }

            if (generatePOIImages)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localScale = Vector3.one * sphereScale*0.5f;                
                obj.GetComponent<MeshRenderer>().material.color = mod == 0 ? Color.black : Color.cyan;
                obj.transform.position = newPos;
                faceMetaData.Add(obj);

                if(insertPoint)
                {
                    Vector3 pos = 0.5f * (newPos + previousPoint);
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.transform.localScale = Vector3.one * sphereScale;
                    obj.GetComponent<MeshRenderer>().material.color = Color.green;
                    obj.transform.position = pos;
                    faceMetaData.Add(obj);
                }
            }
            previousPoint = newPos;
            i++;
        }

        foreach (var idx in EyeRegionTopology.caruncle_idxs)
        {
            Vector3 v_3d = eye.transform.localToWorldMatrix * Vector3.Scale(meshEyeRegion.vertices[idx], mirror);
            Vector3 newPos = v_3d + center + FaceCenter;

            poi.Caruncle.Point3D.Add(-newPos);//change to face reference (X towards left eye; Y towards feet; Z towards back)
            poi.Caruncle.Point2D.Add(get2DPos(Camera.main.WorldToScreenPoint(newPos)));

            if (generatePOIImages)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localScale = Vector3.one * sphereScale;
                obj.GetComponent<MeshRenderer>().material.color = Color.blue;
                obj.transform.position = newPos;
                faceMetaData.Add(obj);
            }
        }

        poi.GlobeCenter3D = center + FaceCenter;
        poi.GlobeCenter2D = get2DPos(Camera.main.WorldToScreenPoint(poi.GlobeCenter3D));
        poi.GlobeCenter3D *= -1f;//change to face reference (X towards left eye; Y towards feet; Z towards back)

        Vector3 corneaLocalCenter = ball.CorneaCenter;
        poi.CorneaCenter3D = ball.transform.rotation * corneaLocalCenter;
        poi.CorneaCenter3D += center + FaceCenter;
        poi.CorneaCenter2D = get2DPos(Camera.main.WorldToScreenPoint(poi.CorneaCenter3D));
        poi.CorneaCenter3D *= -1f;//change to face reference (X towards left eye; Y towards feet; Z towards back)

        Vector3 irisCenter = Vector3.zero;
        foreach (var idx in EyeRegionTopology.iris_idxs)
        {            
            Vector3 v_3d = ball.transform.localToWorldMatrix * Vector3.Scale(meshEyeBall.vertices[idx], mirror);
            irisCenter += meshEyeBall.vertices[idx];
            Vector3 newPos = v_3d + center + FaceCenter;

            poi.Iris.Point3D.Add(-newPos);//change to face reference (X towards left eye; Y towards feet; Z towards back)
            poi.Iris.Point2D.Add(get2DPos(Camera.main.WorldToScreenPoint(newPos)));

            if (generatePOIImages)
            {                
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localScale = Vector3.one * sphereScale;
                obj.GetComponent<MeshRenderer>().material.color = Color.magenta;
                obj.transform.position = newPos;
                faceMetaData.Add(obj);
            }
        }
        irisCenter /= EyeRegionTopology.iris_idxs.Length;

        poi.IrisCenter3D = ball.transform.localToWorldMatrix * irisCenter;
        poi.IrisCenter3D += center + FaceCenter;        
        poi.IrisCenter2D = get2DPos(Camera.main.WorldToScreenPoint(poi.IrisCenter3D));
        poi.IrisCenter3D *= -1f;//change to face reference (X towards left eye; Y towards feet; Z towards back)

        Vector3 pupilCenter = Vector3.zero;
        foreach (var idx in EyeRegionTopology.iris_idxs)
        {
            //get data from the "second" sphere of the eyeball that represents the iris
            Vector3 irisBallCenter = ball.GetComponent<SphereCollider>().center;
            float irisBallRadius = ball.GetComponent<SphereCollider>().radius;
            //faceManager.EyeIrisSize changes the vertices so we force the initial iris to the sphere ray
            Vector3 irisStart = irisBallCenter + irisBallRadius * (meshEyeBall.vertices[idx] - irisBallCenter).normalized;
            //calculate the axis of rotation to move the irisPoint to the pupil position
            Vector3 axis = Vector3.Cross((irisStart - irisBallCenter).normalized, Vector3.forward);

            //To calculate the angle we use the real iris ray in order to take into account the faceManager.EyeIrisSize
            Vector3 pupil = irisBallCenter + Vector3.forward * (meshEyeBall.vertices[idx] - irisBallCenter).magnitude;            
            float dist = (pupil - meshEyeBall.vertices[idx]).magnitude;
            
            float angle = 2f * Mathf.Asin(dist / (2 * irisBallRadius)) / 2f; //this is the angle when pupilSize = 0;           
            angle -= Mathf.Deg2Rad * getPupilAngleOffset();
            
            Vector3 newPos = RotateAroundPivot(irisStart, axis, irisBallCenter, Mathf.Rad2Deg * angle);
            newPos = ball.transform.localToWorldMatrix * newPos;
            newPos += center + FaceCenter;
            
            poi.Pupil.Point3D.Add(-newPos);//change to face reference (X towards left eye; Y towards feet; Z towards back)
            poi.Pupil.Point2D.Add(get2DPos(Camera.main.WorldToScreenPoint(newPos)));

            pupilCenter += newPos;

            if (generatePOIImages)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.localScale = Vector3.one * sphereScale*0.25f;
                obj.GetComponent<MeshRenderer>().material.color = Color.red;
                obj.transform.position = newPos;
                faceMetaData.Add(obj);
            }
        }
        pupilCenter /= EyeRegionTopology.iris_idxs.Length;
        poi.PupilCenter3D = pupilCenter;
        poi.PupilCenter2D = get2DPos(Camera.main.WorldToScreenPoint(pupilCenter));
        poi.PupilCenter3D *= -1;//change to face reference (X towards left eye; Y towards feet; Z towards back)

        if (right) //export with user_centric reference
            xmlPOI.POIRight.Add(poi);
        else
            xmlPOI.POILeft.Add(poi);
    }

    protected float getPupilAngleOffset()
    {
        float adjustment = 0.04f;
        if (pupilSizeBetween(-1.5f,1.5f))
            return faceManager.FinalPupilSize * 3.2f * (1f + adjustment);
        
        /* deal with negative values */
        if (pupilSizeBetween(-2.4f, -1.5f))
            return faceManager.FinalPupilSize * 3.12f * (1f+adjustment);

        if (pupilSizeBetween(-2.8f, -2.4f))
            return faceManager.FinalPupilSize * 2.8f * (1f + adjustment);

        if (pupilSizeBetween(-3.1f, -2.8f))
            return faceManager.FinalPupilSize * 2.75f * (1f + adjustment);

        if (pupilSizeBetween(-3.35f, -3.1f))
            return faceManager.FinalPupilSize * 2.7f * (1f + adjustment);

        if (pupilSizeBetween(-4f, -3.35f))
            return faceManager.FinalPupilSize * 2.65f * (1f + adjustment);

        /* deal with positive values */
        if (pupilSizeBetween(1.5f, 2.4f))
            return faceManager.FinalPupilSize * 3.15f * (1f - adjustment);

        if (pupilSizeBetween(2.4f, 2.8f))
            return faceManager.FinalPupilSize * 3.05f * (1f - adjustment);

        if (pupilSizeBetween(2.8f, 4f))
            return faceManager.FinalPupilSize * 2.95f * (1f - adjustment);

        return faceManager.FinalPupilSize * 3f * (1f - adjustment);
    }

    protected bool pupilSizeBetween(float min, float max)
    {
        return faceManager.FinalPupilSize >= min && faceManager.FinalPupilSize <= max;
    }
    public Vector3 RotateAroundPivot(Vector3 point, Vector3 axis, Vector3 pivot, float angle)
    {
        Vector3 v = point - pivot;
        v = Quaternion.AngleAxis(angle, axis) * v;
        return v + pivot;
    }
    
    private int getLastDirectory()
    {
        return Directory.GetDirectories(OUTPUT_DIR).Length;
    }

    private void saveDetails() {

        lastDirNb = getLastDirectory() + 1;
        currentDir = OUTPUT_DIR + Path.DirectorySeparatorChar + lastDirNb;
        if (!Directory.Exists(currentDir))
            Directory.CreateDirectory(currentDir);
        currentDir += Path.DirectorySeparatorChar;

        lightingController.SaveSceneToFile(currentDir + "scene.xml");
        faceManager.SaveFaceToFile(currentDir + "userid.xml");
        faceManager.SaveHeadposeToFile(currentDir + "headpose.xml");
        SaveCameraToFile(currentDir + "camera.xml");
        Debug.Log("Configuration saved!"); 
	}

    private void saveEyesPOI()
    {
        // Create a texture the size of the screen, RGB24 format
        int width = Camera.main.targetTexture != null ? Camera.main.targetTexture.width : Screen.width;
        int height = Camera.main.targetTexture != null ? Camera.main.targetTexture.height : Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        RenderTexture.active = Camera.main.targetTexture;

        // Read screen contents into the texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        RenderTexture.active = null;

        byte[] imgBytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(string.Format(currentDir + "{0}_poi.png", framesSaved), imgBytes);
        
        
        foreach (GameObject obj in faceMetaData)
            GameObject.Destroy(obj);

        faceMetaData.Clear();
        
    }  
}
