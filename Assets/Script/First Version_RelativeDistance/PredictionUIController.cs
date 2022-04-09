using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PredictionUIController : MonoBehaviour
{
    [System.Serializable]
    public class PredictionParameter
    {
        public float Threshold = 0.3f;
        public float PredictionDely = 0.1f;
        public float MovenmentSpeed = 5;
        public float RotationSpeed = 5;
        public Vector3 ArmPositionToInvisible = new Vector3(0.1f, -1, 0);
        public bool InputLock = false;
        public Vector3 OriginPosition = new Vector3(0, 0, 10);
        public Vector3 MovementScaleOffset = Vector3.one;

        public bool Set(BodyPrediction prediction)
        {
            if (prediction != null)
            {
                prediction.threshold = Threshold;
                prediction.PredictionDelay = PredictionDely;
                prediction.MovementSpeed = MovenmentSpeed;
                prediction.RotationSpeed = RotationSpeed;
                prediction.ArmPositionToInvisible = ArmPositionToInvisible;
                prediction.FocusInputBlock = InputLock;
                prediction.OriginPosition = OriginPosition;
                prediction.MovementScaleOffset = MovementScaleOffset;
            }

            return prediction != null;
        }
        public bool Get(BodyPrediction prediction)
        {
            if (prediction != null)
            {
                Threshold = prediction.threshold;
                PredictionDely = prediction.PredictionDelay;
                MovenmentSpeed = prediction.MovementSpeed;
                RotationSpeed = prediction.RotationSpeed;
                ArmPositionToInvisible = prediction.ArmPositionToInvisible;
                InputLock = prediction.FocusInputBlock;
                OriginPosition = prediction.OriginPosition;
                MovementScaleOffset = prediction.MovementScaleOffset;
            }

            return prediction != null;
        }
    }
    [System.Serializable]
    public class FaceParameter
    {
        public bool BlinkEye = true;
        public float BlinkMinInterval = 1;
        public float BlinkMaxInterval = 5;
        public float BlinkSpeed = 0.1f;
        public float Mic_Sensitivity = 75;
        public int SpeakOpenIndex = 1;

        public bool Set(FaceController face)
        {
            if (face != null)
            {
                face.BlinkEye = BlinkEye;
                face.BlinkMinInterval = BlinkMinInterval;
                face.BlinkMaxInterval = BlinkMaxInterval;
                face.BlinkSpeed = BlinkSpeed;
                face.Mic_Sensitivity = Mic_Sensitivity;
                face.SpeakOpenIndex = SpeakOpenIndex;
            }

            return face != null;
        }
        public bool Get(FaceController face)
        {
            if (face != null)
            {
                BlinkEye = face.BlinkEye;
                BlinkMinInterval = face.BlinkMinInterval;
                BlinkMaxInterval = face.BlinkMaxInterval;
                BlinkSpeed = face.BlinkSpeed;
                Mic_Sensitivity = face.Mic_Sensitivity;
                SpeakOpenIndex = face.SpeakOpenIndex;
            }

            return face != null;
        }
    }

    public BodyPrediction bodyPrediction;
    public FaceController faceController;

    public PredictionParameter predictionParameter;
    public FaceParameter faceParameter;

    public GameObject Panel;

    public GameObject EyeSliderParent;
    public GameObject MouseSliderParent;

    List<Slider> EyeSlider = new();
    List<Slider> MouseSlider = new();

    [Space(10)]

    public InputField XInput;
    public InputField YInput;
    public InputField ZInput;

    void Start()
    {
        if (System.IO.File.Exists(Application.dataPath + "/data/PredictionSetting.txt"))
        {
            LoadFile();
        }else
        {
            SaveFile();
        }

        for (int i = 0; i < EyeSliderParent.transform.childCount; i++)
        {
            EyeSliderParent.transform.GetChild(i).gameObject.GetComponentInChildren<Text>().text =
                faceController.EyeShapeKey.GetKey(i);

            EyeSlider.Add(EyeSliderParent.transform.GetChild(i).gameObject.GetComponentInChildren<Slider>());
        }
        for (int i = 0; i < MouseSliderParent.transform.childCount; i++)
        {
            MouseSliderParent.transform.GetChild(i).gameObject.GetComponentInChildren<Text>().text =
                faceController.MouseShapeKey.GetKey(i);

            MouseSlider.Add(MouseSliderParent.transform.GetChild(i).gameObject.GetComponentInChildren<Slider>());
        }


        MouseSlider[0].value = 1;//Smail 기본값
        Vector3ToInputs(predictionParameter.OriginPosition);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            if (Panel.activeSelf)
            {
                Panel.SetActive(false);
            }
            else
            {
                Panel.SetActive(true);
            }
        }

        if (Panel.activeSelf)
        {
            // 값 변경 동기화

            {
                //InputPosition = bodyPrediction.OriginPosition;
                //predictionParameter.OriginPosition = bodyPrediction.OriginPosition;
                bodyPrediction.OriginPosition = predictionParameter.OriginPosition;
                predictionParameter.OriginPosition = InputsToVector3();

            }

            if (! faceController.BlinkEye)
            {
                faceController.EyeShapeKey.SetVaule(0, EyeSlider[0].value * 100);
                EyeSlider[0].value = faceController.EyeShapeKey.GetVaule(0) * 0.01f;
            }

            for(int i = 1; i < EyeSlider.Count; i++)
            {
                faceController.EyeShapeKey.SetVaule(i, EyeSlider[i].value * 100);
                EyeSlider[i].value = faceController.EyeShapeKey.GetVaule(i) * 0.01f;
            }
            for(int i = 0; i < MouseSlider.Count; i++)
            {
                faceController.MouseShapeKey.SetVaule(i, MouseSlider[i].value * 100);
                MouseSlider[i].value = faceController.MouseShapeKey.GetVaule(i) * 0.01f;
            }

            faceController.Apply();
        }
    }

    public void SaveFile()
    {
        SaveLoad.Save(predictionParameter, Application.dataPath + "/data", "PredictionSetting", "txt");
        SaveLoad.Save(faceParameter, Application.dataPath + "/data", "FaceSetting", "txt");

    }
    public void LoadFile()
    {
        SaveLoad.Load(Application.dataPath + "/data", "PredictionSetting", "txt", out predictionParameter);
        SaveLoad.Load(Application.dataPath + "/data", "FaceSetting", "txt", out faceParameter);

        predictionParameter.Set(bodyPrediction);
        faceParameter.Set(faceController);

    }

    public void Vector3ToInputs(Vector3 pos)
    {
        XInput.text = pos.x.ToString("0.###");
        YInput.text = pos.y.ToString("0.###");
        ZInput.text = pos.z.ToString("0.###");
    }
    public Vector3 InputsToVector3()
    {
        Vector3 temp = Vector3.zero;

        temp.x = float.Parse(XInput.text);
        temp.y = float.Parse(YInput.text);
        temp.z = float.Parse(ZInput.text);

        return temp;
    }
}
