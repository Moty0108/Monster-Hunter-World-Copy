using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestDataBase : MonoBehaviour
{

    public List<QuestData.Quest> questDataBase = new List<QuestData.Quest>();
    public GameObject questDefault;
     List<GameObject> questLsit = new List<GameObject>();
    public RectTransform questListTransform;
    public GameObject QuestionWindow;
    public GameObject QuestionWindow2;

    // Use this for initialization
    void Start()
    {
        float y = 0;
        foreach (QuestData.Quest quest in questDataBase)
        {
            GameObject temp;
            temp = Instantiate(questDefault, questDefault.transform);
            temp.GetComponent<QuestObject>().questName = quest.questName;
            temp.GetComponent<QuestObject>().questSprite = quest.questSprite;
            temp.GetComponent<QuestObject>().monsterSprite = quest.monsterSprite;
            temp.GetComponent<QuestObject>().mapDifficulty = quest.mapDifficulty;
            temp.GetComponent<QuestObject>().QuestCompleteSet(quest.complete);

            if(quest.complete)
                temp.GetComponent<QuestObject>().QuestionWindow = QuestionWindow2;
            else
                temp.GetComponent<QuestObject>().QuestionWindow = QuestionWindow;

            temp.transform.parent = questListTransform;
            temp.transform.localPosition = new Vector3(0, y, 0);
            y -= temp.GetComponent<RectTransform>().rect.height;


            temp.transform.localScale = new Vector3(1f, 1f, 1f);
            questLsit.Add(temp);

            if(questLsit.Count == 1)
                temp.GetComponent<QuestObject>().ChangeValue(quest);
               
                
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}
