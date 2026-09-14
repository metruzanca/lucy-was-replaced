using UnityEngine;
using UnityEngine.EventSystems;

public class MarkdownLink : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public MarkdownText markdown;

	public string url;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!string.IsNullOrEmpty(url) && !(markdown == null))
		{
			markdown.TriggerLinkClick(url);
		}
	}
}
