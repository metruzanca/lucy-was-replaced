using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ResearchMenu : MonoBehaviour
{
	private class UnlockLayout
	{
		public int xOffset;

		public List<UnlockLayout> children;

		public UnlockBox box;

		public bool CollidesWith(UnlockLayout sibling)
		{
			HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
			FillTakenSet(hashSet);
			HashSet<Vector2Int> hashSet2 = new HashSet<Vector2Int>();
			sibling.FillTakenSet(hashSet2);
			hashSet.IntersectWith((IEnumerable<Vector2Int>)hashSet2);
			return hashSet.Count > 0;
		}

		private void FillTakenSet(HashSet<Vector2Int> taken, Vector2Int totalOffset = default(Vector2Int))
		{
			taken.Add(totalOffset + new Vector2Int(xOffset, 0));
			taken.Add(totalOffset + new Vector2Int(xOffset + 1, 0));
			foreach (UnlockLayout child in children)
			{
				child.FillTakenSet(taken, totalOffset + new Vector2Int(xOffset, -1));
			}
		}

		public int GetWidth()
		{
			return GetMaxXOffset() - GetMinXOffset();
		}

		private int GetMaxXOffset()
		{
			if (children.Count == 0)
			{
				return xOffset;
			}
			return Enumerable.Max<UnlockLayout>((IEnumerable<UnlockLayout>)children, (Func<UnlockLayout, int>)((UnlockLayout x) => x.GetMaxXOffset())) + xOffset;
		}

		private int GetMinXOffset()
		{
			if (children.Count == 0)
			{
				return xOffset;
			}
			return Enumerable.Min<UnlockLayout>((IEnumerable<UnlockLayout>)children, (Func<UnlockLayout, int>)((UnlockLayout x) => x.GetMinXOffset())) + xOffset;
		}

		public void SetUIPositions(float horizontalSpacing, float verticalSpacing, Vector2 startPosition, ref Rect boundary)
		{
			Vector2 vector = startPosition + new Vector2((float)xOffset * horizontalSpacing, 0f);
			((RectTransform)box.transform).anchoredPosition = vector;
			foreach (UnlockLayout child in children)
			{
				child.SetUIPositions(horizontalSpacing, verticalSpacing, startPosition + new Vector2((float)xOffset * horizontalSpacing, 0f - verticalSpacing), ref boundary);
			}
			boundary.min = Vector2.Min(boundary.min, vector);
			boundary.max = Vector2.Max(boundary.max, vector);
		}
	}

	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private Image background;

	[SerializeField]
	private ColoredButton openCloseButton;

	[SerializeField]
	private GameObject plusButton;

	[SerializeField]
	private GameObject infoButton;

	[SerializeField]
	private GameObject farmIcon;

	[SerializeField]
	private GameObject unlockIcon;

	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private UnlockBox researchPrefab;

	[SerializeField]
	private RectTransform linePrefab;

	[SerializeField]
	private float horizontalSpacing;

	[SerializeField]
	private float verticalSpacing;

	[SerializeField]
	private float paddingVertical;

	[SerializeField]
	private float paddingHorizontal;

	[SerializeField]
	private float zoomSpeed = 1.1f;

	[SerializeField]
	private float maxZoomScale = 2f;

	[SerializeField]
	private float minZoomScale = 0.1f;

	private Dictionary<string, UnlockBox> allBoxes = new Dictionary<string, UnlockBox>();

	private List<UnlockBox> rootUnlocks = new List<UnlockBox>();

	public HashSet<string> openedUnlockDocs = new HashSet<string>();

	public bool IsOpen => base.transform.GetChild(0).gameObject.activeInHierarchy;

	private void Start()
	{
		SetViewportOptions("viewport sliding");
		OptionHolder.OnOptionChanged += SetViewportOptions;
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		background.color = theme.unlocks.BackgroundColor;
	}

	private void Update()
	{
		ItemBlock inventory = MainSim.Inst.GetInventory();
		Dictionary<string, int> unlocks = MainSim.Inst.GetUnlocks();
		bool flag = true;
		foreach (UnlockBox rootUnlock in rootUnlocks)
		{
			rootUnlock.SetupRec(unlockable: true, openedUnlockDocs, inventory, unlocks, out var allUnlocked);
			flag = flag && allUnlocked;
		}
		if (!MainSim.Inst.leaderboardManager.IsRunning && flag && !MainSim.Inst.MightBeSimulating())
		{
			Achievements.UnlockAchievement("UNLOCK_EVERYTHING");
		}
	}

	private void SetViewportOptions(string option)
	{
		if (option == "viewport sliding")
		{
			scrollRect.inertia = OptionHolder.GetString("viewport sliding") == "enabled";
		}
	}

	public void Setup()
	{
		for (int num = container.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(container.GetChild(num).gameObject);
		}
		allBoxes.Clear();
		rootUnlocks.Clear();
		foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
		{
			if (allUnlock.enabled)
			{
				UnlockBox unlockBox = UnityEngine.Object.Instantiate(researchPrefab, container);
				unlockBox.unlockSO = allUnlock;
				if (string.IsNullOrEmpty(allUnlock.parentUnlock))
				{
					rootUnlocks.Add(unlockBox);
				}
				allBoxes[allUnlock.unlockName] = unlockBox;
			}
		}
		foreach (UnlockBox value in allBoxes.Values)
		{
			if (!string.IsNullOrEmpty(value.unlockSO.parentUnlock) && allBoxes.ContainsKey(value.unlockSO.parentUnlock))
			{
				allBoxes[value.unlockSO.parentUnlock].children.Add(value);
			}
		}
		UnlockLayout layout = GetLayout(rootUnlocks[0]);
		Rect boundary = default(Rect);
		layout.SetUIPositions(horizontalSpacing, verticalSpacing, new Vector2(0f, 0f - paddingVertical), ref boundary);
		container.sizeDelta = boundary.size + new Vector2(paddingHorizontal * 2f, paddingVertical * 2f);
		foreach (UnlockBox value2 in allBoxes.Values)
		{
			if (value2.children.Count == 0)
			{
				continue;
			}
			Vector2 anchoredPosition = ((RectTransform)value2.transform).anchoredPosition;
			RectTransform rectTransform = (RectTransform)value2.children[0].transform;
			float num2 = anchoredPosition.y - rectTransform.anchoredPosition.y;
			Vector2 end = anchoredPosition + Vector2.down * num2 * 0.82f;
			value2.lines.Add(DrawLine(anchoredPosition, end));
			float num3 = anchoredPosition.x;
			float num4 = anchoredPosition.x;
			foreach (UnlockBox child in value2.children)
			{
				RectTransform rectTransform2 = (RectTransform)child.transform;
				value2.lines.Add(DrawLine(rectTransform2.anchoredPosition + Vector2.up * num2 * 0.18f, rectTransform2.anchoredPosition));
				num3 = Mathf.Max(num3, rectTransform2.anchoredPosition.x);
				num4 = Mathf.Min(num4, rectTransform2.anchoredPosition.x);
			}
			value2.lines.Add(DrawLine(new Vector2(num4, end.y), new Vector2(num3, end.y)));
		}
	}

	private UnlockLayout GetLayout(UnlockBox unlock)
	{
		UnlockLayout unlockLayout = new UnlockLayout();
		unlockLayout.children = Enumerable.ToList<UnlockLayout>(Enumerable.Select<UnlockBox, UnlockLayout>((IEnumerable<UnlockBox>)Enumerable.OrderBy<UnlockBox, int>((IEnumerable<UnlockBox>)unlock.children, (Func<UnlockBox, int>)((UnlockBox u) => u.unlockSO.order)), (Func<UnlockBox, UnlockLayout>)((UnlockBox u) => GetLayout(u))));
		unlockLayout.box = unlock;
		int num = 0;
		for (int i = 0; i < unlockLayout.children.Count; i++)
		{
			unlockLayout.children[i].xOffset = num;
			for (int j = 0; j < i; j++)
			{
				while (i > 0 && unlockLayout.children[i].CollidesWith(unlockLayout.children[j]))
				{
					num++;
					unlockLayout.children[i].xOffset = num;
				}
			}
			num += 2;
		}
		int num2 = (num - 1) / 2;
		foreach (UnlockLayout child in unlockLayout.children)
		{
			child.xOffset -= num2;
		}
		return unlockLayout;
	}

	private int GetSubtreeDepth(UnlockBox unlock)
	{
		if (unlock.children.Count == 0)
		{
			return 0;
		}
		return Enumerable.Max<UnlockBox>((IEnumerable<UnlockBox>)unlock.children, (Func<UnlockBox, int>)GetSubtreeDepth) + 1;
	}

	public Image DrawLine(Vector2 start, Vector2 end)
	{
		RectTransform rectTransform = UnityEngine.Object.Instantiate(linePrefab, container);
		rectTransform.localPosition = Vector3.zero;
		Vector2 vector = end - start;
		rectTransform.sizeDelta *= new Vector2(1f, vector.magnitude);
		rectTransform.anchoredPosition = start;
		float z = 57.29578f * Mathf.Atan2(vector.y, vector.x) + 90f;
		rectTransform.Rotate(new Vector3(0f, 0f, z));
		rectTransform.SetAsFirstSibling();
		return rectTransform.GetComponent<Image>();
	}

	public void OpenCloseMenu()
	{
		bool flag = !IsOpen;
		MainSim.Inst.workspace.gameObject.SetActive(!flag);
		base.transform.GetChild(0).gameObject.SetActive(flag);
		farmIcon.SetActive(flag);
		unlockIcon.SetActive(!flag);
		plusButton.SetActive(!flag);
		infoButton.SetActive(!flag);
	}

	public void DisableMenu()
	{
		openCloseButton.gameObject.SetActive(value: false);
		if (IsOpen)
		{
			OpenCloseMenu();
		}
	}

	public void Scroll(float zoom)
	{
		if (zoom > 0f)
		{
			base.transform.localScale = Vector2.one * Mathf.Clamp(base.transform.localScale.x * zoomSpeed, minZoomScale, maxZoomScale);
		}
		else if (zoom < 0f)
		{
			base.transform.localScale = Vector2.one * Mathf.Clamp(base.transform.localScale.x / zoomSpeed, minZoomScale, maxZoomScale);
		}
	}
}
