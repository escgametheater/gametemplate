using UnityEngine;
using System.Collections;
using Esc;

public class GameController : MonoBehaviour {


	private ServerConnection serverConnection;
	private ShowControlInterface showControl;

	void Awake() {
		showControl = ShowControlInterface.Instance;
		showControl.enableLogOutput();

		ServerConnection.EnableDeveloperMode();
		serverConnection = ServerConnection.Instance;
		serverConnection.OnConnected += ServerConnection_OnConnected;
		serverConnection.OnDisconnected += ServerConnection_OnDisconnected;
	}

	void ServerConnection_OnDisconnected ()
	{
		Debug.Log("ServerConnection_OnDisconnected");
	}

	void ServerConnection_OnConnected() {
		Debug.Log("ServerConnection_OnConnected");
	}

	void Start() {

	}

	void Update() {

	}

	public void OnGUI() {
		
			string sendLighting = "Send Lighting Calls";
			if(GUI.Button (new Rect (265, 120, 250, 50), sendLighting)){
				SendLightingCalls();
			}
	}

	public void SendLightingCalls() {
		Debug.Log("SendLightingCalls");
		for (int i = 0; i < 1000; i++){
			showControl.SetLightingAccents(LightingColor.LIGHT_BLUE, Timing.ONE);
		}
	}
}
