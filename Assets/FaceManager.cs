using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SimpleJSON;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine.UI;

public class FaceManager : MonoBehaviour
{
    public GameObject EyeBallLeft;
    public GameObject EyeBallRight;      

    public bool IsFaceReady { get; private set; }

    public bool SmoothNose { get; private set; }
    public float SmoothNoseSize { get; private set; }

    public float SkinThickness { get; private set; }
    public bool DoShrinkWrap { get; private set; }

    public Vector3 LookAtPoint;// { get; protected set; }
    public bool MoveCamera { get; set; }

    public Vector3[] FaceRandomMeshFromPca { get; private set; }
    public float EyeIrisSize { get; private set; }

    public float FinalPupilSize { get; private set; }   
    protected float _nominalPupilSize;
    protected float _variablePupilSize;
    protected bool _isPupilvariable;

    protected float _eyeIrisRotationLeft;
    protected float _eyeIrisRotationRight;
    protected float _eyeScleraRotationLeft;
    protected float _eyeScleraRotationRight;

    protected int FaceCurrentAppearence;
    protected int EyeTexture;

    protected EyeRegionPCA facePca { get; private set; }

    protected XMLUserId userParameters;
    protected XMLHeadpose headposeParameters;
    protected int currentHeadpose;
    protected HeadposeDef customHeadpose;

    protected Vector3 defaultHeadposeOrientation;

    protected Material faceMaterial;      // Skin shader material
    protected Material faceMaterialRight;// Skin shader material
    protected Material noseMaterial;// Skin shader material



    protected List<Texture2D> faceColorTexs = new List<Texture2D>();      // Eye region color textures
    protected List<Texture2D> faceColorLdTexs = new List<Texture2D>();    // Look-down version of eye-region texs
    protected List<Texture2D> faceBumpTexs = new List<Texture2D>();       // Bumpmap eye-region texs

    protected List<Texture2D> eyeColorTexs = new List<Texture2D>();		// Eye region color textures
    protected Dictionary<string, Texture2D> eyeColorTexsDict = new Dictionary<string, Texture2D>();
    protected Material eyeMaterial;
    protected Material eyeRightMaterial;

    public bool Initialised { get; protected set; }

    public GameObject LookAtSphere;

    private void OnEnable()
    {
        LightMeter.OnPhotUpdate += OnPhotUpdate;
    }
    private void OnDisable()
    {
        LightMeter.OnPhotUpdate -= OnPhotUpdate;
    }

    private void Awake()
    {
        MoveCamera = false;
        // initialize material for later modifications
        faceMaterial = Resources.Load("Materials/FaceMaterial", typeof(Material)) as Material;
        faceMaterialRight = Resources.Load("Materials/FaceMaterialRight", typeof(Material)) as Material;
        noseMaterial = Resources.Load("Materials/NoseMaterial", typeof(Material)) as Material;
        
        List<string> texIds = new List<string>();
        for (int i = 1; i <= 5; i++)
        {
            texIds.Add(string.Format("f{0:00}", i));
        }
        for (int i = 1; i <= 15; i++)
        {
            texIds.Add(string.Format("m{0:00}", i));
        }

        foreach (string texId in texIds)
        {
            string fn = string.Format("SkinTextures/{0}_color", texId);
            faceColorTexs.Add(Resources.Load(fn) as Texture2D);
            faceColorLdTexs.Add(Resources.Load(fn.Replace("color", "color_look_down")) as Texture2D);
            faceBumpTexs.Add(Resources.Load(fn.Replace("color", "disp")) as Texture2D);
        }

        // initialize collection of color textures
        foreach (Texture2D c in Resources.LoadAll("IrisTextures", typeof(Texture2D)))
        {
            eyeColorTexs.Add(c);
            eyeColorTexsDict.Add(c.name, c);
        }

        // initialize material for later modifications
        eyeMaterial = Resources.Load("Materials/EyeMaterial", typeof(Material)) as Material;
        eyeRightMaterial = Resources.Load("Materials/EyeRightMaterial", typeof(Material)) as Material;

        // initialise PCA for mesh randomization
        facePca = this.GetComponent<EyeRegionPCA>();
    }

