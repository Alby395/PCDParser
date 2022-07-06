using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Test: MonoBehaviour
{

    private MeshFilter _mf;
    private ParticleSystem _ps;

    private PCDData _pcd;

    private void Start()
    {
        _mf = GetComponent<MeshFilter>();
        _ps = GetComponent<ParticleSystem>();
    }

    public void LoadPCD()
    {
        StartCoroutine(LoadPCDCoroutine());
    }

    private IEnumerator LoadPCDCoroutine()
    {
        UriBuilder uriBuilder = new UriBuilder();
        uriBuilder.Scheme = "file";
        uriBuilder.Path = "D:/Other Projects/PointCloud/Assets/object3d.pcd";
        print(uriBuilder.Uri);

        UnityWebRequest request = UnityWebRequest.Get(uriBuilder.Uri);


        yield return request.SendWebRequest();

        byte[] bytes = request.downloadHandler.data;
        
        yield return StartCoroutine(PCDParser.ParsePCD(bytes, transform, _mf));
    }
}