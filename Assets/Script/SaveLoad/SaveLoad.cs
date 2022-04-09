using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoad<T>
{
    private readonly string ext = ".txt";//확장자

    private FileStream file;

    public void Save(T tempData, string filename, string path)
    {
        string data = JsonUtility.ToJson(tempData);
        string FilePath = path + "/" + filename + ext;

        if(!Directory.Exists(path + "/"))
        {
            Directory.CreateDirectory(path + "/");
        }
        if (!File.Exists(FilePath))
        {
            file = File.Create(FilePath);
            file.Close();
        }
        File.WriteAllText(FilePath, data);
    }

    public bool Load(string filename, string path, out T deserialized)
    {
        string FilePath = path + "/" + filename + ext;
        string json = "";

        if(File.Exists(FilePath))
        {
            //file = File.Open(path + "/" + filename + ext, FileMode.Open);
            json = File.ReadAllText(FilePath);
        }else
        {
            deserialized = default;
            return false;
        }

        deserialized = JsonUtility.FromJson<T>(json);
        return true;

    }
}

public static class SaveLoad
{
    [System.Serializable]
    class Wrapping<V>
    {
        public Wrapping(V data)
        {
            Data = data;
        }

        public V Data;
    }

    public static void Save<T>(T data, string Path, string FileName, string ext)
    {
        string FilePath = Path + "/" + FileName + "." + ext; 

        if (!Directory.Exists(Path + "/"))
        {
            Directory.CreateDirectory(Path + "/");
        }
        if (!File.Exists(FilePath))
        {
            FileStream file = File.Create(FilePath);
            file.Close();
        }
        File.WriteAllText(FilePath, JsonUtility.ToJson(new Wrapping<T>(data)));

    }//ext는 확장자 . 빼고 입력 / Auto Wrapping
    public static bool Load<T>(string Path, string FileName, string ext, out T deserialized)
    {
        string FilePath = Path + "/" + FileName + "." + ext;
        string json = "";

        if (File.Exists(FilePath))
        {
            //file = File.Open(path + "/" + filename + ext, FileMode.Open);
            json = File.ReadAllText(FilePath);
        }
        else
        {
            deserialized = default;
            return false;
        }

        deserialized = JsonUtility.FromJson<Wrapping<T>>(json).Data;
        return true;
    }//ext는 확장자 . 빼고 입력 / Auto Wrapping

}