    void Start()
    {
        LookAtPoint = new Vector3(0, 0, 15f);

        EyeIrisSize = 1.0f;
        SkinThickness = 0.25f;
        DoShrinkWrap = true;
        SmoothNose = true;
        SmoothNoseSize = 0.1f;
        FaceCurrentAppearence = 0;

        defaultHeadposeOrientation = new Vector3(0, 180, 0);//no rotation

        userParameters = new XMLUserId();
        headposeParameters = new XMLHeadpose();
        ResetHeadposePosition();
        customHeadpose = new HeadposeDef();
        Initialised = false;
        IsFaceReady = false;


    }

    private void LateUpdate()
    {
        if (!Initialised)
            return;

        faceMaterial.SetFloat("_SmoothNose", SmoothNose ? 1 : 0);
        faceMaterial.SetFloat("_SmoothNoseSize", SmoothNoseSize);
        faceMaterialRight.SetFloat("_SmoothNose", SmoothNose ? 1 : 0);
        faceMaterialRight.SetFloat("_SmoothNoseSize", SmoothNoseSize);
        eyeMaterial.SetVector("_LookAt", LookAtPoint);
        eyeMaterial.SetFloat("_PupilSize", FinalPupilSize);

        eyeRightMaterial.SetVector("_LookAt", LookAtPoint);
        eyeRightMaterial.SetFloat("_PupilSize", FinalPupilSize);

        if (!MoveCamera)
            return;

        //move Gaze
        moveGazePoint();

        //move headpose
        moveHeadpose();

        HeadposeDef hd = cameraToHeadpose();
        customHeadpose.Position = hd.Position;
        customHeadpose.Rotation = hd.Rotation;
        customHeadpose.LookAtPoint = hd.LookAtPoint;
    }

    protected void moveGazePoint()
    {
        Vector3 pointer = LookAtPoint;
        bool applyChange = false;
        if (Input.GetKey(KeyCode.LeftControl))
        {
            pointer.z = Mathf.Clamp(pointer.z + Input.mouseScrollDelta.y, 10f, 100f);
            applyChange = true;
        }
        if (Input.GetMouseButton(2))
        {
            pointer = Camera.main.ScreenToWorldPoint(Input.mousePosition+Vector3.forward*5f);
            pointer.z = LookAtPoint.z;
            applyChange = true;
        }

        if (applyChange)
            LookAtPoint = pointer;
    }

    protected void moveHeadpose()
    {
        float MouseSensitivity = 4f;
        float deltaX = Input.GetAxis("Mouse X") * MouseSensitivity;
        float deltaY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        if (Input.GetMouseButton(0))
        {
            //remove the face offset
            Camera.main.transform.position -= Camera.main.transform.rotation * -customHeadpose.Position;

            if (!Input.GetKey(KeyCode.LeftControl))
                Camera.main.transform.RotateAround(Vector3.zero, transform.localRotation * Vector3.up, deltaX);
            Camera.main.transform.RotateAround(Vector3.zero, transform.localRotation * Vector3.right, -deltaY);            

            //add the face offset
            Camera.main.transform.position += Camera.main.transform.rotation * -customHeadpose.Position;

            if (Input.GetKey(KeyCode.LeftControl))
                Camera.main.transform.Rotate(Vector3.forward * deltaX);
        }

        if (!Input.GetKey(KeyCode.LeftControl))
            Camera.main.orthographicSize += Input.mouseScrollDelta.y * -0.5f;
        Camera.main.orthographicSize = Mathf.Clamp((float)Camera.main.orthographicSize, 1f, 30);
    }


    public void ChangeShaderAttribute(string name, float value)
    {
        faceMaterial.SetFloat(name, value);
        faceMaterialRight.SetFloat(name, value);
        noseMaterial.SetFloat(name, value);
    }

