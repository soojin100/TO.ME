using UnityEngine;

using TOME.Map;
namespace TOME.Dialogue
{
    /// <summary>대사·컷신이 진행되는 동안 이 오브젝트를 숨긴다.
    /// MapBusyVisibility 가 DialogueManager.IsPlaying 을 보고 껐다 켠다(대사가 끝나면 원래대로 복구).
    ///
    /// 숨길 대상을 이름이나 계층 경로로 찾지 않고 이 컴포넌트로 표시한다 —
    /// 씬마다 계층이 달라도(맵은 CraftOpenButton, 스테이지는 CraftHandleBar·InventoryBar)
    /// 같은 규칙이 적용되고, 나중에 숨길 UI가 늘어도 컴포넌트만 붙이면 된다.
    ///
    /// 대사 시작 시점에 이미 꺼져 있던 오브젝트는 건드리지 않는다(복구 때 잘못 켜지 않게).</summary>
    [DisallowMultipleComponent]
    public class HideDuringDialogue : MonoBehaviour
    {
    }
}
