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
        [SerializeField] TMP_Text    resultText;        // To.You 스타일 텍스트형 결과 (선택)
        [SerializeField] string      winText  = "스테이지 클리어!";
        [SerializeField] string      loseText = "실패...";
        [SerializeField] GameObject  starsGroup;
        [SerializeField] Transform   rewardContainer;
        [SerializeField] GameObject  rewardIconPrefab;   // Image + TMP_Text
        [SerializeField] Button      returnButton;
        [SerializeField] Button      retryButton;        // 다시하기 (선택)
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
            if (retryButton)
            {
                retryButton.onClick.AddListener(OnRetry);
                retryButton.gameObject.SetActive(false);
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
            if (resultText)   resultText.text = win ? winText : loseText;
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
            if (retryButton)  retryButton.gameObject.SetActive(true);
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

        void OnRetry()
        {
            if (root) root.SetActive(false);
            Time.timeScale = 1f;
            // 같은 스테이지로 재진입 (페이드 전환). 노드/스테이지 정보가 있으면 EnterStage, 없으면 현재 씬 리로드.
            if (GameManager.I != null && GameManager.I.CurrentStage != null)
                GameManager.I.EnterStage(GameManager.I.CurrentNode, GameManager.I.CurrentStage);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
