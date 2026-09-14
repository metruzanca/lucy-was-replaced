using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class Tickbox : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	[SerializeField]
	private GameObject tick;

	[SerializeField]
	private UnityEvent ticked;

	[SerializeField]
	private UnityEvent unticked;

	[SerializeField]
	private bool isTicked;

	private void Awake()
	{
		tick.SetActive(isTicked);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		isTicked = !isTicked;
		if (isTicked)
		{
			ticked.Invoke();
		}
		else
		{
			unticked.Invoke();
		}
		tick.SetActive(isTicked);
	}
}
