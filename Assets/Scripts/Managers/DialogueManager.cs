using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TOME.Core;
using TOME.Data;
using TOME.Utils;

namespace TOME.Managers
{
    /// <summary>UI 측 Advance() 호출로 다음 줄 진행. 본 적 있으면 TryPlay false.</summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager I { get; private set; }

        [SerializeField] TextAsset dialogueCsv;
        Dictionary<string, DialogueEntry> table;

        public event Action<DialogueEntry> OnLine;
        public event Action OnEnd;

        public bool IsPlaying { get; private set; }

        bool _advance;
        bool _skip;
        string _startId;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public void PreloadAll() => table = CsvImporter.LoadDialogue(dialogueCsv);

        public bool TryPlay(string startId)
        {
            if (IsPlaying) return false;
            if (string.IsNullOrEmpty(startId) || table == null) return false;
            if (SaveSystemManager.I != null && SaveSystemManager.I.HasSeenDialogue(startId)) return false;
            _startId = startId;
            StartCoroutine(Run(startId));
            return true;
        }

        /// <summary>UI 클릭/탭 시 호출.</summary>
        public void Advance() { _advance = true; }

        /// <summary>스킵 버튼: 현재 대사 시퀀스를 즉시 종료.</summary>
        public void SkipAll()
        {
            if (!IsPlaying) return;
            _skip = true;
            _advance = true;
        }

        IEnumerator Run(string startId)
        {
            IsPlaying = true;
            _skip = false;
            string cur = startId;
            while (!_skip && !string.IsNullOrEmpty(cur) && table.TryGetValue(cur, out var e))
            {
                OnLine?.Invoke(e);
                _advance = false;
                while (!_advance) yield return null;
                cur = e.next;
            }
            IsPlaying = false;
            _skip = false;
            SaveSystemManager.I?.MarkDialogueSeen(startId);
            OnEnd?.Invoke();
        }
    }
}
