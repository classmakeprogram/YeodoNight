using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹(또는 씬 템플릿) 기반 초경량 오브젝트 풀.
/// Instantiate/Destroy 반복 대신 재사용해서 GC 스파이크를 줄인다.
/// </summary>
public class SimplePool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Stack<GameObject> idle = new Stack<GameObject>();

    public SimplePool(GameObject prefab, Transform parent = null, int prewarm = 0)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < prewarm; i++)
            idle.Push(Create());
    }

    private GameObject Create()
    {
        GameObject go = Object.Instantiate(prefab, parent);
        go.SetActive(false);
        return go;
    }

    /// <summary>풀에서 꺼내 배치하고 활성화한다.</summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject go = idle.Count > 0 ? idle.Pop() : Create();
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    /// <summary>다 쓴 오브젝트를 풀에 반환한다.</summary>
    public void Release(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        if (parent != null) go.transform.SetParent(parent, false);
        idle.Push(go);
    }
}
