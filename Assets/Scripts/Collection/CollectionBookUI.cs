using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using TOME.Characters;
using TOME.Crafting;
using TOME.Save;
namespace TOME.Collection
{
    public class CollectionBookUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private RecipeSO[] recipes;

        [Header("Main Book UI")]
        [Tooltip("도감 전체 팝업 패널")]
        [SerializeField] private GameObject bookRootPanel;
        [Tooltip("도감을 열어주는 메인 버튼 (도감 열릴 때 비활성화됨)")]
        [SerializeField] private GameObject openButtonObject;
        [Tooltip("캐릭터 슬롯들이 생성될 그리드 부모 (CharacterGrid)")]
        [SerializeField] private Transform contentParent;
        [Tooltip("도감 메인 캐릭터 슬롯 프리패브 (UI_CollectionEntry)")]
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private Button closeButton;

        [Header("Detail Popup (해금 캐릭터 상세창)")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailPortrait;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailAbilityText;
        [Tooltip("조합 재료 아이콘들이 들어갈 부모 공간 (Horizontal/GridLayoutGroup)")]
        [SerializeField] private Transform ingredientsParent;
        [Tooltip("조합 재료 아이콘 전용 UI Image 프리패브")]
        [SerializeField] private GameObject ingredientIconPrefab;
        [SerializeField] private Button detailCloseButton;

        [Header("Unknown Popup (미해금 알림창)")]
        [SerializeField] private GameObject unknownPanel;
        [SerializeField] private TMP_Text unknownMessageText;
        [SerializeField] private Button unknownCloseButton;

        [Header("Texts")]
        [Tooltip("미해금 카드를 눌렀을 때 알림창에 표시할 문구.")]
        [SerializeField] private string unknownMessage = "아직 발견하지 못한 조합 캐릭터예요!";
        [Tooltip("레시피/캐릭터 어느 쪽에도 능력 설명이 없을 때 상세창에 표시할 문구.")]
        [SerializeField] private string defaultAbilityText = "특별한 패시브 능력이 없는 일반 캐릭터입니다.";
        [Tooltip("미해금 슬롯에 표시할 글자.")]
        [SerializeField] private string lockedMarkText = "?";

        private void Awake()
        {
            // 버튼 이벤트 바인딩
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (detailCloseButton != null) detailCloseButton.onClick.AddListener(() => detailPanel.SetActive(false));
            if (unknownCloseButton != null) unknownCloseButton.onClick.AddListener(() => unknownPanel.SetActive(false));

            // 초기 팝업 비활성화 및 도감 오픈 버튼 활성화
            if (bookRootPanel != null) bookRootPanel.SetActive(false);
            if (detailPanel != null) detailPanel.SetActive(false);
            if (unknownPanel != null) unknownPanel.SetActive(false);
            if (openButtonObject != null) openButtonObject.SetActive(true);
        }

        public void Open()
        {
            if (bookRootPanel != null) bookRootPanel.SetActive(true);

            // 도감 창이 열렸으므로 도감 열기 버튼 숨기기
            if (openButtonObject != null) openButtonObject.SetActive(false);

            RebuildEntries();
        }

        public void Close()
        {
            if (bookRootPanel != null) bookRootPanel.SetActive(false);
            if (detailPanel != null) detailPanel.SetActive(false);
            if (unknownPanel != null) unknownPanel.SetActive(false);

            // 도감 창이 닫혔으므로 도감 열기 버튼 다시 표시
            if (openButtonObject != null) openButtonObject.SetActive(true);
        }

        private void RebuildEntries()
        {
            if (contentParent == null || entryPrefab == null) return;

            // 기존 슬롯 삭제
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }

            if (recipes == null) return;

            HashSet<string> seen = new HashSet<string>();

            foreach (var recipe in recipes)
            {
                if (recipe != null && recipe.result != null && seen.Add(recipe.result.id))
                {
                    CreateEntry(recipe);
                }
            }
        }

