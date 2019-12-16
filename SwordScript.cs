using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordScript : MonoBehaviour {

    public bool attacked;
    public GameObject particle1;
    public GameObject particleAttack_map;
    public Transform prcPos;
    AudioSource audioSource;
    PlayerCamera pc;
    PlayerOutlineController po;

	// Use this for initialization
	void Start () {
        attacked = false;
        audioSource = GetComponent<AudioSource>();
        pc = GetComponentInParent(typeof(PlayerCamera)) as PlayerCamera;
        po = GetComponentInParent(typeof(PlayerOutlineController)) as PlayerOutlineController;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "Player" && attacked)
        {
            if (other.tag == "Deviljho" || other.tag == "Map")
            {
                attacked = false;
                Debug.Log("Attack : " + other.name);
                pc.StartCameraShake();
                po.ActiveOutLineWeak();

                if (other.CompareTag("Deviljho"))
                {
                    SoundManager.Instance.PlaySound(audioSource, "Weapon", "AttackMonster");
                    Instantiate(particle1, prcPos.position, prcPos.localRotation);
                    if (GameObject.Find("DeviljhoFolder").GetComponent<MonsterController>().isSturn)
                    {
                        GameObject.Find("DeviljhoFolder").GetComponent<MonsterController>().Damege(30);
                    }
                    else
                    {
                        GameObject.Find("DeviljhoFolder").GetComponent<MonsterController>().Damege(15);
                    }
                }

                else
                {
                    SoundManager.Instance.PlaySound(audioSource, "Weapon", "AttackMap");
                    Instantiate(particleAttack_map, prcPos.position, prcPos.localRotation);
                }
                    
            }
        }

    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag != "Player" && attacked)
        {
            if (other.tag == "Deviljho" || other.tag == "Map")
            {
                attacked = false;
                Debug.Log("Attack : " + other.name);
                pc.StartCameraShake();
                po.ActiveOutLineWeak();

                if (other.CompareTag("Deviljho"))
                {

                    SoundManager.Instance.PlaySound(audioSource, "Weapon", "AttackMonster");
                    Instantiate(particle1, prcPos.position, prcPos.localRotation);
                    if (GameObject.Find("DeviljhoFolder").GetComponent<MonsterController>().isSturn)
                    {
                        GameObject.Find("DeviljhoFolder").GetComponent<MonsterController>().Damege(30);
                    }
                    else
                    {
                        GameObject.Find("DeviljhoFolder").GetComponent<MonsterController>().Damege(15);
                    }
                }
                else
                {
                    SoundManager.Instance.PlaySound(audioSource, "Weapon", "AttackMap");
                    Instantiate(particleAttack_map, prcPos.position, prcPos.localRotation);
                }
                    
            }
        }
    }
}
