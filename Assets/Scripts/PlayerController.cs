using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    [Header("移动速度(米/秒)")]
    private float moveSpeed = 10;

    [SerializeField]
    [Range(0.1f, 2f)]
    [Header("旋转速度根据鼠标移动的倍率")]
    private float rotateScale = 0.5f;

    private Vector2? lastMousePos = null;

    [SerializeField]
    [Header("雪地")]
    private Transform snowField;

    [SerializeField]
    [Header("雪地材质")]
    private Material snowFieldMaterial;

    private RenderTexture trackRt;
    [SerializeField]
    [Header("轨迹图宽高")]
    private int rTWidthAndHeight = 512;

    [SerializeField]
    [Header("脚印贴图")]
    private Texture2D demoTexture;

    [SerializeField]
    [Range(0.1f, 10f)]
    [Header("轨迹贴图放大倍率")]
    private float sizeScale = 0.1f;

    [SerializeField]
    [Header("轨迹差值距离，必须大于0")]
    private float trackInterpolation = 1.0f;

    /// <summary>
    /// 轨迹临时列表
    /// </summary>
    List<Vector3> tempList = new List<Vector3>();

    /// <summary>
    /// 记录上一帧位置
    /// </summary>
    Vector3 lastPos;

    private void Start()
    {
        snowFieldMaterial = snowField.GetComponent<MeshRenderer>().sharedMaterial;
        trackRt = new RenderTexture(rTWidthAndHeight, rTWidthAndHeight, 0, RenderTextureFormat.ARGB32);
        snowFieldMaterial.SetTexture("_TrackTex", trackRt);
        lastPos = transform.position;
        drawToPosition(lastPos);
    }

    private void Update()
    {
        ControlPlayer();
        SetPosToWhite();
    }

    void SetPosToWhite()
    {
        if ((lastPos - transform.position).sqrMagnitude > 0.16f)
        {
            GetLinearPointBetweenTwoPoints(lastPos, transform.position, trackInterpolation, ref tempList);
            for (int i = 0; i < tempList.Count; i++)
            {
                drawToPosition(tempList[i]);
            }

            lastPos = transform.position;
        }
    }

    void ControlPlayer()
    {
        if (Input.GetMouseButton(1))
        {
            if (lastMousePos != null)
            {
                var lastPos = (Vector2)lastMousePos;
                var mouseDeltaPos = new Vector2(Input.mousePosition.x - lastPos.x, Input.mousePosition.y - lastPos.y);
                // 绕Y轴旋转
                var rota = Quaternion.Euler(0, mouseDeltaPos.x * rotateScale, 0);
                transform.rotation = transform.rotation * rota;
            }

            lastMousePos = Input.mousePosition;
        }
        else
        {
            lastMousePos = null;
        }

        var axis = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (Mathf.Abs(axis.x) >= 0.01f || Mathf.Abs(axis.z) >= 0.01f)
        {
            axis = transform.rotation * axis;
            axis = axis.normalized * moveSpeed * Time.deltaTime;
            transform.Translate(axis, Space.World);
        }
    }

    void drawToPosition(Vector3 pos)
    {
        var ray = new Ray(pos, Vector3.down);
        if (Physics.Raycast(ray, out var hit, 10))
        {
            var uv = hit.textureCoord; // 中心点
            DrawTextureToRenderTexture(demoTexture, sizeScale, Mathf.FloorToInt(uv.x * trackRt.width), Mathf.FloorToInt((1 - uv.y) * trackRt.height), trackRt);
        }
    }

    void GetLinearPointBetweenTwoPoints(Vector3 startPos, Vector3 endPos, float interval, ref List<Vector3> posList)
    {
        if (interval <= 0)
        {
            Debug.LogError("插值不能小于0");
            return;
        }

        posList.Clear();
        posList.Add(startPos);
        var direc = endPos - startPos;
        var dirSqr = direc.sqrMagnitude;
        var intervalSqr = interval * interval;
        Vector3 posA = startPos;
        while (dirSqr > intervalSqr)
        {
            var temp = GetPosInDirectionByDistance(posA, direc, interval);
            posList.Add(temp);
            dirSqr = (endPos - temp).sqrMagnitude;
            posA = temp;
        }

        posList.Add(endPos);
    }

    Vector3 GetPosInDirectionByDistance(Vector3 pos, Vector3 direc, float interval)
    {
        return pos + direc.normalized * interval;
    }

    void DrawTextureToRenderTexture(Texture2D src, float scale, int centerXOnDst, int centerYOnDst, RenderTexture rt)
    {
        int scaleWidth = Mathf.FloorToInt(src.width * scale);
        int scaleHight = Mathf.FloorToInt(src.height * scale);
        int srcWidth = scaleWidth;
        int srcHeight = scaleHight;

        int dstX = centerXOnDst - scaleWidth / 2;
        if (dstX < 0)
        {
            var cha = Mathf.Abs(dstX);
            dstX = 0;
            srcWidth -= cha;
        }
        else if (dstX >= rt.width)
        {
            return;
        }

        int dstY = centerYOnDst - scaleHight / 2;
        if (dstY < 0)
        {
            var cha = Mathf.Abs(dstY);
            dstY = 0;
            srcHeight -= cha;
        }
        else if (dstY >= rt.height)
        {
            return;
        }

        if (dstX + srcWidth >= rt.width)
        {
            var cha = (dstX + srcWidth) - rt.width;
            srcWidth -= cha;
        }

        if (dstY + srcHeight >= rt.height)
        {
            var cha = (dstY + srcHeight) - rt.height;
            srcHeight -= cha;
        }

        var old = RenderTexture.active;
        RenderTexture.active = rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, rt.width, rt.height, 0);
        Rect srcRect = new Rect(0, 0, srcWidth / (float)scaleWidth, srcHeight / (float)scaleHight);
        Rect screenRect = new Rect(dstX, dstY, srcWidth, srcHeight);
        Graphics.DrawTexture(screenRect, src, srcRect, 0, 0, 0, 0);
        GL.PopMatrix();
        RenderTexture.active = old;
    }
}
