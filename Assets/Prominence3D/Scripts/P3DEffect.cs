using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// 表示中のエフェクトを管理するクラス
/// </summary>
public class P3DEffect : MonoBehaviour
{
    public GameObject _particlePrefab;  //パーティクルのプレハブ

    List<GameObject> _particleList = new List<GameObject>();    //パーティクルのリスト
    P3DNode _rootNode;      //ルートノード
    int _nowFrame = 0;      //現在のフレーム
    bool _isLoop = false;   //ループするかどうか
    int _FPS = 60;          //FPS（P3DEngineのインスペクタで設定した値）



    /// <summary>
    /// インスタンス作成直後に呼ばれる
    /// </summary>
    /// <param name="rootNode">ルートノード情報</param>
    /// <param name="isLoop">ループするかどうか</param>
    public void Create(P3DNode rootNode, bool isLoop)
    {
        _rootNode = rootNode;
        _isLoop = isLoop;
        _nowFrame = 0;

        //ルートパーティクル作成
        GameObject rootParticle = (GameObject)Instantiate(_particlePrefab, this.transform.position, new Quaternion(0, 0, 0, 0));
        rootParticle.GetComponent<P3DParticle>().Create(rootNode, null);

        //リストに追加
        _particleList.Add(rootParticle);

        //親子にする
        rootParticle.transform.parent = this.gameObject.transform;

        //ルートは非表示
        rootParticle.SetActive(false);

        //インスペクタで入力されたFPS（軌跡の長さを求めるのに使用）
        _FPS = GameObject.Find("P3DEngine").GetComponent<P3DEngine>()._FPS;
    }


    /// <summary>
    /// 自エフェクトの全パーティクルを更新
    /// </summary>
    public bool UpdateProcess()
    {
        //foreachにすると途中で数が変化するのでエラーになる
        for (int i = 0; i < _particleList.Count; i++ )
        {
            //パーティクルを更新　戻り値は寿命が来たかどうか
            bool destroy = _particleList[i].GetComponent<P3DParticle>().UpdateProcess(this.gameObject);

            //寿命が来てて、0番じゃない場合（0番はルートパーティクルなので消しちゃだめ）
            if(destroy && i > 0)
            {
                //消滅時に発生する子
                foreach (P3DNode node in _particleList[i].GetComponent<P3DParticle>().Node.Child)
                {
                    AddParticleKill(node, _particleList[i]);
                }

                //子に親の消滅を知らせる（これをしないと親にアクセスしようとして落ちる）
                foreach(GameObject particle in _particleList)
                {
                    //パーティクルの親が自分だったら
                    if(particle.GetComponent<P3DParticle>().Parent == particle)
                    {
                        //親をNULLにする
                        particle.GetComponent<P3DParticle>().Parent = null;
                    }
                }

                //消去
                Destroy(_particleList[i].gameObject);
                _particleList.RemoveAt(i);
                i--;
            }
        }

        //エフェクト終了
        if (_particleList.Count == 0 || _nowFrame >= _particleList[0].GetComponent<P3DParticle>().Node._kill.span)
        {
            if (_isLoop)
            {
                _nowFrame = 0;
                _particleList[0].GetComponent<P3DParticle>().Life = 0;
            }
            else
                return true;
        }

        _nowFrame++;

        return false;
    }


