using UnityEngine;

namespace TOME.Core
{
    /// <summary>맵 씬에서 카메라 시야를 Texture2D로 캡처. Stage 진입 시 배경으로 사용.</summary>
    public static class StageBackgroundCapture
    {
        public static Texture2D Capture(Camera cam, int width = 540, int height = 960)
        {
            if (cam == null) return null;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            tex.name = "StageBackground_Capture";
            return tex;
        }
    }
}
