using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Reflection;

public class GuaranteedModule : MonoBehaviour
{
    public KMSelectable LButton, RButton, Parent, BottomFaceSel;
    public Transform[] Anchors;
    public int AnchorsLength;
    public Transform Divider;
    public KMAudio Audio;

    private Transform _cover, _topFace, _bottomFace, _bonusAnchor;
    private int _animState, _currentPage;
    private Dictionary<Transform, Transform> _modules = new Dictionary<Transform, Transform>();
    private List<List<Transform>> _pages;
    private List<bool> _emptyPages;

    private void Awake()
    {
        _pages = Anchors.Take(Anchors.Length - 1).SplitLength(AnchorsLength).Select(e => e.ToList()).ToList();
        _emptyPages = Enumerable.Repeat(true, _pages.Count).ToList();
        _bonusAnchor = Anchors.Last();
        _bonusAnchor.gameObject.SetActive(false);

        _cover = transform.GetChild(0).GetChild(7);
        _topFace = transform.GetChild(0).GetChild(3).GetChild(0);
        _bottomFace = transform.GetChild(0).GetChild(3).GetChild(1);

        Type type = Type.GetType("ExcludeFromStaticBatch, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        transform.GetChild(0).gameObject.AddComponent(type);
    }

    private void Start()
    {
        StartCoroutine(Init());

        LButton.OnInteract += delegate ()
        {
            Debug.Log("[Cursed Casings] Casing button pressed.");
            StartCoroutine(Animate(false));
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, LButton.transform);
            return false;
        };

        RButton.OnInteract += delegate ()
        {
            Debug.Log("[Cursed Casings] Casing button pressed.");
            StartCoroutine(Animate(true));
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, RButton.transform);
            return false;
        };
    }

    private IEnumerator Animate(bool forwards)
    {
        if(_animState == 1)
            yield break;
        _animState = 1;

        int prevPage = _currentPage;
        do
        {
            _currentPage += forwards ? 1 : _pages.Count - 1;
            _currentPage %= _pages.Count;
        }
        while(_emptyPages[_currentPage] == true);

        float time = Time.time;
        while(Time.time - time < .5f)
        {
            _cover.gameObject.transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, Mathf.Min(1, 4 * (Time.time - time))), 1f, 1f);
            _pages[prevPage].ForEach(t => t.localPosition = new Vector3(t.localPosition.x, Mathf.Lerp(0f, .2f, Mathf.Max(0f, 1f - 2f * (Time.time - time))), t.localPosition.z));
            Divider.localPosition = new Vector3(0f, Mathf.Lerp(0f, .2f, Mathf.Max(0f, 2f * (Time.time - time) - 1f, 1f - 2f * (Time.time - time))), 0f);
            UpdateAnchors();

            yield return null;
        }

        _pages[prevPage].ForEach(t => t.localScale = Vector3.zero);
        _pages[_currentPage].ForEach(t => t.localScale = 1.2f * Vector3.one);

        while(Time.time - time < 1f)
        {
            _cover.gameObject.transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, Mathf.Min(1, 4 - 4 * (Time.time - time))), 1f, 1f);
            _pages[_currentPage].ForEach(t => t.localPosition = new Vector3(t.localPosition.x, Mathf.Lerp(0f, .2f, Mathf.Max(0f, 2f * (Time.time - time) - 1f)), t.localPosition.z));
            Divider.localPosition = new Vector3(0f, Mathf.Lerp(0f, .2f, Mathf.Max(0f, 2f * (Time.time - time) - 1f, 1f - 2f * (Time.time - time))), 0f);
            UpdateAnchors();

            yield return null;
        }

        _pages[_currentPage].ForEach(t => t.localPosition = new Vector3(t.localPosition.x, .2f, t.localPosition.z));

        UpdateAnchors();

        _animState = 0;
    }

    private void UpdateAnchors()
    {
        foreach(KeyValuePair<Transform, Transform> kvp in _modules)
        {
            kvp.Value.position = kvp.Key.position;
            kvp.Value.localScale = kvp.Key.localScale;
        }
    }

    private IEnumerator Init()
    {
        /*
#if !UNITY_EDITOR
        Component state = FindObjectsOfType<Component>().Where(c => c.GetType().Name.Contains("GameplayState")).First();
        PropertyInfo prop = state.GetType().GetProperty("Bomb", BindingFlags.Public | BindingFlags.Instance);
        yield return new WaitUntil(() => prop.GetValue(state, new object[0]) != null);
#endif
        */
        yield return null;
        yield return new WaitForSeconds(1f);
        yield return null;

        _modules.Add(transform.GetChild(0).GetChild(3).GetChild(2), transform.GetChild(0).GetChild(9));
        transform.GetChild(0).GetChild(9).localScale = Vector3.zero;

        List<Transform> modules = new List<Transform>();
        List<Transform> empties = new List<Transform>();

        Transform trt = transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1);
        if(trt.gameObject.name.ContainsIgnoreCase("Timer") && !trt.gameObject.name.ContainsIgnoreCase("Module"))
        {
            _modules[transform.GetChild(0).GetChild(3).GetChild(2)] = trt;
        }
        else
        {
            if(trt.gameObject.name.ContainsIgnoreCase("Empty"))
            {
                empties.Add(trt);
            }
            else
            {
                _emptyPages[(transform.GetChild(0).childCount - 11) / AnchorsLength] = false;
                modules.Add(trt);
            }
        }

        for(int i = transform.GetChild(0).childCount - 2; i > 9; i--)
        {
            Transform tr = transform.GetChild(0).GetChild(i);
            if(tr.gameObject.name.ContainsIgnoreCase("Empty"))
            {
                empties.Add(tr);
            }
            else
            {
                _emptyPages[(i - 10) / AnchorsLength] = false;
                modules.Add(tr);
            }
        }

        modules = modules.Shuffle();

        for(int i = 0; i < modules.Count; i++)
            _modules.Add(Anchors[i], modules[i]);
        for(int i = 0; i < empties.Count; i++)
            _modules.Add(Anchors[i + modules.Count], empties[i]);

        Component obj = Parent.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        Component buttonObj = LButton.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        Component button2Obj = RButton.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        Component bottomObj = _bottomFace.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        FieldInfo fld = obj.GetType().GetField("Children", BindingFlags.Instance | BindingFlags.Public);
        Array initial = (Array)fld.GetValue(obj);
        Array bottomInitial = (Array)fld.GetValue(bottomObj);
        Array final = Array.CreateInstance(fld.FieldType.GetElementType(), initial.Length + 3 + bottomInitial.Length);
        initial.CopyTo(final, 0);
        bottomInitial.CopyTo(final, initial.Length + 3);
        final.SetValue(buttonObj, initial.Length);
        final.SetValue(null, initial.Length + 1);
        final.SetValue(button2Obj, initial.Length + 2);

        FieldInfo fldParent = obj.GetType().GetField("Parent", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        foreach(object mod in initial)
        {
            if(mod != null)
                fldParent.SetValue(mod, obj);
        }

        obj.GetType().GetMethod("DeactivateImmediateChildSelectableAreas", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        bottomObj.GetType().GetMethod("DeactivateImmediateChildSelectableAreas", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        fld.SetValue(bottomObj, Array.CreateInstance(fld.FieldType.GetElementType(), 0));
        fld.SetValue(obj, final);
        obj.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        bottomObj.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);

        yield return null;

        Enumerable.Range(1, _pages.Count - 1).ToList().ForEach(j => _pages[j].ForEach(t => { t.localScale = Vector3.zero; t.localPosition = new Vector3(t.localPosition.x, 0f, t.localPosition.z); }));
        _pages[0].ForEach(t => { t.localScale = 1.2f * Vector3.one; t.localPosition = new Vector3(t.localPosition.x, .2f, t.localPosition.z); });
        UpdateAnchors();

        /*
        yield return new WaitForSecondsRealtime(20f);
        PrintHierarchy(1);
    }
    
    public static void PrintHierarchy(int indent = 4)
    {
        foreach(GameObject g in FindObjectsOfType<GameObject>().Where(g => !g.transform.parent).ToArray())
            PrintHierarchy(g, indent);
    }

    public static void PrintHierarchy(GameObject obj, int indent = 4, ushort depth = 0)
    {
        string space = new string(Enumerable.Repeat(' ', indent * depth).ToArray());

        if(obj != null)
        {
            Debug.Log(space + obj.name);
            Debug.LogWarning(space + obj.GetComponents<Component>().Select(c => c.GetType().Name + "::" + LookReflection(c)).Join("///"));
        }

        foreach(Transform child in obj.transform)
            PrintHierarchy(child.gameObject, indent, (ushort)(depth + 1));
    }

    public static string LookReflection(object o)
    {
        return o.GetType().GetFields().Select(m => m.Name + ":" + m.ReflectedType + ":" + m.GetValue(o)).Join("/");
        */
    }
}

public static class Ex
{
    public static IEnumerable<IEnumerable<T>> SplitLength<T>(this IEnumerable<T> obj, int len)
    {
        int i;
        for(i = 0; i < obj.Count() - len; i += len)
        {
            yield return obj.Skip(i).Take(len);
        }
        if(obj.Skip(i).Count() > 0)
            yield return obj.Skip(i);
    }
}