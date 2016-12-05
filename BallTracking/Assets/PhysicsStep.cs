using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class PhysicsStep
{
  // Ball data
  Vector3 position, velocity, accel;
  Matrix4x4 localTf;
  
  
  // Simulations variables
  float g;
  float ballRadius, ballMass, ballSection;
  float kSpinDecay, kSliceDecay;
  Vector3 magnusForce, dragForce;   // Expressed in N (SI)
  float rho;                        // air density, kg/m^3
  float liftCoeff, driftCoeff;
  Vector3 magnusAccel, dragAccel;
  float radsec;
  Vector3 omega;
  Vector3[] outputM;
  Vector2[] outputR;
  float coeffsM, coeffsD;
  float kBounce;
  Vector3 VforwardLocal, VsideLocal, VupLocal;
  Vector3 sideForce, sideAccel;
  float coeffsS;
  float impactAlpha, beta;
  Vector3 V, Vn, Vg, Vgn, Vs, Vu, Vl, Vabs, P;
  float Vg_mag;
  Vector2 Vt, Vtn;
  Vector4 Vbuf;
  Matrix4x4 Rx, Rz;
  float bx, bz, by;
  float bounce_treshold;
  bool landed;
  Vector2 dragCoeff;
  


  public PhysicsStep()
  {
    // Shared
    g = 9.81f;
    kSpinDecay = Mathf.Pow(0.999f,Time.fixedDeltaTime/0.001f);
    kSliceDecay = Mathf.Pow(0.999f, Time.fixedDeltaTime / 0.001f);
    outputM = new Vector3[3];
    ballRadius = 0.0335f;
    ballMass = 0.058f;
    ballSection = 0.0071f;
    rho = 1.23f;
    dragCoeff.Set(0.2f,0.3f);
    kBounce = 0.666f;
    bounce_treshold = 0.1f;
  }
  
  // used vars: V, P, Sp, Sl, dt, Vm, Vn, Vg, Vgn, Vgm, VFL, VSL, VUL, coeffs

  public Vector3[] Step(Vector3 p, Vector3 v, float Spin, float Slice, float dt)
  {
    // Omega: vector containing rad/millisecond rotations
    omega.x = 6.2832f * (Spin / 60000);     // Back/Top Spin (Pitch)
    omega.y = 6.2832f * (Slice / 60000);    // Left/Right effect (Yaw)

    P = p;
    V = v;
    Vn = V.normalized;
    Vu.Set(0, 1, 0);
    Vg = V;
    Vg.y = 0;
    Vgn = Vg.normalized;

    if(Vg.magnitude == 0)
    {
      liftCoeff = driftCoeff = 0;
      VforwardLocal.Set(0, 1, 0);
      VsideLocal.Set(1, 0, 0);
      VupLocal.Set(0,0, 1);
    }
    else
    {
      // Normalized movement versors, ball local reference frame
      VforwardLocal = Vg.normalized;
      VsideLocal = Vector3.Cross(Vu, VforwardLocal).normalized;
      VupLocal = Vector3.Cross(Vn, VsideLocal).normalized;
      VforwardLocal = Vector3.Cross(VupLocal, VsideLocal).normalized;
      
      // Lift and Drift coeffs: ratio of ball rotation and ball speed
      liftCoeff = Mathf.Sqrt( Mathf.Pow(Vn.x,2) + Mathf.Pow(Vn.z,2) ) * (ballRadius * omega.x * 1000) / V.magnitude;
      driftCoeff = Mathf.Sqrt(Mathf.Pow(Vn.x, 2) + Mathf.Pow(Vn.z, 2)) * (ballRadius * omega.y * 1000) / V.magnitude;
    }

    if(Mathf.Abs(P.z) > 18)
    {
      V.z *= -0.2f;
      if(P.z > 17.8f)
        P.z = 17.8f;
      else
        P.z = -17.8f;
    }
    if(Mathf.Abs(P.x) > 9)
    {
      V.x *= -0.2f;
      if (P.x > 9)
        P.x = 9;
      else
        P.x = -9;
    }

    Vg = V;
    Vg.y = 0;
    Vgn = Vg.normalized;
    Vgn = Vg.normalized;
  
    if (P.y < 0)
    {
      // Bounce
      landed = Mathf.Abs(V.y) < bounce_treshold;
             
      if(landed)
      {
        V.x += V.y * Vgn.x;
        V.z += V.y * Vgn.z;
        V.y = 0;       
        Spin = 0;
        Slice = 0;
      }
      else
      {
        impactAlpha = Mathf.Atan2(V.y, Vg.magnitude);
        beta = (((Spin + 5000) / 10000) - 0.5f) * Mathf.Abs(impactAlpha);
        V.y = V.magnitude * Mathf.Sin( -impactAlpha -beta);
      
        // Dissipate energy
        V *= kBounce;

        V.x = (V.magnitude * Mathf.Cos( -impactAlpha -beta)) * Vgn.x;
        V.z = (V.magnitude * Mathf.Cos( -impactAlpha -beta)) * Vgn.z;

        if (Spin >= 0)
          Spin /= 2;
        else
          Spin /= -4;
        Slice /= 2;
      }

      P.y = 0;
    }
    else
    {
      // Drag
      // ----
      coeffsD = -0.5f * dragCoeff[landed ? 1 : 0] * rho * ballSection;
      dragAccel.x = coeffsD * Mathf.Pow(v.magnitude, 2) * v.normalized.x / ballMass;
      dragAccel.y = coeffsD * Mathf.Pow(v.magnitude, 2) * v.normalized.y / ballMass;
      dragAccel.z = coeffsD * Mathf.Pow(v.magnitude, 2) * v.normalized.z / ballMass;
            
      // Magnus - Top/Back Spin
      // -------------
      coeffsM = 0.25f * liftCoeff * rho * ballSection;

      magnusAccel.x = coeffsM * Mathf.Pow(v.magnitude, 2) * Vector3.Cross(VsideLocal, Vn).normalized.x / ballMass;
      magnusAccel.y = coeffsM * Mathf.Pow(v.magnitude, 2) * Vector3.Cross(VsideLocal, Vn).normalized.y / ballMass;
      magnusAccel.z = coeffsM * Mathf.Pow(v.magnitude, 2) * Vector3.Cross(VsideLocal, Vn).normalized.z / ballMass;

      // Magnus - Left/Right Spin
      // ------------------------
      coeffsS = driftCoeff * rho * ballSection;

      sideAccel.x = coeffsS * Mathf.Pow(v.magnitude, 2) * Vector3.Cross(VupLocal, Vn).normalized.x / ballMass;
      sideAccel.y = coeffsS * Mathf.Pow(v.magnitude, 2) * Vector3.Cross(VupLocal, Vn).normalized.y / ballMass;
      sideAccel.z = coeffsS * Mathf.Pow(v.magnitude, 2) * Vector3.Cross(VupLocal, Vn).normalized.z / ballMass;

      // Update V
      V.x += dt * (dragAccel.x + magnusAccel.x + sideAccel.x);
      V.y += dt * (dragAccel.y + magnusAccel.y + sideAccel.y - g);
      V.z += dt * (dragAccel.z + magnusAccel.z + sideAccel.z);
    }

    P += dt * V;
    Spin *= kSpinDecay;
    Slice *= kSliceDecay;

    outputM[0] = P;
    outputM[1] = V;
    outputM[2] = new Vector3(Spin, Slice, 0);

    return outputM;
  }

}
