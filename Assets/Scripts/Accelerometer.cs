using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Accelerometer : MonoBehaviour {

	Button	buttonStart;
	Button	buttonStop;

	bool is_recording;
	int serial;
	string currentFileName;
	List<Vector3> recordBuffer;


	// Use this for initialization
	void Start () {
		GameObject gobj;
		gobj = GameObject.Find("ButtonStart");
		buttonStart = gobj.GetComponent<Button>();
		buttonStart.onClick.AddListener(OnButtonStart);

		gobj = GameObject.Find("ButtonStop");
		buttonStop = gobj.GetComponent<Button>();
		buttonStop.onClick.AddListener(OnButtonStop);

		is_recording = false;
		serial = 1;
		currentFileName = null;
		recordBuffer = new List<Vector3>();
	}
	
	// Update is called once per frame
	void Update () {
		//	Unityの座標系		画面奥	+Z
		//	Androidのセンサー	画面手前	+Z
		Vector3 mov =	new Vector3(Input.acceleration.x, Input.acceleration.y, -Input.acceleration.z);
		transform.position = mov;

		record(mov);
	}

	void record(Vector3 mov)
	{
		if(is_recording == false) {
			return;
		}

		recordBuffer.Add(mov);
	}

	void OnButtonStart()
	{
		is_recording = true;

	}

	void OnButtonStop()
	{
		is_recording = false;

		List<float> values = new List<float>();
		foreach(Vector3 mov in recordBuffer) {
			float p = mov.x;
			//p = Mathf.Clamp(p, -1, 1);
			values.Add(p);
		}

		string path = Application.persistentDataPath;
		string fname = "vib_" + string.Format("{0:d6}", serial) + ".txt";

		using(StreamWriter wr = new StreamWriter(path+"/"+fname)) {
			foreach(float val in values) {
				wr.Write(val.ToString()+",");
			}
			wr.Flush();
			wr.Close();
		}

		recordBuffer.Clear();
		serial++;
	}
}
