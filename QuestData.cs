using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestData : MonoBehaviour {

    // 사냥, 토벌, 포획, 채집
    public enum QuestType
    {
        사냥 = 1, 토벌, 포획, 채집
    }

    [System.Serializable]
    public class Quest
    {
        public string   questName;
        public QuestType questType;
        public Sprite   questSprite;
        public int      gold;
        public string   clearTime;
        public Sprite   mapSpriete;
        public string   mapName;
        public int      mapDifficulty;
        public string   questCondition;
        public string   failCondition;
        public string   monster;
        public Sprite   monsterSprite;
        public string   client;
        public string   Explanation;
        public bool     complete;
    }
}
