using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NoseRegion : MonoBehaviour
{    
    public GameObject controlMeshObj;

    protected class Vertex: IEquatable<Vertex>, IComparable<Vertex>
    {
        public Vector3 v;
        public Vector3 n;
        public Vector2 uv;

        public Vertex(Vector3 v, Vector3 n, Vector2 uv)
        {
            this.v = v;
            this.n = n;
            this.uv = uv;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = v.GetHashCode();
                result = (result * 397) ^ n.GetHashCode();
                result = (result * 397) ^ uv.GetHashCode();
                return result;
            }
        }

        public bool Equals(Vertex other)
        {
            return v.Equals(other.v) && n.Equals(other.n) && uv.Equals(other.uv);
        }

        public int CompareTo(Vertex b)
        {
            if (Equals(b))
                return 0;
            if (v.y > b.v.y || (v.y == b.v.y && (v.x > b.v.x || (v.x == b.v.x && v.z >= b.v.z))))
                return 1;

            return -1;
        }
    }

    protected class Edge : IEquatable<Edge>, IComparable<Edge>
    {
        public Vertex v1;
        public Vertex v2;

        public Edge(Vertex v1, Vertex v2)
        {
            if (v1.CompareTo(v2) >= 0) //order by y
            {
                this.v1 = v1;
                this.v2 = v2;
            }
            else
            {
                this.v1 = v2;
                this.v2 = v1;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = v1.GetHashCode();
                result = (result * 397) ^ v2.GetHashCode();
                return result;
            }
        }

        public bool Equals(Edge other)
        {
            return v1.Equals(other.v1) && v2.Equals(other.v2);
        }

        public int CompareTo(Edge b)
        {
            return (v1.CompareTo(b.v1));
        }
    }

    public void UpdateNose(Vector3 center)
    {
        return;
        center.Scale(new Vector3(1f/transform.localScale.x, 1f / transform.localScale.y, 1f / transform.localScale.z));
                
        Edge[] edge = GetMeshEdges(controlMeshObj.transform.GetComponent<MeshFilter>().mesh);

        int vertCount = edge.Count() * 2 + 2;
        Vector3[] new_vs = new Vector3[vertCount];
        Vector3[] new_ns = new Vector3[vertCount];
        Vector2[] new_uvs = new Vector2[vertCount];

        int nbTriangles = 3 * (vertCount -2);
        int[] new_ts = new int[nbTriangles];

        for (int i = 0; i < edge.Count(); i++)
        {
            int index = i * 2;
            if (i == 0)
            {
                //v0 (up right)
                new_vs[(i * 2) + 0] = center + edge[i].v1.v;
                new_ns[index + 0] = edge[i].v1.n;
                new_uvs[index + 0] = edge[i].v1.uv;
                //v1 (up left)
                new_vs[index + 1] = center + edge[i].v1.v;
                new_vs[index + 1].x *= -1f;
                new_ns[index + 1] = edge[i].v1.n;
                Vector3 n1 = Quaternion.AngleAxis(180, Vector3.forward) * edge[i].v1.n;
                n1.y *= -1.0f;
                new_ns[index + 1] = n1;
                new_uvs[index + 1] = edge[i].v1.uv;                
            }
            //v2 (bottom right)
            new_vs[index + 2] = center + edge[i].v2.v;
            new_ns[index + 2] = edge[i].v2.n;
            new_uvs[index + 2] = edge[i].v2.uv;
            //v1 (bottom left)
            new_vs[index + 3] = center + edge[i].v2.v;
            new_vs[index + 3].x *= -1f;
            new_ns[index + 3] = edge[i].v2.n;
            Vector3 n2 = Quaternion.AngleAxis(180, Vector3.forward) * edge[i].v2.n;
            n2.y *= -1.0f;
            new_ns[index + 3] = n2;
            new_uvs[index + 3] = edge[i].v2.uv;

            //triangle1
            new_ts[i * 6 + 0] = index + 0;
            new_ts[i * 6 + 1] = index + 2;
            new_ts[i * 6 + 2] = index + 1;
            //triangle2
            new_ts[i * 6 + 3] = index + 1;
            new_ts[i * 6 + 4] = index + 2;
            new_ts[i * 6 + 5] = index + 3;

        }

        Mesh mesh = new Mesh();
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = new_vs;
        mesh.normals = new_ns;
        mesh.uv = new_uvs;
        mesh.triangles = new_ts;      
    }

    //get only the edges that are common to both side of the face
    private Edge[] GetMeshEdges(Mesh mesh)
    {
        Dictionary<Edge,int> edges = new Dictionary<Edge, int>();

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            Vertex v1 = new Vertex(mesh.vertices[mesh.triangles[i]], mesh.normals[mesh.triangles[i]], mesh.uv[mesh.triangles[i]]);
            Vertex v2 = new Vertex(mesh.vertices[mesh.triangles[i + 1]], mesh.normals[mesh.triangles[i + 1]], mesh.uv[mesh.triangles[i + 1]]);
            Vertex v3 = new Vertex(mesh.vertices[mesh.triangles[i + 2]], mesh.normals[mesh.triangles[i + 2]], mesh.uv[mesh.triangles[i + 2]]);

            Edge v1v2 = new Edge(v1, v2);
            if (!edges.ContainsKey(v1v2))
            {
                edges.Add(v1v2, 0);
            }
            edges[v1v2]++;

            Edge v1v3 = new Edge(v1, v3);
            if (!edges.ContainsKey(v1v3))
                edges.Add(v1v3, 0);
            edges[v1v3]++;

            Edge v2v3 = new Edge(v2, v3);
            if (!edges.ContainsKey(v2v3))
                edges.Add(v2v3, 0);
            edges[v2v3]++;
        }

        List<Edge> es = new List<Edge>();
        foreach(KeyValuePair<Edge, int> edge in edges)
        {
            if (edge.Value == 1) //edge is unique, it means it is the border
            {
                Vector3 dir = edge.Key.v1.v - edge.Key.v2.v;
                dir.z = 0;
                float angle = Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(dir.normalized, Vector3.up));
                float thresholdAngle = 40;
                if ( (angle < thresholdAngle || angle > (180- thresholdAngle)) && edge.Key.v1.v.x > 0) //left side
                    es.Add(edge.Key);
            }
        }

        //makes sure that the edges belong to the left size of the half face only!
        return continuousEdges(es.OrderByDescending(o => o).ToList());
    }

    protected Edge[] continuousEdges(List<Edge> edges)
    {
        List<Edge> toremove = new List<Edge>();
        Edge previousItem = edges[0];
        for(int i = 1; i < edges.Count; ++i)
        {
            if(!edges[i].v1.Equals(previousItem.v2))
                toremove.Add(edges[i]);
            else
                previousItem = edges[i];
        }
        foreach (Edge e in toremove)
            edges.Remove(e);
        return edges.ToArray();
    }
}

