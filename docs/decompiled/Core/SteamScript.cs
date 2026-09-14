using Steamworks;
using UnityEngine;

public class SteamScript : MonoBehaviour
{
	private CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;

	private void OnEnable()
	{
		if (SteamManager.Initialized)
		{
			m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create((APIDispatchDelegate<NumberOfCurrentPlayers_t>)OnNumberOfCurrentPlayers);
		}
	}

	private void Update()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SteamAPICall_t numberOfCurrentPlayers = SteamUserStats.GetNumberOfCurrentPlayers();
			m_NumberOfCurrentPlayers.Set(numberOfCurrentPlayers, (APIDispatchDelegate<NumberOfCurrentPlayers_t>)null);
			Debug.Log("Called GetNumberOfCurrentPlayers()");
		}
	}

	private void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (pCallback.m_bSuccess != 1 || bIOFailure)
		{
			Debug.Log("There was an error retrieving the NumberOfCurrentPlayers.");
		}
		else
		{
			Debug.Log("The number of players playing your game: " + pCallback.m_cPlayers);
		}
	}
}
