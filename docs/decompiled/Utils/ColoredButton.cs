using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class ColoredButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler, ITooltipHandler
{
	[SerializeField]
	private Image buttonPanel;

	[SerializeField]
	private Button button;

	[SerializeField]
	private TextMeshProUGUI nameText;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private AudioClip pressedSound;

	[SerializeField]
	private AudioClip hoverSound;

	[SerializeField]
	private float holdDuration;

	[SerializeField]
	private Transform tooltipPos;

	[SerializeField]
	private TooltipInfo.Anchor tooltipAnchor;

	public string tooltipName;

	public string tooltipDescription;

	private bool _isRunning;

	public UnityEvent onHeld = new UnityEvent();

	private ColorBlock? overrideColors;

	public Button.ButtonClickedEvent OnClick => button.onClick;

	public bool Interactable
	{
		get
		{
			return button.interactable;
		}
		set
		{
			button.interactable = value;
		}
	}

	public bool IsRunning
	{
		get
		{
			return _isRunning;
		}
		set
		{
			if (_isRunning != value)
			{
				_isRunning = value;
				UpdateColor();
			}
		}
	}

	public string Text
	{
		get
		{
			return ((TMP_Text)nameText).text;
		}
		set
		{
			((TMP_Text)nameText).text = value;
		}
	}

	public ColorBlock? OverrideColors
	{
		get
		{
			return overrideColors;
		}
		set
		{
			overrideColors = value;
			UpdateColor();
		}
	}

	private void Start()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		if ((UnityEngine.Object)(object)nameText != null)
		{
			((Graphic)(object)nameText).color = theme.ui.ButtonTextColor;
		}
		if (icon != null)
		{
			icon.color = theme.ui.ButtonTextColor;
		}
		button.colors = theme.ui.button.ToColorBlock();
		UpdateColor();
	}

	public void UpdateColor(float brightness = 1f)
	{
		ColorTheme theme = ThemeManager.Inst.Theme;
		ColorBlock? colorBlock = OverrideColors;
		ColorBlock colors;
		if (!colorBlock.HasValue)
		{
			colors = (IsRunning ? theme.code.button_exec.ToColorBlock() : theme.ui.button.ToColorBlock());
		}
		else
		{
			ColorBlock valueOrDefault = colorBlock.GetValueOrDefault();
			colors = valueOrDefault;
		}
		colors.normalColor = Color.Lerp(colors.normalColor, colors.pressedColor, brightness - 1f);
		button.colors = colors;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (Interactable)
		{
			FMODSoundManager.PlaySound(SoundEffectType.ButtonHovered, default(Vector3));
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (Interactable)
		{
			FMODSoundManager.PlaySound(SoundEffectType.ButtonPressed, default(Vector3));
			if (holdDuration > 0f)
			{
				TimerManager.StartTimer(OnHoldFinish, holdDuration * Time.timeScale);
			}
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (holdDuration > 0f)
		{
			TimerManager.StopTimer(OnHoldFinish);
		}
	}

	private void OnHoldFinish()
	{
		onHeld.Invoke();
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		return new TooltipInfo(Localizer.Localize(tooltipDescription), 0.4f, tooltipPos.position, tooltipAnchor);
	}

	public void TooltipGone()
	{
	}
}
