using UnityEngine;
using System.Collections;

/// <summary>
/// ひとつのパーティクルを管理するクラス
/// </summary>
public class P3DParticle : MonoBehaviour
{
    public P3DNode _node = null;          //自分のノード
    public GameObject _parent = null;     //親パーティクルオブジェクト
    int _life = 0;              //発生してからのフレーム数

    Vector3 _prevPos = Vector3.zero;       //前回の位置
    Vector3 _move = Vector3.zero;          //移動方向ベクトル
    Vector3 _scale;
    int _lifeFrame = 0;         //寿命
    int _killFrame = 0;         //消去フレーム
    float _rotate_angle = 0.0f;	//自転角度（度）
    Vector3 _rotate_axis = Vector3.forward;	//自転軸
    int _roteta_dir = 0;		//自転向き
    float _revolve_angle = 0.0f;	//公転角度（度）
    Vector3 _revolve_axis = Vector3.forward;	//公転軸
    int _revolve_dir = 0;		//自転向き
    float _speedRnd = 0.0f;		//速度のランダム値
    float _scaleRnd = 0.0f;		//サイズのランダム値
    float _rotateRnd = 0.0f;	    //自転ランダム値
    float _revolveRnd = 0.0f;	    //公転ランダム値
    float _colorRnd = 0.0f;		//明るさ誤差ランダム値
    float _alphaRnd = 0.0f;		//透明度ランダム値
    Vector3 _tracks = Vector3.zero;		//実際に移動した軌跡（子が追従するのに使う）
    Vector3 _gravity = Vector3.zero;		//重力
    Vector3 _absorb = Vector3.zero;		//吸引位置
    Material _material = null;     //マテリアル
    Quaternion _randomDirection = new Quaternion(0, 0, 0, 1); //向きランダムの場合のクォータニオン
    Quaternion _rotateMatrix;//自転
    float _textureAnimFrame = 0;		//テクスチャアニメーションの現在のコマ

    int _createNum = 0; 	//子パーティクルを何個発生するか（その瞬間だけ使用）
    int _childNum = 0;		//今発生させた子の数（その瞬間だけ使用）



    //各プロパティ
    public int CreateNum
    {
        set { _createNum = value; }
    }
    public int ChildNum
    {
        get { return _childNum; }
        set { _childNum = value; }
    }

    public GameObject Parent
    {
        get { return _parent; }
        set { _parent = value; }
    }
    public int Life
    {
        get { return _life; }
        set { _life = value; }
    }


    public P3DNode Node
    {
        get { return _node; }
        set { _node = value; }
    }

