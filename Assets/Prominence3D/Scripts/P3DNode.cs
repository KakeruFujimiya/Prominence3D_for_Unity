using UnityEngine;
using System.Collections;
using System.IO;
using System;


/////////////////////////////////////定数宣言

public enum GRAPH	//グラフ
{
    SIZE_X,		//サイズ
    SIZE_Y,
    SIZE_Z,
    ROTATE,		//自転
    REVOLUT,		//公転
    R,			//カラー
    G,
    B,
    A,
    SPEED,		//速度
    SIZE_XYZ,
    SHAKE,		//ぶれ
    MAX
};

public enum EMITTER_TYPE
{
    VCIRCLE,		//円（垂直）
    HCIRCLE,		//円（水平）
    SPHERE,		//球
    HEMISPHERE,	//半球
    CYLINDER,		//円柱
    BOX,			//直方体
    MAX,
};

//エミッタ―範囲
public enum EMITTER_SCOPE
{
    IN,	    //内部
    OUT,	//外側
    OTHER	//任意
};

public enum TIMING_TYPE
{
    ABSOLUTE,//絶対フレーム
    PARENT,	//親フレーム
    DESTROY	//親消滅時
};

//形状
public enum PRIMITIVE_TYPE
{
    BILLBOARD,
    POLYGON,
    HEMISPHERE,
    CYLINDER,
    TRAIL,
    MAX
};

//パーティクルの向き
public enum PRIMITIVE_DIRECTION
{
    FRONT,
    BACK,
    RIGHT,
    LEFT,
    UP,
    DOWN,

    RANDOM
};

//回転軸
public enum ROTATE_AXIS
{
    X,
    Y,
    Z,
    DIRECTION_X,
    DIRECTION_Y,
    DIRECTION_Z,
    PARENT_DIR_AXIS,
    PARENT_MOVE_AXIS
};

//回転方向
public enum ROTATE_DIRECTION
{
    FORWARD,
    REVERSE,
    RANDOM,
};



/////////////////////////////////データ構造体

//発生源
public struct Emitter
{
    public EMITTER_TYPE type;	    //形状
    public Vector3 size;	//サイズ
    public Vector3 pos;	    //中心位置
    public EMITTER_SCOPE scope;	    //範囲
    public int err;	        //範囲を「任意」にした時の値
    public bool fix;	    //向き固定
    public bool scaleFix;	//サイズ固定
    public bool isEvenInterval;	//等間隔
}



//発生
public struct Timing
{
    public TIMING_TYPE type;
    public int start, end;

    public int often;	//頻度
    public int often_err;

    public int num;	//個数
    public int num_err;
}


//消滅
public struct Kill
{
    public int span;
    public int span_err;

    public bool isFrame;
    public int frame;
    public int frame_err;

    public bool isLand;
}


//移動
public struct Move
{
    public bool isOneWay;	//一方向？
    public int oneway_err;	//一方向時の誤差
    public float dirY, dirX;	//一方向時の向き
    public int speed;		//速度
    public int speed_err;	//速度誤差
    public bool isFollow;	//親に追従
    public int gravity;	//重力
    public Vector3 absorbPos;	//吸引位置
    public float absorbPawer;	//吸引力
    public int shake;			//ぶれ
    public bool isParentShake;	//ぶれ（親の影響）
}



//形状
public struct Shape
{
    public PRIMITIVE_TYPE type;	//形
    public string fileName;			//3Dモデルを選択したときのファイル名
    public PRIMITIVE_DIRECTION dir;	//向き
    public bool isDir;				//進行方向を向く
    public bool isUnderCut;			//地下非表示
}


//画像
public struct Texture
{
    public string fName;	//ファイルネーム（ファイル削除時に一時的に使用）
    public int ID;		//素材番号
    public bool isAdd;	//加算合成か？
    public bool isAnim;	//アニメーションするか
    public int w, h;	//コマの並び
    public int speed;	//速度
    public bool isLoop;	//繰り返すか
    public Texture2D texture;   //テクスチャ
}


//サイズ
public struct Scale
{
    public int max;
    public int max_err;
    public int trailLength;		//軌跡の長さ
    public bool trailThickChange;		//太さタイプ
}


//カラー
public struct Color
{
    public bool isAlphaParent;		//アルファが親の影響を受けるか
    public int brigRnd;			//明るさ誤差
    public int alphaRnd;			//透明度誤差
}


//回転
public struct Rotate
{
    //自転
    public ROTATE_AXIS axis;
    public int angle;
    public int angle_err;
    public ROTATE_DIRECTION dir;
    public bool isSameParent;

