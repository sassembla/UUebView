using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiyamasuCanvas : MonoBehaviour
{
    public void Close()
    {
        // canvasの可視設定を変更する。
        this.gameObject.SetActive(false);
    }
}
