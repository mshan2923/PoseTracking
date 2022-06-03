using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Math
{
    /// <summary>
    /// Return To Degrees
    /// </summary>
    /// <param name="A"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    public static float Dot(Vector3 A, Vector3 B)
    {
        return Mathf.Acos(Vector3.Dot(A, B)) * Mathf.Rad2Deg;
    }
    /// <summary>
    /// Closest On infinity Length Line
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="Target"></param>
    /// <returns></returns>
    public static Vector3 ClosePointOnDirection(Vector3 origin, Vector3 direction, Vector3 Target)
    {
        return origin + Vector3.Project(Target - origin, direction);
        // line_start + Vector3.Project(point - line_start, line_end - line_start);

        /*
        Vector3 lhs = Target - origin;
        float dotP = Vector3.Dot(lhs, direction);
        return origin + direction * dotP;
        *///=====Other Way
    }

    // For finite lines:
    public static Vector3 ClosePointOnLine(Vector3 origin, Vector3 end, Vector3 target)
    {
        Vector3 direction = end - origin;
        float length = direction.magnitude;
        direction.Normalize();
        float project_length = Mathf.Clamp(Vector3.Dot(target - origin, direction), 0f, length);
        return origin + direction * project_length;
    }
    public static bool InRange(float vaule, float min, float max, bool equalPass = true)
    {
        if (equalPass)
        {
            return vaule >= min && vaule <= max;
        }else
        {
            return vaule > min && vaule < max;
        }
    }
    public static bool InRange(Vector3 vaule, Vector3 min, Vector3 max, bool equalPass = true)
    {
        return InRange(vaule.x, min.x, max.x, equalPass)
            && InRange(vaule.y, min.y, max.y, equalPass)
            && InRange(vaule.z, min.z, max.z, equalPass);
    }
    public static Vector3 Direction2Rotation(Vector3 Dir)
    {
        Vector3 Lrot = Vector3.zero;

        Lrot.y = Mathf.Acos(Vector3.Normalize(new Vector3(Dir.x, 0, Dir.z)).x) * Mathf.Rad2Deg;
        if (Dir.z < 0)
        {
            Lrot.y = 360 - Lrot.y;
        }

        float XZLength = new Vector3(Dir.x, 0, Dir.z).magnitude;
        float YOffset = Mathf.Abs(1 / Mathf.Max(Mathf.Sin(Lrot.y * Mathf.Deg2Rad), Mathf.Cos(Lrot.y * Mathf.Deg2Rad)));

        //Lrot.z = Mathf.Atan(Dir.y) * Mathf.Rad2Deg;//이건 45 이상으로 안됨
        Lrot.z = Mathf.Atan((Dir.y * YOffset) / XZLength) * Mathf.Rad2Deg;

        return Lrot;
    }
    public static Vector3 Rotation2Direction(Vector3 Rot)
    {
        Vector3 temp = new Vector3(Cos2(Rot.y), (Sin2(Rot.z) / Cos2(Rot.z)), Sin2(Rot.y));
        float YawRate = Mathf.Min(Mathf.Abs(temp.x), Mathf.Abs(temp.z)) / Mathf.Max(Mathf.Abs(temp.x), Mathf.Abs(temp.z));

        if (Mathf.Abs(temp.x) > Mathf.Abs(temp.z))
        {
            return new Vector3((temp.x > 0 ? 1 : -1), temp.y, (temp.z > 0 ? YawRate : YawRate * -1));
        }
        else
        {
            return new Vector3((temp.x > 0 ? YawRate : YawRate * -1), temp.y, (temp.z > 0 ? 1 : -1));
        }
    }

    public static float Sin2(float Degreese)
    {
        return Mathf.Sin(Degreese * Mathf.Deg2Rad);
    }
    public static float Cos2(float Degreese)
    {
        return Mathf.Cos(Degreese * Mathf.Deg2Rad);
    }

    public static float Pow2(float vaule)
    {
        return vaule * vaule;
    }
    public static Vector2[] ContactTwoCircle(Vector2 Origin, float OriginRadius, Vector2 Target, float TargetRadius)
    {
        //https://blog.daum.net/chamtech/8891791

        Vector2[] result = new Vector2[2];

        float Dis = (Origin - Target).magnitude;

        if (Dis <= (OriginRadius + TargetRadius))
        {
            float gaping = Mathf.Acos((Pow2(OriginRadius) - Pow2(TargetRadius) + Pow2(Dis)) / (2 * OriginRadius * Dis)) * Mathf.Rad2Deg;
            //T1

            float Dot = Mathf.Atan2((Target.y - Origin.y), (Target.x - Origin.x)) * Mathf.Rad2Deg;//T2

            //Debug.Log("Angle : (" + gaping + " + " + Dot + ")");
            result[0].x = Origin.x + OriginRadius * Cos2(gaping + Dot);
            result[0].y = Origin.y + OriginRadius * Sin2(gaping + Dot);
            //Debug.Log(Origin + " + " + OriginRadius + " * (" + Cos2(gaping + Dot) + " , " + Sin2(gaping + Dot) + " )");
            //T3 , T4

            result[1].x = Origin.x + OriginRadius * Cos2(Dot - gaping);
            result[1].y = Origin.x + OriginRadius * Sin2(Dot - gaping);
            //T5 , T6

            return result;
        }else
        {
            Debug.Log("Not Contect / " + (OriginRadius + TargetRadius) + " / " + Dis);
            return null;
        }
    }
}