    /// <summary>
    /// 公転軸
    /// </summary>
    public ROTATE_AXIS revAxis;

    /// <summary>
    /// 公転最大角度
    /// </summary>
    public int revAngle;    
    
    /// <summary>
    /// 公転角度誤差
    /// </summary>
    public int revAngle_err;

    /// <summary>
    /// 公転方向
    /// </summary>
    public ROTATE_DIRECTION revDir;
}


///////////////////////////////////////////////////////クラス
/// <summary>
/// ノード（Prominence3Dで設定した情報）を管理するクラス
/// </summary>
[System.Serializable]
public class P3DNode// : MonoBehaviour
{
    public Emitter _emitter;
    public Timing _timing;
    public Kill _kill;
    public Move _move;
    public Shape _shape;
    public Texture _texture;
    public Scale _scale;
    public Color _color;
    public Rotate _rotate;
    public Quaternion _particleDirection = new Quaternion(0, 0, 0, 1);   //パーティクルの向きを変えるクォータニオン



    P3DNode _parent;    //親ノード
    ArrayList _child = new ArrayList();// 子ノード
    bool _isDraw;		//表示するか
    P3DGraph[] _pGraph = new P3DGraph[(int)GRAPH.MAX];

    byte[] _fs;
    int _index;


    public P3DNode Parent
    {
        get { return _parent; }
        set { _parent = value; }
    }

    public ArrayList Child
    {
        get { return _child; }
    }

    public bool IsDraw
    {
        get { return _isDraw; }
        set { _isDraw = value; }
    }



    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="parentNode">親ノード</param>
    public P3DNode(P3DNode parentNode)
    {
        _parent = parentNode;

        string[] graphName = { "サイズ【幅】", "サイズ【高さ】", "サイズ【奥行き】", "自転角度", "公転角度", "カラー【赤】", "カラー【緑】", "カラー【青】", "カラー【不透明度】", "速度", "サイズ【全体】", "ぶれ" };

        for (int i = 0; i < (int)GRAPH.MAX; i++)
        {
            if (i == (int)GRAPH.SPEED)
                _pGraph[i] = new P3DSpeedGraph(graphName[i]);
            else
                _pGraph[i] = new P3DGraph(graphName[i]);


        }
    }



    /// <summary>
    /// ファイルをロードする
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <returns>読み込みインデックス</returns>
    public int Load(byte[] fs, int index)
    {
        _fs = fs;
        _index = index;
        _isDraw = GetBool();

        _emitter.type = (EMITTER_TYPE)GetValue();
        _emitter.size.x = GetValue();
        _emitter.size.y = GetValue();
        _emitter.size.z = GetValue();
        _emitter.pos.x = GetValue();
        _emitter.pos.y = GetValue();
        _emitter.pos.z = GetValue();
        _emitter.scope = (EMITTER_SCOPE)GetValue();
        _emitter.err = (int)GetValue();
        _emitter.fix = GetBool();
        _emitter.scaleFix = GetBool();
        _emitter.isEvenInterval = GetBool();

        _timing.type = (TIMING_TYPE)GetValue();
        _timing.start = (int)GetValue();
        _timing.end = (int)GetValue();
        _timing.often = (int)GetValue();
        _timing.often_err = (int)GetValue();
        _timing.num = (int)GetValue();
        _timing.num_err = (int)GetValue();

        _kill.span = (int)GetValue();
        _kill.span_err = (int)GetValue();
        _kill.isFrame = GetBool();
        _kill.frame = (int)GetValue();
        _kill.frame_err = (int)GetValue();
        _kill.isLand = GetBool();

        _move.isOneWay = GetBool();
        _move.oneway_err = (int)GetValue();
        _move.dirX = GetValue();
        _move.dirY = GetValue();
        _move.speed = (int)GetValue();
        _move.dirX = GetValue();
        _move.dirY = GetValue();
        _move.speed = (int)GetValue();
        _move.speed_err = (int)GetValue();
        _move.isFollow = GetBool();
        _move.gravity = (int)GetValue();
        _move.absorbPawer = GetValue();
        _move.absorbPos.x = GetValue();
        _move.absorbPos.y = GetValue();
        _move.absorbPos.z = GetValue();
        _move.shake = (int)GetValue();
        _move.isParentShake = GetBool();

        _shape.type = (PRIMITIVE_TYPE)GetValue();

        //3Dモデルのロード
        int length = 0;
	    if (_shape.type >= PRIMITIVE_TYPE.MAX)
	    {
		    length = (int)GetValue();

            //文字数分だけファイル名を取得
            byte[] byteName = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byteName[i] = _fs[_index + i];
            }
            string fileName = System.Text.Encoding.Default.GetString(byteName);
            _index += length;


            //メッシュをロードし、Primitiveクラスのリストに追加
            _shape.type = (PRIMITIVE_TYPE)P3DPrimitive.LoadMesh(fileName);


	    }







