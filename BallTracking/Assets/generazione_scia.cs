using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class Generazione_scia : MonoBehaviour
{
	public GameObject palla_scia;
	private GameObject clone;

	public BasicLaunchSim test_tiro;
	bool tiro = false;
	int counter = 0;
	int numero_frame = 0;
	public Stopwatch timer;


	// Use this for initialization
	void Start ()
	{
		test_tiro = (BasicLaunchSim)GameObject.FindObjectOfType (typeof(BasicLaunchSim));
		timer = new Stopwatch ();
		timer.Start ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		UnityEngine.Debug.Log ("SONO UPDATE");
		numero_frame++;
		//Debug.Log (richiama_RobotLauncher.launch_event);
		if (test_tiro.generazione) {
			UnityEngine.Debug.Log ("TIRO FATTO");
			tiro = true;
		}


		if (tiro) {
			if (numero_frame < 300) {
				counter++;



				// creazione del clone ogni update per salvare la scia
				clone = (GameObject)Instantiate (palla_scia);
				clone.transform.position = (this.transform.position);
				// salvo la posizione del clone corrente

				// controllo se c è una posizione del clone precedente

				// creazione del cilindro dalla posizione del clone corrente a quella del clone precedente

				// posizione del clone corrente diventa pos clone precedente


				UnityEngine.Debug.Log ("AUMENTO CONTEGGIO 1-5");
				if (counter == 2) {

					// crezione del clone ogni 4 frame
					clone.transform.position = (this.transform.position);

					//debug posizione del clone
					//Debug.Log (this.transform.position);

					// colorazione del clone di verde
					clone.GetComponent<Renderer> ().material.color = Color.green;

					// debug di stampa del frame
					UnityEngine.Debug.Log ("STAMPO FRAME PALLA");

					//azzero il counter
					counter = 0;

				}
			}
			if (numero_frame == 300){
				timer.Stop ();
				UnityEngine.Debug.Log (timer.Elapsed);
			}
		}
	}

}