    /////////////////////////////////////////////////////////////////////////////// ここから初期化処理 /////////////////////////////////////////////////////////////////////////////// 
    /// <summary>
    /// インスタンス作成時によばれる
    /// </summary>
    /// <param name="node">情報が入っているノード</param>
    /// <param name="parent">親パーティクル</param>
    public void Create(P3DNode node, GameObject parent)
    {
        _node = node;
        _parent = parent;

        this.transform.position = CreatePosition(); //発生位置
        _move = CreateMoveVector();                 //移動方向
        _randomDirection = CreateDirMatrix();       //向きがランダムの場合に使うクォータニオン

        //寿命
        float ratio = Ratio(node._kill.span_err);
        _lifeFrame = (int)(node._kill.span * ratio);
        ratio = Ratio(node._kill.frame_err);
        _killFrame = (int)(_node._kill.frame * ratio);

        //速度ランダム
        _speedRnd = Ratio(node._move.speed_err);

        //サイズランダム
        _scaleRnd = Ratio(node._scale.max_err);

        //回転ランダム値
        _rotateRnd = Ratio(_node._rotate.angle_err);
        _revolveRnd = Ratio(_node._rotate.revAngle_err);

        //明るさランダム値
        _colorRnd = Ratio(_node._color.brigRnd);

        //透明度ランダム値
        _alphaRnd = Ratio(_node._color.alphaRnd);

        //自転軸
        switch (_node._rotate.axis)
        {
            case ROTATE_AXIS.X:
                _rotate_axis = Vector3.right;
                break;
            case ROTATE_AXIS.Y:
                _rotate_axis = Vector3.up;
                break;
            case ROTATE_AXIS.Z:
                _rotate_axis = Vector3.forward;
                break;
        }

        //自転向き
        switch (_node._rotate.dir)
        {
            case ROTATE_DIRECTION.FORWARD:
                _roteta_dir = -1;
                break;
            case ROTATE_DIRECTION.REVERSE:
                _roteta_dir = 1;
                break;
            case ROTATE_DIRECTION.RANDOM:
                if (Random.Range(0, 100) % 2 == 0)
                    _roteta_dir = 1;
                else
                    _roteta_dir = -1;
                break;
        }

        //公転軸
        switch (_node._rotate.revAxis)
        {
            case ROTATE_AXIS.X:
                _revolve_axis = Vector3.right;
                break;
            case ROTATE_AXIS.Y:
                _revolve_axis = Vector3.up;
                break;
            case ROTATE_AXIS.Z:
                _revolve_axis = Vector3.forward;
                break;
            default:
                _revolve_axis = _parent.GetComponent<P3DParticle>()._tracks;
                break;
        }

        //公転向き
        switch (_node._rotate.revDir)
        {
            case ROTATE_DIRECTION.FORWARD:
                _revolve_dir = -1;
                break;
            case ROTATE_DIRECTION.REVERSE:
                _revolve_dir = 1;
                break;
            case ROTATE_DIRECTION.RANDOM:
                if (Random.Range(0, 100) % 2 == 0)
                    _revolve_dir = 1;
                else
                    _revolve_dir = -1;
                break;
        }


        //マテリアルへのアクセス用
        _material = this.transform.GetChild(0).GetComponent<Renderer>().material;


        //シェーダー切り替え
        if (_node._shape.type == PRIMITIVE_TYPE.CYLINDER)    //円柱の場合
        {
            //加算合成かどうか
            if (_node._texture.isAdd)
                _material.shader = Shader.Find("Prominence3D/CylinderAdd");
            else
                _material.shader = Shader.Find("Prominence3D/CylinderStanderd");
        }
        else //円柱以外
        {
            //加算合成かどうか
            if (_node._texture.isAdd)
                _material.shader = Shader.Find("Prominence3D/Add");
            else
                _material.shader = Shader.Find("Prominence3D/Standerd");
        }

        //非表示の場合
        if (!_node.IsDraw)
        {
            this.transform.gameObject.SetActive(false);
        }

    }


