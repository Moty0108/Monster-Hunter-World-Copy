using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOutlineController : MonoBehaviour {
    public Renderer[] outline;
    public ParticleSystem particle;
    public ParticleSystem particle2;
    float width = 0;
    IEnumerator cor;
    // Use this for initialization
    void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
        for (int i = 0; i < outline.Length; i++)
        {
            outline[i].material.SetFloat("_Outline", width);
        }
    }

    void StartOutLine(IEnumerator action)
    {
        width = 0;
        if (cor != null)
        {
            particle.Stop();
            particle2.Stop();
            StopCoroutine(cor);
        }

        cor = action;
        StartCoroutine(cor);
    }

    public void ActiveOutLineStrong()
    {   
        StartOutLine(OutLineStrong());
    }

    public void ActiveOutLineWeak()
    {
        StartOutLine(OutLineWeak());
    }


    IEnumerator OutLineWeak()
    {
        
        while(true)
        {
            width += 4f;
            if (width > 20f)
                break;

            yield return new WaitForSeconds(0.0001f);
        }

        while (true)
        {
            width -= 4f;
            if (width == 0f)
                break;

            yield return new WaitForSeconds(0.1f);
        }
    }

    void ParticleStart()
    {
        particle.Play();
        Debug.Log("aa");
    }

    void ParticleStop()
    {
        Debug.Log("aa");
        particle.Stop();
        particle.Clear();
        
    }

    IEnumerator OutLineStrong()
    {
        
        particle2.Play();
        particle.Play();
        while (true)
        {
        
            width += 2f;
            if (width > 40f)
            {
                
                break;
            }

            yield return new WaitForSeconds(0.0001f);
        }

        
        while (true)
        {
            width -= 1f;
            if (width < 0f)
            {
                width = 0;
                break;
            }

            yield return new WaitForSeconds(0.0001f);
        }
        particle.Stop();
        particle2.Stop();
    }
}
