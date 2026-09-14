using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
	private const float tweenTime = 0f;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private GameObject container;

	[SerializeField]
	private CustomStandaloneInputModule inputModule;

	[SerializeField]
	private Image background;

	[SerializeField]
	private MarkdownText text;

	[SerializeField]
	private TextMeshProUGUI docsNotice;

	private Vector2 offset = new Vector2(-20f, -20f);

	private TooltipInfo info;

	private GameObject tooltipGameObject;

	public GameObject Container => container;

	public TooltipInfo Info => info;

	private void Update()
	{
		GameObject gameObject = inputModule.GetPointerData()?.pointerCurrentRaycast.gameObject;
		if (gameObject != tooltipGameObject)
		{
			if (tooltipGameObject != null)
			{
				tooltipGameObject.GetComponent<TooltipAccessor>()?.TooltipGone();
			}
			tooltipGameObject = gameObject;
			UpdateTooltip();
		}
		if (info != null)
		{
			UpdatePosition();
			if (!string.IsNullOrEmpty(info.docs) && Input.GetKeyDown(KeyCode.Mouse1))
			{
				MainSim.Inst.workspace.OpenAndGoTo(info.docs);
			}
		}
	}

	private void Start()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
		text.Setup("", delegate
		{
		});
	}

	private void OnDestroy()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		background.color = theme.ui.TooltipColor;
	}

	private void UpdateTooltip()
	{
		container.transform.localScale = Vector3.zero;
		TooltipAccessor tooltipAccessor = tooltipGameObject?.GetComponent<TooltipAccessor>();
		if (tooltipAccessor != null)
		{
			info = tooltipAccessor.GetTooltipInfo(UpdateTooltip);
			if (info != null)
			{
				ScheduleTooltip();
			}
		}
		else
		{
			info = null;
		}
	}

	private void ScheduleTooltip()
	{
		GameObject go = tooltipGameObject;
		if (info.delay <= 0f)
		{
			SetTooltip(go);
			return;
		}
		TimerManager.StartTimer(delegate
		{
			SetTooltip(go);
		}, info.delay);
	}

	private void SetTooltip(GameObject tooltipObject)
	{
		if (!(tooltipObject != tooltipGameObject) && info != null)
		{
			if (string.IsNullOrEmpty(info.text))
			{
				container.transform.localScale = Vector3.zero;
				return;
			}
			text.UpdateText(info.text);
			container.transform.localScale = Vector3.one;
			UpdatePosition();
			((Component)(object)docsNotice).gameObject.SetActive(!string.IsNullOrEmpty(info.docs));
		}
	}

	public bool CanShowTooltipImmediate()
	{
		if (!(tooltipGameObject == null))
		{
			return tooltipGameObject == MainSim.Inst.workspace.container.gameObject;
		}
		return true;
	}

	public void SetTooltipImmediate(TooltipInfo info)
	{
		if (CanShowTooltipImmediate())
		{
			this.info = info;
			tooltipGameObject = null;
			SetTooltip(null);
		}
	}

	public void CloseTooltip()
	{
		if (!string.IsNullOrEmpty(info?.text) && container.transform.localScale != Vector3.zero)
		{
			container.transform.localScale = Vector3.zero;
		}
	}

	private void UpdatePosition()
	{
		RectTransform rectTransform = (RectTransform)base.transform;
		Vector2 vector = offset;
		if (Input.mousePosition.x * 2f > (float)Screen.width)
		{
			TooltipInfo tooltipInfo = info;
			if (tooltipInfo == null || tooltipInfo.anchor != TooltipInfo.Anchor.BottomRight)
			{
				TooltipInfo tooltipInfo2 = info;
				if (tooltipInfo2 == null || tooltipInfo2.anchor != TooltipInfo.Anchor.TopRight)
				{
					goto IL_008d;
				}
			}
		}
		TooltipInfo tooltipInfo3 = info;
		Vector2 pivot = default(Vector2);
		if (tooltipInfo3 == null || tooltipInfo3.anchor != TooltipInfo.Anchor.BottomLeft)
		{
			TooltipInfo tooltipInfo4 = info;
			if (tooltipInfo4 == null || tooltipInfo4.anchor != TooltipInfo.Anchor.TopLeft)
			{
				pivot.x = 0f;
				vector *= new Vector2(-1f, 1f);
				goto IL_00bd;
			}
		}
		goto IL_008d;
		IL_00bd:
		if (Input.mousePosition.y * 2f > (float)Screen.height)
		{
			TooltipInfo tooltipInfo5 = info;
			if (tooltipInfo5 == null || tooltipInfo5.anchor != TooltipInfo.Anchor.TopLeft)
			{
				TooltipInfo tooltipInfo6 = info;
				if (tooltipInfo6 == null || tooltipInfo6.anchor != TooltipInfo.Anchor.TopRight)
				{
					goto IL_0137;
				}
			}
		}
		TooltipInfo tooltipInfo7 = info;
		if (tooltipInfo7 == null || tooltipInfo7.anchor != TooltipInfo.Anchor.BottomLeft)
		{
			TooltipInfo tooltipInfo8 = info;
			if (tooltipInfo8 == null || tooltipInfo8.anchor != TooltipInfo.Anchor.BottomRight)
			{
				pivot.y = 0f;
				vector *= new Vector2(1f, -1f);
				goto IL_0167;
			}
		}
		goto IL_0137;
		IL_0137:
		pivot.y = 1f;
		goto IL_0167;
		IL_008d:
		pivot.x = 1f;
		goto IL_00bd;
		IL_0167:
		rectTransform.pivot = pivot;
		rectTransform.anchoredPosition = ((Vector2)Input.mousePosition + vector) / canvas.scaleFactor;
		if (info != null && info.fixedPosition != Vector3.zero)
		{
			rectTransform.position = info.fixedPosition;
		}
	}
}