    public void RandomizeGaze()
    {
        randomizeGaze(customHeadpose.Clone());
    }

    protected void randomizeGaze(HeadposeDef hp)
    {
        LookAtPoint = new Vector3(UnityEngine.Random.Range(-7f, 7f), UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(15, 100));        
        hp.LookAtPoint = LookAtPointToCamera(LookAtPoint);
        headposeParameters.Headpose.Add(hp);

        updateCustomHeadpose(hp);

        currentHeadpose = -1; //no diaporama        
    }

    public void RandomizeHeadpose()
    {
        HeadposeDef hp = new HeadposeDef();
        hp.Position = Vector3.forward * UnityEngine.Random.Range(25f, 40f);
        hp.Rotation = new Vector3(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(-30f, 30f), 0);
        headposeToCamera(hp);

        randomizeGaze(hp);

        updatePupilSize();
    }

    public void RandomizeAppearance()
    {
        FaceRandomMeshFromPca = facePca.RandomizeMesh();
        FaceCurrentAppearence = UnityEngine.Random.Range(0, faceColorTexs.Count);

        // Slightly decrease iris size on random
        EyeIrisSize = UnityEngine.Random.Range(0.9f, 1.0f);

        // also modify pupil size via material
        _isPupilvariable = true;
        _nominalPupilSize = UnityEngine.Random.Range(-0.5f, 0.5f);

        _eyeIrisRotationLeft = UnityEngine.Random.Range(-180, 180);
        _eyeIrisRotationRight = UnityEngine.Random.Range(-180, 180);

        _eyeScleraRotationLeft = UnityEngine.Random.Range(-15, 15);
        _eyeScleraRotationRight = UnityEngine.Random.Range(-15, 15);

        if (UnityEngine.Random.value > 0.5f) EyeTexture = eyeColorTexs.IndexOf(eyeColorTexsDict["eyeball_brown"]);
        else EyeTexture = UnityEngine.Random.Range(0, eyeColorTexs.Count);


        randomizeKappaAngles();


        initialiseShaders();

        updatePupilSize();
        Initialised = true;
    }

    public void OnPhotUpdate(LightMeter lightMeter)
    {
        float vpMin = -3f;
        float vpMax = 1.5f;
        float phMin = lightMeter.PhMin;
        float phMax = lightMeter.PhMax;

        _variablePupilSize = ((vpMax * phMax - vpMin * phMin) - (vpMax - vpMin) * lightMeter.Phot) / (phMax - phMin);
        updatePupilSize();
        IsFaceReady = true;
    }

    public void updatePupilSize()
    {        
        FinalPupilSize = _isPupilvariable ? _nominalPupilSize + _variablePupilSize : _nominalPupilSize;
        initialiseShaders();        
    }

    protected void randomizeKappaAngles()
    {
        //insert vector by columns
        Matrix4x4 a = new Matrix4x4(
            new Vector4(2.4978f, -0.1955f, 0.7594f, 0.5402f),
            new Vector4(0, 2.6711f, -0.0024f, 1.7290f),
            new Vector4(0, 0, 1.8148f, -0.5109f),
            new Vector4(0, 0, 0, 1.9616f));

        Vector4 z = new Vector4(SyntheseyesUtils.NextGaussianDouble(), SyntheseyesUtils.NextGaussianDouble(), SyntheseyesUtils.NextGaussianDouble(), SyntheseyesUtils.NextGaussianDouble()); //choose randomly

        Vector4 mu = new Vector4(5.4237f, 2.5152f, 2.1375f, 4.4458f);

        Vector4 kappa = a * z + mu;

        EyeBallRight.GetComponent<EyeballController>().HorizontalAngle = kappa.x;
        EyeBallRight.GetComponent<EyeballController>().VerticalAngle = kappa.y;
        EyeBallLeft.GetComponent<EyeballController>().HorizontalAngle = kappa.z;
        EyeBallLeft.GetComponent<EyeballController>().VerticalAngle = kappa.w;                        
    }

