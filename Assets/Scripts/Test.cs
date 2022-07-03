using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public class Test: MonoBehaviour
{

    private MeshFilter _mf;
    private PCDData _pcd;

    private void Start()
    {
        _mf = GetComponent<MeshFilter>();

        byte[] b = File.ReadAllBytes("C:\\Users\\alby3\\Downloads\\5_icon.pcd");

        PCDParser.ParsePCD(b, (data) => _pcd = data);

        StartCoroutine(ShowPointCloud());
    }

    private IEnumerator ShowPointCloud()
    {
        while(!PCDParser.ready)
            yield return null;

        Mesh m = new Mesh();
        
        m.SetVertices(_pcd.position);
        m.SetIndices(Enumerable.Range(0, _pcd.points).ToArray(), MeshTopology.Points, 0);
        m.SetColors(_pcd.color);
        m.UploadMeshData(true);

        _mf.mesh = m;
    }
}