using UnityEngine;

[ExecuteAlways]
public class CAVEProjection : MonoBehaviour
{
    public Transform eye;
    public Transform lowerLeft;
    public Transform lowerRight;
    public Transform upperLeft;
    public float nearClip = 0.1f;
    public float farClip = 100f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!cam || !eye || !lowerLeft || !lowerRight || !upperLeft) return;

        Vector3 pa = lowerLeft.position;
        Vector3 pb = lowerRight.position;
        Vector3 pc = upperLeft.position;
        Vector3 pe = eye.position;

        Vector3 vr = (pb - pa).normalized;
        Vector3 vu = (pc - pa).normalized;
        Vector3 vn = -Vector3.Cross(vr, vu).normalized;

        float d = -Vector3.Dot((pa - pe), vn);
        float l = Vector3.Dot(vr, (pa - pe)) * nearClip / d;
        float r = Vector3.Dot(vr, (pb - pe)) * nearClip / d;
        float b = Vector3.Dot(vu, (pa - pe)) * nearClip / d;
        float t = Vector3.Dot(vu, (pc - pe)) * nearClip / d;

        Matrix4x4 P = PerspectiveOffCenter(l, r, b, t, nearClip, farClip);
        cam.projectionMatrix = P;

        Matrix4x4 M = new Matrix4x4();
        M.SetColumn(0, new Vector4(vr.x, vr.y, vr.z, 0));
        M.SetColumn(1, new Vector4(vu.x, vu.y, vu.z, 0));
        M.SetColumn(2, new Vector4(vn.x, vn.y, vn.z, 0));
        M.SetColumn(3, new Vector4(0, 0, 0, 1));
        M = M.transpose;

        cam.worldToCameraMatrix = M * Matrix4x4.Translate(-pe);
    }

    Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0f * near / (right - left);
        float y = 2.0f * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0f * far * near) / (far - near);
        float e = -1.0f;

        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x; m[0, 1] = 0; m[0, 2] = a; m[0, 3] = 0;
        m[1, 0] = 0; m[1, 1] = y; m[1, 2] = b; m[1, 3] = 0;
        m[2, 0] = 0; m[2, 1] = 0; m[2, 2] = c; m[2, 3] = d;
        m[3, 0] = 0; m[3, 1] = 0; m[3, 2] = e; m[3, 3] = 0;
        return m;
    }
}
