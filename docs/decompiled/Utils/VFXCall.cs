using UnityEngine;

internal struct VFXCall
{
	public VFXType effect;

	public Vector3 localPos;

	public bool changeColor;

	public Color color;

	public string text;

	public VFXCall(VFXType effect, Vector3 localPos, bool changeColor, Color color, string text)
	{
		this.effect = effect;
		this.localPos = localPos;
		this.changeColor = changeColor;
		this.color = color;
		this.text = text;
	}
}
