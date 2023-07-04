using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public class EyeballController : MonoBehaviour {

    public FaceManager faceManager;
    public bool RightEye = true;
    protected bool _initialised;

    public float HorizontalAngle
    {
        get { return _angleH; }
        set
        {
            _angleH = value;
            _initialVisualRotation = Quaternion.AngleAxis(RightEye ? -_angleH : _angleH, Vector3.up) * Quaternion.AngleAxis(-_angleV, Vector3.right);
        }
    }
    protected float _angleH;

    public float VerticalAngle
    {
        get { return _angleV; }
        set
        {
            _angleV = value;
            _initialVisualRotation = Quaternion.AngleAxis(RightEye ? -_angleH : _angleH, Vector3.up) * Quaternion.AngleAxis(-_angleV, Vector3.right);
        }
    }
    protected float _angleV;

    protected Quaternion _initialVisualRotation;
    public Vector3 IrisCenter { get; protected set; }

    public Vector3 CorneaCenter
    {
        get
        {
            float d = 1.0752f;
            float rg = 1.2f;
            float rc = 0.8f;
            float h = d - Mathf.Sqrt((d * d) + (rc * rc) - (rg * rg));
            return new Vector3(0, 0, h);
        }
        protected set { }
    }

    protected static int[] EyeIris_idxs = { 3, 7, 11, 14, 18, 21, 25, 29, 33, 37, 41, 45, 49, 52, 56, 60, 64, 68, 72, 76, 80, 84, 88, 92, 96, 100, 104, 108, 112, 116, 120, 124 };
    protected static Vector3[] EyeIris_start_pos = new Vector3[EyeIris_idxs.Length];

    // Use this for initialization
    void Awake () {

        _initialised = false;
        HorizontalAngle = 0;
        VerticalAngle = 0;
        InitialiseIris();
    }

    public void InitialiseIris()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        IrisCenter = Vector3.zero;
        for (int i = 0; i < EyeIris_idxs.Length; i++)
        {
            EyeIris_start_pos[i] = vertices[EyeIris_idxs[i]];
            IrisCenter += vertices[EyeIris_idxs[i]] / (float)EyeIris_idxs.Length;
        }            
    }

    // Update is called once per frame
    public void UpdateEyeballSymetricalRotation(Vector3 faceCentre, Vector3 position, bool forceInitialisation)
    {
        if (!faceManager.Initialised)
            return;

        if (!_initialised || forceInitialisation)
            initialiseEyeball();

        //data for symetry
        Quaternion initialVisualRotation = Quaternion.AngleAxis(-_angleH, Vector3.up) * Quaternion.AngleAxis(-_angleV, Vector3.right);
        position = faceCentre + position;
        Vector3 lookAtPoint = Vector3.Scale(new Vector3(-1,1,1), (faceManager.LookAtPoint - faceCentre)) + faceCentre;

        Vector3 visualDir = initialVisualRotation * Vector3.forward;
        transform.rotation = getEyeRotation(visualDir, position, lookAtPoint);        
    }

    public void UpdateEyeballRotation(Vector3 position, bool forceInitialisation)
    {
        if (!faceManager.Initialised)
            return;

        if (!_initialised || forceInitialisation)
            initialiseEyeball();


        Vector3 visualDir = _initialVisualRotation * Vector3.forward;

        transform.rotation = getEyeRotation(visualDir, position, faceManager.LookAtPoint);  
    }

    protected Quaternion getEyeRotation(Vector3 visualDir, Vector3 position, Vector3 lookAtPoint)
    {       
        float aux_dot = Vector3.Dot(visualDir, CorneaCenter.normalized);
        float h = CorneaCenter.magnitude;
        float d = (lookAtPoint - position).magnitude;
        float lambda_pp = -aux_dot + Mathf.Sqrt(aux_dot * aux_dot - h * h + d * d); // distance between ghost of lookatpoint in primary position and cornea center in primary position

        Vector3 corneaPos = position + CorneaCenter;
        Vector3 LookAtPointGhost = corneaPos + visualDir * lambda_pp;

        Vector3 LookAtPointGhostDir = (LookAtPointGhost - position).normalized;
        Vector3 LookAtPointdir = (lookAtPoint - position).normalized;

        return Quaternion.FromToRotation(LookAtPointGhostDir, LookAtPointdir);
    }


    protected void initialiseEyeball()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        
        // re-position limbus vertices
        for (int i = 0; i < EyeIris_idxs.Length; i++)
        {            
            Vector3 offset = EyeIris_start_pos[i] - IrisCenter;
            vertices[EyeIris_idxs[i]] = IrisCenter + offset * faceManager.EyeIrisSize;
        }

        // finally update mesh
        mesh.vertices = vertices;

        _initialised = true;
    }

}
