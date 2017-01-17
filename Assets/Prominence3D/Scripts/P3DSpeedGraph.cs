using UnityEngine;
using System.Collections;

/// <summary>
/// スピード調整グラフ
/// （スピードのグラフだけ、他のとは少し仕様が違う）
/// </summary>
public class P3DSpeedGraph : P3DGraph
{
    public P3DSpeedGraph(string name)
        : base(name)
    {
        _isSpeedGraph = true;
    }



    public override void Calc()
    {
        for (int i = 0; i < _posList.Count - 1; i++)
        {
            Vector2[] point = new Vector2[4];
            point[0] = _posList[i];
            point[3] = _posList[i + 1];

            if (point[0].x == 0)
            {
                point[1] = point[0];
            }
            else
            {
                Vector2 arm = _posList[i + 1] - _posList[i - 1];
                arm.Normalize();
                arm *= ((point[3].x - point[0].x) / 2);
                point[1] = point[0] + arm;
            }

            if (point[3].x == 100)
            {
                point[2] = point[3];
            }
            else
            {
                Vector2 arm = _posList[i] - _posList[i + 2];
                arm.Normalize();
                arm *= ((point[3].x - point[0].x) / 2);
                point[2] = point[3] + arm;
            }



            Vector2 prev = Vector2.zero;

            float step = 1.0f / (point[3].x - point[0].x);
            for (float t = 0.0f; t <= 1.2f; t += step)
            {
                Vector2 p;

                float k = 1.0f - t;
                p.x = k * k * k * point[0].x + 3.0f * k * k * t * point[1].x + 3.0f * k * t * t * point[2].x + t * t * t * point[3].x;
                p.y = k * k * k * point[0].y + 3.0f * k * k * t * point[1].y + 3.0f * k * t * t * point[2].y + t * t * t * point[3].y;

                //上下超えないように
                if (p.y > 100) p.y = 100;
                if (p.y < 0) p.y = 0;

                if (p.x > 100) break;

                //データ格納
                if (t > 0)
                {
                    for (int l = (int)Mathf.Round(prev.x), j = 0; l < (int)Mathf.Round(p.x); l++, j++)
                    {
                        _value[l] = (float)Mathf.Round(prev.y + (p.y - prev.y) / (p.x - prev.x) * j);


                        //スピードグラフのみの特殊使用
                        if (point[0].y >= 50 && point[3].y >= 50 && _value[l] < 50) _value[l] = 50;
                        if (point[0].y <= 50 && point[3].y <= 50 && _value[l] > 50) _value[l] = 50;
                    }
                }
                prev.x = p.x;
                prev.y = p.y;

            }

            //両端
            _value[(int)point[0].x] = point[0].y;
            _value[(int)point[3].x] = point[3].y;


            //スムージング
            for (int j = 0; j < 30; j++)
            {
                float[] buf = new float[101];
                for (int m = (int)(point[0].x + 1); m < (int)(point[3].x - 1); m++)
                {
                    buf[m] = (_value[m - 1] + _value[m + 1]) / 2;
                }

                for (int m = (int)(point[0].x + 1); m < (int)(point[3].x - 1); m++)
                {
                    _value[m] = buf[m];

                }
            }

        }
    }



    /// <summary>
    /// 任意の時点のグラフの値を返す
    /// </summary>
    /// <param name="life">今のフレーム数</param>
    /// <param name="span">寿命</param>
    /// <returns></returns>
    public override float GetValue(int life, int span)
    {
        if (life > span)
            life = span;

        float f = 100.0f * life / span;
        int i = (int)f;
        if (i > 99)  i = 99;
        if (i < 0) i = 0;

       // Debug.Log(life);
        return (float)((100.0f - (_value[i] * (1.0f - (f - i)) + _value[i + 1] * (f - i))) / 100.0f) * 2.0f - 1.0f;
    }

}
