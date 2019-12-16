using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{


    public GameObject camPosition;
    public float sensitivity;
    Vector3 camOffset;
    IEnumerator cor;
    bool shake;

    // Use this for initialization
    void Start()
    {
        shake = false;
        camOffset = camPosition.transform.position - transform.position;
    }

    // Update is called once per frame
    void Update()
    {

    }

    float x;
   

    private void LateUpdate()
    {
        x = Input.GetAxis("Mouse X");



        if (!shake)
        {
            GameObject.Find("MainCamera").transform.position = transform.position + camOffset;
            GameObject.Find("MainCamera").transform.RotateAround(transform.position, new Vector3(0, x, 0), sensitivity * Time.deltaTime);
            GameObject.Find("MainCamera").transform.LookAt(new Vector3(transform.position.x, transform.position.y + 15, transform.position.z));
            camOffset = GameObject.Find("MainCamera").transform.position - transform.position;

            GameObject.Find("ParticleCamera").transform.position = transform.position + camOffset;
            GameObject.Find("ParticleCamera").transform.RotateAround(transform.position, new Vector3(0, x, 0), sensitivity * Time.deltaTime);
            GameObject.Find("ParticleCamera").transform.LookAt(new Vector3(transform.position.x, transform.position.y + 15, transform.position.z));
            camOffset = GameObject.Find("ParticleCamera").transform.position - transform.position;
        }
    }

    public void StartCameraShake()
    {
        if (cor != null)
            StopCoroutine(cor);

        cor = Shake();
        StartCoroutine(cor);
    }

    IEnumerator Shake()
    {
        Time.timeScale = 0.7f;
        float shakePower = 3f;
        Vector3 shakeCameraPos;
        Vector3 cameraPos = GameObject.Find("MainCamera").transform.position;
        shake = true;
        while (true)
        {
            if (shakePower > 0.0f)
            {
                shakePower -= 10.0f * Time.deltaTime;
            }
            else
                break;

            shakeCameraPos = (Random.insideUnitSphere * shakePower / 2) + cameraPos;
            
            GameObject.Find("MainCamera").transform.position = shakeCameraPos;
            GameObject.Find("ParticleCamera").transform.position = shakeCameraPos;


            yield return new WaitForSecondsRealtime(0.02f);
        }

        shake = false;
        Time.timeScale = 1f;
    }

}
