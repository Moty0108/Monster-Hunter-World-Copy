using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestObject : MonoBehaviour {

    public string questName;
    public Sprite questSprite;
    public Sprite monsterSprite;
    public int mapDifficulty;
    public GameObject complete;
    public bool b_complete;

    public SpriteRenderer questSpriteScene;
    //public Text questNameScene;
    public TMPro.TMP_Text questNameScene;
    public Text mapDifficultyScene;

    public GameObject QuestionWindow;

    private QuestDataBase questDataBase;
    bool b_isClicked = false;
    List<buttonscript> button = new List<buttonscript>();
    int click = 0;
    AudioSource audio;

    // Use this for initialization
    void Start () {
        questDataBase = GameObject.Find("QuestDB").GetComponent<QuestDataBase>();
        audio = GameObject.Find("Main Camera").GetComponent<AudioSource>();
        questNameScene.SetText(questName);
        //questNameScene.text = questName;
        questSpriteScene.sprite = questSprite;
        mapDifficultyScene.text = mapDifficulty.ToString();

        foreach (buttonscript temp in QuestionWindow.GetComponentsInChildren<buttonscript>())
        {
            button.Add(temp);
        }

        StartCoroutine(Clicked());
    }
	
	// Update is called once per frame
	void Update () {
		if(b_isClicked)
        {
            foreach(buttonscript temp in button)
            {
                if (temp.button.name == "예" && temp.button.b_isClicked)
                {
                    SoundManager.Instance.PlaySound(audio, "UI", "Ddiddring");
                    Debug.Log("예 클릭");
                    //LoadingManager.LoadScene("Player");
                    Invoke("LoadLevel", 0.5f);
                    temp.button.b_isClicked = false;
                }
                if (temp.button.name == "아니오" && temp.button.b_isClicked)
                {
                    SoundManager.Instance.PlaySound(audio, "UI", "Ddiring");
                    b_isClicked = false;
                    Debug.Log("아니오 클릭");
                    QuestionWindow.SetActive(false);
                    temp.Reset();
                }
                if (temp.button.name == "확인" && temp.button.b_isClicked)
                {
                    SoundManager.Instance.PlaySound(audio, "UI", "Ddiring");
                    b_isClicked = false;
                    Debug.Log("확인 클릭");
                    QuestionWindow.SetActive(false);
                    temp.Reset();
                }
            }

            
        }
	}

    void LoadLevel()
    {
        LoadingManager.LoadScene("Player");
    }

    public void QuestCompleteSet(bool _complete)
    {
        complete.SetActive(_complete);
    }

    public void Selected()
    {
        foreach (QuestData.Quest quest in questDataBase.questDataBase)
        {
            if(quest.questName == questName)
            {
                if(click == 0)
                SoundManager.Instance.PlaySound(audio, "UI", "PageMove");
                click++;
                ChangeValue(quest);
            }
        }

        
    }

    IEnumerator Clicked()
    {

        while(true)
        {
            if(click == 1)
            {
                yield return new WaitForSeconds(0.5f);
                if (click >= 2)
                {
                    SoundManager.Instance.PlaySound(audio, "UI", "PageMove2");
                    QuestionWindow.SetActive(true);
                    b_isClicked = true;
                }
                click = 0;
            }
            

            yield return new WaitForFixedUpdate();
        }
    }
        

    public void ChangeValue(QuestData.Quest quest)
    {
        GameObject.Find("QuestName_Text").GetComponent<TMP_Text>().text = questName;
        GameObject.Find("MonsterSprite_Sprite").GetComponent<SpriteRenderer>().sprite = monsterSprite;
        GameObject.Find("Gold_Text").GetComponent<Text>().text = quest.gold + "z";
        GameObject.Find("ClearTime_Text").GetComponent<Text>().text = quest.clearTime + "분";
        GameObject.Find("QuestCondition_Text").GetComponent<Text>().text = quest.questCondition;
        GameObject.Find("FailCondition_Text").GetComponent<Text>().text = quest.failCondition;
        GameObject.Find("Monster_Text").GetComponent<Text>().text = quest.monster;
        GameObject.Find("Client_Text").GetComponent<Text>().text = quest.client;
        GameObject.Find("Explanation_Text").GetComponent<Text>().text = quest.Explanation;
        GameObject.Find("MapName_Text").GetComponent<Text>().text = quest.mapName;
        GameObject.Find("QuestType_Text").GetComponent<Text>().text = quest.questType.ToString();
        GameObject.Find("QuestType_Sprite").GetComponent<SpriteRenderer>().sprite = questSprite;
        GameObject.Find("Map_Sprite").GetComponent<SpriteRenderer>().sprite = quest.mapSpriete;
        GameObject.Find("Map_Sprite").GetComponent<Transform>().localScale = new Vector3(0.9f, 0.7f, 0);

        GameObject.Find("Selector").GetComponent<Transform>().transform.localPosition = transform.localPosition;

        GamingManager.Instance.SetQuest(quest);


    }
}