    protected void initialiseShaders()
    {
        faceMaterial.SetTexture("_Tex2dColor", faceColorTexs[FaceCurrentAppearence]);
        faceMaterial.SetTexture("_Tex2dColorLd", faceColorLdTexs[FaceCurrentAppearence]);
        faceMaterial.SetTexture("_BumpTex", faceBumpTexs[FaceCurrentAppearence]);

        faceMaterialRight.SetTexture("_Tex2dColor", faceColorTexs[FaceCurrentAppearence]);
        faceMaterialRight.SetTexture("_Tex2dColorLd", faceColorLdTexs[FaceCurrentAppearence]);
        faceMaterialRight.SetTexture("_BumpTex", faceBumpTexs[FaceCurrentAppearence]);

        noseMaterial.SetTexture("_Tex2dColor", faceColorTexs[FaceCurrentAppearence]);
        noseMaterial.SetTexture("_Tex2dColorLd", faceColorLdTexs[FaceCurrentAppearence]);
        noseMaterial.SetTexture("_BumpTex", faceBumpTexs[FaceCurrentAppearence]);

        eyeMaterial.SetFloat("_PupilSize", FinalPupilSize);
        eyeMaterial.SetTexture("_MainTex", eyeColorTexs[EyeTexture]);
        eyeMaterial.SetFloat("_IrisRotationAngle", _eyeIrisRotationLeft);
        eyeMaterial.SetFloat("_ScleraRotationAngle", _eyeScleraRotationLeft);

        eyeRightMaterial.SetFloat("_PupilSize", FinalPupilSize);
        eyeRightMaterial.SetTexture("_MainTex", eyeColorTexs[EyeTexture]);
        eyeRightMaterial.SetFloat("_IrisRotationAngle", _eyeIrisRotationRight);
        eyeRightMaterial.SetFloat("_ScleraRotationAngle", _eyeScleraRotationRight);

        faceMaterial.SetFloat("_SmoothNose", SmoothNose ? 1 : 0);
        faceMaterial.SetFloat("_SmoothNoseSize", SmoothNoseSize);
        faceMaterialRight.SetFloat("_SmoothNose", SmoothNose ? 1 : 0);
        faceMaterialRight.SetFloat("_SmoothNoseSize", SmoothNoseSize);
    }

    public void LoadFaceFromFile(string file)
    {
        var serializer = new XmlSerializer(typeof(XMLUserId));
        var stream = new FileStream(file, FileMode.Open);
        userParameters = serializer.Deserialize(stream) as XMLUserId;
        stream.Close();

        // load PCA parameters
        facePca.Offset = userParameters.PcaOffset;
        facePca.Scale = userParameters.PcaScale;
        facePca.Coeffs = userParameters.PcaCoeffs;
        FaceRandomMeshFromPca = facePca.GetMesh();

        // load texture parameters
        FaceCurrentAppearence = userParameters.Texture;
        SmoothNose = userParameters.SmoothNose;
        SmoothNoseSize = userParameters.SmoothNoseSize;
        SkinThickness = userParameters.SkinThickness;
        DoShrinkWrap = userParameters.DoShrinkWrap;

        // load pupil and iris parameters
        EyeIrisSize = userParameters.EyeIrisSize;
        EyeTexture = userParameters.EyeTexture;
        _nominalPupilSize = userParameters.NominalPupilSize;
        _isPupilvariable = userParameters.IsPupilVariable;


        _eyeIrisRotationLeft = userParameters.EyeIrisRotationLeft;
        _eyeIrisRotationRight = userParameters.EyeIrisRotationRight;
        _eyeScleraRotationLeft = userParameters.EyeScleraRotationLeft;
        _eyeScleraRotationRight = userParameters.EyeScleraRotationRight;

    // load eyeball parameters: transform from user_centric reference
    EyeBallLeft.GetComponent<EyeballController>().VerticalAngle = userParameters.VerticalAngleLeft;
        EyeBallLeft.GetComponent<EyeballController>().HorizontalAngle = userParameters.HorizontalAngleLeft;
        EyeBallRight.GetComponent<EyeballController>().VerticalAngle = userParameters.VerticalAngleRight;
        EyeBallRight.GetComponent<EyeballController>().HorizontalAngle = userParameters.HorizontalAngleRight;

        initialiseShaders();
        Initialised = true;
    }