    /// <summary>
    /// パーティクルの発生位置を算出
    /// </summary>
    /// <returns>発生位置</returns>
    Vector3 CreatePosition()
    {
        if (_node.Parent == null)
            return Vector3.zero;

        Vector3 position = Vector3.zero;


        //エミッタの形状
        switch (_node._emitter.type)
        {
            //直方体
            case EMITTER_TYPE.BOX:
                position.x = Random.Range(-_node._emitter.size.x / 2, _node._emitter.size.x / 2);
                position.y = Random.Range(-_node._emitter.size.y / 2, _node._emitter.size.y / 2);
                position.z = Random.Range(-_node._emitter.size.z / 2, _node._emitter.size.z / 2);

                //外側へ
                if (_node._emitter.scope != EMITTER_SCOPE.IN)
                {
                    switch (Random.Range(0, 3))
                    {
                        case 0: position.x = _node._emitter.size.x / 2; break;
                        case 1: position.y = _node._emitter.size.y / 2; break;
                        case 2: position.z = _node._emitter.size.z / 2; break;
                    }
                    if (Random.Range(0, 2) > 0) position *= -1;
                }
                break;

            //球・半球
            case EMITTER_TYPE.SPHERE:
            case EMITTER_TYPE.HEMISPHERE:
                {
                    //デフォルトで長さ0.5の横向きベクトルを作る
                    //内側に配置する場合、中央に密集しないよう2乗して外側へ寄せている
                    float r = (_node._emitter.scope != EMITTER_SCOPE.IN) ? 0.5f : (1.0f - Mathf.Pow(Random.Range(0.0f, 1.0f), 4)) * 0.5f;
                    Vector3 v = new Vector3(r, 0, 0);

                    //ランダムな方向に回転
                    v = Quaternion.EulerAngles(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)) * v;

                    v.Scale(_node._emitter.size);

                    position = v;

                    //半球の場合は、下向きだったらYを反転
                    if (_node._emitter.type == EMITTER_TYPE.HEMISPHERE && position.y < 0)
                        position.y *= -1;
                }
                break;

            //円柱・円（水平）
            case EMITTER_TYPE.HCIRCLE:
            case EMITTER_TYPE.CYLINDER:
                {
                    //デフォルトで長さ0.5の横向きベクトルを作る
                    //内側に配置する場合、中央に密集しないよう2乗して外側へ寄せている
                    float r = (_node._emitter.scope != EMITTER_SCOPE.IN) ? 0.5f : (1.0f - Mathf.Pow(Random.Range(0.0f, 1.0f), 2)) * 0.5f;
                    Vector3 v = new Vector3(r, 0, 0);

                    //円柱だったら高さもランダム
                    if (_node._emitter.type == EMITTER_TYPE.CYLINDER)
                    {
                        v.y = Random.Range(-_node._emitter.size.y / 2, _node._emitter.size.y / 2);
                    }



                    //等間隔
                    if (_node._emitter.isEvenInterval)
                    {
                        v = Quaternion.AngleAxis(360.0f / _parent.GetComponent<P3DParticle>()._createNum * _parent.GetComponent<P3DParticle>()._childNum, Vector3.up) * v;
                    }

                    //等間隔じゃない
                    else
                    {
                        v = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.up) * v;
                    }

                    //長さを発生源のサイズに合わせる
                    v.Scale(new Vector3(_node._emitter.size.x, 1, _node._emitter.size.z));


                    position = v;
                }
                break;

            //円（垂直）
            case EMITTER_TYPE.VCIRCLE:
                {
                    float r = (_node._emitter.scope != EMITTER_SCOPE.IN) ? 0.5f : (1.0f - Mathf.Pow(Random.Range(0.0f, 1.0f), 2)) * 0.5f;
                    Vector3 v = new Vector3(r, 0, 0);

                    if (_node._emitter.isEvenInterval)
                        v = Quaternion.AngleAxis(360.0f / _parent.GetComponent<P3DParticle>()._createNum * _parent.GetComponent<P3DParticle>()._childNum, Vector3.forward) * v;
                    else
                        v = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.forward) * v;

                    v.Scale(new Vector3(_node._emitter.size.x, _node._emitter.size.y, _node._emitter.size.z));

