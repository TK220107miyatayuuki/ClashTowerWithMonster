using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

//assetからデータを取り込む処理
public static class SetFieldFromAssets
{
   public static async UniTask<T> SetField<T>(string address)
   {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        await handle.ToUniTask();
        if (handle.Status == AsyncOperationStatus.Succeeded) return handle.Result;
        else return (T)default;

   }
}
