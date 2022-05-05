using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
//using TensorFlowLite;
using UnityEngine.UI;

public class DebugPredicter : MonoBehaviour
{
    public GameObject CameraView;
    RectTransform Canvas;
    public GameObject DebugPointer;
    public Vector2 PointerSize = Vector2.one * 10;

    public Text FPSPanel;
    public int LockFrame = 60;

    Queue<GameObject> Pointers = new Queue<GameObject>();
    //Map<PoseNet.Part, GameObject> ActivePointer = new();//Map<Predicter.Slot,Gameobject>À¸·Î º¯°æ

    //Predicter predicter;

    public int LastVisiblePart = 0;

    public bool Active = true;

    void Start()
    {
        //predicter = GetComponent<Predicter>();
        Canvas = CameraView.transform.parent.GetComponent<RectTransform>();

        Debug.Log(CameraView.transform.parent.GetComponent<RectTransform>().sizeDelta);

        if (LockFrame > 0 )
        {
            Application.targetFrameRate = LockFrame;
        }
    }

    void Update()
    {
        if (Active)
        {
            LastVisiblePart = 0;
            AllReturnPool();
            /*
            for (int i = 0; i < predicter.results.Length; i++)
            {
                if (predicter.results[i].confidence > predicter.threshold)
                {
                    LastVisiblePart++;

                    var obj = GetPool(predicter.results[i].part);
                    obj.transform.SetParent(CameraView.transform);

                    //obj.transform.localPosition = RateToScreenPos(new Vector2(predicter.results[i].x, predicter.results[i].y), PointerSize);

                    obj.GetComponent<RectTransform>().anchoredPosition = RateToScreenPos(new Vector2(predicter.results[i].x, predicter.results[i].y));
                    obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y, 0);

                    //Debug.Log(new Vector2(predicter.results[i].x, predicter.results[i].y) + " : " +
                    //    RateToScreenPos(new Vector2(predicter.results[i].x, predicter.results[i].y)) + " / " + obj.transform.position);
                }
            }*/
        }else
        {
            //if (ActivePointer.Count > 0)
            {
                //AllReturnPool();
            }
        }

        if (FPSPanel != null)
        {
            FPSPanel.text = (1 / Time.deltaTime).ToString("000.##");
        }
    }

    public Vector2 RateToScreenPos(Vector2 rate)
    {
        //Min : pointerSize * 0.5f / Max : (Canvas.Size.y * Vector2.one) - (pointerSize * 0.5f)

        //return (pointerSize * 0.5f + ((Canvas.sizeDelta.y * Vector2.one) - pointerSize) * rate) * new Vector2(1, -1);//ÇÇ¹þ : ÁÂ»ó´Ü
        return (rate - Vector2.one * 0.5f) * Canvas.sizeDelta.y * new Vector2(1, -1);
    }

    public GameObject GetPool()//(PoseNet.Part part)
    {
        if (Pointers.Count > 0)
        {
            var obj = Pointers.Dequeue();
            obj.SetActive(true);

            //ActivePointer.Add(part, obj);
            return obj;
        }
        else
        {
            var obj = GameObject.Instantiate(DebugPointer);
            //ActivePointer.Add(part, obj);

            return obj;
        }
    }
    public void AllReturnPool()
    {

        //for (int i = 0; i < ActivePointer.Count; i++)
        {
            //var obj = ActivePointer.GetVaule(i);

            //Pointers.Enqueue(obj);
            //obj.SetActive(false);
        }

        //ActivePointer.Clear();
    }
}
