using UnityEngine;
using System.Collections;

public class PlayerAction : MonoBehaviour
{
    //グローバル変数
    CharacterController ctrl;
    public float rotateSpeed = 100.0f;  //主人公の回転の速度
    public float runSpeed = 500.0f;     //主人公の移動速度

    /// <summary>
    /// 初期化
    /// </summary>
	void Start () 
    {
        ctrl = GetComponent<CharacterController>();
	}
	
    /// <summary>
    /// 更新処理
    /// </summary>
	void Update () {
        //プレイヤーの移動
        ctrl.SimpleMove(transform.forward * Input.GetAxis("Vertical") * Time.deltaTime * runSpeed);
        transform.Rotate(Vector3.up, Input.GetAxis("Horizontal") * Time.deltaTime * rotateSpeed); 
	}

    /// <summary>
    /// トリガー接触
    /// </summary>
    /// <param name="info"></param>
    void OnTriggerEnter(Collider info)
    {
        //それぞれのエフェクトを発生
        if (info.gameObject.name == "Cylinder_0")
            GameObject.Find("P3DEngine").GetComponent<P3DEngine>().Play(0, Vector3.zero, false);

        if (info.gameObject.name == "Cylinder_1")
            GameObject.Find("P3DEngine").GetComponent<P3DEngine>().Play(1, Vector3.zero, false);

        if (info.gameObject.name == "Cylinder_2")
            GameObject.Find("P3DEngine").GetComponent<P3DEngine>().Play(2, Vector3.zero, false);

        if (info.gameObject.name == "Cylinder_3")
            GameObject.Find("P3DEngine").GetComponent<P3DEngine>().Play(3, Vector3.zero, false);

    }
}
