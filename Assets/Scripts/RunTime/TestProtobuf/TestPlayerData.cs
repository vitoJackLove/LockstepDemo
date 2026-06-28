using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rogue.Test
{
    /// <summary>
    /// 测试用玩家数据 - 用于测试Protobuf转换工具
    /// </summary>
    [Serializable]
    public class TestPlayerData
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public int PlayerId;

        /// <summary>
        /// 玩家名称
        /// </summary>
        public string PlayerName;

        /// <summary>
        /// 玩家等级
        /// </summary>
        public int Level;

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline;

        /// <summary>
        /// 玩家金币
        /// </summary>
        public long Gold;

        /// <summary>
        /// 玩家经验值
        /// </summary>
        public float Experience;

        /// <summary>
        /// 玩家健康值
        /// </summary>
        public double Health;

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegisterTime;

        /// <summary>
        /// 玩家UUID
        /// </summary>
        public Guid PlayerGuid;

        /// <summary>
        /// 玩家头像数据
        /// </summary>
        public byte[] AvatarData;

        /// <summary>
        /// 玩家位置
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 玩家旋转
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// 玩家颜色
        /// </summary>
        public Color PlayerColor;

        /// <summary>
        /// 背包物品ID列表
        /// </summary>
        public List<int> InventoryItemIds;

        /// <summary>
        /// 好友ID数组
        /// </summary>
        public int[] FriendIds;

        /// <summary>
        /// 玩家属性字典
        /// </summary>
        public Dictionary<string, float> Attributes;

        /// <summary>
        /// 英雄信息列表
        /// </summary>
        public List<TestHeroInfo> Heroes;

        /// <summary>
        /// 成就集合
        /// </summary>
        public HashSet<int> Achievements;

        /// <summary>
        /// 可选称号
        /// </summary>
        public string? Title;

        /// <summary>
        /// VIP等级(可空)
        /// </summary>
        public int? VipLevel;

        /// <summary>
        /// 游戏时长
        /// </summary>
        public TimeSpan PlayTime;

        /// <summary>
        /// 当前状态
        /// </summary>
        public PlayerState CurrentState;

        /// <summary>
        /// 上次登录IP
        /// </summary>
        private string _lastLoginIp;

        /// <summary>
        /// 创建时间戳
        /// </summary>
        public long CreateTimestamp;

        // 自动属性
        public int Age { get; set; }
        public string Email { get; set; }
        public bool IsVip { get; set; }
    }

    /// <summary>
    /// 英雄信息
    /// </summary>
    [Serializable]
    public class TestHeroInfo
    {
        public int HeroId;
        public string HeroName;
        public int StarLevel;
        public int Power;
        public List<TestSkillInfo> Skills;
        public TestHeroStats Stats;
    }

    /// <summary>
    /// 技能信息
    /// </summary>
    [Serializable]
    public class TestSkillInfo
    {
        public int SkillId;
        public string SkillName;
        public int Level;
        public float Cooldown;
        public bool IsPassive;
    }

    /// <summary>
    /// 英雄属性统计
    /// </summary>
    [Serializable]
    public class TestHeroStats
    {
        public int Attack;
        public int Defense;
        public int Hp;
        public int Mp;
        public float CritRate;
        public float CritDamage;
    }

    /// <summary>
    /// 玩家状态枚举
    /// </summary>
    public enum PlayerState
    {
        Offline = 0,
        Online = 1,
        InBattle = 2,
        InQueue = 3,
        AFK = 4,
    }
}
