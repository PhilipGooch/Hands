using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//https://github.com/NxRLab/ModernRobotics/blob/master/doc/roblib.pdf
public static class ModernRobotics
{

    public static bool relativeDynamics = true;
    //public static bool debug;
    #region BASIC HELPER FUNCTIONS

    public static bool NearZero(float z)
    {
        return Mathf.Abs(z) < 1e-6f;
    }

    #endregion

    #region  CHAPTER 3: RIGID-BODY MOTIONS

    ////Takes a 3-vector (angular velocity).
    ////Returns the skew symmetric matrix in so3.

    public static Matrix3x3 VecToso3(Vector3 omg)
    {
        return new Matrix3x3(
             0, -omg[2], omg[1],
            omg[2], 0, -omg[0],
            -omg[1], omg[0], 0);
    }


    ////Takes a 3x3 skew-symmetric matrix (an element of so(3)).
    ////Returns the corresponding vector (angular velocity).

    public static Vector3 so3ToVec(Matrix3x3 so3mat)
    {
        return new Vector3(so3mat[2, 1], so3mat[0, 2], so3mat[1, 0]);
    }


    //Takes A 3-vector of exponential coordinates for rotation.
    //Returns unit rotation axis omghat and the corresponding rotation angle
    //theta.
    public static void AxisAng3(Vector3 expc3, out Vector3 axis, out float angle)
    {
        axis = expc3.normalized;
        angle = expc3.magnitude;
    }


    //Takes a so(3) representation of exponential coordinates.
    //Returns R in SO(3) that is achieved by rotating about omghat by theta from
    //an initial orientation R = I.

    public static Matrix3x3 MatrixExp3(Matrix3x3 so3mat)
    {
        var omgtheta = so3ToVec(so3mat);
        var theta = (float)omgtheta.magnitude;
        if (NearZero(theta))
            return Matrix3x3.identity;
        else
        {
            var omgmat = so3mat / theta;
            return Matrix3x3.identity + Mathf.Sin(theta) * omgmat
               + (1 - Mathf.Cos(theta)) * (omgmat * omgmat);
        }
    }

    //Takes transformation matrix T in SE(3). 
    //Returns R: The corresponding rotation matrix,
    //        p: The corresponding position vector.
    public static void TransToRp(Matrix4x4 T, out Matrix3x3 R, out Vector3 p)
    {

        R = new Matrix3x3(
            T[0, 0], T[0, 1], T[0, 2],
            T[1, 0], T[1, 1], T[1, 2],
            T[2, 0], T[2, 1], T[2, 2]);
        p = new Vector3(T[0, 3], T[1, 3], T[2, 3]);
    }



    //Takes a transformation matrix T. 
    //Returns its inverse.
    //Uses the structure of transformation matrices to avoid taking a matrix
    //inverse, for efficiency.
    public static Matrix4x4 TransInv(Matrix4x4 T)
    {
        Matrix3x3 R;
        Vector3 p;
        TransToRp(T, out R, out p);
        var Rt = R.transposed;
        ////return np.r_[np.c_[Rt, -np.dot(Rt, p)], [[0, 0, 0, 1]]]
        return Matrix4x4Util.Create(Rt, -Rt * p, 0,0,0,1);

    }

    //Converts a spatial velocity vector into a 4x4 matrix in se3
    //param V: A 6-vector representing a spatial velocity
    //return: The 4x4 se3 representation of V
    public static Matrix4x4 VecTose3(Vector6 V)
    {
        return Matrix4x4Util.Create(
            VecToso3(V.FirstVector3()), V.SecondVector3(),
            0, 0, 0, 0);

    }

    // Converts an se3 matrix into a spatial velocity vector
    //param se3mat: A 4x4 matrix in se3
    //return: The spatial velocity 6-vector corresponding to se3mat
    public static Vector6 se3ToVec(Matrix4x4 se3mat)
    {
        return new Vector6(se3mat[2, 1], se3mat[0, 2], se3mat[1, 0],
                 se3mat[0, 3], se3mat[1, 3], se3mat[2, 3]);
    }

    //Takes T a transformation matrix SE(3).
    //Returns the corresponding 6x6 adjoint representation [AdT].
    public static Matrix6x6 Adjoint(Matrix4x4 T)
    {

        Matrix3x3 R;
        Vector3 p;
        TransToRp(T, out R, out p);

        return new Matrix6x6(R, Matrix3x3.zero,
            VecToso3(p) * R, R);

        //return np.r_[np.c_[R, np.zeros((3, 3))],
        //     np.c_[np.dot(VecToso3(p), R), R]]
    }

