using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// メッシュを作成する
/// </summary>
public class P3DPrimitive : MonoBehaviour
{
    //全種類のメッシュ
    static private List<Mesh> _meshTable = new List<Mesh>();

    //頂点データ構造体
    struct P3DVertex
    {
        public Vector3 Pos;	//位置
        public Vector2 Uv;	//UV
    }


    /// <summary>
    /// 全メッシュを作成
    /// </summary>
    /// <param name="polyDiv">ポリゴン分割数</param>
    public static void Create(int polyDiv)
    {
        CreateBillboard();
        CreateQuad();
        CreateHemisphere(polyDiv);
        CreateCylinder(polyDiv);
        CreateTrail();
    }


    /// <summary>
    /// 任意のメッシュを取得
    /// </summary>
    /// <param name="type">種類</param>
    /// <returns>メッシュ</returns>
    public static Mesh GetMesh(PRIMITIVE_TYPE type)
    {
        return _meshTable[(int)type];
    }


    /// <summary>
    /// ビルボード用メッシュ作成
    /// </summary>
    static void CreateBillboard()
    {
        List<Vector3> vertexPos = new List<Vector3>();
        List<Vector2> vertexUv = new List<Vector2>();
        List<int> indexList = new List<int>();

        vertexPos.Add(new Vector3(-0.5f, 0.5f, 0));
        vertexPos.Add(new Vector3(0.5f, 0.5f, 0));
        vertexPos.Add(new Vector3(-0.5f, -0.5f, 0));
        vertexPos.Add(new Vector3(0.5f, -0.5f, 0));

        vertexUv.Add(new Vector2(0, 0));
        vertexUv.Add(new Vector2(1, 0));
        vertexUv.Add(new Vector2(0, 1));
        vertexUv.Add(new Vector2(1, 1));

        indexList.Add(0);
        indexList.Add(1);
        indexList.Add(2);
        indexList.Add(2);
        indexList.Add(1);
        indexList.Add(3);



        //メッシュ作成
        Mesh mesh;
        mesh = new Mesh();
        mesh.name = "Billboard";
        mesh.vertices = vertexPos.ToArray();
        mesh.triangles = indexList.ToArray();
        mesh.uv = vertexUv.ToArray();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 99999999);
        _meshTable.Add(mesh);
    }

    /// <summary>
    /// 平面メッシュ作成
    /// </summary>
    static void CreateQuad()
    {
        List<Vector3> vertexPos = new List<Vector3>();
        List<Vector2> vertexUv = new List<Vector2>();
        List<int> indexList = new List<int>();

        vertexPos.Add(new Vector3(-0.5f, 0.5f, 0));
        vertexPos.Add(new Vector3(0.5f, 0.5f, 0));
        vertexPos.Add(new Vector3(-0.5f, -0.5f, 0));
        vertexPos.Add(new Vector3(0.5f, -0.5f, 0));

        vertexUv.Add(new Vector2(0, 0));
        vertexUv.Add(new Vector2(1, 0));
        vertexUv.Add(new Vector2(0, 1));
        vertexUv.Add(new Vector2(1, 1));

        indexList.Add(0);
        indexList.Add(1);
        indexList.Add(2);
        indexList.Add(2);
        indexList.Add(1);
        indexList.Add(3);


        //メッシュ作成
        Mesh mesh;
        mesh = new Mesh();
        mesh.name = "Quad";
        mesh.vertices = vertexPos.ToArray();
        mesh.triangles = indexList.ToArray();
        mesh.uv = vertexUv.ToArray();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 99999999);
        _meshTable.Add(mesh);

    }

    /// <summary>
    /// 半球メッシュ作成
    /// </summary>
    static void CreateHemisphere(int splitPolygon)
    {
        List<Vector3> vertexPos = new List<Vector3>();

        //縦１列分
        List<Vector3> vecLine = new List<Vector3>();
        Vector3 right = new Vector3(0.5f, 0, 0);
        for (float a = 0; a < 90.0f; a += 90.0f / splitPolygon)
        {
            Vector3 v = Quaternion.AngleAxis(a, Vector3.up) * right;
            vecLine.Add(v);
        }

        //位置だけ全部入れる
        for (float a = 0; a < 360.0f; a += 360.0f / splitPolygon)
        {
            for (int b = 0; b < vecLine.Count; b++)
            {
                Vector3 pos = Quaternion.AngleAxis(a, Vector3.forward) * vecLine[b];
                vertexPos.Add(pos);
            }
        }

        //UV
        List<Vector2> vertexUv = new List<Vector2>();
        for (int i = 0; i < vertexPos.Count; i++)
        {
            Vector2 uv = new Vector2(vertexPos[i].x + 0.5f, vertexPos[i].y + 0.5f);
            vertexUv.Add(uv);
        }

        //インデックス
        List<int> indexList = new List<int>();
        for (int r = 0; r < splitPolygon; r++)
        {
            for (int i = 0; i < splitPolygon - 1; i++)
            {
                indexList.Add((r * splitPolygon + i) % vertexPos.Count);
                indexList.Add((r * splitPolygon + i + splitPolygon + 1) % vertexPos.Count);
                indexList.Add((r * splitPolygon + i + splitPolygon) % vertexPos.Count);

                indexList.Add((r * splitPolygon + i) % vertexPos.Count);
                indexList.Add((r * splitPolygon + i + 1) % vertexPos.Count);
                indexList.Add((r * splitPolygon + i + splitPolygon + 1) % vertexPos.Count);
            }
        }


        //頂点
        for (int r = 0; r < splitPolygon; r++)
        {
            indexList.Add((r * splitPolygon + (splitPolygon - 1)) % vertexPos.Count);
            indexList.Add(vertexPos.Count);
            indexList.Add(((r + 1) * splitPolygon + (splitPolygon - 1)) % vertexPos.Count);
        }
        Vector3 p = new Vector3(0.0f, 0.0f, -0.5f);
        Vector2 u = new Vector2(0.5f, 0.5f);
        vertexPos.Add(p);
        vertexUv.Add(u);


        //メッシュ作成
        Mesh mesh;
        mesh = new Mesh();
        mesh.name = "Hemisphere";
        mesh.vertices = vertexPos.ToArray();
        mesh.triangles = indexList.ToArray();
        mesh.uv = vertexUv.ToArray();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 99999999);
        _meshTable.Add(mesh);

    }


    /// <summary>
    /// 円柱メッシュ作成
    /// </summary>
    /// <param name="splitPolygon"></param>
    static void CreateCylinder(int splitPolygon)
    {
        List<Vector3> vertexPos = new List<Vector3>();
        List<Vector2> vertexUv = new List<Vector2>();
        List<int> indexList = new List<int>();

        //頂点位置
        for (float a = 0; a <= 360.0f; a += 360.0f / splitPolygon)
        {
            vertexPos.Add(Quaternion.AngleAxis(a, Vector3.forward) * new Vector3(0.5f, 0, 0));
            vertexUv.Add(new Vector2(1.0f - a / 360.0f, 0.0f));

            vertexPos.Add(Quaternion.AngleAxis(a, Vector3.forward) * new Vector3(0.5f, 0, -1.0f));
            vertexUv.Add(new Vector2(1.0f - a / 360.0f, 1.0f));
        }

        //インデックス
        for (int r = 0; r < splitPolygon; r++)
        {
            indexList.Add((r * 2 + 0) % vertexPos.Count);
            indexList.Add((r * 2 + 1) % vertexPos.Count);
            indexList.Add((r * 2 + 2) % vertexPos.Count);

            indexList.Add((r * 2 + 1) % vertexPos.Count);
            indexList.Add((r * 2 + 3) % vertexPos.Count);
            indexList.Add((r * 2 + 2) % vertexPos.Count);
        }


        //メッシュ作成
        Mesh mesh;
        mesh = new Mesh();
        mesh.name = "Cylinder";
        mesh.vertices = vertexPos.ToArray();
        mesh.triangles = indexList.ToArray();
        mesh.uv = vertexUv.ToArray();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 99999999);
        _meshTable.Add(mesh);

    }

    /// <summary>
    /// 軌跡はUnityの機能を使うのでメッシュはいらない。
    /// </summary>
    static void CreateTrail()
    {
        Mesh mesh;
        mesh = new Mesh();
        _meshTable.Add(mesh);
    }


    /// <summary>
    /// メッシュデータをロードしてリストに追加
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <returns>リスト上のインデックス</returns>
    public static int LoadMesh(string fileName)
    {
        Mesh mesh = new Mesh();
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/Resources/" + fileName);

        _meshTable.Add(mesh);
        return _meshTable.Count - 1;
    }





}
