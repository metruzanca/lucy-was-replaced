using UnityEngine;

public class WarningIcon : MonoBehaviour
{
	private RectTransform rectTransform;

	private bool popUp;

	private bool dismiss;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		rectTransform.localScale = Vector3.zero;
	}

	private void Update()
	{
		if (popUp)
		{
			LeanTween.cancel(rectTransform);
			LeanTween.scale(rectTransform, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
			popUp = false;
		}
		if (dismiss)
		{
			LeanTween.cancel(rectTransform);
			LeanTween.scale(rectTransform, Vector3.zero, 0.3f).setEase(LeanTweenType.easeInBack);
			dismiss = false;
		}
	}

	public void PopUp()
	{
		popUp = true;
		dismiss = false;
	}

	public void Dismiss()
	{
		dismiss = true;
		popUp = false;
	}

	public void Pressed()
	{
		Dismiss();
		MainSim.Inst.workspace.OpenAndGoTo("docs/output.md");
	}
}
