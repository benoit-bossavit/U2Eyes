using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightMeter : MonoBehaviour
{   
    public Camera cam;
    public GameObject sensor;
    public Text PhotText;

    public float Phot;
    public float PhMin;
    public float PhMax;
    
    public  delegate void PhotUpdated(LightMeter lightMeter);
    public static event PhotUpdated OnPhotUpdate;

    private Rect rec = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
    
    private List<Color> _samples;
    private List<float> _brightness;
    private List<Vector3> _camPOS;
    private List<Vector3> _camROT;
    private int _camId = 0;
    

    protected float _currentPhot;
    protected float _previousPhot;

    void Start()
    {
        _camPOS = new List<Vector3>();
        _camROT = new List<Vector3>();

        //front:
        _camPOS.Add(new Vector3(0.0f, 0.0f, 1f));
        _camROT.Add(new Vector3(180.0f, 0.0f, 0.0f));
        //top:
        _camPOS.Add(new Vector3(0.0f, 1f, 0.0f));
        _camROT.Add(new Vector3(90.0f, 0.0f, 0.0f));
        //bottom:
        _camPOS.Add(new Vector3(0.0f, -1f, 0.0f));
        _camROT.Add(new Vector3(-90.0f, 0.0f, 0.0f));
        //left:
        _camPOS.Add(new Vector3(-1f, 0.0f, 0.0f));
        _camROT.Add(new Vector3(0.0f, 90.0f, 0.0f));
        //right:
        _camPOS.Add(new Vector3(1f, 0.0f, 0.0f));
        _camROT.Add(new Vector3(0.0f, -90.0f, 0.0f));


        _samples = new List<Color>();
        _brightness = new List<float>();
        for(int i = 0; i < _camPOS.Count; ++i)
        {
            _samples.Add(Color.gray);
            _brightness.Add(0);
        }

        _previousPhot = -1;

        PhMin = 0f;
        PhMax = 1f;
    }

    public void UpdatePhotometer()
    {
        _previousPhot = -1f;
    }
    static int gg = 0;
    void Update()
    {
        if (_previousPhot == _currentPhot && _currentPhot != Phot)
        {
            Phot = _currentPhot;
            OnPhotUpdate(this);
            PhotText.text = Phot.ToString("F4");
        }

        GetPix();
        SumPix();
    }

    private void GetPix()
    {
        cam.gameObject.SetActive(true);
        //adjust camera:
        cam.transform.localPosition = _camPOS[_camId];
        cam.transform.localRotation = Quaternion.Euler(_camROT[_camId]);


        sensor.SetActive(true);
        //get samples:
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
        //RenderTexture rt_temp = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        RenderTexture rt_temp = new RenderTexture(1, 1, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        RenderTexture rt_prev = RenderTexture.active;
        cam.targetTexture = rt_temp;
        cam.Render();
        RenderTexture.active = rt_temp;
        tex.ReadPixels(rec, 0, 0, false);
        _samples[_camId] = tex.GetPixelBilinear(0, 0);
        _brightness[_camId] += 0.299f * _samples[_camId].r + 0.587f * _samples[_camId].g + 0.114f * _samples[_camId].b;
        //cleanup:
        cam.targetTexture = null;
        RenderTexture.active = rt_prev;
        //RenderTexture.ReleaseTemporary(rt_temp);
        Destroy(rt_temp);
        Destroy(tex);
        Destroy(cam.targetTexture);

        //sensor.SetActive(false);
        cam.gameObject.SetActive(false);
        //update index:
        _camId++;

    }

    private void SumPix()
    {            
        if (_camId >= _samples.Count)
        {
            _previousPhot = _currentPhot;

            _currentPhot = 0;
            //calculate avg brightness
            foreach(float b in _brightness)
                _currentPhot += b /_brightness.Count;
            
            //reset for next iteration            
            _camId = 0;
            BrightnessZero();
        }
    }

    private void BrightnessZero()
    {
        for (int i = 0; i < _brightness.Count; ++i)
            _brightness[i] = 0.0f;
    }

}