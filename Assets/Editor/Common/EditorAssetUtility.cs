using System.IO;
using UnityEditor;

namespace TOME.EditorTools
{
    /// <summary>CSV 생성기들이 공유하는 에셋 폴더/파일명 유틸.</summary>
    public static class EditorAssetUtility
    {
        /// <summary>중간 폴더까지 포함해 에셋 폴더를 보장한다. (예: Assets/Data/Stages/Chapter01)</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>camelCase id → 첫 글자만 대문자 (기존 에셋 파일명 규칙과 일치).</summary>
        public static string Pascal(string id)
            => string.IsNullOrEmpty(id) ? id : char.ToUpperInvariant(id[0]) + id.Substring(1);
    }
}
