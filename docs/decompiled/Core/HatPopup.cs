using System.Collections;
using UnityEngine;

public class HatPopup : MonoBehaviour
{
	private static HatPopup _instance;

	[SerializeField]
	private MarkdownText text;

	[SerializeField]
	private RectTransform scaleObject;

	[SerializeField]
	private RectTransform greenOverlayBox;

	[SerializeField]
	private float animationDuration = 0.3f;

	[SerializeField]
	private float displayDuration = 4f;

	private volatile HatSO hatSO;

	private bool isPopupActive;

	public static HatPopup Inst => _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
	}

	private void Start()
	{
		scaleObject.localScale = new Vector3(0f, 1f, 1f);
		greenOverlayBox.localScale = new Vector3(0f, 1f, 1f);
	}

	private void Update()
	{
		HatSO hatSO = this.hatSO;
		if (hatSO != null && !isPopupActive)
		{
			this.hatSO = null;
			text.UpdateText(CodeUtilities.LocalizeAndFormat("new_hat_unlocked", hatSO));
			StartCoroutine(ShowPopupAnimation());
		}
	}

	public void ShowPopup(HatSO hat)
	{
		hatSO = hat;
	}

	private IEnumerator ShowPopupAnimation()
	{
		isPopupActive = true;
		yield return StartCoroutine(GreenOverlayEffect());
		yield return new WaitForSeconds(displayDuration);
		yield return StartCoroutine(TweenScaleX(scaleObject, 1f, 0f, animationDuration));
		isPopupActive = false;
	}

	private IEnumerator GreenOverlayEffect()
	{
		Vector2 originalPivot = greenOverlayBox.pivot;
		Vector2 originalAnchoredPosition = greenOverlayBox.anchoredPosition;
		SetPivotAndAdjustPosition(greenOverlayBox, new Vector2(0f, 0.5f));
		yield return StartCoroutine(TweenScaleX(greenOverlayBox, 0f, 1f, animationDuration));
		scaleObject.localScale = Vector3.one;
		SetPivotAndAdjustPosition(greenOverlayBox, new Vector2(1f, 0.5f));
		yield return StartCoroutine(TweenScaleX(greenOverlayBox, 1f, 0f, animationDuration));
		greenOverlayBox.pivot = originalPivot;
		greenOverlayBox.anchoredPosition = originalAnchoredPosition;
	}

	private void SetPivotAndAdjustPosition(RectTransform rectTransform, Vector2 newPivot)
	{
		Vector2 pivot = rectTransform.pivot;
		Vector2 vector = new Vector2((newPivot.x - pivot.x) * rectTransform.rect.width, (newPivot.y - pivot.y) * rectTransform.rect.height);
		rectTransform.pivot = newPivot;
		rectTransform.anchoredPosition += vector;
	}

	private IEnumerator TweenScaleX(RectTransform target, float fromScaleX, float toScaleX, float duration)
	{
		float elapsedTime = 0f;
		Vector3 startScale = target.localScale;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			float num = elapsedTime / duration;
			num = num * num * (3f - 2f * num);
			float x = Mathf.Lerp(fromScaleX, toScaleX, num);
			target.localScale = new Vector3(x, startScale.y, startScale.z);
			yield return null;
		}
		target.localScale = new Vector3(toScaleX, startScale.y, startScale.z);
	}

	private IEnumerator TweenScale(RectTransform target, Vector3 fromScale, Vector3 toScale, float duration)
	{
		float elapsedTime = 0f;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			float num = elapsedTime / duration;
			num = num * num * (3f - 2f * num);
			target.localScale = Vector3.Lerp(fromScale, toScale, num);
			yield return null;
		}
		target.localScale = toScale;
	}
}
