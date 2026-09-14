using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageLoader : MonoBehaviour
{
	public string image;

	private Image img;

	public bool IsLoading { get; private set; }

	public async void Show(string image, string basePath = null)
	{
		try
		{
			await ShowAsync(image, basePath, base.destroyCancellationToken);
		}
		catch (OperationCanceledException)
		{
		}
	}

	public async Awaitable ShowAsync(string image, string basePath = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (!(img == null) || TryGetComponent<Image>(out img))
		{
			this.image = image;
			img.sprite = null;
			IsLoading = true;
			Sprite sprite = await ResolveSprite(image, basePath, cancellation);
			if (img != null)
			{
				img.sprite = sprite;
			}
			IsLoading = false;
		}
	}

	public static async Awaitable<Texture2D> ResolveTexture(string image, string basePath = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (Path.GetExtension(image).Length == 0)
		{
			return ResourceManager.GetSprite(image).texture;
		}
		if (image.StartsWith("file:") || image.StartsWith("http"))
		{
			UnityWebRequest request = UnityWebRequestTexture.GetTexture(image, true);
			try
			{
				await Awaitable.FromAsyncOperation((AsyncOperation)(object)request.SendWebRequest(), cancellation);
				if ((int)request.result != 1)
				{
					Debug.LogError("ImageLoader: Failed to load image URL '" + image + "'");
					return null;
				}
				return DownloadHandlerTexture.GetContent(request);
			}
			finally
			{
				((IDisposable)request)?.Dispose();
			}
		}
		string path = image;
		if (basePath != null && !Path.IsPathFullyQualified(path))
		{
			path = Path.Combine(basePath, path);
		}
		if (!File.Exists(path))
		{
			Debug.LogError("ImageLoader: Streaming assets image not found '" + path + "'");
			return null;
		}
		byte[] array = await File.ReadAllBytesAsync(path);
		if (array == null)
		{
			Debug.LogError("ImageLoader: Failed to load streaming assets image '" + path + "'");
			return null;
		}
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: true);
		if (!ImageConversion.LoadImage(texture2D, array, true))
		{
			Debug.LogError("ImageLoader: Failed to decode streaming assets image '" + path + "'");
			return null;
		}
		return texture2D;
	}

	public static async Awaitable<Sprite> ResolveSprite(string image, string basePath = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (Path.GetExtension(image).Length == 0)
		{
			return ResourceManager.GetSprite(image);
		}
		Texture2D texture2D = await ResolveTexture(image, basePath, cancellation);
		if (texture2D == null)
		{
			return null;
		}
		return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.zero);
	}

	private void OnEnable()
	{
		if (!TryGetComponent<Image>(out img))
		{
			base.enabled = false;
		}
		else if (img.sprite == null && !IsLoading && !string.IsNullOrEmpty(image))
		{
			Show(image);
		}
	}
}