    //Takes a parametric description of a scre axis and converts it to a 
    //normalized screw axis
    //param q: A point lying on the screw axis
    //param s: A unit vector in the direction of the screw axis
    //param h: The pitch of the screw axis
    //return: A normalized screw axis described by the inputs
    public static Vector6 ScrewToAxis(Vector3 q, Vector3 s, float h)
    {
        return new Vector6(s, Vector3.Cross(q, s) + h*s);
    }


    // Takes a se(3) representation of exponential coordinates.
    // Returns a T matrix SE(3) that is achieved by traveling along/about the
    // screw axis S for a distance theta from an initial configuration T = I.

    public static Matrix4x4 MatrixExp6(Matrix4x4 se3mat)
    {

        var omgtheta = so3ToVec(se3mat.ToMatrix3x3());

        if (NearZero(omgtheta.magnitude))
        {
            return Matrix4x4Util.Create(
                Matrix3x3.identity,
                new Vector3(se3mat[0, 3], se3mat[1, 3], se3mat[2, 3]),
                0,0,0,1);
        }
        else
        {

            var theta = omgtheta.magnitude;// AxisAng3(omgtheta)[1]
            var omgmat = se3mat.ToMatrix3x3() / theta;

            return Matrix4x4Util.Create(
                MatrixExp3(se3mat.ToMatrix3x3()),

                       (Matrix3x3.identity * theta
                              + (1 - Mathf.Cos(theta)) * omgmat
                              + (theta - Mathf.Sin(theta)) * (omgmat * omgmat))
                        * new Vector3(se3mat[0, 3], se3mat[1, 3], se3mat[2, 3]) / theta,

                       0, 0, 0, 1);

        }
    }

   
    #endregion

    #region CHAPTER 8: DYNAMICS OF OPEN CHAINS

    //Takes 6-vector spatial velocity.
    //Returns the corresponding 6x6 matrix [adV].
    //Used to calculate the Lie bracket [V1, V2] = [adV1]V2

    public static Matrix6x6 ad(Vector6 V)
    {
        
        var omgmat = VecToso3(V.FirstVector3());
        return new Matrix6x6(
            omgmat, Matrix3x3.zero,
            VecToso3(V.SecondVector3()), omgmat);
    }

    
    //Takes thetalist: n-vector of joint variables,
    //      dthetalist: n-vector of joint rates,
    //      ddthetalist: n-vector of joint accelerations,
    //      g: Gravity vector g,
    //      Ftip: Spatial force applied by the end-effector expressed in frame 
    //            {n+1},
    //      Mlist: List of link frames {i} relative to {i-1} at the home 
    //             position,
    //      Glist: Spatial inertia matrices Gi of the links,
    //      Slist: Screw axes Si of the joints in a space frame, in the format
    //             of a matrix with axes as the columns,.
    //Returns taulist: The n-vector of required joint forces/torques.
    //This function uses forward-backward Newton-Euler iterations to solve the 
    //equation:
    //taulist = Mlist(thetalist)ddthetalist + c(thetalist,dthetalist) \
    //          + g(thetalist) + Jtr(thetalist)Ftip
   
    public static float [] InverseDynamics(float [] thetalist, float [] dthetalist, float [] ddthetalist, Vector3 g, Vector6 Ftip, Matrix4x4[] Mlist, Matrix6x6 [] Glist, Vector6[] Slist)
    {
        //Slist.Transpose();
        var n = thetalist.Length;
        var Mi = Matrix4x4.identity;
        var Ai = new Vector6[n];
        var AdTi = new Matrix6x6[n + 1];
        var Vi = new Vector6[n+1];
        var Vdi = new Vector6[n+1];
        Vdi[0] = new Vector6(Vector3.zero, -g);
        AdTi[n] = Adjoint(TransInv(Mlist[n]));
        var Fi = Ftip;
        var taulist = new float[n];
        for (int i = 0; i < n; i++)
        {
            Mi = Mi * Mlist[i];
            Ai[i] = Adjoint(TransInv(Mi)) * Slist[i];
            AdTi[i] = Adjoint(MatrixExp6(VecTose3(Ai[i] * -thetalist[i])) * TransInv(Mlist[i]));



            Vi[i + 1] = AdTi[i] * Vi[i] 
                + Ai[i] * dthetalist[i];
            Vdi[i+1] = AdTi[i]*Vdi[i] 
                + Ai[i]* ddthetalist[i]
                + ad(Vi[i+1])*Ai[i]*dthetalist[i];
        }

        for (int i = n - 1; i >= 0; i--)
        {
            Fi = AdTi[i + 1].transposed * Fi
                + Glist[i] * Vdi[i + 1]
                - (ad(Vi[i + 1]).transposed) * (Glist[i] * Vi[i + 1]);

//            Debug.LogFormat("{0}\n{1}\n{2}\n{3}\n{4}\n----------------", i, Fi.ToString(), AdTi[i + 1].transposed * Fi, Glist[i] * Vdi[i + 1], (ad(Vi[i + 1]).transposed) * (Glist[i] * Vi[i + 1]));

            taulist[i] = Vector6.Dot(Fi, Ai[i]);               
        }
        return taulist;
    }