                    position = v;
                }
                break;
        }




        //エミッタ表面
        float y = position.y;
        if (_node._emitter.scope == EMITTER_SCOPE.OTHER)
        {
            float r = 1.0f - Random.Range(0.0f, (float)_node._emitter.err) / 100.0f;
            position *= r;
        }
        if (_node._emitter.type == EMITTER_TYPE.CYLINDER)
            position.y = y;


        //親に合わせて発生位置拡大
        if (!_node._emitter.scaleFix && _parent)
        {


            Vector3 s =_parent.GetComponent<P3DParticle>().GetScale();

            position.x *= s.x;
            position.y *= s.y;
            position.z *= s.z;
        }



        //親に合わせて発生位置回転
        if (!_node._emitter.fix && _parent)
        {
            //発生源が円柱のときの移動方向
            {
                _move = position;
                _move.y = 0;

                _move = _parent.GetComponent<P3DParticle>()._rotateMatrix * _move;
            }

            position = _parent.GetComponent<P3DParticle>()._rotateMatrix * position;
        }



        position += _node._emitter.pos;

        //発生位置を親パーティクルに合わせる
        if (_parent != null)
        {
            position += _parent.transform.position;
        }

        return position;
    }


    Vector3 GetScale()
    {
        Vector3 s;
        s.x = _node.GetGraph(GRAPH.SIZE_X).GetValue(_life, _lifeFrame)
            * ((float)_node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame));
        s.y = _node.GetGraph(GRAPH.SIZE_Y).GetValue(_life, _lifeFrame)
            * ((float)_node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame));
        s.z = _node.GetGraph(GRAPH.SIZE_Z).GetValue(_life, _lifeFrame)
            * ((float)_node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame));

        return s;
    }


    /// <summary>
    /// パーティクルの初期移動方向を算出
    /// </summary>
    /// <returns>移動方向ベクトル</returns>
    Vector3 CreateMoveVector()
    {
        Vector3 move = Vector3.zero;

        if (_node._move.isOneWay)	//一方向
        {
            move = Vector3.up;
            move = Quaternion.Euler(_node._move.dirX, _node._move.dirY, 0) * move;

            //誤差
            if (_node._move.oneway_err > 0)
            {
                Vector3 randVec = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                Vector3 axis = Vector3.Cross(move, randVec);
                move = Quaternion.AngleAxis(Random.Range(0.0f, (float)_node._move.oneway_err) / 2.0f, axis) * move;
            }
        }
        else
        {
            if (_parent)
            {
                if (_node._emitter.type != EMITTER_TYPE.CYLINDER)
                    move = this.transform.position - _parent.transform.position - _node._emitter.pos;

            }
            else
            {
                move = this.transform.position - _node._emitter.pos;
                if (_node._emitter.type == EMITTER_TYPE.CYLINDER)
                {
                    move.y = 0.0f;
                }

            }


            if (move.magnitude <= 0)
            {
                move = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                if (_node._emitter.type == EMITTER_TYPE.HCIRCLE) move.y = 0;
                if (_node._emitter.type == EMITTER_TYPE.VCIRCLE) move.z = 0;
            }
        }

        return move.normalized;
    }



    /// <summary>
    /// 向きがランダムな場合の回転を作る
    /// </summary>
    /// <returns></returns>
    Quaternion CreateDirMatrix()
    {
        Quaternion m = new Quaternion(0, 0, 0, 1);

        //向きがランダムの場合
        if (_node._shape.dir == PRIMITIVE_DIRECTION.RANDOM)
        {
            Vector3 v = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f));

            //2Dの場合はZのみ回転
            if (_node._shape.type == PRIMITIVE_TYPE.BILLBOARD)
            {
                v.x = 0.0f;
                v.y = 0.0f;
            }
            m = Quaternion.AngleAxis(Random.Range(0, 360), v);

        }
        return m;
    }



    /////////////////////////////////////////////////////////////////////////////// ここから更新処理 /////////////////////////////////////////////////////////////////////////////// 
    /// <summary>
    /// パーティクルの更新処理
    /// </summary>
    /// <param name="effect">自分を管理するEffectオブジェクト（親）</param>
    /// <returns>寿命が来たかどうか</returns>
    public bool UpdateProcess(GameObject effect)
    {
        //移動前の位置を記憶
        _prevPos = this.transform.position;

        //親がいない＝ルートパーティクル以外を移動
        if (_node.Parent != null)
        {
            //ビルボード
            if (_node._shape.type == PRIMITIVE_TYPE.BILLBOARD)
            {
                this.transform.rotation = Camera.main.transform.rotation;
            }

            UpdateMove();       //移動
            UpdateScale();      //スケール
            UpdateColor();      //カラー
            UpdateRotate();     //自転
            UpdateRevolution(); //公転
            UpdateDirection();  //進行方向を向く
            TextureAnimation();





        }


        //子発生
        foreach (P3DNode childNode in _node.Child)
        {
            effect.GetComponent<P3DEffect>().AddParticle(childNode, this.gameObject);
        }

        //実際に移動したベクトル
        _tracks = this.transform.position - _prevPos;

        //ライフをカウント
        _life++;

        //寿命がきたかどうか
        return _life >= _lifeFrame;
    }

    private void TextureAnimation()
    {
        //テクスチャアニメーション
        if (_node._texture.isAnim)
        {
            float w = 1.0f / _node._texture.w;
            float h = 1.0f / _node._texture.h;
            int x = (int)_textureAnimFrame % _node._texture.w;
            int y = (int)_textureAnimFrame / _node._texture.w;

            Matrix4x4 mScale, mTrans, mTex;
            mScale = Matrix4x4.Scale(new Vector3(w, h, 1.0f));
            mTrans = Matrix4x4.identity;
            mTrans[0, 3] = w * (float)x;
            mTrans[1, 3] = h * (float)y;
            mTex = mTrans * mScale;
            _material.SetMatrix("_matTexture", mTex);

            Debug.Log("X:" + x + " y:" + y);




            //カウントアップ
            _textureAnimFrame = ((float)_node._texture.speed / 100) * (_life - 1);

    

            //終了まで来た
            if (_textureAnimFrame >= _node._texture.w * _node._texture.h)
            {
                //ループ
                if (_node._texture.isLoop)
                {
                    int amari = (int)_textureAnimFrame % (_node._texture.w * _node._texture.h);
                    float syosu = _textureAnimFrame - (int)_textureAnimFrame;
                    _textureAnimFrame = syosu + amari;
                }


                //止める
                else
                    _textureAnimFrame = (float)(_node._texture.w * _node._texture.h - 1);
            }

        }

        else
        {
            _material.SetMatrix("_matTexture", Matrix4x4.identity);
        }
    }

 

    /// <summary>
    /// 位置の更新
    /// </summary>
    void UpdateMove()
    {
        //移動
        Vector3 m;
        m = _move.normalized;
        m *= ((float)_node._move.speed * _speedRnd * _node.GetGraph(GRAPH.SPEED).GetValue(_life, _lifeFrame) / 50.0f);
        this.transform.position += m;


        //重力
        this.transform.position += _gravity;
        _gravity += new Vector3(0, (float)_node._move.gravity * -0.0001f, 0);


        //吸引
        this.transform.position += _absorb;
        Vector3 ab = _node._move.absorbPos / 10.0f - this.transform.position;
        ab = ab.normalized;
        ab *= 0.0001f * (float)_node._move.absorbPawer;
        _absorb += ab;



        //親に追従
        if (_node._move.isFollow)
        {
            this.transform.position += _parent.GetComponent<P3DParticle>()._tracks;
        }


        //ぶれ
        Vector3 shake = new Vector3(Random.Range(0.0f, (float)_node._move.shake / 100.0f), 0, 0);
        shake *= _node.GetGraph(GRAPH.SHAKE).GetValue(_life, _lifeFrame);
        shake = Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)) * shake;
        this.transform.GetChild(0).position = this.transform.position + shake;


    }


    /// <summary>
    /// スケールの更新
    /// </summary>
    void UpdateScale()
    {
        //Vector3 scale;
        _scale.x = ((float)_node._scale.max * _node.GetGraph(GRAPH.SIZE_X).GetValue(_life, _lifeFrame) / 10.0f)
            * ((float)_node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame));
        _scale.y = ((float)_node._scale.max * _node.GetGraph(GRAPH.SIZE_Y).GetValue(_life, _lifeFrame) / 10.0f)
            * ((float)_node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame));
        _scale.z = ((float)_node._scale.max * _node.GetGraph(GRAPH.SIZE_Z).GetValue(_life, _lifeFrame) / 10.0f)
            * ((float)_node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame));
        _scale *= _scaleRnd;

        //円柱の場合
        if (_node._shape.type == PRIMITIVE_TYPE.CYLINDER)
        {
            //先端と根元を別々に拡大
            Matrix4x4 mBottom, mTop;
            mBottom = Matrix4x4.Scale(new Vector3(_scale.x, _scale.x, 1));
            mTop = Matrix4x4.Scale(new Vector3(_scale.y, _scale.y, _scale.z));
            _material.SetMatrix("_matTop", mTop);
            _material.SetMatrix("_matBottom", mBottom);

        }

        //通常
        else
        {
            this.transform.GetChild(0).transform.localScale = _scale;
        }

        //トレイル（軌跡）にもセット
        this.GetComponent<TrailRenderer>().startWidth = ((float)_node._scale.max * _node.GetGraph(GRAPH.SIZE_XYZ).GetValue(_life, _lifeFrame) / 10.0f);
        this.GetComponent<TrailRenderer>().endWidth = this.GetComponent<TrailRenderer>().startWidth;


    }


    /// <summary>
    /// カラーの更新
    /// </summary>
    void UpdateColor()
    {
        UnityEngine.Color color;
        color.r = (_node.GetGraph(GRAPH.R).GetValue(_life, _lifeFrame)) * _colorRnd;
        if (color.r > 255) color.r = 255;

        color.g = (_node.GetGraph(GRAPH.G).GetValue(_life, _lifeFrame)) * _colorRnd;
        if (color.g > 255) color.g = 255;

        color.b = (_node.GetGraph(GRAPH.B).GetValue(_life, _lifeFrame)) * _colorRnd;
        if (color.b > 255) color.b = 255;

        color.a = (_node.GetGraph(GRAPH.A).GetValue(_life, _lifeFrame)) * _alphaRnd;
        if (color.a > 255) color.a = 255;

        //アルファ親の影響
        if (_node._color.isAlphaParent)
        {
            if (_parent)
                color.a *= _parent.GetComponent<P3DParticle>().GetAlpha();
            else
                color.a = 0;
        }

        //色をセット
        _material.SetColor("_TintColor", color);

        //トレイル（軌跡）にもセット
        this.GetComponent<TrailRenderer>().GetComponent<Renderer>().material.SetColor("_TintColor", color);
    }


    /// <summary>
    /// 自転の更新
    /// </summary>
    void UpdateRotate()
    {
        //自転軸（移動方向で変わる場合）
        switch (_node._rotate.axis)
        {
            case ROTATE_AXIS.DIRECTION_X:
                _rotate_axis = Vector3.Cross(_tracks, Vector3.up);
                if (_rotate_axis.magnitude < 0.01)
                    _rotate_axis = Vector3.right;
                break;

            case ROTATE_AXIS.DIRECTION_Y:
                _rotate_axis = Vector3.Cross(_tracks, Vector3.right);
                if (_rotate_axis.magnitude < 0.01)
                    _rotate_axis = Vector3.up;
                break;
            case ROTATE_AXIS.DIRECTION_Z:
                _rotate_axis = (this.transform.position - _prevPos);
                break;

            case ROTATE_AXIS.PARENT_DIR_AXIS:
                _rotate_axis = Vector3.back;
                if (_parent)
                {
                    _rotate_axis = _parent.GetComponent<P3DParticle>()._node._particleDirection * _rotate_axis;
                    _rotate_axis = _parent.GetComponent<P3DParticle>()._rotateMatrix * _rotate_axis;
                }
                break;

            case ROTATE_AXIS.PARENT_MOVE_AXIS:
                if (_parent)
                {
                    _rotate_axis = _parent.GetComponent<P3DParticle>()._tracks;
                }
                break;
        }
        _rotate_axis.Normalize();

        //回転角度
        float angle = ((float)_node._rotate.angle * _rotateRnd * _roteta_dir * _node.GetGraph(GRAPH.ROTATE).GetValue(_life, _lifeFrame));

        //自転
        _rotateMatrix = Quaternion.AngleAxis(angle, _rotate_axis);
        this.transform.GetChild(0).localRotation = _rotateMatrix * _node._particleDirection * _randomDirection;
    }


    /// <summary>
    /// 公転の更新
    /// </summary>
    void UpdateRevolution()
    {
        //公転軸
        if (_node._rotate.revAxis == (ROTATE_AXIS)3)	//親の向きを軸にする場合
        {
            if (_parent)
            {
                _revolve_axis = Vector3.back;
                _revolve_axis = _parent.GetComponent<P3DParticle>()._node._particleDirection * _revolve_axis;
                _revolve_axis = _parent.GetComponent<P3DParticle>()._rotateMatrix * _revolve_axis;
            }
        }


        if (_node._rotate.revAxis == (ROTATE_AXIS)4)	//親の移動ベクトルを軸にする場合
        {
            if (_parent)
                _revolve_axis = _parent.GetComponent<P3DParticle>()._tracks;
        }

        


        //最初のフレームでなければ、一度角度を初期状態に戻す
        if (_life > 0)
        {
            Vector3 v;	//基準点
            if (_parent == null)
                v = this.transform.position - _node._emitter.pos;	        //親がいなければ、発生源からのベクトル
            else
                v = this.transform.position - _parent.transform.position;   //親がいれば、親からのベクトル

            //1フレーム前の角度
            float prevRevolve = ((float)_node._rotate.revAngle * _revolveRnd * _revolve_dir * _node.GetGraph(GRAPH.REVOLUT).GetValue(_life - 1, _lifeFrame));


            //逆回転
            v = Quaternion.AngleAxis(-prevRevolve, _revolve_axis) * v;

            //位置を変更（これで、公転してない状態）
            if (_parent == null)
                this.transform.position = _node._emitter.pos + v;
            else
                this.transform.position = _parent.transform.position + v;

            //移動方向も逆回転
            _move = Quaternion.AngleAxis(-prevRevolve, _revolve_axis) * _move;

        }

        //ここから今フレームの公転
        Vector3 v2;
        if (_parent == null)
            v2 = this.transform.position - _node._emitter.pos;	        //親がいなければ、発生源からのベクトル
        else
            v2 = this.transform.position - _parent.transform.position;   //親がいれば、親からのベクトル

        //今回の公転角度
        _revolve_angle = ((float)_node._rotate.revAngle * _revolveRnd * _revolve_dir * _node.GetGraph(GRAPH.REVOLUT).GetValue(_life, _lifeFrame));

        //回転
        v2 = Quaternion.AngleAxis(_revolve_angle, _revolve_axis) * v2;

        //位置を変更
        if (_parent == null)
            this.transform.position = _node._emitter.pos + v2;
        else
            this.transform.position = _parent.transform.position + v2;

        //移動方向も回転
        _move = Quaternion.AngleAxis(_revolve_angle, _revolve_axis) * _move;
    }


    /// <summary>
    /// 進行方向を向く
    /// </summary>
    private void UpdateDirection()
    {
        if (_node._shape.isDir)
        {
            Quaternion matLook = new Quaternion(0, 0, 0, 1);

            Vector3 axis;
            float angle;
            Vector3 realMove = _tracks.normalized;

            //移動してない
            if (realMove.magnitude == 0)
            {

                realMove = _move.normalized;
            }

            if (_node._shape.type != PRIMITIVE_TYPE.BILLBOARD)
            {

                if (!(realMove.z > -1.002 && realMove.z < -1.008))
                {
                    axis = Vector3.Cross(realMove, Vector3.forward).normalized;
                    angle = Mathf.Acos(Vector3.Dot(realMove, Vector3.forward)) * Mathf.Rad2Deg;
                    matLook = Quaternion.AngleAxis(-angle, axis);
                }
            }

            //2Dの場合
            else
            {
                //カメラから見た移動ベクトル
                realMove = Quaternion.Inverse(Camera.main.transform.rotation) * realMove;
                realMove.z = 0;
                realMove = realMove.normalized;

                //回転角度
                angle = Mathf.Acos(Vector3.Dot(realMove, Vector3.right)) * Mathf.Rad2Deg;
                axis = Vector3.Cross(realMove, Vector3.right).normalized;
                if (axis.z > 0)
                    angle *= -1;

                matLook = Quaternion.AngleAxis(angle, Vector3.forward);

            }

            this.transform.GetChild(0).localRotation *= matLook;
        }
    }


    /////////////////////////////////////////////////////////////////////////////// その他処理 /////////////////////////////////////////////////////////////////////////////// 

    /// <summary>
    /// 現在のアルファ値取得（子に反映させるのに必要）
    /// </summary>
    /// <returns></returns>
    float GetAlpha()
    {
        float alpha = (_node.GetGraph(GRAPH.A).GetValue(_life, _lifeFrame)) * _alphaRnd;
        if (alpha > 255) alpha = 255;
        return alpha;
    }


    /// <summary>
    /// 値をばらつかせる
    /// 引数が0のときは100％、1の時は0～200％の乱数を返す
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    float Ratio(int i)
    {
        return (i == 0) ? ((float)1.0f) : ((float)(Random.Range(0, i * 2) + (100 - i)) / 100.0f);
    }

}
