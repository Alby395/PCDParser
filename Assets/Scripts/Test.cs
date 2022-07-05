using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class Test: MonoBehaviour
{

    private MeshFilter _mf;
    private ParticleSystem _ps;

    private PCDData _pcd;

    private void Start()
    {
        _mf = GetComponent<MeshFilter>();
        _ps = GetComponent<ParticleSystem>();

        byte[] b = new byte[1];//File.ReadAllBytes("/Users/albertopatti/Git/PCDParser/Assets/5_icon.pcd");

        StartCoroutine(PCDParser.ParsePCD(b, transform, _mf));

    }
/*
    private IEnumerator ShowPointCloudMesh()
    {
        print("MESH");
        while(!PCDParser.ready)
            yield return null;

        Mesh m = new Mesh();

        m.SetVertices(_pcd.position);
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetIndices(Enumerable.Range(0, _pcd.points).ToArray(), MeshTopology.Points, 0);
        m.SetColors(_pcd.color);
        m.UploadMeshData(true);

        _mf.mesh = m;

        print("FINE MESH");
    }

    private IEnumerator ShowPointCloudParticles()
    {
        while(!PCDParser.ready)
            yield return null;
        
        var particles = new ParticleSystem.Particle[_pcd.points];
 
        for (int i = 0; i < particles.Length; ++i)
        {
            particles[i].position = _pcd.position[i];
            particles[i].startSize = 0.005f;
            particles[i].startColor = _pcd.color[i];
        }

        _ps.SetParticles(particles);
        _ps.Pause();
    }
    */
}