    //Takes plist: parent index list
    //      thetalist: n-vector of joint variables,
    //      dthetalist: n-vector of joint rates,
    //      ddthetalist: n-vector of joint accelerations,
    //      g: Gravity vector g,
    //      Ftiplist: Spatial forces applied by the end-effectors expressed in their frame (zero for the rest)
    //      Mlist: List of link frames {i} relative to {i-1} at the home 
    //             position,
    //      Glist: Spatial inertia matrices Gi of the links, (ignored for end effectors)
    //      Slist: Screw axes Si of the joints in a space frame, in the format (Vector6.zero for end effectors)
    //             of a matrix with axes as the columns,.
    //Returns taulist: The n-vector of required joint forces/torques.
    //This function uses forward-backward Newton-Euler iterations to solve the 
    //equation:
    //taulist = Mlist(thetalist)ddthetalist + c(thetalist,dthetalist) \
    //          + g(thetalist) + Jtr(thetalist)Ftip

    public static float[] InverseDynamicsTree(int[] plist, float[] thetalist, float[] dthetalist, float[] ddthetalist, Vector3 g, Vector6[] Ftiplist, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist)
    {
        var n = thetalist.Length;
        var Mi = new Matrix4x4[n];
        var Ai = new Vector6[n];
        var AdTi = new Matrix6x6[n];
        var Vi = new Vector6[n];
        var Vdi = new Vector6[n];
        var Fi = new Vector6[n];
       
        var taulist = new float[n];

        for (int i = 0; i < n; i++)
        {
            var pi = plist[i];

            var Vipi = pi < 0 ? Vector6.zero : Vi[pi];
            var Vdipi = pi < 0 ? new Vector6(Vector3.zero, -g) : Vdi[pi];
            if (plist[i] < 0)//root
                Mi[i] = Mlist[i];
            else
                Mi[i] = Mi[plist[i]] * Mlist[i];

            
            Ai[i] = Adjoint(TransInv(Mi[i])) * Slist[i]; // screw axix in body frame
            AdTi[i] = Adjoint(MatrixExp6(VecTose3(Ai[i] * -thetalist[i])) * TransInv(Mlist[i])); // parent frame to local frame transform (including the angle around screw)

            Vi[i] = AdTi[i] * Vipi              // parent velocity expressed in local frame
                + Ai[i] * dthetalist[i];        // own velocity around local screw axis
            Vdi[i] = AdTi[i] * Vdipi            // parent acceleration expressed in local frame
                + Ai[i] * ddthetalist[i]        // own acceleration (desired) around screw axis
                + ad(Vi[i ]) * Ai[i] * dthetalist[i]; // acceleratoin due to velocity product term (achieved by derivation of the above)

            Fi[i] = Ftiplist[i]                                 // external force
                + Glist[i] * Vdi[i]                             // forces needed to create acceleration
                - (ad(Vi[i]).transposed) * (Glist[i] * Vi[i]);  // forces caused by velocity ( might be ignored by unity integration)
        }


        for (int i = n - 1; i >= 0; i--)
        {
            taulist[i] = Vector6.Dot(Fi[i], Ai[i]);     // force projected to screw axis

            if (plist[i] >= 0) // accumulate forces in parent
            {
                var pi = plist[i];
                Fi[pi] += AdTi[i].transposed * Fi[i];
            }
        }
        return taulist;
    }

