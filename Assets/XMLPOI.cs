using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot("POI")]
public class XMLPOI
{
    public XMLPOI()
    {
        POILeft = new List<POIDef>();
        POIRight = new List<POIDef>();
    }

    public float FinalPupilSize;
    public float Phot;
    public List<POIDef> POILeft; //one per headpose
    public List<POIDef> POIRight;//one per headpose
   
}

public class POIDef
{
    public POIDef()
    {
        Caruncle = new POIPoint();
        InteriorMargin = new POIPoint();
        //InteriorMarginInterpolated = new POIPoint();
        Iris = new POIPoint();
        Pupil = new POIPoint();
    }

    public POIPoint Caruncle;
    public POIPoint InteriorMargin;
    //public POIPoint InteriorMarginInterpolated;
    public POIPoint Iris;    
    public POIPoint Pupil;
    public Vector2 IrisCenter2D;
    public Vector3 IrisCenter3D;
    public Vector2 PupilCenter2D;
    public Vector3 PupilCenter3D;
    public Vector2 CorneaCenter2D;
    public Vector3 CorneaCenter3D;
    public Vector2 GlobeCenter2D;
    public Vector3 GlobeCenter3D;
}

public class POIPoint
{
    public POIPoint()
    {
        Point2D = new List<Vector2>();
        Point3D = new List<Vector3>();
    }

    public List<Vector2> Point2D;
    public List<Vector3> Point3D;    
}
