using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    Rigidbody ship;

    float qtrScreenH;
    float qtrScreenW;

    bool adjustPitch = false;
    bool adjustYaw = false;
    bool adjustRoll = false;
    bool adjustThrustX = false;
    bool adjustThrustY = false;
    bool adjustThrustZ = false;

    [ReadOnlyAttribute]
    public Vector3 mousePosition;
    public float mouseDeadZone = 0.1f;
    Vector3 centerScreen;

    float pitch = 0.0f;
    float yaw = 0.0f;
    float roll = 0.0f;

    float pitchDiff = 0.0f;
    float yawDiff = 0.0f;

    Vector3 thrust = Vector3.zero;
    
    // THROTTLE
    public float throttle = 100f;
    [Range(0,50)]
    public float throttleAmount = 0.25f;
    [Range(0,500f)]
    public float maxThrottle = 4f;
    [Range(-500,100f)]
    public float minThrottle = -2f;

    // FLIGHT CONTROL PARAMETERS
    [Range(0, 100f)]
    public float pitchStrength = 1.5f;
    [Range(0, 100f)]
    public float yawStrength = 1.5f;
    [Range(0, 10f)]
    public float rollStrength = 1.5f;

    public bool flightAssist = false;

    // IMPULSE MODE
    float impulseTimer;
    public bool impulseMode = false;
    public float impulseCoolDown = 3.0f;


    /// <summary>
    /// Initialize ship controller and capture screen information.
    /// </summary>
    void Start () {
        centerScreen = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        ship = GetComponent<Rigidbody>();
        qtrScreenH = Screen.height * 0.25f;
        qtrScreenW = Screen.width * 0.25f;
	}


    /// <summary>
    /// Called by the UnityEngine, should update once per frame.
    /// </summary>
    void Update ()
    {
        // Remove this for production build
        DebugUpdate();
        //

        UpdateTimers();
        InputUpdate();

        if (flightAssist)
        {
            DampenTransform();
        }
    }


    void UpdateTimers()
    {
        impulseTimer += Time.deltaTime;
    }


    /// <summary>
    /// Fixed step update used for physics calculations.
    /// </summary>
    void FixedUpdate()
    {
        InputFixedUpdate();
    }


    /// <summary>
    /// Let's try to keep things as clean as possible. property dumps should be out here.
    /// </summary>
    void DebugUpdate()
    {

    }


    /// <summary>
    /// This method handles input that doesn't deal with the physics engine.
    /// </summary>
    void InputUpdate()
    {
        mousePosition = Input.mousePosition;
        pitch = GetPitchValue();
        yaw = GetYawValue();
        roll = GetRollValue();
        thrust.x = Input.GetAxis("Horizontal");
        thrust.y = GetThrustY();
        thrust.z = Input.GetAxis("Vertical"); // Z is forward/Back

        // Set Flags
        adjustPitch = Mathf.Abs(pitch) > 0.1f;
        adjustYaw = Mathf.Abs(yaw) > 0.1f;
        adjustRoll = roll != 0;
        adjustThrustX = Mathf.Abs(thrust.x) > 0.1f;
        adjustThrustY = thrust.y != 0;
        adjustThrustZ = Mathf.Abs(thrust.z) > 0.1f;


        // Throttle up
        if (Input.GetKey(KeyCode.Equals))
        {
            throttle += throttleAmount;
        }

        // Throttle down
        if (Input.GetKey(KeyCode.Minus))
        {
            throttle -= throttleAmount;
        }

        // Toggle Inertial dampeners
        if (Input.GetKeyUp(KeyCode.CapsLock))
        {
            flightAssist = !flightAssist;
        }

        throttle = Mathf.Clamp(throttle, minThrottle, maxThrottle);
    }


    /// <summary>
    /// This method handles the physics related to input.
    /// </summary>
    void InputFixedUpdate()
    {
        // ADJUST PITCH (FORWARD/BACK/TILT/LOCAL X)
        if (adjustPitch)
            ship.AddTorque(transform.right * (-pitch * pitchStrength), ForceMode.Force);

        // ADJUST YAW (LEFT/RIGHT/TURN/LOCAL Y)
        if (adjustYaw)
            ship.AddTorque(transform.up * (yaw * yawStrength), ForceMode.Force);

        // ADJUST ROLL (CLOCKWISE/COUNTERCLOCKWISE/LOCAL Z)
        if(adjustRoll)
            ship.AddTorque(transform.forward * (roll * rollStrength), ForceMode.Force);

        // ADJUST THRUST Z (FORWARD/BACK/LOCAL Z)
        if(adjustThrustZ)
        {
            if(!impulseMode)
            {
                ship.AddForce(transform.forward * (thrust.z * throttle), ForceMode.Force);
            }
            else if(impulseTimer >= impulseCoolDown)
            {
                ship.AddForce(transform.forward * (thrust.z * throttle), ForceMode.Impulse);
                impulseTimer = 0.0f;
            }
        }

        // ADJUST THRUST X (LEFT/RIGHT/STRAFE/LOCAL X)
        if (adjustThrustX)
        {
            if(!impulseMode)
            {
                ship.AddForce(transform.right * (thrust.x * throttle), ForceMode.Force);
            }
            else if (impulseTimer >= impulseCoolDown)
            {
                ship.AddForce(transform.right * (thrust.x * throttle), ForceMode.Impulse);
                impulseTimer = 0.0f;
            }
        }

        // ADJUST THRUST Y (UP/DOWN/ASCEND/DESCEND/LOCAL Y)
        if(adjustThrustY)
        {
            if(!impulseMode)
            {
                ship.AddForce(transform.up * (throttle * thrust.y), ForceMode.Force);
            }
            if (impulseMode && impulseTimer >= impulseCoolDown)
            {
                ship.AddForce(transform.up * (throttle * thrust.y), ForceMode.Impulse);
                impulseTimer = 0.0f;
            }
        }
    }


    /// <summary>
    /// Returns a pitch value based on the relative distance of the mouse from the center of the screen.
    /// </summary>
    /// <returns></returns>
    float GetPitchValue()
    {
        pitchDiff = -(centerScreen.y - mousePosition.y);
        pitchDiff = Mathf.Clamp(pitchDiff,-qtrScreenH,qtrScreenH);

        return (pitchDiff / qtrScreenH);
    }


    /// <summary>
    /// Returns a yaw value based on the relative position of the mouse from the center of the screen.
    /// </summary>
    /// <returns></returns>
    float GetYawValue()
    {
        yawDiff = -(centerScreen.x - mousePosition.x);
        yawDiff = Mathf.Clamp(yawDiff, -qtrScreenW, qtrScreenW);

        return (yawDiff / qtrScreenW);
    }


    /// <summary>
    /// Returns a digital axis.
    /// </summary>
    /// <returns></returns>
    float GetRollValue()
    {
        if(Input.GetKey(KeyCode.Q))
            return 1.0f;

        if(Input.GetKey(KeyCode.E))
            return -1.0f;

        return 0;
    }


    /// <summary>
    /// Returns a digital axis.
    /// </summary>
    /// <returns></returns>
    float GetThrustY()
    {
        if(Input.GetKey(KeyCode.Space))
            return 1;

        if(Input.GetKey(KeyCode.LeftShift))
            return -1;

        return 0.0f;
    }


    /// <summary>
    /// Dampens the velocity and angular velocity of the rigid body over time.
    /// </summary>
    void DampenTransform()
    {
        Vector3 nVeloc = new Vector3(
            Mathf.Lerp(ship.velocity.x, 0, Time.deltaTime * 0.75f),
            Mathf.Lerp(ship.velocity.y, 0, Time.deltaTime * 0.75f),
            Mathf.Lerp(ship.velocity.z, 0, Time.deltaTime * 0.75f)
            );

        Vector3 nAVeloc = new Vector3(
            Mathf.Lerp(ship.angularVelocity.x, 0, Time.deltaTime),
            Mathf.Lerp(ship.angularVelocity.y, 0, Time.deltaTime),
            Mathf.Lerp(ship.angularVelocity.z, 0, Time.deltaTime)
            );

        ship.velocity = nVeloc;
        ship.angularVelocity = nAVeloc;
    }
}