    public void SaveFaceToFile(string file)
    {
        // save PCA parameters
        userParameters.PcaOffset = facePca.Offset;
        userParameters.PcaScale = facePca.Scale;
        userParameters.PcaCoeffs = facePca.Coeffs;

        // save texture parameters
        userParameters.Texture = FaceCurrentAppearence;
        userParameters.SmoothNose = SmoothNose;
        userParameters.SmoothNoseSize = SmoothNoseSize;
        userParameters.SkinThickness = SkinThickness;
        userParameters.DoShrinkWrap = DoShrinkWrap;

        // save pupil and iris parameters
        userParameters.EyeIrisSize = EyeIrisSize;
        userParameters.EyeTexture = EyeTexture;
        userParameters.NominalPupilSize = _nominalPupilSize;
        userParameters.IsPupilVariable = _isPupilvariable;
        userParameters.EyeIrisRotationLeft = _eyeIrisRotationLeft;
        userParameters.EyeIrisRotationRight = _eyeIrisRotationRight;
        userParameters.EyeScleraRotationLeft = _eyeScleraRotationLeft;
        userParameters.EyeScleraRotationRight = _eyeScleraRotationRight;

        //userParameters.GCDistance = (EyeBallLeft.transform.position - EyeBallRight.transform.position).magnitude;

        // save eyeball parameters: transform to user_centric reference
        userParameters.VerticalAngleRight = EyeBallRight.GetComponent<EyeballController>().VerticalAngle;
        userParameters.HorizontalAngleRight = EyeBallRight.GetComponent<EyeballController>().HorizontalAngle;
        userParameters.VerticalAngleLeft = EyeBallLeft.GetComponent<EyeballController>().VerticalAngle;
        userParameters.HorizontalAngleLeft = EyeBallLeft.GetComponent<EyeballController>().HorizontalAngle;

        var serializer = new XmlSerializer(typeof(XMLUserId));
        var stream = new FileStream(file, FileMode.Create);
        serializer.Serialize(stream, userParameters);
        stream.Close();
    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    public static Matrix4x4 FromEuler(Vector3 euler)
    {
        float cosY = Mathf.Cos(euler.y*Mathf.Deg2Rad);     // Yaw
        float sinY = Mathf.Sin(euler.y * Mathf.Deg2Rad);

        float cosP = Mathf.Cos(euler.x * Mathf.Deg2Rad);     // Pitch
        float sinP = Mathf.Sin(euler.x * Mathf.Deg2Rad);

        float cosR = Mathf.Cos(euler.z * Mathf.Deg2Rad);     // Roll
        float sinR = Mathf.Sin(euler.z * Mathf.Deg2Rad);

        Matrix4x4 mat = Matrix4x4.identity;
        mat.m00 = cosY * cosR + sinY * sinP * sinR;
        mat.m10 = cosR * sinY * sinP - sinR * cosY;
        mat.m20 = cosP * sinY;

        mat.m01 = cosP * sinR;
        mat.m11 = cosR * cosP;
        mat.m21 = -sinP;

        mat.m02 = sinR * cosY * sinP - sinY * cosR;
        mat.m12 = sinY * sinR + cosR * cosY * sinP;
        mat.m22 = cosP * cosY;

        return mat.inverse;
    }

    protected void headposeToCamera(HeadposeDef hp)
    {
        Camera.main.transform.position = Vector3.zero; //reset position

        Vector3 euler = new Vector3(-hp.Rotation.x, hp.Rotation.y, -hp.Rotation.z) + defaultHeadposeOrientation;      
        Camera.main.transform.rotation = Quaternion.Euler(euler);// QuaternionFromMatrix(FromEuler(euler));

        
        Vector3 right = -Camera.main.transform.right * hp.Position.x;
        Vector3 up = Camera.main.transform.up * hp.Position.y;
        Vector3 forward = -Camera.main.transform.forward * hp.Position.z;

        Camera.main.transform.position = forward;
        Camera.main.transform.position += right;
        Camera.main.transform.position += up;

        if (Camera.main.orthographic)
            Camera.main.orthographicSize = Math.Abs(hp.Position.z); //place the camera at the correct distance        
    }

    protected HeadposeDef cameraToHeadpose()
    {
        HeadposeDef hd = new HeadposeDef();
        hd.Position = Vector3.Scale(Quaternion.Inverse(Camera.main.transform.rotation) * Camera.main.transform.position,new Vector3(-1, 1, -1));
        hd.Rotation = Vector3.Scale(Camera.main.transform.rotation.eulerAngles, new Vector3(-1, 1, -1)) - defaultHeadposeOrientation;
        hd.LookAtPoint = LookAtPointToCamera(LookAtPoint);

        return hd;
    }


    public void LoadHeadposeFromFile(string file)
    {        
        var serializer = new XmlSerializer(typeof(XMLHeadpose));
        var stream = new FileStream(file, FileMode.Open);
        headposeParameters = serializer.Deserialize(stream) as XMLHeadpose;
        if (headposeParameters.Headpose == null)
            headposeParameters.Headpose = new List<HeadposeDef>();

        if (headposeParameters.Headpose.Count >= 1)
            updateCustomHeadpose(headposeParameters.Headpose[headposeParameters.Headpose.Count - 1]);


        stream.Close();

        ResetHeadposePosition();
    }

    public void SaveHeadposeToFile(string file)
    {
        //if there is a custom headpose, we add it to the list
        if (headposeParameters.Headpose.Count >= 1 && !headposeParameters.Headpose[headposeParameters.Headpose.Count - 1].Equals(customHeadpose))
            headposeParameters.Headpose.Add(customHeadpose.Clone());
        
        var serializer = new XmlSerializer(typeof(XMLHeadpose));
        var stream = new FileStream(file, FileMode.Create);
        serializer.Serialize(stream, headposeParameters);
        stream.Close();
    }

    public bool HasHeadpose()
    {
        return (currentHeadpose >= 0 && currentHeadpose < headposeParameters.Headpose.Count);
    }

    public bool NextHeadpose()
    {
        currentHeadpose++;

        if (!HasHeadpose())
            return false;
        
        //transfer data to the camera
        headposeToCamera(headposeParameters.Headpose[currentHeadpose]);

        LookAtPoint = LookAtPointToWorld(headposeParameters.Headpose[currentHeadpose].LookAtPoint);

        LookAtSphere.transform.position = LookAtPoint;
        updateCustomHeadpose(headposeParameters.Headpose[currentHeadpose]);

        return true;
    }


    protected Vector3 LookAtPointToCamera(Vector3 lookAt)
    {
        return Vector3.Scale(Quaternion.Inverse(Camera.main.transform.rotation) * (lookAt - Camera.main.transform.position), new Vector3(1, -1, 1));
    }

    protected Vector3 LookAtPointToWorld(Vector3 lookAt)
    {
        return Camera.main.transform.position + Camera.main.transform.rotation * Vector3.Scale(lookAt, new Vector3(1, -1, 1));
    }

    public void ResetHeadposePosition()
    {
        currentHeadpose = -1;
    }

    protected void updateCustomHeadpose(HeadposeDef hp)
    {
        customHeadpose.Position = hp.Position;
        customHeadpose.Rotation = hp.Rotation;
        customHeadpose.LookAtPoint = hp.LookAtPoint;
    }
}