    //Takes plist: parent list
    //      jointlist: joint list
    //      Alist: desired relative accelerations in world space
    //      GList: inertias
    //      alist: anchor list in local coordinates
    //      Ftiplist: external forces
    // Return: list of torques
    public static Vector3[] InverseDynamicsCharacter(int[] plist, ConfigurableJoint[] jointList, Vector6[] Alist, Vector3 g, Vector6[] Fextlist)
    {
        var n = jointList.Length;

        //var Ai = new Vector6[n];
        //var AdTi = new Matrix6x6[n];
        //var Vi = new Vector6[n];
        var Vdi = new Vector6[n];
        var Fi = new Vector6[n];

        var taulist = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            var joint = jointList[i];
            var body = joint.GetComponent<Rigidbody>();

            var pi = plist[i];
            var jointP = joint.transform.TransformPoint(joint.anchor);
#if false // joint space
            Vector6 Vipi,Vdipi;
            if(pi<0)
            {
                Vipi = Vector6.zero;
                Vdipi = new Vector6(Vector3.zero, -g);
            }
            else
            {
                Vipi = new Vector6(joint.connectedBody.angularVelocity, joint.connectedBody.velocity);
                Vdipi = Vdi[pi];
            }
            var Mi = joint.transform.localToWorldMatrix;//
            //var Ai = Adjoint(TransInv(Mi)) * Slist[i]; // screw axis in body frame
            var AdTi = Adjoint(TransInv(Mi)); // parent frame to local frame transform (including the angle around screw)

            var realtivevel = new Vector6(body.angularVelocity, body.velocity) - AdTi * Vipi; // velocity at joint
            var jointvel = new Vector6(body.angularVelocity, body.GetPointVelocity(joint.transform.TransformPoint(joint.anchor))) - AdTi*Vipi; // velocity at joint




            var Vi = AdTi * Vipi              // parent velocity expressed in local frame
                + realtivevel;//  Ai * dthetalist[i];        // own velocity around local screw axis
            Vdi[i] = AdTi * Vdipi            // parent acceleration expressed in local frame
                //for now desired acceleration is 0 //+ Ai * ddthetalist[i]        // own acceleration (desired) around screw axis
                + ad(Vi) * realtivevel;// Ai * dthetalist[i]; // acceleratoin due to velocity product term (achieved by derivation of the above)

            var G = Matrix6x6.Diagonal(body.inertiaTensor.x, body.inertiaTensor.y, body.inertiaTensor.z, body.mass, body.mass, body.mass);
            var FBody =// Ftiplist[i]                                 // external force
                 G * Vdi[i]                             // forces needed to create acceleration
                - (ad(Vi).transposed) * (G * Vi);  // forces caused by velocity ( might be ignored by unity integration)
            var FWorld = Adjoint(Mi).transposed * FBody;
#else
            var ViPi = pi<0?Vector6.zero : Adjoint(Matrix4x4.Translate(joint.connectedBody.worldCenterOfMass))* new Vector6(joint.connectedBody.angularVelocity, joint.connectedBody.velocity);
            var Vi = Adjoint(Matrix4x4.Translate(body.worldCenterOfMass)) * new Vector6(body.angularVelocity, body.velocity);

            if (relativeDynamics)
            {
                Vdi[i] = (pi < 0 ? new Vector6(Vector3.zero, -g) : Vdi[pi]) // parent acceleration
                    + Alist[i] // desired acceleration
                      - ad(Vi) * (Vi - ViPi); // acceleration in moving frame of reference
            }
            else
            {
                if (pi < 0)
                    Vdi[i] = new Vector6(Vector3.zero, -g) + Alist[i];
                else
                {
                    // in absolute mode, only linear acceleration at joint point is used for children
                    // as dynamics is solved to prevent world angular velocity from changing
                    var jointAccel = Adjoint(Matrix4x4.Translate(-jointP)) * Vdi[pi];
                    jointAccel[0] = jointAccel[1] = jointAccel[2] = 0;
                    Vdi[i] = jointAccel + Alist[i];
                }
                        
            }

            // wrench is external force + force needed to make acceleration

            var Mtensor = body.transform.localToWorldMatrix* Matrix4x4.TRS(body.centerOfMass, body.inertiaTensorRotation, Vector3.one);
            var MtensorToBody = Matrix4x4.Rotate(body.transform.rotation) * Matrix4x4.TRS(body.centerOfMass, body.inertiaTensorRotation, Vector3.one);

            var G = Matrix6x6.Diagonal(body.inertiaTensor.x, body.inertiaTensor.y, body.inertiaTensor.z, body.mass, body.mass, body.mass);
            //var ViBody = new Vector6(body.angularVelocity, body.velocity);
            var ViTensor = //Adjoint(TransInv(MtensorToBody)) * ViBody;
                Adjoint(TransInv(Mtensor)) * Vi;
            var VdiTensor = Adjoint(TransInv(Mtensor)) * Vdi[i]; // acceleration in tensor space
            var FTensor = G * VdiTensor // torque to reach desired acceleration
               - ad(ViTensor).transposed * G * ViTensor; // counteract gyroscopic effect torque
            var FWorld = Adjoint(TransInv(Mtensor)).transposed * FTensor;

            var FBody = Adjoint(TransInv(Matrix4x4.Translate(-joint.transform.position))).transposed * FWorld;
#endif


            Fi[i] = Fextlist[i] // external wrench
                + FWorld;

            //Debug.DrawRay(joint.transform.position, FBody.FirstVector3()/100, Color.red, .1f);
            //Debug.DrawRay(joint.transform.position, FBody.SecondVector3()/100, Color.green, .1f);


            Debug.DrawRay(joint.transform.position, (Adjoint(Matrix4x4.Translate(-body.position)) * Vdi[i]).FirstVector3() /100, Color.blue, .1f);
            Debug.DrawRay(joint.transform.position, (Adjoint(Matrix4x4.Translate(-body.position)) * Vdi[i]).SecondVector3() / 100, Color.blue, .1f);



            //Debug.DrawRay(joint.transform.position, (Adjoint(Matrix4x4.Translate(-body.position)) * Alist[i]).FirstVector3() / 100, Color.yellow, .1f);
            //Debug.DrawRay(joint.transform.position, (Adjoint(Matrix4x4.Translate(-body.position)) * Alist[i]).SecondVector3() / 100, Color.yellow, .1f);

            //var FAnchor = Adjoint(TransInv(Matrix4x4.Translate(-jointP))).transposed * FWorld;
            //Debug.DrawRay(jointP, FAnchor.FirstVector3() / 100, Color.red, .1f);
            //Debug.DrawRay(jointP, FAnchor.SecondVector3() / 100, Color.green, .1f);

        }


