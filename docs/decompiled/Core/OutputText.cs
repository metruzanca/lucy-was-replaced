using TMPro;
using UnityEngine;

[RequireComponent(typeof(CodeInputField))]
public class OutputText : MonoBehaviour
{
	private bool dirty;

	private CodeInputField txt;

	public CodeInputField Text
	{
		get
		{
			if (txt == null)
			{
				TryGetComponent<CodeInputField>(out txt);
			}
			return txt;
		}
	}

	private void Update()
	{
		if (dirty)
		{
			txt.text = Logger.GetOutputString();
			dirty = false;
		}
	}

	private void Start()
	{
		dirty = true;
		Logger.OnOutputChanged += OutputChanged;
	}

	private void OnDestroy()
	{
		Logger.OnOutputChanged -= OutputChanged;
	}

	private void OutputChanged()
	{
		dirty = true;
	}
}
