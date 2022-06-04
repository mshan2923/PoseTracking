using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LD_FaceController : MonoBehaviour
{
    public SkinnedMeshRenderer Target;
    public Map<string, float> Vaule;

    Animator animator;
    public Quaternion JawDefault;

    [Header(@"Eye")]
    public bool BlinkEye = true;
    public float BlinkMinInterval = 1;
    public float BlinkMaxInterval = 5;
    public float BlinkSpeed = 0.1f;//===0.2ÃÊ µ¿¾È ÇÑ¹ø ±ôºýÀÓ
    public float UpperEye = 25;

    [Header(@"Mouse")]
    AudioSource audio;
    public float Mic_Sensitivity = 75;
    public float loudness = 0;
    public int SpeakOpenIndex = 1;

    public float MouseOpenAngle = 10;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator.GetBoneTransform(HumanBodyBones.Jaw) != null)
            JawDefault = animator.GetBoneTransform(HumanBodyBones.Jaw).transform.localRotation;

        StartCoroutine(BlinkEyeLoop());

        audio = GetComponent<AudioSource>();
        audio.clip = Microphone.Start(null, true, 10, 44100);
        audio.loop = true;
        audio.mute = false;
        while (!(Microphone.GetPosition(null) > 0)) { }
        audio.Play();

        Debug.Log(" Start : " + Microphone.devices[0]);

        UpperEye = Target.GetBlendShapeWeight(0);
    }

    // Update is called once per frame
    void Update()
    {
        loudness = Mathf.Clamp01(GetAveragedVolume(64) * Mic_Sensitivity);//
        //MouseSkinnedMesh.SetBlendShapeWeight(1, Mathf.Clamp(loudness, 0, 100));
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (animator.GetBoneTransform(HumanBodyBones.Jaw) != null)
            animator.SetBoneLocalRotation(HumanBodyBones.Jaw, JawDefault * Quaternion.Euler(Vector3.right * loudness * MouseOpenAngle));
    }

    float GetAveragedVolume(int length)
    {
        float[] data = new float[length];
        float a = 0;
        audio.GetOutputData(data, 0);
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        return a / length;
    }

    IEnumerator BlinkEyeLoop()
    {
        while (BlinkEye)
        {
            yield return new WaitForSeconds(Random.Range(BlinkMinInterval, BlinkMaxInterval));

            StartCoroutine(Blinking());
        }
    }
    IEnumerator Blinking()
    {
        bool reverse = false;
        float AddVaule = 0;

        while (Vaule.GetVaule(0) >= 1 || !reverse)
        {
            AddVaule = (100 / Mathf.Max(BlinkSpeed, 0.1f) * 0.5f) * Time.deltaTime;
            if ((Vaule.GetVaule(1) + AddVaule) > 100)//EyesSkinnedMesh.GetBlendShapeWeight(0)
            {
                reverse = true;
            }
            if ((Vaule.GetVaule(1) - AddVaule) <= 1 && reverse)
            {
                Vaule.SetVaule(0, UpperEye);
                Vaule.SetVaule(1, 0);
                Target.SetBlendShapeWeight(1, 0);
                break;
            }

            if (reverse)
            {
                Vaule.SetVaule(0, Vaule.GetVaule(0) - AddVaule);
                Vaule.SetVaule(1, Vaule.GetVaule(1) - AddVaule);
                //EyesSkinnedMesh.SetBlendShapeWeight(0, EyesSkinnedMesh.GetBlendShapeWeight(0) - AddVaule);
            }
            else
            {
                Vaule.SetVaule(0, Mathf.Clamp(Vaule.GetVaule(0) + AddVaule, 0, 100));
                Vaule.SetVaule(1, Vaule.GetVaule(1) + AddVaule);
                //EyesSkinnedMesh.SetBlendShapeWeight(0, EyesSkinnedMesh.GetBlendShapeWeight(0) + AddVaule);
            }
            Target.SetBlendShapeWeight(0, Vaule.GetVaule(0));
            Target.SetBlendShapeWeight(1, Vaule.GetVaule(1));

            yield return null;
        }
    }
}
