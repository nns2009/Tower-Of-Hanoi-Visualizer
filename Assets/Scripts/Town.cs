using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Town : MonoBehaviour
{
	[Header("Visuals")]
	public Sprite sprite;
	public Material material;
	public string sortingLayerName;
	[Range(0, 2)]
	public float MinBrightness, MaxBrightness;
	[Range(0, 1000)]
	public int colorI;

	[Header("Distribution")]
	public Vector2 MinSize, MaxSize;

	[Range(1, 100)]
	public int XRolls = 1;
	public float XSpan, YShift;
	public int HousesCount;
	public int Seed;

    public void Recreate()
    {
		while (transform.childCount > 0)
			DestroyImmediate(transform.GetChild(0).gameObject);

		var randomState = Random.state;
		Random.InitState(Seed);

		System.Func<float> xRoll = () => Random.Range(-XSpan, XSpan);

		float[] xs = new float[HousesCount];

		for (int i = 0; i < HousesCount; i++)
        {
			var go = new GameObject("House " + i);
			go.transform.parent = transform;

			float x = xRoll();
			if (i != 0)
            {
				System.Func<float, float> scorer =
					v => xs.Take(i).Select(ix => Mathf.Abs(ix - v)).Min();

				float xScore = scorer(x);
				for (int j = 1; j < XRolls; j++)
                {
					float mx = xRoll();
					float mxScore = scorer(mx);

					if (mxScore > xScore)
                    {
						x = mx;
						xScore = mxScore;
                    }
                }
            }
			xs[i] = x;
			float w = Random.Range(MinSize.x, MaxSize.x);
			float h = Random.Range(MinSize.y, MaxSize.y);
			float b = Random.Range(MinBrightness, MaxBrightness); // colorI / 1000f; 

			var sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = sprite;
			sr.material = material;
			sr.sortingLayerName = sortingLayerName;
			sr.drawMode = SpriteDrawMode.Sliced;
			sr.size = new Vector2(w, h);
			sr.color = Color.HSVToRGB(0, 0, b); // Color.Lerp(Color.black, Color.white, b); 

			go.transform.localPosition = new Vector3(x, h / 2 + YShift);
        }

		Random.state = randomState;

		//for (int i = 0; i < 100; i++)
  //      {
		//	Debug.Log(i + ": " + Color.HSVToRGB(0, 0, (float)i / 200));
  //      }
	}

    private void OnDrawGizmosSelected()
    {
		Debug.DrawLine(
			transform.position - Vector3.right * XSpan,
			transform.position + Vector3.right * XSpan,
			Color.red);
		Debug.DrawLine(
			transform.position,
			transform.position + Vector3.up * YShift,
			Color.yellow);
    }
}

[CustomEditor(typeof(Town))]
public class TownEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var man = (Town)target;

		EditorGUI.BeginChangeCheck();
		base.OnInspectorGUI();

		EditorGUILayout.BeginHorizontal();
		bool pressedRecreate = GUILayout.Button("Recreate");
		EditorGUILayout.EndHorizontal();

		if (EditorGUI.EndChangeCheck() || pressedRecreate)
		{
			man.Recreate();
		}
	}
}
