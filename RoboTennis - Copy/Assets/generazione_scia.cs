using UnityEngine;
using System.Collections;

public class generazione_scia : MonoBehaviour
{
	public GameObject palla_scia;
	private GameObject clone;

	public BasicLaunchSim test_tiro;
	bool tiro = false;
	int counter = 0;
	int numero_frame = 0;


	// Use this for initialization
	void Start ()
	{
		test_tiro = (BasicLaunchSim)GameObject.FindObjectOfType (typeof(BasicLaunchSim));
	}
	
	// Update is called once per frame
	void Update ()
	{
		Debug.Log ("SONO UPDATE");
		numero_frame++;
		//Debug.Log (richiama_RobotLauncher.launch_event);
		if (test_tiro.generazione) {
			Debug.Log ("TIRO FATTO");
			tiro = true;
		}


		if (tiro) {
			if (numero_frame < 300) {
				counter++;
				Debug.Log ("AUMENTO CONTEGGIO 1-5");
				if (counter == 4) {

					//GetComponent<Renderer> ().material.color = Color.black;

					clone = (GameObject)Instantiate (palla_scia);
					clone.transform.position = (this.transform.position);
					Debug.Log (this.transform.position);
					clone.GetComponent<Renderer> ().material.color = Color.green;

					Debug.Log ("STAMPO FRAME PALLA");
					counter = 0;

				}
			}
		}
	}

}
