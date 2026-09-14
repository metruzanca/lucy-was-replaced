using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoUI : MonoBehaviour
{
	public VideoPlayer player;

	public RawImage image;

	public AspectRatioFitter fitter;

	private RenderTexture rt;

	private string fallbackTexture;

	public void Play(string video, string fallbackTexture = null, string basePath = null)
	{
		if ((Object)(object)player == null || image == null)
		{
			return;
		}
		if (fallbackTexture == null)
		{
			int num = video.LastIndexOf('|');
			if (num >= 0)
			{
				fallbackTexture = video.Substring(num + 1);
				video = video.Substring(0, num);
			}
		}
		if (Path.GetExtension(video) == "")
		{
			VideoClip val = Resources.Load<VideoClip>(video);
			if ((Object)(object)val == null)
			{
				Debug.LogError("VideoUI: Couldn't load video resource '" + video + "'");
				return;
			}
			player.clip = val;
		}
		else if (video.StartsWith("http") || video.StartsWith("file:"))
		{
			player.url = video;
		}
		else
		{
			string text = video;
			if (basePath != null && !Path.IsPathFullyQualified(text))
			{
				text = Path.Combine(basePath, text);
			}
			player.url = text;
		}
		this.fallbackTexture = fallbackTexture;
		if (((Behaviour)(object)player).enabled)
		{
			player.Prepare();
		}
	}

	public async void ShowFallback(string fallback)
	{
		if (string.IsNullOrEmpty(fallback))
		{
			return;
		}
		Texture2D texture2D = await ImageLoader.ResolveTexture(fallback);
		if (!(texture2D == null))
		{
			image.enabled = true;
			image.texture = texture2D;
			if (fitter != null)
			{
				fitter.aspectRatio = (float)texture2D.width / (float)texture2D.height;
			}
			LayoutRebuilder.MarkLayoutForRebuild(base.transform as RectTransform);
		}
	}

	private void OnEnable()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		if ((Object)(object)player == null || image == null)
		{
			base.enabled = false;
			return;
		}
		image.enabled = false;
		player.errorReceived += new ErrorEventHandler(OnErrorReceived);
		player.prepareCompleted += new EventHandler(OnVideoPrepared);
		if (player.isPrepared)
		{
			OnVideoPrepared(player);
		}
		else if (((Object)(object)player.clip != null || !string.IsNullOrEmpty(player.url)) && !player.isPrepared)
		{
			player.Prepare();
		}
	}

	private void OnDisable()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		player.errorReceived -= new ErrorEventHandler(OnErrorReceived);
		player.prepareCompleted -= new EventHandler(OnVideoPrepared);
	}

	private void OnDestroy()
	{
		if (rt != null)
		{
			Object.Destroy(rt);
			rt = null;
		}
	}

	private void OnErrorReceived(VideoPlayer player, string message)
	{
		Debug.LogError("VideoUI: Failed to play video: " + message);
		ShowFallback(fallbackTexture);
	}

	private void OnVideoPrepared(VideoPlayer source)
	{
		uint width = source.width;
		uint height = source.height;
		if (width == 0 || height == 0)
		{
			return;
		}
		if (rt == null || rt.width != width || rt.height != height)
		{
			if (rt != null)
			{
				Object.Destroy(rt);
			}
			rt = new RenderTexture((int)width, (int)height, 0, RenderTextureFormat.Default, 0)
			{
				antiAliasing = 1,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point,
				anisoLevel = 0
			};
		}
		image.enabled = true;
		if (fitter != null)
		{
			fitter.aspectRatio = (float)width / (float)height;
		}
		player.renderMode = (VideoRenderMode)2;
		player.targetTexture = rt;
		image.texture = rt;
		player.Play();
		LayoutRebuilder.MarkLayoutForRebuild(base.transform as RectTransform);
	}
}
