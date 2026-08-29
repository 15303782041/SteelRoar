using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RankInfo
{
    public string Name;
    public int Score;
    public float time;

    public RankInfo() 
    {
        

    }

    public RankInfo(string name,int score,float time) {
        this.Name = name;
        this.Score = score;
        this.time = time;
    }
    public class RankList
    {
        public List<RankInfo> list; 
    }
}
