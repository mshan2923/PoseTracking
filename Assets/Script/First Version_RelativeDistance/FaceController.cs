using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    public Map<string, float> EyeShapeKey = new();
    public Map<string, float> MouseShapeKey = new();

    public SkinnedMeshRenderer EyesSkinnedMesh;
    public SkinnedMeshRenderer MouseSkinnedMesh;

    public bool BlinkEye = true;
    public float BlinkMinInterval = 1;
    public float BlinkMaxInterval = 5;
    public float BlinkSpeed = 0.1f;//===0.2ÃÊ µ¿¾È ÇÑ¹ø ±ôºýÀÓ

    AudioSource audio;
    public float Mic_Sensitivity = 75;
    public float loudness = 0;
    public int SpeakOpenIndex = 1;

    void Start()
    {
        //EyesSkinnedMesh.SetBlendShapeWeight();
        StartCoroutine(BlinkEyeLoop());

        audio = GetComponent<AudioSource>();
        audio.clip = Microphone.Start(null, true, 10, 44100);
        audio.loop = true;
        audio.mute = false;
        while (!(Microphone.GetPosition(null) > 0)) { }
        audio.Play();
    }

    // Update is called once per frame
    void Update()
    {
        loudness = GetAveragedVolume() * 100 * Mic_Sensitivity;
        MouseSkinnedMesh.SetBlendShapeWeight(1, Mathf.Clamp(loudness, 0, 100));
    }

    IEnumerator BlinkEyeLoop()
    {
        while(BlinkEye)
        {
            yield return new WaitForSeconds(Random.Range(BlinkMinInterval, BlinkMaxInterval));

            StartCoroutine(Blinking());
        }
    }

    IEnumerator Blinking()
    {
        bool reverse = false;
        float AddVaule = 0;

        while(EyeShapeKey.GetVaule(0) >= 1 || !reverse)
        {
            AddVaule = (100 / Mathf.Max(BlinkSpeed, 0.1f) * 0.5f) * Time.deltaTime;
            if ((EyeShapeKey.GetVaule(0) + AddVaule) > 100)//EyesSkinnedMesh.GetBlendShapeWeight(0)
            {
                reverse = true;
            }
            if ((EyeShapeKey.GetVaule(0) - AddVaule) <= 1 && reverse)
            {
                EyeShapeKey.SetVaule(0, 0);
                EyesSkinnedMesh.SetBlendShapeWeight(0, 0);
                break;
            }

            if (reverse)
            {
                EyeShapeKey.SetVaule(0, EyeShapeKey.GetVaule(0) - AddVaule);
                //EyesSkinnedMesh.SetBlendShapeWeight(0, EyesSkinnedMesh.GetBlendShapeWeight(0) - AddVaule);
            }
            else
            {
                EyeShapeKey.SetVaule(0, EyeShapeKey.GetVaule(0) + AddVaule);
                //EyesSkinnedMesh.SetBlendShapeWeight(0, EyesSkinnedMesh.GetBlendShapeWeight(0) + AddVaule);
            }
            EyesSkinnedMesh.SetBlendShapeWeight(0, EyeShapeKey.GetVaule(0));

            yield return null;
        }
    }

    float GetAveragedVolume()
    {
        float[] data = new float[256];
        float a = 0;
        audio.GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / 256;
    }


    public void Apply()
    {
        for (int i = 0; i < EyeShapeKey.Count; i++)
        {
            EyesSkinnedMesh.SetBlendShapeWeight(i, EyeShapeKey.GetVaule(i));
        }

        for (int i = 0; i < MouseShapeKey.Count; i++)
        {
            MouseSkinnedMesh.SetBlendShapeWeight(i, MouseShapeKey.GetVaule(i));
        }
    }
}
