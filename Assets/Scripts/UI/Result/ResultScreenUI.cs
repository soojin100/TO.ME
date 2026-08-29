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
        [SerializeField] GameObject[] starIcons;          // 난이도 별(최대 5). difficulty 만큼 표시
        [SerializeField] Image       characterImage;      // 현재 장착 캐릭터 아이콘
        [SerializeField] Transform   rewardContainer;
        [SerializeField] GameObject  rewardIconPrefab;   // Image + TMP_Text
        [SerializeField] Button      returnButton;
        [SerializeField] Button      retryButton;        // 다시하기 (선택)
        [SerializeField] PlayerShell player;
        [SerializeField] Transform   centerAnchor;
        [SerializeField] float       moveDuration = 0.5f;
        [SerializeField] float       showDelay    = 2f;

        [SerializeField] float swayAmount = 15f;   // 좌우 흔들림 각도
        [SerializeField] float swaySpeed = 2f;    // 흔들림 속도

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

        StageSO _stage;

        public void Show(bool win, StageSO stage = null)
        {
            _stage = stage != null ? stage : (GameManager.I != null ? GameManager.I.CurrentStage : null);
            StartCoroutine(ShowRoutine(win));

            if (characterImage && characterImage.enabled)
                StartCoroutine(SwayCharacter());
        }

        IEnumerator ShowRoutine(bool win)
        {
            if (root)         root.SetActive(true);
            if (clearGraphic) clearGraphic.SetActive(win);
            if (failGraphic)  failGraphic.SetActive(!win);
            if (resultText)   resultText.text = win ? winText : loseText;

            // 보상·캐릭터·별·문구는 즉시 함께 표시
            SetupCharacter();
            SetupStars();
            if (win) ShowRewards();

            // 돌아가기 버튼은 결과를 잠시(showDelay) 보여준 뒤 등장
            yield return new WaitForSecondsRealtime(showDelay);
            if (returnButton) returnButton.gameObject.SetActive(true);
            if (retryButton)  retryButton.gameObject.SetActive(true);
        }

        void SetupCharacter()
        {
            var ch = player != null ? player.CurrentChar : null;
            if (characterImage)
            {
                bool ok = ch != null && ch.icon != null;
                characterImage.enabled = ok;
                if (ok) characterImage.sprite = ch.icon;
            }
            // 월드 강아지 플레이어를 숨기고 결과창 이미지로 대체
            if (player) player.gameObject.SetActive(false);
        }

        void SetupStars()
        {
            if (starsGroup) starsGroup.SetActive(true);
            if (starIcons == null) return;
            int diff = _stage != null ? _stage.difficulty : 0;
            for (int i = 0; i < starIcons.Length; i++)
                if (starIcons[i]) starIcons[i].SetActive(i < diff);
        }

        // 이번 스테이지에서 실제로 획득한 아이템들을 아이콘으로 표시 (수량 텍스트 없음)
        void ShowRewards()
        {
            if (rewardContainer == null || rewardIconPrefab == null) return;
            var items = InventoryManager.I != null ? InventoryManager.I.Items : null;
            if (items == null) return;

            foreach (var it in items)
            {
                if (it == null) continue;
                var go    = Instantiate(rewardIconPrefab, rewardContainer);
                var img   = go.GetComponentInChildren<Image>();
                var label = go.GetComponentInChildren<TMP_Text>();
                if (img)   { img.enabled = it.icon != null; if (it.icon) img.sprite = it.icon; }
                if (label) label.text = string.Empty;   // "+50" 식 수량 대신 이미지로만
            }
        }

        // 흔들흔들
        IEnumerator SwayCharacter()
        {
            var tr = characterImage.rectTransform;
            float elapsed = 0f;

            while (characterImage && characterImage.enabled)
            {
                elapsed += Time.unscaledDeltaTime * swaySpeed;
                float angle = Mathf.Sin(elapsed) * swayAmount;
                tr.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            // 종료 시 원위치
            if (tr) tr.localRotation = Quaternion.identity;
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
                TOME.Core.SceneFader.Go(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
