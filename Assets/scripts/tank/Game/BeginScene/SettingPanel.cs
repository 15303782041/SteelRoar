using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SettingPanel : BasePanel<SettingPanel>
{

    public CustomGUISlider sliderMusic;
    public CustomGUISlider sliderSound;

    public CustomGUIToggle togMusic;
    public CustomGUIToggle togSound;

    public CustomGUIButton btnClose; 

    // Start is called before the first frame update
    void Start()
    {
        sliderMusic.changeValue += (value) => GameDataMgr.Instance.ChangeBKValue(value);
        //�������ֵı仯
        sliderSound.changeValue += (value) => GameDataMgr.Instance.ChangeSoundValue(value);
        //������Ч�ı仯
        togMusic.changeValue += (value) => GameDataMgr.Instance.OpenOrCloseBKMusic(value);

        togSound.changeValue += (value) => GameDataMgr.Instance.OpenOrCloseSound(value);
        

        btnClose.clickEvent += () =>
        {
            HideMe();
            //判断当前所在场景 应该如何判断
            //让面板重新显示出来
           if (SceneManager.GetActiveScene().name == "BeginScene")
            {
                BeginPanel.Instance.ShowMe(); 
            }

        };

        HideMe();
        
        
    }

    public void UpdatePanelInfo()
    {
        MusicData data = GameDataMgr.Instance.musicData;

        sliderMusic.nowValue = data.bkValue;
        sliderSound.nowValue = data.soundValue; 
        togMusic.isSel = data.isOpenBK;
        togSound.isSel = data.isOpenSound;
    }
    
    public override void ShowMe()
    {
        base.ShowMe();
        //ÿ����ʾ����ʱ���������������Ҳ�����ˡ�
        UpdatePanelInfo();

    }
    public override void HideMe()
    {
        base.HideMe();
        //隐藏时候时间就会重置回1了。
        Time.timeScale = 1;
    }
}
