using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("UserId")]
public class XMLUserId
{
    //PCA mesh
    public Vector3 PcaOffset;
    public float PcaScale;
    public float[] PcaCoeffs;

    //texture
    public int Texture;
    public bool SmoothNose;
    public float SmoothNoseSize;
    public float SkinThickness;
    public bool DoShrinkWrap;

    //iris & pupil
    public float EyeIrisSize;
    public int EyeTexture;
    public float NominalPupilSize;
    public bool IsPupilVariable;
    
    //rotation of iris and sclera texture left
    public float EyeIrisRotationLeft;
    public float EyeScleraRotationLeft;
    //rotation of iris and sclera texture right
    public float EyeIrisRotationRight;
    public float EyeScleraRotationRight;

    //eyeballs
    public float VerticalAngleLeft;
    public float HorizontalAngleLeft;
    public float VerticalAngleRight;
    public float HorizontalAngleRight;

}

