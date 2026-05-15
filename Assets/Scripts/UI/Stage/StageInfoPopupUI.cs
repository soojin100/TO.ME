using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TOME.Core;
using TOME.Data;
using TOME.Managers;

namespace TOME.UI
{
    /// <summary>맵에서 노드 선택 시 표시. 클리어 여부에 따라 텍스트 분기.</summary>
    public class StageInfoPopupUI : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image      thumbnail;
        [SerializeField] TMP_Text   characterNameLabel;
        [SerializeField] TMP_Text   introLabel;
        [SerializeField] GameObject clearBadge;
        [SerializeField] Button     startButton;
        [SerializeField] Button     closeButton;

        NodeSO  _node;
        StageSO _stage;

        void Awake()
        {
            if (root) root.SetActive(false);
            if (startButton) startButton.onClick.AddListener(OnStart);
            if (closeButton) closeButton.onClick.AddListener(Hide);
        }

        public void Show(NodeSO node, StageSO stage)
        {
            if (node == null || stage == null) return;
            _node  = node;
            _stage = stage;

            bool cleared = SaveSystemManager.I != null && SaveSystemManager.I.IsNodeCleared(node.id);

            if (root) root.SetActive(true);
            if (thumbnail)
            {
                thumbnail.enabled = stage.thumbnail != null;
                if (stage.thumbnail) thumbnail.sprite = stage.thumbnail;
            }
            if (characterNameLabel)
                characterNameLabel.text = stage.startCharacter ? stage.startCharacter.displayName : stage.title;
            if (introLabel)
            {
                string txt = cleared && !string.IsNullOrEmpty(stage.clearedIntroText)
                    ? stage.clearedIntroText : stage.introText;
                introLabel.text = txt;
            }
            if (clearBadge) clearBadge.SetActive(cleared);
        }

        public void Hide()
        {
            if (root) root.SetActive(false);
        }

        void OnStart()
        {
            Hide();
            if (_node && _stage) GameManager.I?.EnterStage(_node, _stage);
        }
    }
}