    /// <summary>
    /// パーティクルを追加する
    /// </summary>
    /// <param name="node">今から追加するパーティクルの情報</param>
    /// <param name="parentPartcle">親となるパーティクルオブジェクト</param>
    public void AddParticle(P3DNode node, GameObject parentPartcle)
    {
        //期間が“親の消滅時”の場合は別
        if (node._timing.often == 0) return;

        P3DParticle parentParticleClass = parentPartcle.GetComponent<P3DParticle>();

        //絶対フレームか親のフレームか
        int frame;
        switch (node._timing.type)
        {
            //絶対フレーム基準
            case TIMING_TYPE.ABSOLUTE:
                frame = _nowFrame;
                break;

            //親パーティクルのフレーム基準
            case TIMING_TYPE.PARENT:
                frame = parentParticleClass.Life;
                break;

            default:
                return;
        }

        //発生する期間内だった場合
        if (frame >= node._timing.start && frame <= node._timing.end)
        {
            float ratio = Ratio(node._timing.often_err);
            int often = (int)(node._timing.often * ratio);

            //頻度
            if (often == 0 || (frame - node._timing.start) % often == 0)
            {
                float r = Ratio(node._timing.num_err);
                int num = (int)(node._timing.num * ratio);

                parentParticleClass.CreateNum = num;
                for (int i = 0; i < num; i++)
                {
                    parentParticleClass.ChildNum = i;


                    //追加
                    GameObject newParticle = (GameObject)Instantiate(_particlePrefab, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
                    newParticle.GetComponent<P3DParticle>().Create(node, parentPartcle);

                    //軌跡の場合
                    if (node._shape.type == PRIMITIVE_TYPE.TRAIL)
                    {
                        newParticle.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;              //本体は表示しない
                        newParticle.GetComponent<TrailRenderer>().enabled = true;                                    //トレイルを使う
                        newParticle.GetComponent<TrailRenderer>().GetComponent<Renderer>().material.SetTexture("_MainTex", node._texture.texture);  //トレイルにテクスチャをセット
                        newParticle.GetComponent<TrailRenderer>().time = (float)node._scale.trailLength / _FPS;     //長さ
                    }

                    //その他形状の場合
                    else
                    {
                        newParticle.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh = P3DPrimitive.GetMesh(node._shape.type);   //メッシュをセット
                        newParticle.transform.GetChild(0).GetComponent<Renderer>().material.SetTexture("_MainTex", node._texture.texture);  //テクスチャをセット
                    }

                    
                    //リストに追加
                    _particleList.Add(newParticle);

                    //エフェクトオブジェクトの子オブジェクトにする
                    newParticle.transform.parent = this.gameObject.transform;
                }
            }
        }
    }

    /// <summary>
    /// 親の消滅時に子を発生
    /// </summary>
    /// <param name="node">今から発生するパーティクルの情報</param>
    /// <param name="parentPartcle">今、消滅しようとしているパーティクル</param>
    void AddParticleKill(P3DNode node, GameObject parentPartcle)
    {
        P3DParticle parentParticleClass = parentPartcle.GetComponent<P3DParticle>();

        if (node._timing.type == TIMING_TYPE.DESTROY)
        {
            float ratio = Ratio(node._timing.num_err);
            int num = (int)(node._timing.num * ratio);
            parentParticleClass.CreateNum = num;
            for (int i = 0; i < num; i++)
            {
                parentParticleClass.ChildNum = i;

                //追加
                GameObject newParticle = (GameObject)Instantiate(_particlePrefab, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
                newParticle.GetComponent<P3DParticle>().Create(node, parentPartcle);

                //軌跡の場合
                if (node._shape.type == PRIMITIVE_TYPE.TRAIL)
                {
                    newParticle.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;              //本体は表示しない
                    newParticle.GetComponent<TrailRenderer>().enabled = true;                                    //トレイルを使う
                    newParticle.GetComponent<TrailRenderer>().GetComponent<Renderer>().material.SetTexture("_MainTex", node._texture.texture);  //トレイルにテクスチャをセット
                    newParticle.GetComponent<TrailRenderer>().time = (float)node._scale.trailLength / _FPS;     //長さ
                }

                //その他形状の場合
                else
                {
                    newParticle.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh = P3DPrimitive.GetMesh(node._shape.type);   //メッシュをセット
                    newParticle.transform.GetChild(0).GetComponent<Renderer>().material.SetTexture("_MainTex", node._texture.texture);  //テクスチャをセット
                }



                _particleList.Add(newParticle);
                newParticle.transform.parent = this.gameObject.transform;
            }
        }
    }


    /// <summary>
    /// 引数が0のときは100％、1の時は0～200％の乱数を返す
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    float Ratio(int i)
    {
        return (i == 0) ? ((float)1.0f) : ((float)(Random.Range(0, i * 2) + (100 - i)) / 100.0f);
    }
}
