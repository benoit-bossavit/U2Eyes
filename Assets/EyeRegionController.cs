using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System;

public class EyeRegionController : MonoBehaviour {

    public FaceManager faceManager;

    public bool RightEye = true;

    void Start () {

		// mark current mesh as dynamic for faster updates
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		mesh.MarkDynamic();
    }

    public void UpdateEyeRegion()
    {
        if (!faceManager.Initialised)
            return;

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] offsets = new Vector3[872];


        Vector3[] vertices = mesh.vertices;
        Vector3[] newVerts = new Vector3[mesh.vertexCount];

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            newVerts[i] = faceManager.FaceRandomMeshFromPca[i];
        }

        // EYELID ROTATION
        // --------------------------------

        Transform eyeball = RightEye ? faceManager.EyeBallRight.transform : faceManager.EyeBallLeft.transform;
        float rot = eyeball.rotation.eulerAngles.x;
        rot = (rot > 180) ? rot - 360 : rot;

        Vector3 startEyelidPos = newVerts[16];
        Vector3 rotatedEyelidPos = newVerts[16] - new Vector3(0f, 0.0062f, 0f); // 0.0052f
        float topEyelidRotMod = Vector3.Angle(startEyelidPos, rotatedEyelidPos) / 20f;

        float t = 1.0f - ((float)rot + 20f) / 40f;
        float downness = Mathf.Lerp(1.0f, 0.4f, t);

        Vector3 axis = newVerts[34] - newVerts[139];
        Vector3 pvt = newVerts[34];
        Vector3 newPos;

        float[] rot_strengths = { 1.0f, 1.0f, 1.0f, 0.8f, 0.4f, 0.2f };

        for (int i = 0; i < EyeRegionTopology.loops.GetLength(0) - 2; i++)
        {

            float r_outer_corner = rot * rot_strengths[i] * 0.15f * downness;
            int outer_corner_idx = EyeRegionTopology.loops[i, 10];
            newPos = RotateAroundPivot(
                newVerts[outer_corner_idx],
                Vector3.forward,
                Vector3.zero,
                r_outer_corner);
            offsets[outer_corner_idx] = newPos - newVerts[outer_corner_idx];
            newVerts[outer_corner_idx] = newPos;

            axis = newVerts[34] - newVerts[EyeRegionTopology.loops[i, 10]];
            
            for (int j = 0; j < EyeRegionTopology.loops.GetLength(1); j++)
            {

                int idx1 = EyeRegionTopology.loops[i, j];

                t = EyeRegionTopology.middleness(j);
                pvt = Vector3.Lerp(newVerts[34], Vector3.zero, t);

                float r = rot * rot_strengths[i];

                newPos = RotateAroundPivot(
                    newVerts[idx1],
                    axis,
                    pvt,
                    (j < 10) ? r * topEyelidRotMod * downness : r * topEyelidRotMod / 3f * downness);
                offsets[idx1] = newPos - newVerts[idx1];
                newVerts[idx1] = newPos;               
            }
        }

        // smooth outer loops for skin stretching
        for (int i = 3; i < EyeRegionTopology.loops.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < EyeRegionTopology.loops.GetLength(1); j++)
            {
                int idx1 = EyeRegionTopology.loops[i, j];
                int idx2 = EyeRegionTopology.loops[i + 1, j];
                offsets[idx2] = offsets[idx1] * 0.5f;
                newVerts[idx2] += offsets[idx2];
            }
        }

        mesh.vertices = newVerts;

        // SHRINKWRAP
        // --------------------------------
        if (!faceManager.DoShrinkWrap) return;

        offsets = new Vector3[872];

        // shrink interior margin
        float shrinkwrap_distance = 1f;
        foreach (int idx in EyeRegionTopology.interior_margin_idxs)
        {
            Vector3 d = -newVerts[idx].normalized;
            RaycastHit hit;
            int layermask = 1 << 8;
            if (Physics.Raycast(transform.TransformPoint(newVerts[idx]) - d * shrinkwrap_distance, d, out hit, Mathf.Infinity, layermask))
            {
                offsets[idx] += ((hit.distance - shrinkwrap_distance) / 100f) * -newVerts[idx].normalized;
                newVerts[idx] += offsets[idx];                
            }
        }

        newVerts[12] = CastOntoEyeball((newVerts[0] + newVerts[13]) / 2f, Vector3.forward);
        
        // handle caruncle
        float offset_decay = 0.9f;
        offsets[27] = offset_decay * (offsets[12]);
        offsets[25] = offset_decay * (offsets[13] + offsets[12]) / 2f;
        offsets[28] = offset_decay * (offsets[0] + offsets[12]) / 2f;
        offsets[31] = offsets[32] = offsets[30] =
            offset_decay * (offsets[25] + offsets[27] + offsets[28]) / 3f;
        offsets[33] = offset_decay * (offsets[32] + offsets[30]) / 2f;

        foreach (int idx in EyeRegionTopology.caruncle_idxs)
        {
            newVerts[idx] += offsets[idx];            
        }

        // stretch lid skin if too close to eye
        for (int i = 2; i < 5; i++)
        {
            for (int j = 0; j < EyeRegionTopology.loops.GetLength(1); j++)
            {
                int idx = EyeRegionTopology.loops[i, j];
                float skinThicknessAtPoint = downness * ((j < 10) ? faceManager.SkinThickness : faceManager.SkinThickness / 2f);
                offsets[idx] = CastOntoEyeball(newVerts[idx], skinThicknessAtPoint) - newVerts[idx];
                newVerts[idx] += offsets[idx];
            }
        }

        float shrinkfactor = 1f - faceManager.SkinThickness;
        // smooth eye-region edge loops following shrinkwrap
        for (int i = 1; i < EyeRegionTopology.loops.GetLength(0); i++)
        {
            for (int j = 0; j < EyeRegionTopology.loops.GetLength(1); j++)
            {

                int idx1 = EyeRegionTopology.loops[0, j];
                int idx2 = EyeRegionTopology.loops[i, j];
                newVerts[idx2] += offsets[idx1] * rot_strengths[i] * shrinkfactor;
            }
        }

        // blend between look up and look down texture
        float lookDownNess = Mathf.Clamp01(1f - (30f - rot) / 30f);


        int[] noseIdx = { 195, 196, 197, 198, 199, 200, 201, 202, 203, 208, 209 };
        if (mesh.vertexCount > 209)
        {
            float averageX = 0;
            foreach (int idx in noseIdx)
                averageX += newVerts[idx].x;
            averageX /= noseIdx.Length;

            foreach (int idx in noseIdx)
                newVerts[idx].x = averageX;// 2 * averageX - newVerts[idx].x;
        }

        // set new mesh positions
        mesh.vertices = newVerts;
        mesh.RecalculateNormals();
  
        faceManager.ChangeShaderAttribute("_LookDownNess", lookDownNess * lookDownNess);

        // update mesh collider so eyelashes can correctly deform
        // note - this is slow...
        GetComponent<MeshCollider>().sharedMesh = null;
        transform.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    Dictionary<int, GameObject> test;
    public Vector3 RotateAroundPivot(Vector3 point, Vector3 axis, Vector3 pivot, float angle)
    {
        Vector3 v = point - pivot;
        v = Quaternion.AngleAxis(angle, axis) * v;
        return v + pivot;
    }


    Vector3 CastOntoEyeball(Vector3 point)
    {
        return CastOntoEyeball(point, 0f);
    }

    Vector3 CastOntoEyeball(Vector3 point, Vector3 dir)
    {
        return CastOntoEyeball(point, dir, 0f);
    }

    Vector3 CastOntoEyeball(Vector3 point, float dist)
    {
        return CastOntoEyeball(point, point.normalized, dist);
    }

    Vector3 CastOntoEyeball(Vector3 point, Vector3 dir, float dist)
    {
        float shrinkwrap_distance = 1f;

        RaycastHit hit;
        if (Physics.Raycast(transform.TransformPoint(point) + dir * shrinkwrap_distance, -dir, out hit, Mathf.Infinity, 1 << 8))
        {
            if ((hit.distance - shrinkwrap_distance) < dist)
            {
                return (hit.point + dist * dir) / 100f;
            }
            else
            {
                return point;
            }
        }

        return Vector3.zero;
    }

    /* 
    * Addition code for Left eye 
    */
    public void UpdatePosition(Vector3 center)
    {
        if (center == -Vector3.one || !RightEye)
        {
            transform.parent.transform.localScale = Vector3.one;
            transform.parent.transform.rotation = Quaternion.identity;
        }
        else
        {
            transform.parent.transform.localScale = new Vector3(1, -1, 1);
            transform.parent.transform.rotation = Quaternion.Euler(0, 0, 180);
        }

        if (center == -Vector3.one)
            center = Vector3.zero;
        transform.parent.transform.localPosition = RightEye ? -center : center;

    }

    /* 
     * end of addition code for Left eye 
     */
}
