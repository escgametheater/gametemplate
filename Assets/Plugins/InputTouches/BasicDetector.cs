using UnityEngine;
using System.Collections;

public class BasicDetector : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update(){
		if(Input.touchCount>0){
			//foreach(Touch touch in Input.touches){
			for(int i=0; i<Input.touches.Length; i++){
				Touch touch=Input.touches[i];
				if(touch.phase==TouchPhase.Began) Gesture.OnTouchDown(touch.position);
				else if(touch.phase==TouchPhase.Ended) Gesture.OnTouchUp(touch.position);
				else Gesture.OnTouch(touch.position);
			}
		}
		else if(Input.touchCount==0){
			//#if !(UNITY_ANDROID || UNITY_IPHONE) || UNITY_EDITOR
			if(Input.GetMouseButtonDown(0)) Gesture.OnMouse1Down(Input.mousePosition);
			else if(Input.GetMouseButtonUp(0)) Gesture.OnMouse1Up(Input.mousePosition);
			else if(Input.GetMouseButton(0)) Gesture.OnMouse1(Input.mousePosition);
			
			if(Input.GetMouseButtonDown(1)) Gesture.OnMouse2Down(Input.mousePosition);
			else if(Input.GetMouseButtonUp(1)) Gesture.OnMouse2Up(Input.mousePosition);
			else if(Input.GetMouseButton(1)) Gesture.OnMouse2(Input.mousePosition);
			//#endif
		}
	}
	

}
