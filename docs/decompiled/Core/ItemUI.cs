using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour, ITooltipHandler
{
	[SerializeField]
	private float animDuration = 0.1f;

	[SerializeField]
	private float animHeightOffset = 5f;

	[SerializeField]
	private float textWidthPerChar = 12f;

	[SerializeField]
	private float imageScaleDuration = 0.05f;

	[SerializeField]
	private float imageDescaleDurationFactor = 2f;

	[SerializeField]
	private float imageScalePickup = 1.1f;

	[SerializeField]
	private float imageScaleImpact = 1.3f;

	[SerializeField]
	private float impactRatioThreshold = 0.3f;

	[SerializeField]
	private float imagePickupOffset = 5f;

	[SerializeField]
	private Image image;

	[SerializeField]
	private TextMeshProUGUI countText;

	[SerializeField]
	private Transform tooltipPos;

	private ItemSO item;

	private double amount;

	private double newAmount;

	private Action updateTooltipCallback;

	private float updateAnimStartTime = -1f;

	private bool isAnimating;

	private Vector3[] startPositions;

	private Vector3[] endPositions;

	private Vector3[] currentPositions;

	private List<int> animatedCharacters = new List<int>();

	private float imageScalingStartTime = -1f;

	private float imageTargetScale = 1f;

	private float imageStartPosY;

	public void Setup(int itemId, double c)
	{
		item = ResourceManager.GetItem(itemId);
		amount = c;
		((TMP_Text)countText).text = Helper.NrToText(amount);
		RectTransform component = ((Component)(object)countText).GetComponent<RectTransform>();
		component.sizeDelta = new Vector2((float)((TMP_Text)countText).text.Length * textWidthPerChar, component.sizeDelta.y);
		image.sprite = Resources.Load<Sprite>("ItemTextures/" + item.itemName);
		imageStartPosY = image.transform.localPosition.y;
	}

	public void UpdateCount(double c)
	{
		double num = newAmount;
		newAmount = c;
		if (!(newAmount <= num) && !(Time.time - imageScalingStartTime < imageScaleDuration * 3f) && !(OptionHolder.GetString("increment number effect") != "enabled"))
		{
			double num2 = Math.Abs(newAmount - num);
			imageTargetScale = (((num2 + 1.0) / (newAmount + 1.0) > (double)impactRatioThreshold) ? imageScaleImpact : imageScalePickup);
			if (image.transform.localScale.x > imageTargetScale)
			{
				imageTargetScale = image.transform.localScale.x;
			}
			imageScalingStartTime = Time.time;
			float num3 = (image.transform.localScale.x - 1f) / (imageTargetScale - 1f);
			imageScalingStartTime -= num3 * imageScaleDuration;
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
		ThemeManager.Inst.Theme.docs.text.ApplyTo((TMP_Text)(object)countText);
	}

	private void Update()
	{
		if (newAmount != amount && !isAnimating)
		{
			bool increase = newAmount > amount;
			amount = newAmount;
			string text = Helper.NrToText(amount);
			if (updateTooltipCallback != null)
			{
				updateTooltipCallback();
			}
			if (text != ((TMP_Text)countText).text && OptionHolder.GetString("increment number effect") == "enabled")
			{
				StartAnimation(text, increase);
			}
			else if (text != ((TMP_Text)countText).text)
			{
				RectTransform component = ((Component)(object)countText).GetComponent<RectTransform>();
				component.sizeDelta = new Vector2((float)text.Length * textWidthPerChar, component.sizeDelta.y);
				((TMP_Text)countText).text = text;
			}
		}
		UpdateAnimation();
		UpdateImageScale();
	}

	private void StartAnimation(string newText, bool increase)
	{
		if (string.IsNullOrEmpty(newText))
		{
			((TMP_Text)countText).text = newText;
			updateAnimStartTime = -1f;
			return;
		}
		animatedCharacters.Clear();
		StringBuilder stringBuilder = new StringBuilder(newText);
		int num = Mathf.Min(newText.Length, ((TMP_Text)countText).text.Length);
		for (int i = 0; i < num; i++)
		{
			if (newText[i] != ((TMP_Text)countText).text[i] && char.IsDigit(newText[i]))
			{
				animatedCharacters.Add(i);
				stringBuilder.Append(((TMP_Text)countText).text[i]);
			}
		}
		RectTransform component = ((Component)(object)countText).GetComponent<RectTransform>();
		component.sizeDelta = new Vector2((float)newText.Length * textWidthPerChar, component.sizeDelta.y);
		((TMP_Text)countText).text = stringBuilder.ToString();
		((TMP_Text)countText).ForceMeshUpdate(false, false);
		TMP_TextInfo textInfo = ((TMP_Text)countText).textInfo;
		int materialReferenceIndex = textInfo.characterInfo[0].materialReferenceIndex;
		Vector3[] vertices = textInfo.meshInfo[materialReferenceIndex].vertices;
		updateAnimStartTime = Time.time;
		isAnimating = true;
		startPositions = new Vector3[vertices.Length];
		endPositions = new Vector3[vertices.Length];
		currentPositions = new Vector3[vertices.Length];
		float num2 = textInfo.characterInfo[0].topLeft.y + animHeightOffset;
		float num3 = textInfo.characterInfo[0].bottomLeft.y - animHeightOffset;
		for (int j = 0; j < textInfo.characterCount; j++)
		{
			if (!textInfo.characterInfo[j].isVisible)
			{
				continue;
			}
			int vertexIndex = textInfo.characterInfo[j].vertexIndex;
			if (j < newText.Length)
			{
				for (int k = 0; k < 4; k++)
				{
					startPositions[vertexIndex + k] = vertices[vertexIndex + k];
					endPositions[vertexIndex + k] = vertices[vertexIndex + k];
				}
				continue;
			}
			int num4 = animatedCharacters[j - newText.Length];
			int vertexIndex2 = textInfo.characterInfo[num4].vertexIndex;
			for (int l = 0; l < 4; l++)
			{
				startPositions[vertexIndex + l] = vertices[vertexIndex2 + l];
				endPositions[vertexIndex + l] = vertices[vertexIndex2 + l];
				endPositions[vertexIndex + l].y = (increase ? num2 : num3);
				startPositions[vertexIndex2 + l].y = (increase ? num3 : num2);
			}
		}
	}

	private void UpdateAnimation()
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		if (!isAnimating)
		{
			return;
		}
		if (Time.time - updateAnimStartTime > animDuration)
		{
			isAnimating = false;
			((TMP_Text)countText).text = Helper.NrToText(amount);
			return;
		}
		float t = (Time.time - updateAnimStartTime) / animDuration;
		for (int i = 0; i < startPositions.Length; i++)
		{
			currentPositions[i] = Vector3.Lerp(startPositions[i], endPositions[i], t);
		}
		TMP_MeshInfo val = ((TMP_Text)countText).textInfo.meshInfo[0];
		val.mesh.vertices = currentPositions;
		((TMP_Text)countText).UpdateGeometry(val.mesh, 0);
	}

	private void UpdateImageScale()
	{
		float num = (Time.time - imageScalingStartTime) / imageScaleDuration;
		if (num >= 1f)
		{
			num = (1f + imageDescaleDurationFactor - num) / imageDescaleDurationFactor;
		}
		if (!(num < 0f))
		{
			float num2 = Mathf.Lerp(1f, imageTargetScale, num);
			image.transform.localScale = new Vector3(1f / num2, num2, 1f);
			float y = Mathf.Lerp(imageStartPosY, imageStartPosY + imagePickupOffset, num);
			image.transform.localPosition = new Vector3(image.transform.localPosition.x, y, image.transform.localPosition.z);
		}
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		this.updateTooltipCallback = updateTooltipCallback;
		TooltipInfo tooltipInfo = TooltipUtils.ItemTooltip(item.name);
		if (tooltipInfo != null)
		{
			tooltipInfo.fixedPosition = ((tooltipPos != null) ? tooltipPos.position : default(Vector3));
			tooltipInfo.anchor = TooltipInfo.Anchor.BottomRight;
			tooltipInfo.delay = 0f;
		}
		return tooltipInfo;
	}

	public void TooltipGone()
	{
		updateTooltipCallback = null;
	}
}
