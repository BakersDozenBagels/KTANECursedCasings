using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

public class BingoCaseScript : MonoBehaviour
{
    public KMSelectable TopFaceSel, BottomFaceSel;
    public Transform[] Anchors;
    public Transform TimerAnchor;
    public KMAudio Audio;
    public TextMesh[] Texts;

    private static readonly Harmony _harm = new Harmony("Ktane-Cursed-Casings-Bingo");
    private readonly List<Transform> _allAnchors = new List<Transform>();
    private readonly List<bool> _solved = new List<bool>();
    private static readonly List<Color> _colors = new List<Color>()
    {
        Color.black,
        Color.red,
        new Color(255, 165, 0),
        Color.yellow,
        Color.green,
        Color.blue,
        new Color(128, 0, 128),
        new Color(231, 84, 128)
    };
    private readonly List<Component> _modules = new List<Component>();
    private static Type _bcType;
    private static bool _strikeAllowed = true, _harmed;
    private bool _solving;

    private void Awake()
    {
#if !UNITY_EDITOR
        Type type = Type.GetType("ExcludeFromStaticBatch, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        transform.GetChild(0).gameObject.AddComponent(type);

        if(_harmed)
            return;

        _bcType = ReflectionHelper.FindTypeInGame("BombComponent");
        _harm.Patch(_bcType.Method("HandleStrike"), new HarmonyMethod(GetType().Method("Prefix")));
        _harmed = true;
    }

    private void Start()
    {
        StartCoroutine(Init());
#endif
    }

    private IEnumerator Init()
    {
        yield return new WaitForSeconds(1f);
        yield return null;

        Debug.Log("[Cursed Casings] Bingo casing loaded.");

        List<Transform> shuf = Anchors.OrderBy(t => UnityEngine.Random.Range(0, int.MaxValue)).ToList();

        Transform timer = transform.GetChild(0).GetChild(5);
        timer.localScale = Vector3.zero;
        Transform trt = transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1);
        if(trt.gameObject.name.ContainsIgnoreCase("Timer") && !trt.gameObject.name.ContainsIgnoreCase("Module"))
            timer = trt;
        else
        {
            trt.localPosition = shuf[0].localPosition;
            trt.localScale = shuf[0].localScale;
        }
        timer.localScale = TimerAnchor.localScale;
        timer.localPosition = TimerAnchor.localPosition;

        for(int i = 1; i < shuf.Count; i++)
        {
            Transform tr = transform.GetChild(0).GetChild(i + 5);
            //if(tr.GetComponent(bcType) == null)
            //{
            //    i++;
            //    continue;
            //}
            tr.localPosition = shuf[i].localPosition;
            tr.localScale = shuf[i].localScale;
        }

        Component obj = TopFaceSel.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        Component bottomObj = BottomFaceSel.GetComponents<Component>().Where(c => c.GetType().Name.Contains("ModSelectable") && !c.GetType().Name.Contains("KM")).First();
        FieldInfo fld = obj.GetType().GetField("Children", BindingFlags.Instance | BindingFlags.Public);
        Array initial = (Array)fld.GetValue(obj);
        Array bottomInitial = (Array)fld.GetValue(bottomObj);
        Array final = Array.CreateInstance(fld.FieldType.GetElementType(), initial.Length + bottomInitial.Length);
        initial.CopyTo(final, 0);
        bottomInitial.CopyTo(final, initial.Length);

        FieldInfo fldParent = obj.GetType().GetField("Parent", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        foreach(object mod in final)
            if(mod != null)
                fldParent.SetValue(mod, obj);

        obj.GetType().GetMethod("DeactivateImmediateChildSelectableAreas", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        bottomObj.GetType().GetMethod("DeactivateImmediateChildSelectableAreas", BindingFlags.Instance | BindingFlags.Public).Invoke(bottomObj, new object[0]);
        fld.SetValue(bottomObj, Array.CreateInstance(fld.FieldType.GetElementType(), 0));
        fld.SetValue(obj, final);
        obj.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(obj, new object[0]);
        bottomObj.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public).Invoke(bottomObj, new object[0]);

        _allAnchors.AddRange(Anchors);
        _allAnchors.Insert(12, TimerAnchor);
        _solved.AddRange(Enumerable.Repeat(true, 25));

        Type ncType = ReflectionHelper.FindTypeInGame("NeedyComponent");
        FieldInfo ctFld = _bcType.GetField("ComponentType", ReflectionHelper.Flags);
        FieldInfo opFld = _bcType.GetField("OnPass", ReflectionHelper.Flags);
        _delType = opFld.FieldType;
        foreach(Component bc in GetComponentsInChildren(_bcType).Where(c => !c.GetType().Equals(ncType) && (((int)ctFld.GetValue(c)) > 1)))
            opFld.SetValue(bc, Delegate.Combine((Delegate)opFld.GetValue(bc), SolveCheck(bc)));
    }

    private Type _delType;

    private Delegate SolveCheck(Component bc)
    {
        List<float> dists = _allAnchors.Select(a => Vector3.Distance(a.position, bc.transform.position)).ToList();
        int ix = dists.IndexOf(dists.Min());
        _solved[ix] = false;
        _modules.Add(bc);
        return Delegate.CreateDelegate(
            _delType,
            new SolveCheckData { Case = this, Ix = ix },
            GetType().Method("SolveCheckStatic"));
    }

    private class SolveCheckData
    {
        public BingoCaseScript Case;
        public int Ix;
    }

    private static bool SolveCheckStatic(SolveCheckData data, object _)
    {
        data.Case._solved[data.Ix] = true;
        data.Case.CheckBingos();
        return false;
    }

    private void CheckBingos()
    {
        if(_solving)
            return;

        Debug.Log("[Cursed Casings] Module solved, checking bingo grid: " + _solved.Select(b => b ? "T" : "f").Join(""));

        for(int i = 0; i < 5; i++)
            if(_solved.Skip(5 * i).Take(5).All(b => b))
                goto bingo;
        for(int i = 0; i < 5; i++)
            if(_solved.Where((_, ix) => ix % 5 == i).All(b => b))
                goto bingo;
        if(_solved.Where((_, ix) => ix / 5 == ix % 5).All(b => b))
            goto bingo;
        if(_solved.Where((_, ix) => 4 - (ix / 5) == ix % 5).All(b => b))
            goto bingo;
        return;

        bingo:
        StartCoroutine(SolveBomb());
        return;
    }

    private IEnumerator SolveBomb()
    {
        _strikeAllowed = false;
        _solving = true;

        Debug.Log("[Cursed Casings] Bingo! Solving the bomb.");
        Audio.PlaySoundAtTransform("Bingo", transform);
        int iter = 0;
        while(iter != 75)
        {
            yield return new WaitForSeconds(.02f);
            int index = UnityEngine.Random.Range(0, 8);
            foreach(TextMesh t in Texts)
                t.color = _colors[index];
            iter++;
        }
        foreach(TextMesh t in Texts)
            t.color = _colors[4];

        MethodInfo pass = _bcType.Method("HandlePass");

        foreach(Component c in _modules)
        {
            yield return new WaitForSeconds(0.2f);
            pass.Invoke(c, new object[0]);
        }

        _strikeAllowed = true;
        _solving = false;
    }

    private void OnDestroy()
    {
        _strikeAllowed = true;
    }

    private static bool Prefix()
    {
        return _strikeAllowed;
    }
}