using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LossPanel :BasePanel<LossPanel>
{
    public CustomGUIButton btnBack;
    public CustomGUIButton btnGoOn;
    // Start is called before the first frame update
    void Start()
    {
        btnBack.clickEvent += () =>
        {
            //取消暂停
            Time.timeScale = 1;
            //切换场景
          
            SceneManager.LoadScene("BeginScene");
        };
        btnGoOn.clickEvent += () =>
        {
            //取消暂停
            Time.timeScale = 0;
            //再次切换到 游戏场景 就可以 达到所有内容重新加载 从头开始的 目的
            SceneManager.LoadScene("GameScene");
        }; 
        HideMe();
    }

   
}
