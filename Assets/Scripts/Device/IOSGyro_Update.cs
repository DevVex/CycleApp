using UnityEngine;
using System.Collections;

public class IOSGyro_Update : MonoBehaviour {
	
	/// <summary>
	/// Gyroscope controller that works with any device orientation.
	/// </summary>
	
	#region [Private fields]
	
	private bool gyroEnabled = true;
	private const float lowPassFilterFactor = 0.2f;
	
	private readonly Quaternion baseIdentity =  Quaternion.Euler(90, 0, 0);
	private readonly Quaternion landscapeRight =  Quaternion.Euler(0, 0, 90);
	private readonly Quaternion landscapeLeft =  Quaternion.Euler(0, 0, -90);
	private readonly Quaternion upsideDown =  Quaternion.Euler(0, 0, 180);
	
	private Quaternion cameraBase =  Quaternion.identity;
	private Quaternion calibration =  Quaternion.identity;
	private Quaternion baseOrientation =  Quaternion.Euler(90, 0, 0);
	private Quaternion baseOrientationRotationFix =  Quaternion.identity;
	
	private Quaternion referanceRotation = Quaternion.identity;
	private bool debug = true;
	
	
	//Debug Labels
	public bool showOrientLabel;
	public bool showCalibrateLabel;
	public bool showCamBaseLabel;
	public bool showGyroAltLabel;
	public bool showTransformLabel;
	

	
	#endregion
	
	#region [Unity events]
	

	protected void Start () 
	{
		AttachGyro();
	}
	
	protected void Update() 
	{
		if (!gyroEnabled)
			return;

#if UNITY_IPHONE
		
		transform.rotation = Quaternion.Slerp(transform.rotation,
		                                      cameraBase * ( ConvertRotation(referanceRotation * Input.gyro.attitude) * GetRotFix()), lowPassFilterFactor);
	
#endif
    }
	
	protected void OnGUI()
	{
		//if (!debug)
			return;
		
		
		if (showOrientLabel)
			GUILayout.Label("Orientation: " + Screen.orientation);
		if (showCalibrateLabel)
			GUILayout.Label("Calibration: " + calibration);
		if (showCamBaseLabel)
			GUILayout.Label("Camera base: " + cameraBase);
		if (showGyroAltLabel)
			GUILayout.Label("input.gyro.attitude: " + Input.gyro.attitude);
		if (showTransformLabel)
			GUILayout.Label("transform.rotation: " + transform.rotation);
		
		//Spacers
		GUILayout.Label("");
		GUILayout.Label("");
		GUILayout.Label("");
		GUILayout.Label("");
		GUILayout.Label("");
		GUILayout.Label("");
		
		
		if (GUILayout.Button("Update gyro calibration (Horizontal only)", GUILayout.Height(20)))
		{
			UpdateCalibration(true);
		}
		
		if (GUILayout.Button("Update camera base rotation (Horizontal only)", GUILayout.Height(20)))
		{
			UpdateCameraBaseRotation(true);
		}
		
		if (GUILayout.Button("Reset base orientation", GUILayout.Height(20)))
		{
			ResetBaseOrientation();
		}
		
		if (GUILayout.Button("Reset camera rotation", GUILayout.Height(20)))
		{
			transform.rotation = Quaternion.identity;
		}
	}
	
	#endregion
	
	#region [Public methods]
	
	public void TurnOffGyro(){
		Input.gyro.enabled = !Input.gyro.enabled;
	}
	
	/// <summary>
	/// Attaches gyro controller to the transform.
	/// </summary>
	private void AttachGyro()
	{
		gyroEnabled = true;
		ResetBaseOrientation();
		UpdateCalibration(true);
		UpdateCameraBaseRotation(true);
		RecalculateReferenceRotation();
	}
	
	/// <summary>
	/// Detaches gyro controller from the transform
	/// </summary>
	private void DetachGyro()
	{
		gyroEnabled = false;
	}
	
	#endregion
	
	#region [Private methods]
	
	/// <summary>
	/// Update the gyro calibration.
	/// </summary>
	private void UpdateCalibration(bool onlyHorizontal)
	{
		if (onlyHorizontal)
		{
			var fw = (Input.gyro.attitude) * (-Vector3.forward);
			fw.z = 0;
			if (fw == Vector3.zero)
			{
				calibration = Quaternion.identity;
			}
			else
			{
				calibration = (Quaternion.FromToRotation(baseOrientationRotationFix * Vector3.up, fw));
			}
		}
		else
		{
			calibration = Input.gyro.attitude;
		}
	}
	
	/// <summary>
	/// Update the camera base rotation.
	/// </summary>
	/// <param name='onlyHorizontal'>
	/// Only y rotation.
	/// </param>
	private void UpdateCameraBaseRotation(bool onlyHorizontal)
	{
		if (onlyHorizontal)
		{
			var fw = transform.forward;
			fw.y = 0;
			if (fw == Vector3.zero)
			{
				cameraBase = Quaternion.identity;
			}
			else
			{
				cameraBase = Quaternion.FromToRotation(Vector3.forward, fw);
			}
		}
		else
		{
			cameraBase = transform.rotation;
		}
	}
	
	/// <summary>
	/// Converts the rotation from right handed to left handed.
	/// </summary>
	/// <returns>
	/// The result rotation.
	/// </returns>
	/// <param name='q'>
	/// The rotation to convert.
	/// </param>
	private static Quaternion ConvertRotation(Quaternion q)
	{
		return new Quaternion(q.x, q.y, -q.z, -q.w);	
	}
	
	/// <summary>
	/// Gets the rot fix for different orientations.
	/// </summary>
	/// <returns>
	/// The rot fix.
	/// </returns>
	private Quaternion GetRotFix()
	{
		#if UNITY_3_5
		
		#else
		return Quaternion.identity;
		#endif
	}
	
	/// <summary>
	/// Recalculates reference system.
	/// </summary>
	private void ResetBaseOrientation()
	{
		baseOrientationRotationFix = GetRotFix();
		baseOrientation = baseOrientationRotationFix * baseIdentity;
	}
	
	/// <summary>
	/// Recalculates reference rotation.
	/// </summary>
	private void RecalculateReferenceRotation()
	{
		referanceRotation = Quaternion.Inverse(baseOrientation)*Quaternion.Inverse(calibration);
	}
	
	#endregion
	
	
	
}