        _shape.dir = (PRIMITIVE_DIRECTION)GetValue();
        _shape.isDir = GetBool();
        _shape.isUnderCut = GetBool();

        //使用しているテクスチャのファイル名の文字数を取得
        length = (int)GetValue();

        //テクスチャロード
        if (length > 0)
        {


            //文字数分だけファイル名を取得
            byte[] byteName = new byte[length];
            for (int i = 0; i < length; i++)
            {
                byteName[i] = _fs[_index + i];
            }
            string fileName = System.Text.Encoding.Default.GetString(byteName);
            _index += length;

            //ファイル名が0文字以上（テクスチャ指定している場合）
            if (length > 0)
            {
                string assetName = System.IO.Path.GetFileNameWithoutExtension(fileName);    //拡張子とる
                _texture.texture = Resources.Load(assetName) as Texture2D;
            }
        }

        _texture.isAdd = GetBool();
        _texture.isAnim = GetBool();
        _texture.w = (int)GetValue();
        _texture.h = (int)GetValue();
        _texture.speed = (int)GetValue();
        _texture.isLoop = GetBool();

        _scale.max = (int)GetValue();
        _scale.max_err = (int)GetValue();
        _scale.trailLength = (int)GetValue();
        _scale.trailThickChange = GetBool();

        _color.isAlphaParent = GetBool();
        _color.brigRnd = (int)GetValue();
        _color.alphaRnd = (int)GetValue();

        _rotate.axis = (ROTATE_AXIS)GetValue();
        _rotate.angle = (int)GetValue();
        _rotate.angle_err = (int)GetValue();
        _rotate.dir = (ROTATE_DIRECTION)GetValue();
        _rotate.isSameParent = GetBool();
        _rotate.revAxis = (ROTATE_AXIS)GetValue();
        _rotate.revAngle = (int)GetValue();
        _rotate.revAngle_err = (int)GetValue();
        _rotate.revDir = (ROTATE_DIRECTION)GetValue();


        //グラフ
        for (int i = 0; i < (int)GRAPH.MAX; i++)
        {
            int graphPointNum;
            graphPointNum = (int)GetValue();


            for (int j = 0; j < graphPointNum; j++)
            {
                Vector2 point;
                point.x = GetValue();
                point.y = GetValue();

                _pGraph[i].AddPoint(point);
            }
            _pGraph[i].Calc();
        }


        switch(_shape.dir)
        {
            case PRIMITIVE_DIRECTION.FRONT:
                _particleDirection = Quaternion.AngleAxis(0, Vector3.up);
                break;

            case PRIMITIVE_DIRECTION.BACK:
                _particleDirection = Quaternion.AngleAxis(180, Vector3.up);
                break;

            case PRIMITIVE_DIRECTION.RIGHT:
                _particleDirection = Quaternion.AngleAxis(90, Vector3.up);
                break;

            case PRIMITIVE_DIRECTION.LEFT:
                _particleDirection = Quaternion.AngleAxis(-90, Vector3.up);
                break;

            case PRIMITIVE_DIRECTION.UP:
                _particleDirection = Quaternion.AngleAxis(90, Vector3.right);
                break;

            case PRIMITIVE_DIRECTION.DOWN:
                _particleDirection = Quaternion.AngleAxis(-90, Vector3.right);
                break;
        }




        ///子ノードの数をロード
        int childNum = (int)GetValue();

        //子ノードをロード
        for (int i = 0; i < childNum; i++)
        {
            P3DNode pNewNode = new P3DNode(this);
            _index = pNewNode.Load(_fs, _index);
            _child.Add(pNewNode);
        }

        return _index;
    }


    float GetValue()
    {
        int resut = BitConverter.ToInt32(_fs, _index);
        _index += 4;
        return (float)resut / 100.0f;
    }

    bool GetBool()
    {
        return (GetValue() > 0);
    }

    public void SetKillSpan(int span)
    {
        _kill.span = span;
    }

    public void AddChild(P3DNode node)
    {
        _child.Add(node);
    }

    public P3DGraph GetGraph(GRAPH i)
    {
        return _pGraph[(int)i];
    }


}
