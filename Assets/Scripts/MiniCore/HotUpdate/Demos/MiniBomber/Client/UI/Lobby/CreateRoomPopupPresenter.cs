using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 创建房间弹窗 Presenter。
    /// </summary>
    public sealed class CreateRoomPopupPresenter : AUIWindowPresenter<CreateRoomPopupView>
    {
        #region Private 私有成员

        private static readonly int[] Durations = { 120, 300, 600 }; // 下拉框索引对应时长。
        private RoomComponent room; // 房间组件。
        private MiniBomberClientFlowComponent flow; // 客户端流程组件。
        private bool commandRunning; // 是否已有创建房间请求执行中。
        private bool released; // Presenter 是否已经随窗口释放。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 重置缓存弹窗状态、配置时长选项并绑定按钮。
        /// </summary>
        protected override void OnBind()
        {
            room = Global.Get<RoomComponent>(this);
            flow = Global.Get<MiniBomberClientFlowComponent>(this);
            commandRunning = false;
            View.ShowPrompt(string.Empty);
            View.SetCommandInteractable(true);
            View.BindActions(Bindings, Submit, Close);
        }

        /// <summary>
        /// 清空业务引用。
        /// </summary>
        protected override void OnDispose()
        {
            released = true;
            commandRunning = false;
            room = null;
            flow = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 从按钮回调启动创建房间任务。
        /// </summary>
        private void Submit()
        {
            if (commandRunning)
            {
                return;
            }

            SubmitAsync().Forget();
        }

        /// <summary>
        /// 创建房间并进入房间界面。
        /// </summary>
        /// <returns>创建流程完成任务。</returns>
        private async MTask SubmitAsync()
        {
            View.GetCreateInput(out string roomName, out int durationIndex);
            int index = Mathf.Clamp(durationIndex, 0, Durations.Length - 1);
            commandRunning = true;
            View.SetCommandInteractable(false);
            bool created = false;
            try
            {
                View.ShowPrompt("正在创建房间...");
                MiniBomberCommandResult result = await room.CreateAsync(roomName, Durations[index]);
                if (released)
                {
                    return;
                }

                View.ShowPrompt(result.Message);
                if (result.IsSuccess)
                {
                    created = true;
                    MiniBomberClientFlowComponent flowComponent = flow;
                    IUIService service = Context.Service;
                    UIWindowHandle handle = Context.Handle;
                    View.ShowPrompt("创建成功，正在进入房间...");
                    await flowComponent.NavigateAsync(MiniBomberClientDestinationKind.Room);
                    await service.CloseAsync(handle);
                }
            }
            catch (Exception exception)
            {
                LogSwitch.Error($"MiniBomber 创建房间失败：{exception}");
                if (!released)
                {
                    View.ShowPrompt(created
                        ? "房间已经创建，但界面切换失败；重新登录可恢复房间状态"
                        : "创建房间失败，请检查网络连接后重试");
                }
            }
            finally
            {
                commandRunning = false;
                if (!released)
                {
                    View.SetCommandInteractable(true);
                }
            }
        }

        /// <summary>
        /// 关闭创建房间弹窗。
        /// </summary>
        private void Close()
        {
            if (commandRunning)
            {
                return;
            }

            Context.Service.CloseAsync(Context.Handle).Forget();
        }

        #endregion
    }
}
