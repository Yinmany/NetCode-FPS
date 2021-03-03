using UnityEditorInternal;
using UnityEngine;

namespace MyGameLib.NetCode.Editor
{
    public static class CodeGenServiceUtils
    {
        
        
        public static T Load<T>(string assetPath) where T : ScriptableObject
        {
            Object[] arr =
                InternalEditorUtility.LoadSerializedFileAndForget(assetPath);
            if (arr != null && arr.Length > 0)
            {
                return arr[0] as T;
            }

            return null;
        }
    }
}