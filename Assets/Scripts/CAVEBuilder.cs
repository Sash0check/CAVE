using Klak.Spout;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class DisplayEntry
{
    public string name;
    public string DisplayIndex;
    public float width;
    public float height;
    public int textureWidth;
    public int textureHeight;
}

[System.Serializable]
public class CaveConfig
{
    public float[] eyePosition;
    public DisplayEntry[] displays;
}

[System.Serializable]
public struct WallRT
{
    public RenderTexture rt;
    public int x, y, w, h;
}


public class CAVEBuilder : MonoBehaviour
{
    public string configPath = "cave_config.json";
    public Material wallMaterial;
    public float nearClip = 0.1f;
    public float farClip = 100f;
    public bool showWallsInCameras = false;

    private Transform eyeTransform;
    private Transform wallsParent;
    private Transform camerasParent;
    private Transform cornersParent;
    private Transform spoutsParent;
    SpoutResources _resources = null;

    void Start()
    {
        _resources = (SpoutResources)Resources.Load("SpoutResources");
        Debug.Log(_resources);
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        wallsParent = new GameObject("Walls").transform;
        camerasParent = new GameObject("Cameras").transform;
        cornersParent = new GameObject("Corners").transform;
        spoutsParent = new GameObject("Spouts").transform;

        wallsParent.SetParent(transform, false);
        camerasParent.SetParent(transform, false);
        cornersParent.SetParent(transform, false);
        spoutsParent.SetParent(transform, false);

        string fullPath = Path.Combine(Application.streamingAssetsPath, configPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError("CAVE config not found: " + fullPath);
            return;
        }

        string json = File.ReadAllText(fullPath);
        CaveConfig config = JsonUtility.FromJson<CaveConfig>(json);

        GameObject eyeObj = new GameObject("Eye");
        Vector3 eyePos = config.eyePosition != null && config.eyePosition.Length == 3
            ? new Vector3(config.eyePosition[0], config.eyePosition[1], config.eyePosition[2])
            : new Vector3(0, 1.6f, 0);
        eyeObj.transform.SetParent(transform, false);
        eyeObj.transform.localPosition = eyePos;
        eyeTransform = eyeObj.transform;

        //GetComponent<SpoutSender>().sourceTexture = finalRT;

        foreach (var disp in config.displays)
            CreateWall(disp);


    }

    void CreateWall(DisplayEntry config)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = $"Wall_{config.name}";
        wall.transform.SetParent(wallsParent, false);
        wall.transform.localScale = new Vector3(config.width, config.height, 1);
        wall.GetComponent<Renderer>().material = new Material(wallMaterial);

        Vector3 pos = Vector3.zero;
        Vector3 wallRot = Vector3.zero;
        float dist = 0f;

        // compute wall distance automatically from wall size
        switch (config.name.ToLower())
        {
            case "front":
                dist = config.width / 2f;
                pos = new Vector3(0, 0, dist);
                wallRot = new Vector3(0, 180, 0);
                break;
            case "back":
                dist = config.width / 2f;
                pos = new Vector3(0, 0, -dist);
                wallRot = Vector3.zero;
                break;
            case "left":
                dist = config.width / 2f;
                pos = new Vector3(-dist, 0, 0);
                wallRot = new Vector3(0, 90, 0);
                break;
            case "right":
                dist = config.width / 2f;
                pos = new Vector3(dist, 0, 0);
                wallRot = new Vector3(0, -90, 0);
                break;
            case "floor":
                dist = config.height / 2f;
                pos = new Vector3(0, -dist, 0);
                wallRot = new Vector3(90, 180, 0);
                break;
            case "ceiling":
                dist = config.height / 2f;
                pos = new Vector3(0, dist, 0);
                wallRot = new Vector3(-90, 180, 0);
                break;
        }

        wall.transform.localPosition = pos;
        wall.transform.localRotation = Quaternion.Euler(wallRot + new Vector3(0, 180, 0));

        int caveLayer = LayerMask.NameToLayer("CaveWalls");
        wall.layer = caveLayer;

        int displayIndex = 0;
        int.TryParse(config.DisplayIndex, out displayIndex);

        GameObject camObj = new GameObject($"Camera_{config.name}");
        camObj.transform.SetParent(camerasParent, false);
        camObj.transform.localPosition = eyeTransform.localPosition;
        camObj.transform.LookAt(wall.transform, transform.up);

        var cam = camObj.AddComponent<Camera>();
        cam.targetDisplay = displayIndex;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = nearClip;
        cam.farClipPlane = farClip;

        if (!showWallsInCameras)
            cam.cullingMask &= ~(1 << caveLayer);

        Vector3 halfW = wall.transform.right * (config.width / 2);
        Vector3 halfH = wall.transform.up * (config.height / 2);

        Vector3 pLL = wall.transform.position - halfW - halfH;
        Vector3 pLR = wall.transform.position + halfW - halfH;
        Vector3 pUL = wall.transform.position - halfW + halfH;

        pLL = transform.InverseTransformPoint(pLL);
        pLR = transform.InverseTransformPoint(pLR);
        pUL = transform.InverseTransformPoint(pUL);

        var proj = camObj.AddComponent<CAVEProjection>();
        proj.eye = eyeTransform;
        proj.lowerLeft = CreateCorner("LL_" + config.name, pLL);
        proj.lowerRight = CreateCorner("LR_" + config.name, pLR);
        proj.upperLeft = CreateCorner("UL_" + config.name, pUL);
        proj.nearClip = nearClip;
        proj.farClip = farClip;

        RenderTexture rt = new RenderTexture(config.textureWidth, config.textureHeight, 24);
        rt.name = $"RT_{config.name}";
        cam.targetTexture = rt;
        wall.GetComponent<Renderer>().material.mainTexture = rt;

        GameObject senderObg = new GameObject($"Spout_{config.name}");
        senderObg.transform.SetParent( spoutsParent);
        SpoutSender sender = senderObg.AddComponent<SpoutSender>();
        sender.SetResources(_resources);
        sender.spoutName = config.name;
        sender.captureMethod = CaptureMethod.Texture;
        sender.sourceTexture = rt;
    }

    Transform CreateCorner(string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(cornersParent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }
}
