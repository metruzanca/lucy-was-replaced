using UnityEngine;

public class CameraController : MonoBehaviour
{
	[SerializeField]
	private float zoomSpeed;

	[SerializeField]
	private float xFactor;

	[SerializeField]
	private float yFactor;

	[SerializeField]
	private float cameraXRotFact;

	[SerializeField]
	private float islandScale;

	[SerializeField]
	private RectTransform contentRect;

	[SerializeField]
	private RectTransform windowRect;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private Transform sceneScaler;

	public float zoom = 1f;

	private Vector3 startPos;

	private void Start()
	{
		startPos = base.transform.position;
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDestroy()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		if (TryGetComponent<Camera>(out var component))
		{
			component.backgroundColor = theme.ui.BackgroundColor;
		}
	}

	private void Update()
	{
		Vector3 vector = (contentRect.anchoredPosition + windowRect.anchoredPosition) / Screen.height * canvas.scaleFactor * zoom;
		vector.y *= yFactor;
		vector.x *= xFactor;
		base.transform.position = vector + new Vector3(startPos.x * canvas.scaleFactor * zoom, startPos.y, startPos.z);
		sceneScaler.localScale = Vector3.one * islandScale * canvas.scaleFactor * zoom / ((float)Screen.height / 1080f);
		FMODSoundManager.zoomLevelField(zoom);
	}
}
