using System.IO;
using System.IO.Pipes;
using System.Threading;
using UnityEngine;

namespace System.Collections.Generic.Integrations;

public class Ipc : MonoBehaviour
{
	public static Ipc Instance;

	private Thread _t1;

	private Thread _t2;

	private bool _isRunning;

	private bool sendTap;

	private void Awake()
	{
		Instance = this;
	}

	public void Start()
	{
		if (!Application.isEditor)
		{
			_isRunning = true;
			_t1 = new Thread((ThreadStart)delegate
			{
				ServerThread("BongoCatxTheFarmerWasReplaced");
			})
			{
				IsBackground = true
			};
			_t1.Start();
			_t2 = new Thread((ThreadStart)delegate
			{
				ServerThread("TapTapLootxTheFarmerWasReplaced");
			})
			{
				IsBackground = true
			};
			_t2.Start();
		}
	}

	private void OnDestroy()
	{
		_isRunning = false;
		if (_t1 != null)
		{
			try
			{
				_t1.Interrupt();
				_t1.Abort();
			}
			catch
			{
			}
			_t1 = null;
		}
		if (_t2 != null)
		{
			try
			{
				_t2.Interrupt();
				_t2.Abort();
			}
			catch
			{
			}
			_t2 = null;
		}
	}

	public void Increment()
	{
		sendTap = true;
	}

	private void ServerThread(string pipe)
	{
		while (_isRunning)
		{
			try
			{
				using NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream(pipe, PipeDirection.InOut, 1);
				namedPipeServerStream.WaitForConnection();
				StreamString streamString = new StreamString((Stream)(object)namedPipeServerStream);
				while (_isRunning && namedPipeServerStream.IsConnected)
				{
					if (sendTap)
					{
						streamString.WriteString("1");
						sendTap = false;
					}
					Thread.Sleep(20);
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			if (_isRunning)
			{
				Thread.Sleep(1000);
			}
		}
	}
}
