using System.Collections.Generic;
using UnityEngine;

namespace TOME.Map
{
    /// <summary>맵 씬에서 카메라 시야를 Texture2D로 캡처. Stage 진입 시 배경으로 사용.
    /// 캡처 동안 게임플레이 오버레이(UI Canvas = 스테이지 버튼 등, MapPickup = 아이템)는 잠시 숨겨
    /// 배경 풍경만 담는다(아이콘/아이템이 배경에 박히지 않도록).</summary>
    public static class StageBackgroundCapture
    {
        public static Texture2D Capture(Camera cam, int width = 540, int height = 960)
        {
            if (cam == null) return null;

            // --- 캡처에서 제외할 오버레이를 잠시 숨김 ---
            var hiddenCanvases = new List<Canvas>();
            foreach (var c in Object.FindObjectsByType<Canvas>())
                if (c != null && c.enabled) { c.enabled = false; hiddenCanvases.Add(c); }

            var hiddenRenderers = new List<Renderer>();
            foreach (var p in Object.FindObjectsByType<MapPickup>())
            {
                if (p == null) continue;
                foreach (var r in p.GetComponentsInChildren<Renderer>(false))
                    if (r != null && r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
            }

            // --- 렌더 ---
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

            // --- 숨긴 오버레이 복원 ---
            foreach (var c in hiddenCanvases)  if (c != null) c.enabled = true;
            foreach (var r in hiddenRenderers) if (r != null) r.enabled = true;

            return tex;
        }
    }
}
