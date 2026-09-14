using System;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindOptionUI : OptionUI
{
	[SerializeField]
	private ColoredButton button;

	private OptionSO optionSO;

	private bool active;

	private KeyCombination enteringKeyCombination;

	public override void Setup(OptionSO optionSO)
	{
		base.Setup(optionSO);
		this.optionSO = optionSO;
		UpdateValue();
		button.GetComponent<Button>().onClick.AddListener(Clicked);
	}

	public override void UpdateValue()
	{
		button.Text = OptionHolder.GetKeyCombination(optionName).ToString(display: true);
	}

	public void Clicked()
	{
		enteringKeyCombination = new KeyCombination(KeyCode.None, alt: false, ctrl: false, shift: false);
		active = true;
		button.Text = "<Enter Key>";
	}

	private void Update()
	{
		if (!active || !Input.anyKeyDown)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
		{
			enteringKeyCombination.shift = true;
			button.Text = enteringKeyCombination.ToString(display: true);
			return;
		}
		if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
		{
			enteringKeyCombination.ctrl = true;
			button.Text = enteringKeyCombination.ToString(display: true);
			return;
		}
		if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
		{
			enteringKeyCombination.alt = true;
			button.Text = enteringKeyCombination.ToString(display: true);
			return;
		}
		KeyCode keyCode = KeyCode.None;
		foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKeyDown(value))
			{
				keyCode = value;
			}
		}
		if (((KeyBindOptionSO)optionSO).canBeMouseButton || (keyCode != KeyCode.Mouse0 && keyCode != KeyCode.Mouse1 && keyCode != KeyCode.Mouse2))
		{
			enteringKeyCombination.key = keyCode;
			OptionHolder.SetOption(optionSO.optionName, enteringKeyCombination);
			active = false;
		}
	}

	private void OnDisable()
	{
		if (active)
		{
			active = false;
			UpdateValue();
		}
	}
}
