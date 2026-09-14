using UnityEngine;

public class UtilsGameObjects : MonoBehaviour
{
	private static UtilsGameObjects _instance;

	private WarningPopup warningPopup;

	public static UtilsGameObjects Inst => _instance;

	public WarningPopup WarningPopup
	{
		get
		{
			if (warningPopup == null)
			{
				warningPopup = Object.FindAnyObjectByType<WarningPopup>(FindObjectsInactive.Include);
				if (warningPopup == null)
				{
					Debug.LogError("WarningPopup not found in scene!");
				}
			}
			return warningPopup;
		}
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
	}
}
