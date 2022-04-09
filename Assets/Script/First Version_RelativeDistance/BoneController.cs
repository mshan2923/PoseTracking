using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BoneController : MonoBehaviour
{
    public BodyPrediction Prediction;
    Animator C_Animator;

    //public float ForwardLength = 10;
    //public Vector3 LookOffset = Vector3.zero;

    public float Z_Sensitive = 0.4f;

    Quaternion DefaultRotation;

    public Vector3 MinArm = new Vector3(0.35f, 0, 0);//0.35, 0, 0
    public Vector3 MaxArm= new Vector3(0.75f, 1, 0);//0.75, 1.2, 0

    public float ScaleOffset = 1;
    public float MoveAreaDistance = 50;

    Vector3 ObjPos = Vector3.zero;

    void Start()
    {
        C_Animator = gameObject.GetComponent<Animator>();

        DefaultRotation = gameObject.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        //C_Animator.SetBoneLocalRotation(HumanBodyBones.Head, Prediction.FaceSmoothRotation);// IK일때만 가능

        if (Prediction.bodyInfos[0].IsVisible || Prediction.bodyInfos[1].IsVisible)
        {

        }
        else
        {
            gameObject.transform.position = Prediction.OriginPosition;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        ObjPos.x = Prediction.SmoothIKs.BodyPosition.x * MoveAreaDistance * 0.25f;
        ObjPos.y = (Prediction.OriginPosition + (Prediction.SmoothIKs.BodyPosition - Prediction.OriginPosition) * MoveAreaDistance).y;
        ObjPos.z = (Prediction.OriginPosition + (Prediction.OriginPosition - Prediction.SmoothIKs.BodyPosition) * MoveAreaDistance).z;
        gameObject.transform.position = ObjPos;


        //C_Animator.SetLookAtWeight(1);//---이거쓸경우 SetBoneLocalRotation 가 Pitch만 됨
        //C_Animator.SetLookAtPosition( (Prediction.bodyInfos[0].Position - Vector2.one * 0.5f) * 2 * ForwardLength);

        C_Animator.SetBoneLocalRotation(HumanBodyBones.Neck, Prediction.SmoothIKs.FaceRotation);

        C_Animator.SetBoneLocalRotation(HumanBodyBones.Spine, Quaternion.Euler(Vector3.forward * Prediction.SmoothIKs.BodyRotation.z));//Spine Or Chest ????
        gameObject.transform.rotation = DefaultRotation * Quaternion.Euler(Vector3.up * Prediction.SmoothIKs.BodyRotation.y);


        //C_Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);

        C_Animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 1);
        C_Animator.SetIKHintPosition(AvatarIKHint.LeftElbow, RateToWorld(Prediction.SmoothIKs.L_ElbowPosition, true, Z_Sensitive));

        C_Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);//IK 우선순위
        C_Animator.SetIKPosition(AvatarIKGoal.LeftHand, RateToWorld(Prediction.SmoothIKs.L_WristPosition, true, Z_Sensitive));
        //IK 값 적용 /====월드 위치 라서 Local To World 필요
        //C_Animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.identity);

        //LeftArmOffset / Prediction.PredictedIKs.L_WristPosition


        //Debug.Log("Elbow : " + RateToWorld(Prediction.PredictedIKs.L_ElbowPosition, true, Z_Sensitive)
        //    + " / Wrist : " + RateToWorld(Prediction.PredictedIKs.L_WristPosition, true, Z_Sensitive));
        //--------

        C_Animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 1);
        C_Animator.SetIKHintPosition(AvatarIKHint.RightElbow, RateToWorld(Prediction.SmoothIKs.R_ElbowPosition, false, Z_Sensitive));

        C_Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);//IK 우선순위
        C_Animator.SetIKPosition(AvatarIKGoal.RightHand, RateToWorld(Prediction.SmoothIKs.R_WristPosition, false, Z_Sensitive));


        if (Prediction.bodyInfos[2].IsVisible)
        {
            C_Animator.SetBoneLocalRotation(HumanBodyBones.Spine, Quaternion.Euler(0, 0, Prediction.SmoothIKs.HipPosition.x * 45));
            gameObject.transform.position += MinArm.x * gameObject.transform.localScale.x * Prediction.SmoothIKs.HipPosition;

            //Debug.Log(C_Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation);//노출해야 사용가능

            //C_Animator.SetBoneLocalRotation(HumanBodyBones.LeftUpperLeg, Quaternion.Euler(temp));
            //Prediction.PredictedIKs.L_KneeRotation * C_Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).rotation
            //C_Animator.SetBoneLocalRotation(HumanBodyBones.RightUpperLeg, Quaternion.Euler(temp));

            //C_Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            //C_Animator.SetIKPosition(AvatarIKGoal.LeftFoot, Multiply(temp, gameObject.transform.localScale) + gameObject.transform.position);
            //C_Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            //C_Animator.SetIKPosition(AvatarIKGoal.RightFoot, Multiply(temp, gameObject.transform.localScale) + gameObject.transform.position);
        }
    }

    //팔꿈치만 보이는경우 , 팔 Z축 인식

    Vector3 Multiply(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    Vector3 RateToWorld(Vector3 Rate, bool Left , float z_Sensitive = 0.5f)
    {
        
        Vector3 result = Vector3.zero;
        result.x = MinArm.x + ((MaxArm.x - MinArm.x) * Rate.x * ScaleOffset);
        result.y = MinArm.y + ((MaxArm.y - MinArm.y) * (Rate.y + 1) * ScaleOffset);

        //result.z = Mathf.LerpUnclamped(DownArm.z, SideArm.z, Rate.z);
        result.z = Rate.z * z_Sensitive;

        //result *= ScaleOffset;
        if (Left)
        {
            result.x *= -1;
        }

        result = gameObject.transform.rotation * result;

        //result += Vector3.left * Mathf.Tan(Prediction.SmoothIKs.BodyRotation.z * Mathf.Deg2Rad);//어깨 기울기에 따라서 x 축 이동

        result = Multiply(gameObject.transform.localScale, result) ;

        result += gameObject.transform.position;
        return result;
        

        /*
        Vector3 result = Vector3.zero;
        result.x = MinArm.x + ((MaxArm.x - MinArm.x) * Rate.x * ScaleOffset);
        result.y = MinArm.y + ((MaxArm.y - MinArm.y) * (Rate.y + 1) * ScaleOffset);
        result.z = Rate.z * z_Sensitive;
        //result = Multiply(gameObject.transform.localScale, result);
        result *= ScaleOffset;
        result = gameObject.transform.rotation * result;

        if (Left)
        {
            result.x *= -1;
            result += C_Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
        }
        else
        {
            result += C_Animator.GetBoneTransform(HumanBodyBones.RightShoulder).position;

        }
        //
        return result;

        //gameObject.transform.position + Multiply(gameObject.transform.localScale, ArmOffset)
        //result += C_Animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position;
        *///안됨
    }
}
