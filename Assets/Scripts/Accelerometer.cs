using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Accelerometer : MonoBehaviour {

	Button	buttonStart;
	Button	buttonStop;
	Button	buttonCalibration;

	bool is_recording;
	List<Vector3> recordBuffer;

	// 重力加速度を除去するための調整用ベクトル
	Vector3 calibrationVector = Vector3.zero;

	Text textCalibration;
	Text textRecording;
	Text textAccel;

	const float VIBRATION_STRENGTH_THRESHOLD = 0.3f;	//	動かさずにいた状態でのおおよその値をベースに算出した数値(デバイスに依存しそう)
	const float THRESHOLD_RATE = 0.9f;					//	閾値付近での値の割合
	float vibrateAttenuationRate;

	// Use this for initialization
	void Start () {
		GameObject gobj;
		gobj = GameObject.Find("ButtonStart");
		buttonStart = gobj.GetComponent<Button>();
		buttonStart.onClick.AddListener(onButtonStart);

		gobj = GameObject.Find("ButtonStop");
		buttonStop = gobj.GetComponent<Button>();
		buttonStop.onClick.AddListener(onButtonStop);

		gobj = GameObject.Find("ButtonCalibration");
		buttonCalibration = gobj.GetComponent<Button>();
		buttonCalibration.onClick.AddListener(onButtonCalibration);

		gobj = GameObject.Find("TextCalibration");
		textCalibration = gobj.GetComponent<Text>();

		gobj = GameObject.Find("TextRecording");
		textRecording = gobj.GetComponent<Text>();

		gobj = GameObject.Find("TextAccel");
		textAccel = gobj.GetComponent<Text>();

		is_recording = false;
		recordBuffer = new List<Vector3>();

		StartCoroutine("updateIndicator");

		//	手ブレを減衰するための倍率
		vibrateAttenuationRate = -Mathf.Log(1 / THRESHOLD_RATE - 1)/VIBRATION_STRENGTH_THRESHOLD;

		//Debug.Log(getTimestamp());
	}
	
	// Update is called once per frame
	void Update () {
		//	Unityの座標系		画面奥	+Z
		//	Androidのセンサー	画面手前	+Z
		Vector3 mov = new Vector3(Input.acceleration.x, Input.acceleration.y, -Input.acceleration.z);
		transform.position = mov;	//動きと描画を合わせるため、Z方向を反転したものをセット

		record(Input.acceleration);

		Vector3 accel = Input.acceleration - calibrationVector;
		textAccel.text = string.Format("({0:F4},{1:F4},{2:F4})", accel.x, accel.y, accel.z);
	}

	IEnumerator updateIndicator()
	{
		float alpha = 1.0f;
		float dir = -0.025f;

		while(true)
		{
			float a;
			if( is_recording )
			{
				alpha += dir;
				if( alpha > 1 || alpha < 0 )
				{
					dir *= -1;
				}
				alpha = Mathf.Clamp01(alpha);

				a = alpha;
			}
			else{
				a = 0;
			}

			Color color = textRecording.color;
			color.a = a;
			textRecording.color = color;

			yield return null;
		}
	}


	/// <summary>
	/// 現在の日時文字列を取得
	/// </summary>
	/// <returns>The timestamp.</returns>
	string getTimestamp()
	{
		System.DateTime dtime = System.DateTime.Now;
		string textTime = string.Format("{0:D4}{1:D2}{2:D2}-{3:D2}{4:D2}{5:D2}", dtime.Year,dtime.Month,dtime.Day,dtime.Hour,dtime.Minute,dtime.Second);
		return textTime;
	}

	/// <summary>
	/// 加速度ベクトルを記録
	/// </summary>
	/// <param name="mov">Mov.</param>
	void record(Vector3 mov)
	{
		if(is_recording == false) {
			return;
		}

		recordBuffer.Add(mov);
	}

	/// <summary>
	/// 記録開始
	/// </summary>
	void onButtonStart()
	{
		is_recording = true;

	}

	/// <summary>
	/// 記録終了とファイルへの書き出し
	/// </summary>
	void onButtonStop()
	{
		is_recording = false;

		writeRecordBufferToFile();
		recordBuffer.Clear();
	}


	/// <summary>
	/// 手ブレなどを抑えるための減衰を適用
	/// </summary>
	/// <returns>The attenuation.</returns>
	/// <param name="p">P.</param>
	float applyAttenuation(float p)
	{
		return 1/(1+Mathf.Exp(-vibrateAttenuationRate * p)) * p;
	}

	/// <summary>
	/// 記録していた内容をファイルに書き出す
	/// </summary>
	void writeRecordBufferToFile()
	{
		List<float> values = new List<float>();
		foreach(Vector3 mov in recordBuffer) {
			//	振動の強さ(ベクトルの大きさ)のみを参照する
			float p = (mov - calibrationVector).magnitude;
			//	減衰を適用
			p = applyAttenuation(p);
			values.Add(p);
		}

		string path = Application.persistentDataPath;
		string fname = "vib-" + getTimestamp() + ".txt";

		using(StreamWriter wr = new StreamWriter(path+"/"+fname)) {
			foreach(float val in values) {
				wr.Write( val.ToString()+"f," );	//	コピペでそのまま使える形にする
			}
			wr.Flush();
			wr.Close();
		}
	}

	/// <summary>
	/// キャリブレーション
	/// </summary>
	void onButtonCalibration()
	{
		//	現在の加速度をキャリブレーションに使用する
		//	重力加速度を想定
		calibrationVector = Input.acceleration;
	
		textCalibration.text = string.Format("({0:F4},{1:F4},{2:F4})", calibrationVector.x, calibrationVector.y, calibrationVector.z);
	}
}