        for (int i = n - 1; i >= 0; i--)
        {
            // transfor wrench to anchor frame of reference
            var joint = jointList[i];
            var jointP = joint.transform.TransformPoint(joint.anchor);
            var jointSpaceF =Adjoint(TransInv(Matrix4x4.Translate(-jointP))).transposed* Fi[i];
            Debug.DrawRay(jointP, jointSpaceF.FirstVector3() / 100, Color.red, .1f);
            Debug.DrawRay(jointP, jointSpaceF.SecondVector3() / 100, Color.green, .1f);
            taulist[i] = jointSpaceF.FirstVector3(); // torque at anchor
          //  Debug.LogFormat()
            if (plist[i] >= 0) // accumulate forces in parent
            {
                var pi = plist[i];
                Fi[pi] += Fi[i];
            }
        }
        return taulist;
    }

    //Takes plist: parent index list
    //      thetalist: n-vector of joint variables,
    //      dthetalist: n-vector of joint rates,
    //      ddthetalist: n-vector of joint accelerations,
    //      g: Gravity vector g,
    //      Ftiplist: Spatial forces applied by the end-effectors expressed in their frame (zero for the rest)
    //      Mlist: List of link frames {i} relative to {i-1} at the home 
    //             position,
    //      Glist: Spatial inertia matrices Gi of the links, (ignored for end effectors)
    //      Slist: Screw axes Si of the joints in a space frame, in the format (Vector6.zero for end effectors)
    //             of a matrix with axes as the columns,.
    //Returns taulist: The n-vector of required joint forces/torques.
    //This function uses forward-backward Newton-Euler iterations to solve the 
    //equation:
    //taulist = Mlist(thetalist)ddthetalist + c(thetalist,dthetalist) \
    //          + g(thetalist) + Jtr(thetalist)Ftip

    public static float[] InverseDynamicsFloatingBase(int[] plist, float[] thetalist, float[] dthetalist, float[] ddthetalist, Vector6 vbase, Vector3 g, Vector6[] Ftiplist, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist, out Vector6 rootWrench)
    {
        var n = thetalist.Length;
        var Mi = new Matrix4x4[n];
        var Ai = new Vector6[n];
        var AdTi = new Matrix6x6[n];
        var Vi = new Vector6[n];
        var Vdi = new Vector6[n];
        var Fi = new Vector6[n];
        var Ici = new Matrix6x6[n];
        var taulist = new float[n];

        //// init base
        //Mi[0] = Mlist[0];
        Vi[0] = vbase; // get actual velocity
        Vdi[0] = new Vector6(Vector3.zero, -g); // TODO: check if sum of all feedback forces is better
        Ici[0] = Glist[0];
        Fi[0] = Ftiplist[0] // external forces in local frame
                + Glist[0] * Vdi[0] // acceleration forces
                - (ad(Vi[0]).transposed) * (Glist[0] * Vi[0]);

        // same as regular
        for (int i = 1; i < n; i++)
        {
            var pi = plist[i];


            var Vipi = pi < 0 ? Vector6.zero : Vi[pi];
            var Vdipi = pi < 0 ? new Vector6(Vector3.zero, -g) : Vdi[pi];
            if (plist[i] < 0)//root
                Mi[i] = Mlist[i];
            else
                Mi[i] = Mi[plist[i]] * Mlist[i];

            Ai[i] = Adjoint(TransInv(Mi[i])) * Slist[i];
            AdTi[i] = Adjoint(MatrixExp6(VecTose3(Ai[i] * -thetalist[i])) * TransInv(Mlist[i]));

            Vi[i] = AdTi[i] * Vipi
                + Ai[i] * dthetalist[i];
            Vdi[i] = AdTi[i] * Vdipi
                + Ai[i] * ddthetalist[i]
                + ad(Vi[i]) * Ai[i] * dthetalist[i];

            Ici[i] = Glist[i];

            Fi[i] = Ftiplist[i] // exteernal forces
                + Glist[i] * Vdi[i] // acceleration forces
                - (ad(Vi[i]).transposed) * (Glist[i] * Vi[i]); // square forces
        }


        for (int i = n - 1; i >= 1; i--)
        {
            var pi = plist[i];
            Ici[pi] += AdTi[i].transposed * Ici[i] * AdTi[i];
            Fi[pi] += AdTi[i].transposed * Fi[i];
        }


        var alist = new Vector6[n];
        alist[0] = -Ici[0].transposed * Fi[0];// todo check if transposed matches inverse
        for (int i = 1; i < n; i++)
        {
            alist[i] = AdTi[i] * alist[plist[i]];
            taulist[i] = Vector6.Dot(Fi[i] + Ici[i] * alist[i], Ai[i]);
        }


        //for (int i = n - 1; i >= 0; i--)
        //{
        //    taulist[i] = Vector6.Dot(Fi[i], Ai[i]);

        //    if (plist[i] >= 0) // accumulate forces in parent
        //    {
        //        var pi = plist[i];
        //        Fi[pi] += AdTi[i].transposed * Fi[i];
        //    }
        //}
        rootWrench = Fi[0];
        return taulist;
    }

    public static Vector6 TipForceToSegment(Matrix4x4 Mtip, Vector6 Ftip)
    {
        var AdTi = Adjoint(TransInv(Mtip));
        return AdTi.transposed *Ftip;
    }

    //Takes thetalist: A list of joint variables,
    //      Mlist: List of link frames i relative to i-1 at the home position,
    //      Glist: Spatial inertia matrices Gi of the links,
    //      Slist: Screw axes Si of the joints in a space frame, in the format
    //             of a matrix with axes as the columns.
    //Returns M: The numerical inertia matrix M(thetalist) of an n-joint serial 
    //           chain at the given configuration thetalist.
    //This function calls InverseDynamics n times, each time passing a 
    //ddthetalist vector with a single element equal to one and all other inputs
    //set to zero. 
    //Each call of InverseDynamics generates a single column, and these columns 
    //are assembled to create the inertia matrix.
    public static float[][] MassMatrix(float[] thetalist, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist)
    {

        var n = thetalist.Length;
        var dthetalist = new float[n];
        var ddthetalist = new float[n];
        var M = new float [n][];
        for (int i = 0; i < n; i++)
        {
            ddthetalist[i] = 1;
            M[i] = InverseDynamics(thetalist, dthetalist, ddthetalist,
                                     Vector3.zero, Vector6.zero, Mlist,
                                     Glist, Slist);
            ddthetalist[i] = 0;
        }
        return M;
    }
    //Takes plist: parent index list
    //      thetalist: A list of joint variables,
    //      Mlist: List of link frames i relative to i-1 at the home position,
    //      Glist: Spatial inertia matrices Gi of the links,
    //      Slist: Screw axes Si of the joints in a space frame, in the format
    //             of a matrix with axes as the columns.
    //Returns M: The numerical inertia matrix M(thetalist) of an n-joint serial 
    //           chain at the given configuration thetalist.
    //This function calls InverseDynamics n times, each time passing a 
    //ddthetalist vector with a single element equal to one and all other inputs
    //set to zero. 
    //Each call of InverseDynamics generates a single column, and these columns 
    //are assembled to create the inertia matrix.
    public static float[][] MassMatrixTree(int []plist, float[] thetalist, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist)
    {

        var n = thetalist.Length;
        var dthetalist = new float[n];
        var ddthetalist = new float[n];
        var Ftiplist = new Vector6[n];
        var M = new float[n][];
        for (int i = 0; i < n; i++)
        {
            ddthetalist[i] = 1;
            M[i] = InverseDynamicsTree(plist, thetalist, dthetalist, ddthetalist,
                                     Vector3.zero, Ftiplist, Mlist,
                                     Glist, Slist);
            ddthetalist[i] = 0;
        }
        return M;
    }

    public static Vector6 TransformV(Matrix4x4 M, Vector6 v)
    {
        return Adjoint(M) * v;
    }
    public static Vector6 TransformF(Matrix4x4 M, Vector6 f)
    {
        return Adjoint(TransInv(M)).transposed * f;
    }
    public static Matrix6x6 TranfsormI(Matrix4x4 M, Matrix6x6 I)
    {
        var AdT = Adjoint(TransInv(M));
        return AdT.transposed * I * AdT;
    }


    #endregion

    #region CHAPTER 11: ROBOT CONTROL
    // thetalist: n-vector of joint variables,
    // dthetalist: n-vector of joint rates,
    // eint: n-vector of the time-integral of joint errors,
    // g: Gravity vector g,
    // Mlist: List of link frames {i} relative to {i-1} at the home position,
    // Glist: Spatial inertia matrices Gi of the links,
    // Slist: Screw axes Si of the joints in a space frame, in the format of a matrix with axes as the columns,
    // thetalistd: n-vector of reference joint variables,
    // dthetalistd: n-vector of reference joint velocities,
    // ddthetalistd: n-vector of reference joint accelerations,
    // Kp: The feedback proportional gain (identical for each joint),
    // Ki: The feedback integral gain (identical for each joint),
    // Kd: The feedback derivative gain (identical for each joint).

    // Returns taulist: The vector of joint forces/torques computed by the feedback linearizing controller at the current instant.


    public static float[] ComputedTorque(float[] thetalist, float[] dthetalist, float[] eint, Vector3 g, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist, float[] thetalistd, float[] dthetalistd, float[] ddthetalistd, float Kp, float Ki, float Kd)
    {
        var n = thetalist.Length;

        var massMatrix = MassMatrix(thetalist, Mlist, Glist, Slist);
        var tau = InverseDynamics(thetalist, dthetalist, ddthetalistd, g, Vector6.zero, Mlist, Glist, Slist);
        for (int i = 0; i < n; i++)
        {
            var e = thetalistd[i] - thetalist[i];
            var pid = Kp * e +
                    Ki * (eint[i] + e) +
                    Kd * (dthetalistd[i] - dthetalist[i]);
            for (int j = 0; j < n; j++)
                tau[j] += massMatrix[i][j] * pid;
        }
        return tau;

    }

    // plist: parent index list
    // thetalist: n-vector of joint variables,
    // dthetalist: n-vector of joint rates,
    // eint: n-vector of the time-integral of joint errors,
    // g: Gravity vector g,
    // Mlist: List of link frames {i} relative to {i-1} at the home position,
    // Glist: Spatial inertia matrices Gi of the links,
    // Slist: Screw axes Si of the joints in a space frame, in the format of a matrix with axes as the columns,
    // thetalistd: n-vector of reference joint variables,
    // dthetalistd: n-vector of reference joint velocities,
    // ddthetalistd: n-vector of reference joint accelerations,
    // Kp: The feedback proportional gain (identical for each joint),
    // Ki: The feedback integral gain (identical for each joint),
    // Kd: The feedback derivative gain (identical for each joint).

    // Returns taulist: The vector of joint forces/torques computed by the feedback linearizing controller at the current instant.


    public static float[] ComputedTorqueTree(int [] plist, float[] thetalist, float[] dthetalist, float[] eint, Vector3 g, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist, float[] thetalistd, float[] dthetalistd, float[] ddthetalistd, float Kp, float Ki, float Kd)
    {
        var n = thetalist.Length;
        var Ftiplist = new Vector6[n];
        var massMatrix = MassMatrixTree(plist, thetalist, Mlist, Glist, Slist);
        var tau = InverseDynamicsTree(plist, thetalist, dthetalist, ddthetalistd, g, Ftiplist, Mlist, Glist, Slist);
        for (int i = 0; i < n; i++)
        {
            var e = thetalistd[i] - thetalist[i];
            var pid = Kp * e +
                    Ki * (eint[i] + e) +
                    Kd * (dthetalistd[i] - dthetalist[i]);
            for (int j = 0; j < n; j++)
                tau[j] += massMatrix[i][j] * pid;
        }
        return tau;

    }
    //    public static Vector3[] InverseDynamicsCharacter(int[] plist, ConfigurableJoint[] jointList, Vector6[] Alist, Vector3 g, Vector6[] Fextlist)

    public static Vector3[] ComputedTorqueCharacter(int[] plist, ConfigurableJoint[] jointList, Vector3 g, Vector6 [] Alist,  float Kp, float Ki, float Kd)
    {
        var n = jointList.Length;
        var Ftiplist = new Vector6[n];
        //var massMatrix = MassMatrixCharacter(plist, thetalist, Mlist, Glist, Slist);

        // try to update AList to force pid
        for (int i = 0; i < n; i++)
        {
            var joint = jointList[i];
            var body = joint.GetComponent<Rigidbody>();
            var angular = body.angularVelocity;
            var rotation = body.rotation;
            var pi = plist[i];

            if (relativeDynamics&& pi >= 0)
            {
                var parent = joint.connectedBody;
                angular -=  parent.angularVelocity;
                rotation = Quaternion.Inverse(parent.rotation)*rotation;
            }
            float angle;
            Vector3 axis;
            rotation.ToAngleAxis(out angle, out axis);
            angle *= Mathf.Deg2Rad;
            var pid =
                Kp * axis * (-angle)
                + Kd * (-angular);
            //pid = Vector3.ClampMagnitude(pid, 10);
            //var AdTi = Adjoint(TransInv(Mtip));
            //return AdTi.transposed * Ftip;

            Alist[i] = //ModernRobotics.Adjoint( joint.transform.localToWorldMatrix)*
                ModernRobotics.Adjoint(Matrix4x4.Translate( joint.transform.position))* 
                new Vector6( pid,Vector3.zero); // acceleration in world frame
            //Alist[i] = body.angularVelocity;
        }
        var tau = InverseDynamicsCharacter(plist, jointList, Alist, g, Ftiplist);


        //for (int i = 0; i < n; i++)
        //{
        //    var e = thetalistd[i] - thetalist[i];
        //    var pid = Kp * e +
        //            Ki * (eint[i] + e) +
        //            Kd * (dthetalistd[i] - dthetalist[i]);
        //    for (int j = 0; j < n; j++)
        //        tau[j] += massMatrix[i][j] * pid;
        //}
        return tau;

    }
    // plist: parent index list
    // thetalist: n-vector of joint variables,
    // dthetalist: n-vector of joint rates,
    // eint: n-vector of the time-integral of joint errors,
    // g: Gravity vector g,
    // Mlist: List of link frames {i} relative to {i-1} at the home position,
    // Glist: Spatial inertia matrices Gi of the links,
    // Slist: Screw axes Si of the joints in a space frame, in the format of a matrix with axes as the columns,
    // thetalistd: n-vector of reference joint variables,
    // dthetalistd: n-vector of reference joint velocities,
    // ddthetalistd: n-vector of reference joint accelerations,
    // Kp: The feedback proportional gain (identical for each joint),
    // Ki: The feedback integral gain (identical for each joint),
    // Kd: The feedback derivative gain (identical for each joint).

    // Returns taulist: The vector of joint forces/torques computed by the feedback linearizing controller at the current instant.


    public static float[] ComputedTorqueFloatingBase(int[] plist, float[] thetalist, float[] dthetalist, float[] eint, Vector6 vbase, Vector3 g, Matrix4x4[] Mlist, Matrix6x6[] Glist, Vector6[] Slist, float[] thetalistd, float[] dthetalistd, float[] ddthetalistd, float Kp, float Ki, float Kd, out Vector6 rootWrench)
    {
        var n = thetalist.Length;
        var Ftiplist = new Vector6[n];
        
        var tau = InverseDynamicsFloatingBase(plist, thetalist, dthetalist, ddthetalistd, vbase, g, Ftiplist, Mlist, Glist, Slist, out rootWrench);

        var massMatrix = MassMatrixTree(plist, thetalist, Mlist, Glist, Slist);
        for (int i = 0; i < n; i++)
        {
            var e = thetalistd[i] - thetalist[i];
            var pid = Kp * e +
                    Ki * (eint[i] + e) +
                    Kd * (dthetalistd[i] - dthetalist[i]);

            for (int j = 0; j < n; j++)
                tau[j] += massMatrix[i][j] * pid;
        }
        return tau;

    }
#endregion


}