using UnityEngine;
using UnityEngine.UI;

public class HorizontalFitLayoutElement : LayoutElement
{
	private RectTransform rt;

	private Image image;

	private VideoUI video;

	public override float preferredHeight
	{
		get
		{
			float num = base.preferredHeight;
			if (num < 0f)
			{
				float reverseAspectRatio = GetReverseAspectRatio();
				if (reverseAspectRatio >= 0f)
				{
					num = rt.rect.width * reverseAspectRatio;
				}
			}
			return num;
		}
		set
		{
			base.preferredHeight = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		TryGetComponent<RectTransform>(out rt);
		TryGetComponent<Image>(out image);
		TryGetComponent<VideoUI>(out video);
	}

	private float GetReverseAspectRatio()
	{
		if (image != null)
		{
			return image.preferredHeight / image.preferredWidth;
		}
		if (video != null && video.fitter != null)
		{
			return 1f / video.fitter.aspectRatio;
		}
		return -1f;
	}
}
