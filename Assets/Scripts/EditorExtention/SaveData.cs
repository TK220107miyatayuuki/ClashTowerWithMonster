using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
   //Dictionaryの要素を分解
   public List<string> controllerPathes = new List<string>();
   public List<string> originalClipnames = new List<string>();
}
