using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;


/// <summary>
/// ロードしたすべてのデータと、表示中のすべてのエフェクトを管理するクラス
/// </summary>
public class P3DEngine : MonoBehaviour 
{
    //インスペクタで設定できる項目
    public string _fileDirectory = "Assets/Resources";         //エフェクトファイルを保存した場所
    public string[] _effectFile;            //ロードしたいファイル名リスト
    public int _FPS = 60;                   //FPS
    public int _polygonDivision = 16;       //円柱や半球の滑らかさ
    public GameObject _effectPrefab;        //エフェクトオブジェクトのプレハブ



    List<P3DNode> _nodeList = new List<P3DNode>();          //ロードしたファイルの配列（各エフェクトの情報はノード情報がツリー構造になっており、ここで管理するのはそのルートの情報）
    List<GameObject> _effectList = new List<GameObject>();  //再生中のエフェクトのリスト


    /// <summary>
    /// ゲーム起動時に呼ばれ、指定したファイルをロードしていく
    /// </summary>
	void Start () 
    {
        //クアッドや円柱、半球などのメッシュを作成
        P3DPrimitive.Create(_polygonDivision);


        //エフェクトファイルをロード
        for(int i = 0; i < _effectFile.Length; i++)
        {
            string filePath = _fileDirectory + "\\" + _effectFile[i] + ".p3b";

            FileInfo fileHandle = new FileInfo(filePath);           //ファイルハンドルの生成
            var readStream = fileHandle.OpenRead();                 //ファイルハンドルから読み込み用ファイルストリームを生成
            byte[] resut = new byte[fileHandle.Length];             //読み込んだバイナリデータ保持する変数
            readStream.Read(resut, 0, (int)(fileHandle.Length));    //ファイルストリームからバイナリデータ取得する
            readStream.Close();                                     //ファイルストリームを解放する

            //ルートノード作成
            P3DNode rootNode;
            rootNode = new P3DNode(null);
            rootNode.IsDraw = false;        //ルートは非表示


            //ここからバイナリデータを読み込んでいく。
            //ほとんどのデータは4バイトずつのデータになっている。

            //Prominence3Dのバイナリデータであることを確認
            if (resut[0] == 'P' && resut[1] == '3' && resut[2] == 'D' && resut[3] == 'B')   
            {
                //続きは4バイト目から読み込む
                int index = 4;

                //ファイルのバージョン
                int fileVersion = BitConverter.ToInt32(resut, index);
                index += 4;

                if (fileVersion > 1)
                {
                    print("※" + _effectFile[i] + "は新しいバージョンのProminence3Dで作られたファイルです。\nランタイムを最新版にしてください。");
                }


                //ルートの寿命（＝そのエフェクトのフレーム数）
                rootNode.SetKillSpan(BitConverter.ToInt32(resut, index));
                index += 4;

                //ノード数
                int nodeNum = BitConverter.ToInt32(resut, index);
                index += 4;

                //全てのノードをロード
                for (int j = 0; j < nodeNum; j++)
                {
                    P3DNode node = new P3DNode(rootNode);
                    index = node.Load(resut, index);
                    rootNode.AddChild(node);
                }

                //リストに追加
                _nodeList.Add(rootNode);
            }
        }

        //　1／60秒ごとに更新処理を呼ぶ
        InvokeRepeating("UpdateProcess", 0, 1.0f / _FPS);
	}


    /// <summary>
    /// 更新処理（60FPS）
    /// </summary>
    void UpdateProcess()
    {
        //再生中の全エフェクトを更新
        for (int i = 0; i < _effectList.Count; i++)
        {
            //i番目のエフェクトを更新（戻り値は再生が終わったかどうか）
            if (_effectList[i].GetComponent<P3DEffect>().UpdateProcess())
            {
                //終了したので削除
                Destroy(_effectList[i]);
                _effectList.RemoveAt(i);
                continue;
            }
        }
    }




    /// <summary>
    /// エフェクトを再生する
    /// </summary>
    /// <param name="id">番号</param>
    /// <param name="position">出現位置</param>
    /// <param name="isLoop">ループフラグ</param>
    /// <returns>作成したエフェクト（パーティクルはこれの子オブジェクトになります）</returns>
    public GameObject Play(int id, Vector3 position, bool isLoop = false)
    {
        if (id < 0 || id >= _nodeList.Count)
            return null;

        //エフェクトを作成
        GameObject effect = (GameObject)Instantiate(_effectPrefab, position, new Quaternion(0, 0, 0, 0));
        effect.GetComponent<P3DEffect>().Create(_nodeList[id], isLoop);

        //リストに追加
        _effectList.Add(effect);

        return effect;
    }
}
