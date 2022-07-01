using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class Test: MonoBehaviour
{

    private ParticleSystem _ps;
    private PCDData _pcd;

    private void Start()
    {
        _ps = GetComponent<ParticleSystem>();

        byte[] b = File.ReadAllBytes(Application.persistentDataPath + "/object3d.pcd");

        PCDParser.ParsePCD(b, (data) => _pcd = data);

        StartCoroutine(ShowPointCloud());
    }

    private IEnumerator ShowPointCloud()
    {
        while(!PCDParser.ready)
            yield return null;

        var particles = new ParticleSystem.Particle[_pcd.points];

        for(int i = 0; i < particles.Length; i++)
        {
            particles[i].position = new Vector3(_pcd.position[i * 3 + 0], _pcd.position[i * 3 + 1], _pcd.position[i * 3 + 2]);
            particles[i].startColor = new Color32(_pcd.color[i * 3 + 0], _pcd.color[i * 3 + 0], _pcd.color[i * 3 + 0], 1);
            particles[i].startSize = 5;
        }
        yield return null;

        _ps.SetParticles(particles);
        _ps.Pause();
    }
}