using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class RankPanel : BasePanel<RankPanel>
{

    public CustomGUIButton btnclose;
     
    private List<CustomGUILabel> labPM =new List<CustomGUILabel>();
    private List<CustomGUILabel> labName = new List<CustomGUILabel>();
    private List<CustomGUILabel> labScore = new List<CustomGUILabel>();
    private List<CustomGUILabel> labTime = new List<CustomGUILabel>();


    // Start is called before the first frame update
    void Start()
    {
        for(int i = 1;i<=10;i++)
        {
            labPM.Add(this.transform.Find("PM/labPM" + i).GetComponent<CustomGUILabel>());
            labName.Add(this.transform.Find("Name/labName" + i).GetComponent<CustomGUILabel>());
            labScore.Add(this.transform.Find("Score/labScore" + i).GetComponent<CustomGUILabel>());
            labTime.Add(this.transform.Find("Time/labTime" + i).GetComponent<CustomGUILabel>()); 
        }


        btnclose.clickEvent += () =>
        {
            HideMe();
            BeginPanel.Instance.ShowMe();
        };


        HideMe();
    }
     
   public override void ShowMe()
    {
        base.ShowMe();
        UpdatePanelInfo();
    }
    public void UpdatePanelInfo()
    {
        List<RankInfo> list = GameDataMgr.Instance.rankData.list;
        for (int i = 0; i < list.Count; i++)
        {
            //名字
            labName[i].content.text = list[i].Name;
            //分数
            labScore[i].content.text = list[i].Score.ToString();
            //时间存储单位是秒
            //把秒数转换成时 分 秒
            int time = (int)list[i].time;
            labTime[i].content.text = "";
            if (time / 3600 > 0)
            {
                labTime[i].content.text += time / 3600 + "时";
            }
            if (time % 3600 / 60 > 0 || labTime[i].content.text != "")
            {
                labTime[i].content.text += time % 3600 / 60 + "分";
            }
            labTime[i].content.text += time % 60 + "秒";
        }
    }
    
}
