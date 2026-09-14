using UnityEngine;

[RequireComponent(typeof(ColoredButton))]
public class FlashingButton : MonoBehaviour
{
	private ColoredButton coloredButton;

	[SerializeField]
	private float brightness = 0.2f;

	[SerializeField]
	private float flashSpeed = 1f;

	private void Awake()
	{
		coloredButton = GetComponent<ColoredButton>();
		coloredButton.OnClick.AddListener(OnButtonPressed);
	}

	private void Update()
	{
		if (MainSim.Inst.IsUnlocked("loops"))
		{
			base.enabled = false;
		}
		else if (MainSim.Inst.GetNumItem(StringIds.GetItemId("hay")) >= 1.0)
		{
			coloredButton.UpdateColor(1.2f + brightness * (Mathf.Sin(Time.time * flashSpeed) + 1f));
		}
	}

	private void OnButtonPressed()
	{
		base.enabled = false;
	}
}
