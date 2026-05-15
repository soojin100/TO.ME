using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Data;
using TOME.Managers;
using TOME.Gameplay.Player;

namespace TOME.UI
{
    /// <summary>승패 결과창. 캐릭터 중앙 이동 → CLEAR/FAIL → 2초 후 돌아가기 버튼.</summary>
    public class ResultScreenUI : MonoBehaviour
    {
        [SerializeField] GameObject  root;
        [SerializeField] GameObject  clearGraphic;
        [SerializeField] GameObject  failGraphic;
        [SerializeField] GameObject  starsGroup;
        [SerializeField] Transform   rewardContainer;
        [SerializeField] GameObject  rewardIconPrefab;   // Image + TMP_Text
        [SerializeField] Button      returnButton;
        [SerializeField] PlayerShell player;
        [SerializeField] Transform   centerAnchor;
        [SerializeField] float       moveDuration = 0.5f;
        [SerializeField] float       showDelay    = 2f;

        void Awake()
        {
            if (root) root.SetActive(false);
            if (returnButton)
            {
                returnButton.onClick.AddListener(OnReturn);
                returnButton.gameObject.SetActive(false);
            }
        }

        public void Show(bool win)
        {
            StartCoroutine(ShowRoutine(win));
        }

        IEnumerator ShowRoutine(bool win)
        {
            if (root)         root.SetActive(true);
            if (clearGraphic) clearGraphic.SetActive(win);
            if (failGraphic)  failGraphic.SetActive(!win);
            if (starsGroup)   starsGroup.SetActive(win);

            if (player && centerAnchor)
            {
                Transform pt = player.transform;
                Vector3 from = pt.position;
                Vector3 to   = centerAnchor.position;
                float t = 0f;
                while (t < moveDuration)
                {
                    t += Time.unscaledDeltaTime;
                    pt.position = Vector3.Lerp(from, to, t / moveDuration);
                    yield return null;
                }
                pt.position = to;
            }

            if (win) ShowRewards();

            yield return new WaitForSecondsRealtime(showDelay);
            if (returnButton) returnButton.gameObject.SetActive(true);
        }

        void ShowRewards()
        {
            var stage = GameManager.I != null ? GameManager.I.CurrentStage : null;
            if (stage == null || stage.rewards == null || rewardContainer == null || rewardIconPrefab == null)
                return;

            foreach (var r in stage.rewards)
            {
                if (!r) continue;
                var go = Instantiate(rewardIconPrefab, rewardContainer);
                var img   = go.GetComponentInChildren<Image>();
                var label = go.GetComponentInChildren<TMP_Text>();
                if (r.type == RewardType.Item && r.item)
                {
                    if (img)   { img.enabled = r.item.icon != null; if (r.item.icon) img.sprite = r.item.icon; }
                    if (label) label.text = $"x{r.amount}";
                }
                else if (r.type == RewardType.Coin)
                {
                    if (img)   img.enabled = false;
                    if (label) label.text = $"+{r.amount}";
                }
                else if (r.type == RewardType.Character && r.character)
                {
                    if (img)   { img.enabled = r.character.icon != null; if (r.character.icon) img.sprite = r.character.icon; }
                    if (label) label.text = r.character.displayName;
                }
            }
        }

        void OnReturn()
        {
            if (root) root.SetActive(false);
            GameManager.I?.ReturnToMap();
        }
    }
}
