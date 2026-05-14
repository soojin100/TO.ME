using System.Collections.Generic;
using UnityEngine;

namespace TOME.Core
{
    public class ObjectPool
    {
        readonly GameObject prefab;
        readonly Transform parent;
        readonly Queue<GameObject> q = new();

        public ObjectPool(GameObject prefab, int prewarm, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++) Release(Object.Instantiate(prefab, parent));
        }

        public GameObject Get(Vector3 pos, Quaternion rot)
        {
            GameObject go = q.Count > 0 ? q.Dequeue() : Object.Instantiate(prefab, parent);
            var t = go.transform;
            t.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject go)
        {
            go.SetActive(false);
            q.Enqueue(go);
        }
    }
}
