using UnityEngine.EventSystems;

public class CustomStandaloneInputModule : StandaloneInputModule
{
	public PointerEventData GetPointerData()
	{
		if (m_PointerData.ContainsKey(-1))
		{
			return m_PointerData[-1];
		}
		return null;
	}
}
