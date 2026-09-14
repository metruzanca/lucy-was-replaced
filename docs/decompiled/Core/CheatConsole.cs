using UnityEngine;

public class CheatConsole : MonoBehaviour
{
	private bool showConsole;

	private string input;

	private void OnGUI()
	{
		if (showConsole)
		{
			GUI.Box(new Rect(0f, Screen.height - 25, Screen.width, 25f), "");
			GUI.backgroundColor = new Color(0f, 0f, 0f, 0f);
			input = GUI.TextField(new Rect(5f, (float)Screen.height - 23f, (float)Screen.width - 10f, 21f), input);
		}
	}

	private void Update()
	{
		if (showConsole && Input.GetKeyDown(KeyCode.Return))
		{
			InvokeCommand();
			input = "";
		}
	}

	private void InvokeCommand()
	{
		if (input == "aint nobody got time for this")
		{
			ToggleFastMode();
		}
	}

	private void GetRichQuickly()
	{
		foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
		{
			MainSim.Inst.UnlockOrUpgrade(allUnlock);
		}
	}

	private void ToggleFastMode()
	{
		if (MainSim.Inst.harvestFactor == 1)
		{
			MainSim.Inst.harvestFactor = 10;
		}
		else
		{
			MainSim.Inst.harvestFactor = 1;
		}
	}
}
