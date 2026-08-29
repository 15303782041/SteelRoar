using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 音乐管理器：统一管理音效的音量/开关应用逻辑，
    /// 收拢原先散落在 BulletObj、TankBaseObj 等处的重复设置代码（每处4行→1行）。
    /// TODO(Day 10 存档系统迁移)：音量状态改为由本类持有并持久化。
    /// 当前为过渡方案，读取 GameDataMgr.musicData（现有设置UI的数据源），保证设置滑条不失效
    /// </summary>
    public class MusicManager : SingletonAutoMono<MusicManager>
    {
        private AudioSource bgmSource;

        void Awake()
        {
            bgmSource = this.GetComponent<AudioSource>();
            if (bgmSource == null) bgmSource = this.gameObject.AddComponent<AudioSource>();
        }

        /// <summary>播放背景音乐（循环）</summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = 1f;
            bgmSource.mute = false;
            bgmSource.Play();
        }

        /// <summary>停止背景音乐</summary>
        public void StopBGM()
        {
            bgmSource.Stop();
        }

        /// <summary>
        /// 按资源名播放音效（Resources/Music/Sound/下）。
        /// 音效资源缺失时静默跳过，防止用null clip创建空AudioSource
        /// </summary>
        public void PlayOneShot(string name)
        {
            if (!isOpenSound) return;
            AudioClip clip = Resources.Load<AudioClip>("Music/Sound/" + name);
            if (clip == null) return;
            AudioSource source = this.gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = soundValue;
            source.Play();
            Destroy(source, clip.length);   // 播完即销毁（一次性组件，不进池）
        }

        /// <summary>
        /// 统一设置某个音效源的音量与开关（替代散落各处的重复代码）。
        /// play为true时顺带播放（用于没有勾选PlayOnAwake的音效源）
        /// </summary>
        public void SetSourceVolume(AudioSource source, bool play = false)
        {
            if (source == null) return;
            source.volume = GameDataMgr.Instance.musicData.soundValue;
            source.mute = !GameDataMgr.Instance.musicData.isOpenSound;
            if (play) source.Play();
        }
    }
}
