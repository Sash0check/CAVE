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

        //foreach (var disp in .displays)
        CreateWalls(config.displays);


    }

    void CreateWalls(DisplayEntry[] displays)
    {
        var front = System.Array.Find(displays, d => d.name.ToLower() == "front");
        var left = System.Array.Find(displays, d => d.name.ToLower() == "left");
        foreach (var disp in displays) {
            float frontWidth = front != null ? front.width : disp.width;
            float sideWidth = left != null ? left.width : disp.width;

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = $"Wall_{disp.name}";
            wall.transform.SetParent(wallsParent, false);
            wall.transform.localScale = new Vector3(disp.width, disp.height, 1);
            wall.GetComponent<Renderer>().material = new Material(wallMaterial);

            Vector3 pos = Vector3.zero;
            Vector3 wallRot = Vector3.zero;

            // compute wall distance automatically from wall size
            switch (disp.name.ToLower())
            {
                case "front":
                    pos = new Vector3(0, 0, sideWidth / 2f);
                    wallRot = new Vector3(0, 180, 0);
                    break;
                case "back":
                    pos = new Vector3(0, 0, -sideWidth / 2f);
                    wallRot = Vector3.zero;
                    break;
                case "left":
                    pos = new Vector3(-frontWidth / 2f, 0, 0);
                    wallRot = new Vector3(0, 90, 0);
                    break;
                case "right":
                    pos = new Vector3(frontWidth / 2f, 0, 0);
                    wallRot = new Vector3(0, -90, 0);
                    break;
            }

            wall.transform.localPosition = pos;
            wall.transform.localRotation = Quaternion.Euler(wallRot + new Vector3(0, 180, 0));

            int caveLayer = LayerMask.NameToLayer("CaveWalls");
            wall.layer = caveLayer;

            int displayIndex = 0;
            int.TryParse(disp.DisplayIndex, out displayIndex);

            GameObject camObj = new GameObject($"Camera_{disp.name}");
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

            Vector3 halfW = wall.transform.right * (disp.width / 2);
            Vector3 halfH = wall.transform.up * (disp.height / 2);

            Vector3 pLL = wall.transform.position - halfW - halfH;
            Vector3 pLR = wall.transform.position + halfW - halfH;
            Vector3 pUL = wall.transform.position - halfW + halfH;

            pLL = transform.InverseTransformPoint(pLL);
            pLR = transform.InverseTransformPoint(pLR);
            pUL = transform.InverseTransformPoint(pUL);

            var proj = camObj.AddComponent<CAVEProjection>();
            proj.eye = eyeTransform;
            proj.lowerLeft = CreateCorner("LL_" + disp.name, pLL);
            proj.lowerRight = CreateCorner("LR_" + disp.name, pLR);
            proj.upperLeft = CreateCorner("UL_" + disp.name, pUL);
            proj.nearClip = nearClip;
            proj.farClip = farClip;

            RenderTexture rt = new RenderTexture(disp.textureWidth, disp.textureHeight, 24);
            rt.name = $"RT_{disp.name}";
            cam.targetTexture = rt;
            wall.GetComponent<Renderer>().material.mainTexture = rt;

            GameObject senderObg = new GameObject($"Spout_{disp.name}");
            senderObg.transform.SetParent(spoutsParent);
            SpoutSender sender = senderObg.AddComponent<SpoutSender>();
            sender.SetResources(_resources);
            sender.spoutName = disp.name;
            sender.captureMethod = CaptureMethod.Texture;
            sender.sourceTexture = rt;
        }
    }

    Transform CreateCorner(string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(cornersParent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }
}
