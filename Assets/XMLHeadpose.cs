using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("Headposes")]
public class XMLHeadpose
{
    public List<HeadposeDef> Headpose;

    public XMLHeadpose()
    {
        Headpose = new List<HeadposeDef>();
    }
}

public class HeadposeDef
{
    public Vector3 Rotation;
    public Vector3 Position;
    public Vector3 LookAtPoint;

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var hp2 = (HeadposeDef)obj;
        return (Rotation == hp2.Rotation && Position == hp2.Position && LookAtPoint == hp2.LookAtPoint);
    }

    public override int GetHashCode()
    {
        return Rotation.GetHashCode() ^ Position.GetHashCode() ^ LookAtPoint.GetHashCode();
    }
    public HeadposeDef Clone()
    {
        HeadposeDef hp = new HeadposeDef();
        hp.Rotation = Rotation;
        hp.Position = Position;
        hp.LookAtPoint = LookAtPoint;
        return hp;
    }
}
