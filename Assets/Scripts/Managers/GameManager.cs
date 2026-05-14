using System.Collections;
using UnityEngine;
using TOME.Core;
using TOME.Data;

namespace TOME.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public NodeSO  CurrentNode  { get; private set; }
        public StageSO CurrentStage { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this; DontDestroyOnLoad(gameObject);
        }

        public void EnterStage(NodeSO node, StageSO stage)
        {
            CurrentNode  = node;
            CurrentStage = stage;
            StartCoroutine(SceneLoader.LoadAsync(SceneKeys.Stage));
        }

        public void ReturnToMap()
        {
            StartCoroutine(SceneLoader.LoadAsync(SceneKeys.Map));
        }
    }
}
