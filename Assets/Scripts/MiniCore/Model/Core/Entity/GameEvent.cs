using UnityEngine;
namespace MiniCore.Model {

    public partial class GameEvent {

        public const string ValueChanged_mouseSpeed = "ValueChanged_mouseSpeed";  //鼠标速度改变
        public const string ValueChanged_voiceVolume = "ValueChanged_voiceVolume";        //音量
        public const string ValueChanged_quality = "ValueChanged_quality";           //画质
        public const string ValueChanged_resolutionRatio = "ValueChanged_resolutionRatio";   //分辨率
        public const string ValueChanged_resolutionMode = "ValueChanged_resolutionMode";     //窗口、全屏模式


        public const string QuestStart = "QuestStart";     //任务开始

        public const string UpdateQuestProgress = "UpdateQuestProgress";       //更新任务进度


        public const string LogInfo = "LogInfo";        //日志信息
        public const string LogWarning = "LogWarning";  //警告信息
        public const string LogError = "LogError";      //错误信息

        public const string OnPointerEnter = "OnPointerEnter";      //鼠标进入
        public const string OnPointerExit = "OnPointerExit";        //鼠标移除


        #region 网络消息


        #endregion
    }

}
