using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputBoss : MonoBehaviour
{
	[SerializeField]
	private float wasdSpeed;

	[SerializeField]
	private GameObject[] UIsToHide;

	private void Update()
	{
		if (0f > Input.mousePosition.x || 0f > Input.mousePosition.y || (float)Screen.width < Input.mousePosition.x || (float)Screen.height < Input.mousePosition.y)
		{
			return;
		}
		float axisRaw = Input.GetAxisRaw("ScrollWheel");
		if (axisRaw != 0f)
		{
			if (MainSim.Inst.menu.gameObject.activeInHierarchy)
			{
				MainSim.Inst.menu.titleWorkspace.Scroll(axisRaw);
			}
			else if (MainSim.Inst.researchMenu.IsOpen)
			{
				MainSim.Inst.researchMenu.Scroll(axisRaw);
			}
			else if (MainSim.Inst.workspace.codeCompleter.IsOpen && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)MainSim.Inst.workspace.codeCompleter.transform, (Vector2)Input.mousePosition, MainSim.Inst.workspace.uiCam))
			{
				MainSim.Inst.workspace.codeCompleter.Scroll(axisRaw);
			}
			else
			{
				MainSim.Inst.workspace.Scroll(axisRaw);
			}
		}
		if (OptionHolder.GetKeyCombination("menu").IsKeyPressed(pressedRightThisFrame: true))
		{
			if (MainSim.Inst.workspace.codeCompleter.IsOpen)
			{
				MainSim.Inst.workspace.codeCompleter.Close();
			}
			else if (MainSim.Inst.menu.gameObject.activeInHierarchy)
			{
				MainSim.Inst.menu.Play();
			}
			else if (MainSim.Inst.researchMenu.IsOpen)
			{
				MainSim.Inst.researchMenu.OpenCloseMenu();
			}
			else if (MainSim.Inst.leaderboardManager.IsLeaderBoardScreenOpen)
			{
				MainSim.Inst.leaderboardManager.OkPressed();
			}
			else if (MainSim.Inst.workspace.searchBox.gameObject.activeInHierarchy)
			{
				MainSim.Inst.workspace.searchBox.CloseSearchBox();
			}
			else
			{
				EventSystem.current?.SetSelectedGameObject(null);
				MainSim.Inst.menu.Open();
			}
		}
		GameObject gameObject = EventSystem.current?.currentSelectedGameObject;
		if (OptionHolder.GetKeyCombination("search").IsKeyPressed(pressedRightThisFrame: true))
		{
			if (gameObject != null && gameObject.TryGetComponent<CodeInputField>(out var component) && component.selectionAnchorPosition != component.selectionFocusPosition)
			{
				int num = Mathf.Min(component.selectionAnchorPosition, component.selectionFocusPosition);
				int num2 = Mathf.Max(component.selectionAnchorPosition, component.selectionFocusPosition);
				string searchTerm = component.text.Substring(num, num2 - num);
				MainSim.Inst.workspace.searchBox.StartSearch(searchTerm);
			}
			else
			{
				MainSim.Inst.workspace.searchBox.StartSearch();
			}
		}
		if (OptionHolder.GetKeyCombination("redo 1").IsKeyPressed(pressedRightThisFrame: true) || OptionHolder.GetKeyCombination("redo 2").IsKeyPressed(pressedRightThisFrame: true))
		{
			MainSim.Inst.workspace.Redo();
		}
		else if (OptionHolder.GetKeyCombination("undo").IsKeyPressed(pressedRightThisFrame: true))
		{
			MainSim.Inst.workspace.Undo();
		}
		if (OptionHolder.GetKeyCombination("save").IsKeyPressed(pressedRightThisFrame: true))
		{
			Saver.Save(MainSim.Inst);
		}
		if (gameObject == null || (gameObject?.GetComponent<CodeInputField>() == null && (Object)(object)gameObject?.GetComponent<TMP_InputField>() == null && !MainSim.Inst.menu.gameObject.activeInHierarchy && !MainSim.Inst.researchMenu.IsOpen))
		{
			if (OptionHolder.GetKeyCombination("move up").IsKeyPressed(pressedRightThisFrame: false))
			{
				MainSim.Inst.workspace.container.anchoredPosition += new Vector2(0f, (0f - wasdSpeed) * Time.deltaTime);
			}
			if (OptionHolder.GetKeyCombination("move left").IsKeyPressed(pressedRightThisFrame: false))
			{
				MainSim.Inst.workspace.container.anchoredPosition += new Vector2(wasdSpeed * Time.deltaTime, 0f);
			}
			if (OptionHolder.GetKeyCombination("move down").IsKeyPressed(pressedRightThisFrame: false))
			{
				MainSim.Inst.workspace.container.anchoredPosition += new Vector2(0f, wasdSpeed * Time.deltaTime);
			}
			if (OptionHolder.GetKeyCombination("move right").IsKeyPressed(pressedRightThisFrame: false))
			{
				MainSim.Inst.workspace.container.anchoredPosition += new Vector2((0f - wasdSpeed) * Time.deltaTime, 0f);
			}
		}
		if (OptionHolder.GetKeyCombination("start execution").IsKeyPressed(pressedRightThisFrame: true))
		{
			CodeWindow component2 = MainSim.Inst.workspace.activeWindow.GetComponent<CodeWindow>();
			if (component2 != null)
			{
				if (EventSystem.current.currentSelectedGameObject == component2.CodeInput.gameObject)
				{
					EventSystem.current.SetSelectedGameObject(null);
				}
				if (MainSim.Inst.StepByStepMode)
				{
					MainSim.Inst.StepByStepMode = false;
					MainSim.Inst.NextExecutionStep();
				}
				else
				{
					Node node = component2.Parse();
					if (node != null)
					{
						MainSim.Inst.StartMainExecution(component2, node);
					}
				}
			}
		}
		if (OptionHolder.GetKeyCombination("stop execution").IsKeyPressed(pressedRightThisFrame: true) && MainSim.Inst.IsExecuting())
		{
			MainSim.Inst.StopMainExecution();
		}
		if (OptionHolder.GetKeyCombination("next step").IsKeyPressed(pressedRightThisFrame: true) && MainSim.Inst.StepByStepMode)
		{
			MainSim.Inst.NextExecutionStep();
		}
		if (OptionHolder.GetKeyCombination("pause execution").IsKeyPressed(pressedRightThisFrame: true))
		{
			MainSim.Inst.StepByStepMode = true;
		}
		if (OptionHolder.GetKeyCombination("hide UI").IsKeyPressed(pressedRightThisFrame: true))
		{
			GameObject[] uIsToHide = UIsToHide;
			foreach (GameObject obj in uIsToHide)
			{
				obj.SetActive(!obj.activeInHierarchy);
			}
		}
	}
}
