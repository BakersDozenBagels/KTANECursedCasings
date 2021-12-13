using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GuaranteedModule : MonoBehaviour
{
    public KMSelectable Button, Parent, BottomFaceSel;
    public Transform[] Anchors;
    public KMAudio Audio;

    private Transform _cover, _topFace, _bottomFace;
    private int _animState;
    private Dictionary<Transform, Transform> _modules = new Dictionary<Transform, Transform>();

    private void Awake()
    {
        _cover = transform.GetChild(0).GetChild(6);
        _topFace = transform.GetChild(0).GetChild(3).GetChild(0);
        _bottomFace = transform.GetChild(0).GetChild(3).GetChild(1);

#if !UNITY_EDITOR
        Type type = Type.GetType("ExcludeFromStaticBatch, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        transform.GetChild(0).gameObject.AddComponent(type);
#endif
    }

    private void Start()
    {
        StartCoroutine(Init());

        Button.OnInteract += delegate () {
            Debug.Log("[Cursed Casings] Casing button pressed.");
            StartCoroutine(Animate());
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, Button.transform);
            return false;
        };
    }

    private IEnumerator Animate()
    {
        if(_animState == 1 || _animState == 3)
            yield break;
        _animState++;
        bool forwards = _animState == 1;

        float time = Time.time;
        while(Time.time - time < 1f)
        {
            _cover.gameObject.transform.localScale = new Vector3(Mathf.Max(0f, 1f - 4f * Mathf.Abs(Time.time - time - .5f)), 1f, 1f);
            (forwards ? _topFace : _bottomFace).localPosition = new Vector3(0f, Mathf.Lerp(0f, .2f, Mathf.Max(0f, 1f - 2f * (Time.time - time))), 0f);
            (forwards ? _bottomFace : _topFace).localPosition = new Vector3(0f, Mathf.Lerp(0f, .2f, Mathf.Max(0f, 2f * (Time.time - time) - 1f)), 0f);
            foreach(KeyValuePair<Transform, Transform> kvp in _modules)
            {
                kvp.Value.position = kvp.Key.position;
            }

            yield return null;
        }

        (forwards ? _bottomFace : _topFace).localPosition = new Vector3(0f, .2f, 0f);

        foreach(KeyValuePair<Transform, Transform> kvp in _modules)
        {
            kvp.Value.position = kvp.Key.position;
        }

        _animState++;
        _animState %= 4;
    }

    private IEnumerator Init()
    {
        yield return new WaitForSeconds(1f);

        for(int i = transform.GetChild(0).childCount - 1; i > 6; i--)
        {
            Transform tr = transform.GetChild(0).GetChild(i);
            tr.localScale = 1.2f * Vector3.one;
            Transform p = Anchors.Where(a => (tr.position - a.position).magnitude < 0.0001f).FirstOrDefault();
            if(p != null)
            {
                _modules.Add(p, tr);
            }
            else
            {
                throw new Exception("A module spawned in a weird place. Error!");
            }
        }

#if !UNITY_EDITOR
        Component obj = Parent.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        Component buttonObj = Button.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        Component bottomObj = _bottomFace.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        FieldInfo fld = obj.GetType().GetField("Children", BindingFlags.Instance | BindingFlags.Public);
        Array initial = (Array)fld.GetValue(obj);
        Array bottomInitial = (Array)fld.GetValue(bottomObj);
        Array final = Array.CreateInstance(fld.FieldType.GetElementType(), initial.Length + 1 + bottomInitial.Length);
        initial.CopyTo(final, 0);
        bottomInitial.CopyTo(final, initial.Length + 1);
        final.SetValue(buttonObj, initial.Length);

        obj.GetType().GetMethod("DeactivateImmediateChildSelectableAreas", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        bottomObj.GetType().GetMethod("DeactivateImmediateChildSelectableAreas", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        fld.SetValue(bottomObj, Array.CreateInstance(fld.FieldType.GetElementType(), 0));
        fld.SetValue(obj, final);
        obj.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        bottomObj.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
#endif
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