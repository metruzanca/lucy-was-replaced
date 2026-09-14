using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnlockBox : MonoBehaviour, ITooltipHandler, IPointerEnterHandler, IEventSystemHandler
{
	public enum UnlockState
	{
		None,
		Unlocked,
		Locked,
		Unlockable,
		Upgradable,
		TooHigh
	}

	[SerializeField]
	private float animDuration = 0.1f;

	[SerializeField]
	private float anumScale = 1.5f;

	[SerializeField]
	private AnimationCurve animCurve;

	[NonSerialized]
	public UnlockSO unlockSO;

	public List<UnlockBox> children = new List<UnlockBox>();

	private Action updateTooltipCallback;

	[SerializeField]
	private Image background;

	[SerializeField]
	private ColoredButton button;

	[SerializeField]
	private TextMeshProUGUI codeText;

	[SerializeField]
	private Image image;

	[SerializeField]
	private RectTransform semiUnlockedRing;

	[SerializeField]
	private GameObject multiUnlockedPanel;

	[SerializeField]
	private TextMeshProUGUI multiUnlockText;

	[SerializeField]
	private float defaultHeight = 150f;

	[SerializeField]
	private float multiUnlockHeight = 225f;

	[NonSerialized]
	public List<Image> lines = new List<Image>();

	private ItemBlock currentCost;

	private UnlockState unlockState;

	private int prevNumUnlocked;

	public void SetupRec(bool unlockable, HashSet<string> openedUnlockDocs, ItemBlock inventory, Dictionary<string, int> unlocks, out bool allUnlocked)
	{
		int valueOrDefault = unlocks.GetValueOrDefault(unlockSO.unlockName, 0);
		UnlockState unlockState = ((valueOrDefault < unlockSO.maxUnlockLevel) ? ((!unlockable) ? UnlockState.Locked : ((valueOrDefault > 0) ? UnlockState.Upgradable : UnlockState.Unlockable)) : ((!unlockSO.IsMultiUnlock || valueOrDefault <= unlockSO.maxUnlockLevel) ? UnlockState.Unlocked : UnlockState.TooHigh));
		ColorTheme theme = ThemeManager.Inst.Theme;
		if (unlockState != this.unlockState || prevNumUnlocked != valueOrDefault)
		{
			this.unlockState = unlockState;
			currentCost = MainSim.Inst.GetUnlockCost(unlockSO);
			if (valueOrDefault == 1 && !openedUnlockDocs.Contains(unlockSO.docs) && !string.IsNullOrEmpty(unlockSO.docs) && !MainSim.Inst.MightBeSimulating())
			{
				MainSim.Inst.workspace.OpenAndGoTo(unlockSO.docs);
				openedUnlockDocs.Add(unlockSO.docs);
			}
			else if (valueOrDefault == 2 && unlockSO.unlockName == "expand" && !openedUnlockDocs.Contains("docs/unlocks/expand_2.md") && !MainSim.Inst.MightBeSimulating())
			{
				MainSim.Inst.workspace.OpenAndGoTo("docs/unlocks/expand_2.md");
				openedUnlockDocs.Add("docs/unlocks/expand_2.md");
			}
			if (unlockSO.mesh != null)
			{
				image.gameObject.SetActive(value: true);
				image.sprite = Resources.Load<Sprite>("UnlockTextures/" + unlockSO.unlockName);
				image.color = (unlockable ? Color.white : new Color(1f, 1f, 1f, 0.1f));
			}
			else
			{
				((TMP_Text)codeText).text = CodeUtilities.SyntaxColor2(unlockSO.displayCode, theme);
				Color32 textColor = theme.code.text.TextColor;
				if (!unlockable)
				{
					textColor.a = 26;
				}
				((Graphic)(object)codeText).color = textColor;
			}
			semiUnlockedRing.gameObject.SetActive(valueOrDefault > 0 && unlockSO.IsMultiUnlock);
			semiUnlockedRing.sizeDelta = new Vector2(semiUnlockedRing.sizeDelta.x, multiUnlockHeight * ((float)valueOrDefault / (float)unlockSO.maxUnlockLevel));
			multiUnlockedPanel.SetActive(unlockSO.IsMultiUnlock);
			Color color = theme.unlocks.UnlockCountTextColor;
			if (!unlockable)
			{
				color.a = 0.1f;
			}
			((Graphic)(object)multiUnlockText).color = color;
			((TMP_Text)multiUnlockText).text = $"{valueOrDefault} / {unlockSO.maxUnlockLevel}";
			RectTransform component = GetComponent<RectTransform>();
			component.sizeDelta = new Vector2(component.sizeDelta.x, unlockSO.IsMultiUnlock ? multiUnlockHeight : defaultHeight);
			foreach (Image line in lines)
			{
				line.color = ((valueOrDefault > 0) ? theme.unlocks.LineUnlockedColor : theme.unlocks.LineLockedColor);
			}
			button.Interactable = this.unlockState == UnlockState.Unlockable || this.unlockState == UnlockState.Upgradable;
			if (this.unlockState == UnlockState.TooHigh)
			{
				Achievements.enabled = false;
				button.Text = "TOO HIGH";
				button.Interactable = true;
			}
			if (updateTooltipCallback != null)
			{
				updateTooltipCallback();
			}
			prevNumUnlocked = valueOrDefault;
		}
		if (this.unlockState == UnlockState.Unlockable || this.unlockState == UnlockState.Upgradable)
		{
			if (inventory.Contains(currentCost))
			{
				button.OverrideColors = theme.unlocks.unlockable.ToColorBlock();
			}
			else
			{
				button.OverrideColors = theme.unlocks.unaffordable.ToColorBlock();
			}
		}
		else
		{
			ColorBlock value = theme.unlocks.unlockable.ToColorBlock();
			if (this.unlockState == UnlockState.Unlocked)
			{
				value.disabledColor = theme.unlocks.AlreadyUnlockedColor;
			}
			else
			{
				value.disabledColor = theme.unlocks.NotUnlockableColor;
			}
			button.OverrideColors = value;
		}
		background.color = theme.unlocks.BoxBackgroundColor;
		if (semiUnlockedRing.TryGetComponent<Image>(out var component2))
		{
			component2.color = theme.unlocks.AlreadyUnlockedColor;
		}
		if (multiUnlockedPanel.TryGetComponent<Image>(out var component3))
		{
			component3.color = theme.unlocks.UnlockCountBackgroundColor;
		}
		SetupChildren(unlockable && valueOrDefault > 0, openedUnlockDocs, inventory, unlocks, out allUnlocked);
		if (valueOrDefault < unlockSO.maxUnlockLevel)
		{
			allUnlocked = false;
		}
	}

	private void SetupChildren(bool unlockable, HashSet<string> openedUnlockDocs, ItemBlock inventory, Dictionary<string, int> unlocks, out bool allUnlocked)
	{
		allUnlocked = true;
		foreach (UnlockBox child in children)
		{
			child.SetupRec(unlockable, openedUnlockDocs, inventory, unlocks, out var allUnlocked2);
			allUnlocked &= allUnlocked2;
		}
	}

	public void ButtonClicked()
	{
		if (NumUnlocked() > unlockSO.maxUnlockLevel)
		{
			Achievements.enabled = true;
			MainSim.Inst.ResetUnlocksToMax();
		}
		else if (MainSim.Inst.UnlockOrUpgrade(unlockSO))
		{
			FMODSoundManager.PlaySound(SoundEffectType.Unlock, Vector3.zero);
			if (!string.IsNullOrEmpty(unlockSO.unlockedHat))
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat(unlockSO.unlockedHat));
			}
		}
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		this.updateTooltipCallback = updateTooltipCallback;
		return TooltipUtils.UnlockTooltip(unlockSO.unlockName);
	}

	public void TooltipGone()
	{
		updateTooltipCallback = null;
	}

	private int NumUnlocked()
	{
		return MainSim.Inst.NumUnlocked(unlockSO.unlockName);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!LeanTween.isTweening(base.gameObject))
		{
			LeanTween.scale(base.gameObject, new Vector3(anumScale, anumScale, 1f), animDuration).setEase(animCurve);
		}
	}

	private void OnEnable()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDisable()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		unlockState = UnlockState.None;
		prevNumUnlocked = 0;
	}
}
