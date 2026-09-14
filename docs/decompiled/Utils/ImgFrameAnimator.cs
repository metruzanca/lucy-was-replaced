using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImgFrameAnimator : MonoBehaviour
{
	private struct FrameAnim
	{
		public Image img;

		public Sprite[] sprites;

		public float startTime;

		public string animName;
	}

	private List<FrameAnim> frameAnims;

	private const float fps = 30f;

	private void Start()
	{
		TimerManager.StartTimer(UpdateAnims, 0.03333333507180214, loop: true);
	}

	public void LoadAnim(Image img, string animName)
	{
		FrameAnim frameAnim = default(FrameAnim);
		frameAnim.sprites = Resources.LoadAll<Sprite>("Gifs/" + animName);
		frameAnim.img = img;
		frameAnim.animName = animName;
		frameAnim.startTime = Time.time;
	}

	private void UpdateAnims()
	{
		foreach (FrameAnim frameAnim in frameAnims)
		{
			float num = frameAnim.sprites.Length;
			float num2 = num / 30f;
			int num3 = Mathf.RoundToInt((Time.time - frameAnim.startTime) % num2 / num2 * num);
			frameAnim.img.sprite = frameAnim.sprites[num3];
		}
	}

	private void OnDestroy()
	{
		TimerManager.StopTimer(UpdateAnims);
	}
}
