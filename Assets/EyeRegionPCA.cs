using UnityEngine;
using System.Collections;
using SimpleJSON;
using System;

public class EyeRegionPCA : MonoBehaviour {

	private float[,] mesh_mean = new float[229,3];
	private float[,] pca_components = new float[687, 20];
	private float[] transformed_mean = new float[20];
	private float[] transformed_std = new float[20];
    private float[] random_coeffs = new float[20];

    private Vector3[] mesh = new Vector3 [229];

    //parameters that identify a specific PCA
    public Vector3 Offset { get; set; }
    public float Scale { get; set; }
    public float[] Coeffs { get; set; }

    protected void loadArray(string filename, float[] arrayToLoad){
		
		TextAsset t = Resources.Load (filename) as TextAsset;
		string[] lines = t.text.Split ('\n');
		
		for (int i=0; i<lines.Length-1; i++) {
			arrayToLoad[i] = float.Parse(lines[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
		}
	}

    protected void loadArray(string filename, float[,] arrayToLoad){

		TextAsset t = Resources.Load (filename) as TextAsset;
		string[] lines = t.text.Split ('\n');
 
		for (int i=0; i<lines.Length-1; i++) {
			string[] line = lines[i].Split(' ');
			for (int j=0; j<arrayToLoad.GetLength(1); j++){
				arrayToLoad[i,j] = float.Parse(line[j], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
			}
		}
	}
	
	void Awake () {

		loadArray ("ShapePCA/mesh_mean", mesh_mean);
		loadArray ("ShapePCA/pca_components", pca_components);
		loadArray ("ShapePCA/transformed_mean", transformed_mean);
		loadArray ("ShapePCA/transformed_std", transformed_std);

        Offset = Vector3.zero;
        Scale = 1f;
        Coeffs = null;
    }

    protected void randomizeCoeffs()
    {
        Coeffs = new float[random_coeffs.Length];
        for (int i = 0; i < random_coeffs.Length; i++)
        {
            Coeffs[i] = SyntheseyesUtils.NextGaussianDouble();
        }
    }

	public Vector3[] RandomizeMesh() {

        randomizeCoeffs();
        return GetMesh();

    }

    public Vector3[] GetMesh()
    {
        if (Coeffs == null)
            return null;

        for (int i = 0; i < random_coeffs.Length; i++)
        {
            random_coeffs[i] = transformed_mean[i] + Coeffs[i] * transformed_std[i] * Scale;
        }

        for (int i = 0; i < mesh_mean.GetLength(0); i++)
        {

            mesh[i].x = -mesh_mean[i, 0] + Offset.x;
            mesh[i].z = -mesh_mean[i, 1] + Offset.z;
            mesh[i].y = mesh_mean[i, 2] + Offset.y;

            for (int j = 0; j < random_coeffs.Length; j++)
            {
                mesh[i].x -= random_coeffs[j] * pca_components[i * 3, j];
                mesh[i].z -= random_coeffs[j] * pca_components[i * 3 + 1, j];
                mesh[i].y += random_coeffs[j] * pca_components[i * 3 + 2, j];
            }

        }
        return mesh;
    }
}
