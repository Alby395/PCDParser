using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public static class PCDParser
{
    private static int ThreadCount = 4;

    public static IEnumerator ParsePCD(byte[] bytes, Transform transform, MeshFilter mf)
    {
        Debug.Log("START");
        var task = ParsePCDThread(bytes);

        while(!task.IsCompleted)
            yield return null;

        PCDData data = task.Result;

        Mesh m = new Mesh();
        m.name = "PointCloud";
        m.SetVertices(data.position);
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetIndices(System.Linq.Enumerable.Range(0, data.points).ToArray(), MeshTopology.Points, 0);
        m.SetColors(data.color);

        yield return null;

        m.UploadMeshData(true);
        
        yield return null;
        mf.mesh = m;
    }


    private static async Task<PCDData> ParsePCDThread(byte[] bytes)
    {
        PCDData pcd = ParseHeader(bytes);

        Debug.Log(pcd.str);

        List<Vector3> position = new List<Vector3>();

        int color_offset = 0;

        pcd.offset.TryGetValue("rgb", out color_offset);

        if(color_offset == 0)
            pcd.offset.TryGetValue("rgba", out color_offset);

        List<Color32> color = new List<Color32>();
        bool colorBool = color_offset > 0;

        if(pcd.data.Equals("ascii"))
        {
            Debug.Log("ASCII NYI");
        }

        if(pcd.data.Equals("binary"))
        {
            int size = pcd.points/ThreadCount;

            Task<PointData>[] tasks = new Task<PointData>[ThreadCount];
            int i;
            
            for(i = 0; i < ThreadCount; i++)
            {
                byte[] data = new byte[pcd.rowSize * size];
                Array.Copy(bytes, pcd.headerLen + i * pcd.rowSize * size, data, 0, size * pcd.rowSize);

                tasks[i] = Task.Run((() => BinaryParse(data, size, pcd.rowSize, pcd.offset, colorBool, color_offset)));
            }       
            
            PointData[] result = await Task.WhenAll(tasks);
            foreach(var d in result)
            {
                position.AddRange(d.points);
                color.AddRange(d.colors);
            }

            pcd.position = position.ToArray();
            pcd.color = color.ToArray();
        }

        pcd.points = pcd.position.Length;

        return pcd;
    }

    private static PointData BinaryParse(byte[] data, int points, int rowSize, Dictionary<string, int> offset, bool color, int color_offset)
    {
        Debug.Log("START THREAD");

        PointData pointData = new PointData();
        pointData.points = new List<Vector3>();
        pointData.colors = new List<Color32>();

        Vector3 positionTmp = new Vector3();
        Color32 colorTmp = new Color();
        colorTmp.a = 1;
        
        int row = 0;
        for(int p = 0; p < points; row += rowSize, p++)
        {
            positionTmp.x = BitConverter.ToSingle(data, row + offset["x"]);
            
            positionTmp.y = BitConverter.ToSingle(data, row + offset["y"]);

            positionTmp.z = BitConverter.ToSingle(data, row + offset["z"]);
                       
            if(!float.IsNaN(positionTmp.x) && !float.IsNaN(positionTmp.y) && !float.IsNaN(positionTmp.z))
            {
                pointData.points.Add(positionTmp);
                
                if(color)
                {
                    colorTmp.b = data[row + color_offset + 0];
                    colorTmp.g = data[row + color_offset + 1];
                    colorTmp.r = data[row + color_offset + 2];
                    pointData.colors.Add(colorTmp);
                }
            }
        }

        Debug.Log("END THREAD");

        return pointData;
    }

    private static PCDData ParseHeader(byte[] bytes)
    {
        PCDData pcd = new PCDData();
        
        string headerText = "";

        int max = bytes.Length;

        // Find DATA field (end of header)
        Regex rg = new Regex("[\r\n]DATA\\s(\\S*)\\s");
        Match m;
        int j = 0;
        do{
            headerText += Convert.ToChar(bytes[j++]);
            m = rg.Match(headerText);
        }while(j < max && !m.Success);
        
        pcd.data = m.Groups[1].Value;
        pcd.headerLen = m.Index + m.Groups[0].Length;
        pcd.str = headerText.Substring(0, pcd.headerLen);
        
        // Remove comments
        pcd.str = Regex.Replace(pcd.str, "\\#.*\\n", "");
        
        // Retrieve value from fields
        m = Regex.Match(pcd.str, "VERSION (.*)");
        if(m.Success)
        {
            Debug.Log(m.Groups[1].Value);
            pcd.version = float.Parse("0" + m.Groups[1].Value);
        }
            
        m = Regex.Match(pcd.str, "FIELDS (.*)");
        if(m.Success)
        {
            pcd.fields = m.Groups[1].Value.Split(' ');
        }
        m = Regex.Match(pcd.str, "SIZE (.*)");
        if(m.Success)
        {
            string[] positionTmp = m.Groups[1].Value.Split(' ');

            int[] val = new int[positionTmp.Length];

            for(int i = 0; i < positionTmp.Length; i++)
            {
                val[i] = int.Parse(positionTmp[i]);
            }

            pcd.size = val;
        }

        m = Regex.Match(pcd.str, "TYPE (.*)");
        if(m.Success)
        {
            pcd.type = m.Groups[1].Value.Split(' ');
        }

        m = Regex.Match(pcd.str, "COUNT (.*)");
        if(m.Success)
        {
            string[] positionTmp = m.Groups[1].Value.Split(' ');

            int[] val = new int[positionTmp.Length];

            for(int i = 0; i < positionTmp.Length; i++)
            {
                val[i] = int.Parse(positionTmp[i]);
            }

            pcd.count = val;
        }

        m = Regex.Match(pcd.str, "WIDTH (.*)");
        if(m.Success)
        {
            pcd.width = int.Parse(m.Groups[1].Value);
        }

        m = Regex.Match(pcd.str, "HEIGHT (.*)");
        if(m.Success)
        {
            pcd.height = int.Parse(m.Groups[1].Value);
        }

        m = Regex.Match(pcd.str, "VIEWPOINT (.*)");
        if(m.Success)
        {
            pcd.viewpoint = m.Groups[1].Value;
        }

        m = Regex.Match(pcd.str, "POINTS (.*)");
        if(m.Success)
        {
            pcd.points = int.Parse(m.Groups[1].Value);
        }
        else
        {
            pcd.points = pcd.width * pcd.height;
        }

        /*
        if (header.count === null) {
            header.count = []
            for (i = 0; i < header.fields; i++) {
            header.count.push(1)
            }
        }
        */
        int sizeSum = 0;
        pcd.offset = new Dictionary<string, int>();

        for(int i = 0; i < pcd.fields.Length; i++)
        {
            
            if(pcd.data.Equals("ascii"))
            {
                pcd.offset.Add(pcd.fields[i], i);
            }
            else if(pcd.data.Equals("binary"))
            {
                pcd.offset.Add(pcd.fields[i], sizeSum);
                sizeSum += pcd.size[i];
            }
            else if(pcd.data.Equals("binary_compressed"))
            {
                pcd.offset.Add(pcd.fields[i], sizeSum);
                sizeSum += pcd.size[i] * pcd.points;
            }
        }

        pcd.rowSize = sizeSum;
        Debug.Log(pcd.ToString());
        return pcd;
    }
}

public struct PCDData
{
    public string str;
    public int headerLen;
    public string data;
    internal float version;
    internal string[] fields;
    internal int[] size;
    internal string[] type;
    internal int[] count;
    internal int width;
    internal int height;
    internal string viewpoint;
    internal int points;
    internal Dictionary<string, int> offset;
    internal int rowSize;
    internal Vector3[] position;
    internal Color32[] color;

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Version " + version);
        builder.AppendLine("field");

        foreach(string val in fields)
        {
            builder.Append(" " + val);
        }

        return builder.ToString();
    }   
}

public struct PointData
{
    public List<Vector3> points;
    public List<Color32> colors;
}
