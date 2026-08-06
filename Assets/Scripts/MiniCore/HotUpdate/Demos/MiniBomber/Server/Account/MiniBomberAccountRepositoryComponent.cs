using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务器账号数据库序列化根对象。
    /// </summary>
    [Serializable]
    public sealed class MiniBomberAccountDatabase
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置下一个可分配玩家身份。
        /// </summary>
        public long NextPlayerId { get; set; } = 1;

        /// <summary>
        /// 获取或设置全部已注册账号。
        /// </summary>
        public List<MiniBomberAccountRecord> Accounts { get; set; } = new List<MiniBomberAccountRecord>();

        #endregion
    }

    /// <summary>
    /// 服务器持久化的单个 MiniBomber 账号记录。
    /// </summary>
    [Serializable]
    public sealed class MiniBomberAccountRecord
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置稳定玩家身份。
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 获取或设置登录账号。
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 获取或设置游戏内唯一玩家名。
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 获取或设置 Base64 随机盐。
        /// </summary>
        public string PasswordSalt { get; set; }

        /// <summary>
        /// 获取或设置 Base64 SHA-256 密码摘要。
        /// </summary>
        public string PasswordHash { get; set; }

        #endregion
    }

    /// <summary>
    /// MiniBomber 注册操作结果。
    /// </summary>
    public readonly struct MiniBomberRegisterResult
    {
        #region Public 公共成员

        /// <summary>
        /// 获取注册业务错误码。
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// 获取面向用户的注册结果消息。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 获取成功创建的账号记录。
        /// </summary>
        public MiniBomberAccountRecord Account { get; }

        /// <summary>
        /// 获取注册操作是否成功。
        /// </summary>
        public bool Succeeded => Code == MiniBomberErrorCode.Success;

        /// <summary>
        /// 创建注册结果。
        /// </summary>
        /// <param name="code">业务错误码。</param>
        /// <param name="message">面向用户的结果消息。</param>
        /// <param name="account">成功创建的账号。</param>
        public MiniBomberRegisterResult(int code, string message, MiniBomberAccountRecord account)
        {
            Code = code;
            Message = message;
            Account = account;
        }

        #endregion
    }

    /// <summary>
    /// 使用 MiniCore 加密存档服务持久化的内网 Demo 账号仓库。
    /// </summary>
    public sealed class MiniBomberAccountRepositoryComponent : AComponent
    {
        #region Public 公共成员

        /// <summary>
        /// 加载服务器账号存档并建立内存索引。
        /// </summary>
        /// <returns>初始化完成任务。</returns>
        public async MTask InitializeAsync()
        {
            if (initialized)
            {
                return;
            }

            saveService = Global.GetService<ISaveService>(this);
            database = await saveService.LoadAsync<MiniBomberAccountDatabase>(MiniBomberConstants.AccountDatabaseSlot) ?? new MiniBomberAccountDatabase();
            database.Accounts ??= new List<MiniBomberAccountRecord>();
            for (int index = 0; index < database.Accounts.Count; index++)
            {
                MiniBomberAccountRecord record = database.Accounts[index];
                if (record == null || string.IsNullOrWhiteSpace(record.Account) || string.IsNullOrWhiteSpace(record.PlayerName) || record.PlayerId <= 0)
                {
                    continue;
                }

                byAccount[record.Account] = record;
                byPlayerName[record.PlayerName] = record;
                byPlayerId[record.PlayerId] = record;
                if (database.NextPlayerId <= record.PlayerId)
                {
                    database.NextPlayerId = record.PlayerId + 1;
                }
            }

            initialized = true;
        }

        /// <summary>
        /// 注册新账号并在成功后立即持久化。
        /// </summary>
        /// <param name="account">登录账号。</param>
        /// <param name="password">不会写入日志的原始密码。</param>
        /// <param name="playerName">游戏内唯一显示名。</param>
        /// <returns>包含错误码和新账号的注册结果。</returns>
        public async MTask<MiniBomberRegisterResult> RegisterAsync(string account, string password, string playerName)
        {
            EnsureInitialized();
            account = account?.Trim();
            playerName = playerName?.Trim();
            if (!IsAccountValid(account) || !IsPasswordValid(password) || !IsPlayerNameValid(playerName))
            {
                return new MiniBomberRegisterResult(MiniBomberErrorCode.InvalidArgument, "账号、密码或玩家姓名格式不正确", null);
            }

            if (byAccount.ContainsKey(account))
            {
                return new MiniBomberRegisterResult(MiniBomberErrorCode.AccountExists, "账号已经存在", null);
            }

            if (byPlayerName.ContainsKey(playerName))
            {
                return new MiniBomberRegisterResult(MiniBomberErrorCode.PlayerNameExists, "玩家姓名已经被使用", null);
            }

            byte[] salt = MiniBomberPasswordHasher.CreateSalt();

            var record = new MiniBomberAccountRecord
            {
                PlayerId = database.NextPlayerId++,
                Account = account,
                PlayerName = playerName,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(MiniBomberPasswordHasher.Hash(password, salt))
            };
            database.Accounts.Add(record);
            byAccount.Add(record.Account, record);
            byPlayerName.Add(record.PlayerName, record);
            byPlayerId.Add(record.PlayerId, record);
            try
            {
                await saveService.SaveAsync(MiniBomberConstants.AccountDatabaseSlot, database);
            }
            catch
            {
                database.Accounts.Remove(record);
                byAccount.Remove(record.Account);
                byPlayerName.Remove(record.PlayerName);
                byPlayerId.Remove(record.PlayerId);
                database.NextPlayerId--;
                throw;
            }

            return new MiniBomberRegisterResult(MiniBomberErrorCode.Success, "注册成功，请登录", record);
        }

        /// <summary>
        /// 在内存索引中验证账号密码。
        /// </summary>
        /// <param name="account">登录账号。</param>
        /// <param name="password">原始密码。</param>
        /// <param name="record">验证成功时返回账号记录。</param>
        /// <returns>账号存在且摘要固定时间比较一致时返回 true。</returns>
        public bool TryAuthenticate(string account, string password, out MiniBomberAccountRecord record)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrEmpty(password) || !byAccount.TryGetValue(account.Trim(), out MiniBomberAccountRecord candidate))
            {
                record = null;
                return false;
            }

            if (!MiniBomberPasswordHasher.Verify(password, candidate.PasswordSalt, candidate.PasswordHash))
            {
                record = null;
                return false;
            }

            record = candidate;
            return true;
        }

        /// <summary>
        /// 按稳定玩家身份查找账号资料。
        /// </summary>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="record">找到的账号记录。</param>
        /// <returns>存在对应账号时返回 true。</returns>
        public bool TryGetByPlayerId(long playerId, out MiniBomberAccountRecord record)
        {
            EnsureInitialized();
            return byPlayerId.TryGetValue(playerId, out record);
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放服务引用和账号内存索引。
        /// </summary>
        protected override void OnDispose()
        {
            byAccount.Clear();
            byPlayerName.Clear();
            byPlayerId.Clear();
            database = null;
            saveService = null;
            initialized = false;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion

        #region Private 私有成员

        private readonly Dictionary<string, MiniBomberAccountRecord> byAccount = new Dictionary<string, MiniBomberAccountRecord>(StringComparer.OrdinalIgnoreCase); // 账号索引。
        private readonly Dictionary<string, MiniBomberAccountRecord> byPlayerName = new Dictionary<string, MiniBomberAccountRecord>(StringComparer.OrdinalIgnoreCase); // 玩家名索引。
        private readonly Dictionary<long, MiniBomberAccountRecord> byPlayerId = new Dictionary<long, MiniBomberAccountRecord>(); // 玩家身份索引。
        private ISaveService saveService; // 项目启用的加密存档服务。
        private MiniBomberAccountDatabase database; // 当前内存账号数据库。
        private bool initialized; // 仓库是否已经完成异步加载。

        /// <summary>
        /// 验证仓库已经完成异步初始化。
        /// </summary>
        private void EnsureInitialized()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("MiniBomber 账号仓库尚未初始化。");
            }
        }

        /// <summary>
        /// 判断登录账号是否符合 Demo 规则。
        /// </summary>
        /// <param name="value">待验证账号。</param>
        /// <returns>长度和字符合法时返回 true。</returns>
        private static bool IsAccountValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 4 || value.Length > 20)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!(character >= 'a' && character <= 'z') && !(character >= 'A' && character <= 'Z') && !(character >= '0' && character <= '9') && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断密码是否符合内网 Demo 最小长度规则。
        /// </summary>
        /// <param name="value">待验证密码。</param>
        /// <returns>长度为六到六十四字符时返回 true。</returns>
        private static bool IsPasswordValid(string value)
        {
            return !string.IsNullOrEmpty(value) && value.Length >= 6 && value.Length <= 64;
        }

        /// <summary>
        /// 判断玩家显示名是否符合长度且不包含控制字符。
        /// </summary>
        /// <param name="value">待验证玩家名。</param>
        /// <returns>玩家名可用时返回 true。</returns>
        private static bool IsPlayerNameValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2 || value.Length > 12)
            {
                return false;
            }

            for (int index = 0; index < value.Length; index++)
            {
                if (char.IsControl(value[index]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
