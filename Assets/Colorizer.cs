using UnityEngine;
using Newtonsoft.Json;

public class Colorizer : MonoBehaviour {
    public MeshRenderer Renderer;
    public KMModSettings Settings;
    public Color[] Colors;

    public class MSJSON
    {
        public bool NoColors;
    }

	// Use this for initialization
	void Start () {
        try
        {
            Settings.RefreshSettings();
            MSJSON set = JsonConvert.DeserializeObject<MSJSON>(Settings.Settings);
            if (set != null) if (set.NoColors) Colors = new Color[] { new Color(0.494f, 0.125f, 0.125f, 1f) };
        }
        catch (JsonReaderException e)
        {
            Debug.LogFormat("[Cursed Bombs] JSON reading failed with error {0}, using default settings.", e.Message);
        }
        Renderer.material.color = Colors.PickRandom();
	}
}