        private void CreateEntry(RecipeSO recipe)
        {
            CharacterSO character = recipe.result;
            bool unlocked = SaveSystemManager.I != null && SaveSystemManager.I.IsCharUnlocked(character.id);

            GameObject entryObj = Instantiate(entryPrefab, contentParent);

            Transform iconTransform = entryObj.transform.Find("Icon");
            Image iconImage = iconTransform?.GetComponent<Image>();
            TMP_Text nameText = entryObj.transform.Find("Name")?.GetComponent<TMP_Text>();
            TMP_Text questionText = entryObj.transform.Find("Question")?.GetComponent<TMP_Text>();
            Button entryBtn = entryObj.GetComponent<Button>();

            // 1. 캐릭터 아이콘 세팅
            if (iconImage != null)
            {
                if (unlocked)
                {
                    // 해금 시: 아이콘 켜고 원본 스프라이트 표시
                    iconImage.gameObject.SetActive(true);
                    iconImage.sprite = character.icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    // 미해금 시: 아이콘 이미지 오브젝트 자체를 끄기 (검은 사각형 방지)
                    iconImage.gameObject.SetActive(false);
                }
            }

            // 2. 캐릭터 이름 텍스트 세팅
            if (nameText != null)
            {
                // 해금 시 이름 표시, 미해금 시 숨김(또는 "?"로 표시)
                nameText.gameObject.SetActive(unlocked);
                if (unlocked)
                {
                    nameText.text = character.displayName;
                }
            }

            // 3. 물음표(?) 텍스트 세팅
            if (questionText != null)
            {
                // 미해금 시에만 표시
                questionText.gameObject.SetActive(!unlocked);
                if (!unlocked)
                {
                    questionText.text = lockedMarkText;
                }
            }

            // 4. 클릭 이벤트 세팅
            if (entryBtn != null)
            {
                entryBtn.onClick.AddListener(() =>
                {
                    if (unlocked) ShowDetail(recipe);
                    else ShowUnknown();
                });
            }
        }

        /// <summary>
        /// 미해금 카드('?') 클릭 시 ("몰라" 알림창)
        /// </summary>
        private void ShowUnknown()
        {
            if (unknownPanel != null)
            {
                if (unknownMessageText != null)
                {
                    unknownMessageText.text = unknownMessage;
                }
                unknownPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 해금된 캐릭터 카드 클릭 시 (상세 정보 + 재료 아이콘 생성)
        /// </summary>
        private void ShowDetail(RecipeSO recipe)
        {
            if (detailPanel == null || recipe == null) return;

            CharacterSO character = recipe.result;

            // 1. 캐릭터 초상화 설정
            if (detailPortrait != null && character != null)
            {
                detailPortrait.sprite = character.icon;
                detailPortrait.preserveAspect = true;
            }

            // 2. 캐릭터 이름 설정
            if (detailNameText != null && character != null)
            {
                detailNameText.text = character.displayName;
            }

            // 3. 스탯(수치) 대신 레시피/캐릭터에 기재된 [능력 설명]을 표시
            if (detailAbilityText != null)
            {
                string abilityDesc = "";

                // ① RecipeSO에 따로 기술된 수식어/능력 설명이 있다면 우선 사용
                if (!string.IsNullOrWhiteSpace(recipe.ability))
                {
                    abilityDesc = recipe.ability;
                }
                // ② 캐릭터SO 자체에 작성된 능력 설명이 있다면 사용
                else if (character != null && !string.IsNullOrWhiteSpace(character.abilityDescription))
                {
                    abilityDesc = character.abilityDescription;
                }
                // ③ 둘 다 작성되지 않았을 경우 표시할 기본 텍스트
                else
                {
                    abilityDesc = defaultAbilityText;
                }

                detailAbilityText.text = abilityDesc;
            }

            // 4. 조합 재료 아이콘 슬롯 동적 생성
            if (ingredientsParent != null)
            {
                // 기존 재료 슬롯 삭제
                for (int i = ingredientsParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(ingredientsParent.GetChild(i).gameObject);
                }

                // 사용된 조합템 개수만큼 슬롯 생성
                if (recipe.ingredients != null && ingredientIconPrefab != null)
                {
                    foreach (var ingredient in recipe.ingredients)
                    {
                        if (ingredient == null) continue;

                        GameObject itemObj = Instantiate(ingredientIconPrefab, ingredientsParent);
                        Image itemImage = itemObj.GetComponent<Image>();
                        if (itemImage != null)
                        {
                            itemImage.sprite = ingredient.icon;
                            itemImage.preserveAspect = true;
                        }
                    }
                }
            }

            detailPanel.SetActive(true);
        }
    }
}