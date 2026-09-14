using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIScaler : MonoBehaviour
{
	[SerializeField]
	private List<CanvasScaler> canvases;

	private List<Vector2> startResolutions;

	private void Start()
	{
		startResolutions = Enumerable.ToList<Vector2>(Enumerable.Select<CanvasScaler, Vector2>((IEnumerable<CanvasScaler>)canvases, (Func<CanvasScaler, Vector2>)((CanvasScaler c) => c.referenceResolution)));
		SetScale("UI size");
		OptionHolder.OnOptionChanged += SetScale;
	}

	private void SetScale(string option)
	{
		if (option == "UI size")
		{
			float @float = OptionHolder.GetFloat("UI size");
			for (int i = 0; i < canvases.Count; i++)
			{
				canvases[i].referenceResolution = startResolutions[i] / new Vector2(1f, @float);
			}
		}
	}
}
