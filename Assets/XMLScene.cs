using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("Scene")]
public class XMLScene
{
    public int SkyboxId;
    public float SkyboxExposure;
    public float SkyboxRotation;

    public float AmbientIntensity;

    public HSBColor LightColor;
    public Vector3 LightDirection;
    public float LightIntensity;

    public float PhMin;
    public float PhMax;
}

