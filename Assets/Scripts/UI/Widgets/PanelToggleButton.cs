using UnityEngine;
using UnityEngine.UI;
using TOME.Combat;
namespace TOME.UI
{
    /// <summary>버튼 클릭 시 한 패널을 켜고 다른 패널을 끈다. 선택적으로 전투를 일시정지/재개.</summary>
    [RequireComponent(typeof(Button))]
    public class PanelToggleButton : MonoBehaviour
    {
        [SerializeField] GameObject toActivate;
        [SerializeField] GameObject toDeactivate;
        [SerializeField] bool pauseCombat;
        [SerializeField] bool resumeCombat;

        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Apply);
        }

        void Apply()
        {
            if (toActivate)   toActivate.SetActive(true);
            if (toDeactivate) toDeactivate.SetActive(false);
            if (pauseCombat)  CombatManager.I?.Pause();
            if (resumeCombat) CombatManager.I?.Resume();
        }
    }
}
