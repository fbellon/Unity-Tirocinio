using System;
using UnityEngine;

/// <summary>
/// RobotSim functionality:
/// - Ricava dati dal RigidBody corrispondente
/// - Applica forze al RigidBody
/// - Salva lo stato corrente, aggiornato (posizione, forze, ecc.)
/// </summary>

public class RobotSim : MonoBehaviour
{
  // Ruote
  int wheels = 2;

  // Componenti nella gerarchia dell'oggetto, collider escluso
  int components_number = 6;

  // Array delle forze (un valore per motore) (0=left, 1=right)
  // Array delle forze agenti, correnti (nota: il nome è coppie, ma in realtà sono applicate forze
  // per semplificare la modellizzazione
  public float[] current_motor_torque;
  // Array delle forze agenti, obbiettivo (setpoint)
  public float[] updated_motor_torque;
  // Sogliatura forze
  public float motor_torque_maximum;
  // Array del RigidBody (oggetto robot)
  public Rigidbody[] components;

  public Vector3 launcher_position;
 
  // Dati del robot:
  // Velocità di avanzamento rispetto all'asse Z del proprio riferimento (non world)
  // Velocità di rotazione rispetto all'asse Y del proprio sist. di rif. (non world)
  // Posizione in coordinate world
  // Orientamento in coordinate world (dominio: 0-360)
  public Vector3 linearVelocity;
  public Vector3 angularVelocity;
  public Vector3 position;
  public float orientation;

  void Start()
  {
    current_motor_torque = new float[wheels];
    updated_motor_torque = new float[wheels];
    components = new Rigidbody[components_number];

    Array.Clear(current_motor_torque, 0, current_motor_torque.Length);
    Array.Clear(updated_motor_torque, 0, updated_motor_torque.Length);
    motor_torque_maximum = 70f;

    for (int i = 0; i < components_number; ++i)
      components[i] = this.GetComponentsInChildren<Rigidbody>()[i];
    
    launcher_position = components[3].transform.position;
    launcher_position.y += 0.3f;
  }

  void MaxOutput()
  {
    // updated_motor_thrust 0 < u < FORCE_MAX
    //for (int i = 0; i < wheels; ++i)
    {
      if (updated_motor_torque[0] < -motor_torque_maximum && updated_motor_torque[1] < -motor_torque_maximum)
      {
        updated_motor_torque[0] = -motor_torque_maximum;
        updated_motor_torque[1] = -motor_torque_maximum;      
      }
      if (updated_motor_torque[0] > motor_torque_maximum && updated_motor_torque[1] > motor_torque_maximum)
      {
        updated_motor_torque[0] = motor_torque_maximum;
        updated_motor_torque[1] = motor_torque_maximum;

      }
    }
  }

  void FixedUpdate()
  {
    MaxOutput();

    orientation = components[5].transform.eulerAngles.y;
    position = components[5].transform.position;
    
    // Applicazione forze
    //for (int i = 1; i < components_number - 3; ++i)
    {
     components[1].AddRelativeForce(Vector3.forward * updated_motor_torque[0]);
     components[2].AddRelativeForce(Vector3.forward * updated_motor_torque[1]);
    }

    // update the current thrust with the newly applied thrust
    current_motor_torque = updated_motor_torque;

    // Ricava le velocità nel sistema di rif. locale
    linearVelocity = transform.InverseTransformDirection(components[0].velocity);
    angularVelocity = transform.InverseTransformDirection(components[0].angularVelocity);

  }


}