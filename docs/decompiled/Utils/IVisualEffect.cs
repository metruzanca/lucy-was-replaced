using UnityEngine;

public interface IVisualEffect
{
	void Play();

	bool IsPlaying();

	void SetColor(Color color);

	float SetText(string text);
}
