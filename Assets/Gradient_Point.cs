using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Gradient_Point : MonoBehaviour
{
    public Gradient gradient;
    public Image image;
    [Range(0,1)]public float sliderTest;
    CapturePoint myCapturePoint;
    private void Awake()
    {
         myCapturePoint = GetComponentInParent<CapturePoint>();
        if (myCapturePoint == null) Destroy(gameObject);

        myCapturePoint.onProgressChange.AddListener(SetImageValue);
        SetImageValue();


    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.position - transform.position;
    }

    void SetImageValue()
    {
        image.color = gradient.Evaluate(myCapturePoint.ZoneProgressNormalized);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        if (image == null) return;
        image.color = gradient.Evaluate(sliderTest);
    }
}
