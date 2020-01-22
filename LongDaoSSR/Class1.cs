using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Harmony12;
using UnityModManagerNet;
using System.Reflection;
using GameData;

//using System.IO;

namespace LongDaoSSR
{
    public static class Main
    {
        public static bool enabled;
        public static UnityModManager.ModEntry.ModLogger logger;
        public static int lastNPCid = -1; //最后生成且还未判断的NPC的id，-1表示无
        public static bool oneFlag = false;
        public static bool isInGame = false;
        /**
         *  招募忠仆时候进行培训的银钱
         **/
        public static int practiceMoney = 0;


        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;


            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (!value) return false;
            enabled = value;
            logger.Log("龙岛忠仆MOD正在运行");
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label("  修改了一些龙岛忠仆的特性，让忠仆更适合太吾村的发展!");
            GUILayout.Label("  获取忠仆可以获取太吾各项属性的10%的资质!内功身法绝技为20%");
            GUILayout.Label("  忠仆默认与太吾处事立场相同!");
            GUILayout.Label("  忠仆会获得抓周特性和随机一个特殊特性!特殊特性为【梦境中人】【神锋敛彩】【璞玉韬光】");
            GUILayout.Label("  忠仆默认16岁，寿命至少为60岁!");
            GUILayout.BeginHorizontal();
            GUILayout.Label("  太吾教育中心：招募忠仆时花费银钱进行专业培训：");
            var guiPracticeMoney = GUILayout.TextField(Main.practiceMoney.ToString(), 3, GUILayout.Width(85));
            if (GUI.changed && !int.TryParse(guiPracticeMoney, out Main.practiceMoney))
            {
                Main.practiceMoney = 0;
            }
            GUILayout.Label("万；每1万银钱可额外提升忠仆各项资质1点");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Label("  <color=#FF0000FF>如果要删除原【龙岛忠仆】本MOD，请在对应存档内按下清除新特性的按钮并存档，避免坏档，清除新特性只影响显示效果，忠仆不会消失</color>");

            //检测存档
            DateFile tbl = DateFile.instance;
            if (tbl == null || Game.Instance.GetCurrentGameStateName().ToString() != eGameState.InGame.ToString())
            {
                GUILayout.Label("  存档未载入!");
            }
            else
            {
                if (GUILayout.Button("清除新特性"))
                {
                    DeletNewFeature();
                }
            }
        }

        /// <summary>
        /// 遍历人物列表并清除新特性
        /// </summary>
        public static void DeletNewFeature()
        {
            List<int> idlist = new List<int>();
            //int num = 0;
            logger.Log("开始清除新特性");
            int[] allCount = Characters.GetAllCharIds();
            logger.Log("人物有" + allCount.Length + "个等待遍历");
            try
            {
                foreach (int actorId in allCount)
                {
                    if (Characters.HasChar(actorId) && Characters.GetCharProperty(actorId, 101) != null && Characters.GetCharProperty(actorId, 101).Contains("4005"))
                    {
                        logger.Log("忠仆人物id：" + actorId);
                        idlist.Add(actorId);
                    }
                }
                logger.Log("检测到" + idlist.Count.ToString() + "个龙岛忠仆，开始清除新特性数据...");
            }
            catch (Exception ex)
            {
                string excptionString = ex.StackTrace;
                logger.Error(excptionString);
            }
            for (int i = 0; i < idlist.Count; i++)
            {
                DeletNPCNewFeature(idlist[i]);
            }
            logger.Log("清除完毕");
        }

        /// <summary>
        /// 清除NPC的新特性
        /// </summary>
        /// <param name="id">NPCid</param>
        public static void DeletNPCNewFeature(int id)
        {
            bool hasNewFeature = false;
            //Dictionary<int, string> npc;
            //npc = DateFile.instance.actorsDate[id];
            string featureStr = Characters.GetCharProperty(id, 101);

            List<int> feature = new List<int>();
            for (int i = 0; i < DateFile.instance.GetActorFeature(id).Count; i++)
            {
                feature.Add(DateFile.instance.GetActorFeature(id)[i]);
            }
            foreach (int f in feature)
            {
                if (f >= 4006 && f <= 4034)//新特性编号范围
                {
                    hasNewFeature = true;
                    featureStr = featureStr.Replace("|" + f.ToString(), "");
                }
            }

            if (hasNewFeature)
            {
                //DateFile.instance.ActorFeaturesCacheReset(id);
                Characters.SetCharProperty(id, 101, featureStr);
                DateFile.instance.ActorFeaturesCacheReset();
            }

        }


        /// <summary>
        /// 在开始游戏界面注入新特性
        /// </summary>
        [HarmonyPatch(typeof(MainMenu), "ShowStartGameWindow")]
        public static class MainMenu_ShowStartGameWindow_Patch
        {
            private static void Postfix()
            {
                if (!Main.enabled)
                {
                    return;
                }
                if (!oneFlag)
                {
                    // AddAllFeature();
                    // debugLogIntIntString(DateFile.instance.actorFeaturesDate, true);//显示全部特性
                }
                return;
            }
        }

        /// <summary>
        /// 获取新生NPC的ID
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "MakeNewActor")]
        public static class DateFile_MakeNewActor_Patch
        {
            private static void Postfix(DateFile __instance, int __result)
            {
                if (!Main.enabled)
                {
                    return;
                }
                logger.Log("新的NPC生成了！id:" + __result);
                lastNPCid = __result;
                //DateFile.instance.ActorFeaturesCacheReset(__result); //刷新特性
                DateFile.instance.ActorFeaturesCacheReset(); //刷新特性缓存
                return;
            }
        }

        /// <summary>
        /// 创建NPC之后，显示新相知之前执行的函数，用来修改龙岛忠仆
        /// </summary>
        [HarmonyPatch(typeof(DateFile), "ChangeFavor")]
        public static class DateFile_ChangeFavor_Patch
        {
            private static void Postfix()
            {
                if (!Main.enabled)
                {
                    return;
                }
                if (lastNPCid != -1)
                {
                    //logger.Log("特性:" + DateFile.instance.actorsDate[lastNPCid][101]);
                    if (IsLongDaoZhongPu(lastNPCid))
                    {
                        try
                        {
                            NpcChange(lastNPCid);
                        }
                        catch (Exception e)
                        {
                            logger.Error("龙岛忠仆Mod变更NPC属性失败!");
                            logger.Error(e.StackTrace);
                            DateFile.instance.ActorFeaturesCacheReset(lastNPCid);
                        }

                    }
                    lastNPCid = -1;
                }
            }
        }

        /// <summary>
        /// 判断指定idNPC是否为龙岛忠仆
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsLongDaoZhongPu(int id)
        {
            List<int> npcFeature = DateFile.instance.GetActorFeature(lastNPCid);
            return npcFeature.Contains(4005);
        }

        /// <summary>
        /// 修改指定idNPC数据
        /// </summary>
        /// <param name="id">忠仆id</param>
        public static void NpcChange(int id)
        {

            //1.【志同道合】龙岛忠仆的处世立场与玩家获得忠仆时的立场相同
            Characters.SetCharProperty(id, 16, Characters.GetCharProperty(DateFile.instance.mianActorId, 16));
            // 设置忠仆年龄为16岁
            Characters.SetCharProperty(id, 11, "16");

            //2.【龙神赐寿】每个龙岛忠仆都被龙神赋予更多的阳寿，终生侍奉主人
            // 忠仆最少年龄为60岁
            // logger.Log("npc[11]年龄 npc[12]健康 npc[13]健康上线:" + Characters.GetCharProperty(id, 11) + " " + Characters.GetCharProperty(id, 12) + " " + Characters.GetCharProperty(id, 13));
            if (Int32.Parse(Characters.GetCharProperty(id, 12)) < 60)
            {
                Characters.SetCharProperty(id, 13, "60");
                Characters.SetCharProperty(id, 12, "60");
            }

            // 修改特性，移除不良特性
            logger.Log("npc 特性 :" + Characters.GetCharProperty(id, 101));
            String[] npcFeature = Characters.GetCharProperty(id, 101).Split('|');
            
            // 是否有抓周特性
            Boolean hasZhuaZhouFeature = false;
            // 是否有特殊特性
            Boolean hasSpecialFeature = false;
            foreach (String featureId in npcFeature)
            {
                int featureItem =  Int32.Parse(featureId);
                // 移除异常生育特质
                if (featureItem >= 1001 && featureItem <= 1003)
                {
                    continue;
                }
                // 是否有抓周、特殊特性 ，没有的话遍历结束后自动加上
                if (featureItem >= 2001 && featureItem <= 2012)
                {
                    hasZhuaZhouFeature = true;
                }
                if (featureItem >= 5001 && featureItem <= 5003)
                {
                    hasSpecialFeature = true;
                }
                //进行不良特性升级 ，-3变为+1 ；-2变为+1 ；+1变为+1
                if (featureItem % 6 == 0 && featureItem < 500)
                {
                    logger.Log("0cacheResetFeatureResult replace :" + Characters.GetCharProperty(id, 101).Replace(featureItem.ToString(), (featureItem-5).ToString()));
                    Characters.SetCharProperty(id, 101, Characters.GetCharProperty(id, 101).Replace(featureItem.ToString(), (featureItem-5).ToString()));
                    continue;
                }
                if (featureItem % 6 == 5 && featureItem < 500)
                {
                    logger.Log("0cacheResetFeatureResult replace :" + Characters.GetCharProperty(id, 101).Replace(featureItem.ToString(), (featureItem-4).ToString()));
                    Characters.SetCharProperty(id, 101, Characters.GetCharProperty(id, 101).Replace(featureItem.ToString(), (featureItem-4).ToString()));
                    continue;
                }
                if (featureItem % 6 == 4 && featureItem < 500)
                {
                    logger.Log("0cacheResetFeatureResult replace :" + Characters.GetCharProperty(id, 101).Replace(featureItem.ToString(), (featureItem-3).ToString()));
                    Characters.SetCharProperty(id, 101, Characters.GetCharProperty(id, 101).Replace(featureItem.ToString(), (featureItem-3).ToString()));
                    continue;
                }
                
            }
            System.Random random = new System.Random();
            if (!hasZhuaZhouFeature)
            {
                Characters.SetCharProperty(id, 101, Characters.GetCharProperty(id, 101) + "|" + Int32.Parse((random.Next(1, 12) + 2000).ToString()));
            }
            if (!hasSpecialFeature)
            {
                Characters.SetCharProperty(id, 101, Characters.GetCharProperty(id, 101) + "|" + Int32.Parse((random.Next(1, 3) + 5000).ToString()));
            }
            // 最终特性
            logger.Log("featureResult :" + Characters.GetCharProperty(id,101));

            //资质均衡
            Characters.SetCharProperty(id, 551, "2");
            Characters.SetCharProperty(id, 651, "2");

            //资质加强

            //培训班增强数
            int practiceIntelligence = 0;
            // 获取太吾身上的余额
            int balance = Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, 406));
            logger.Log("太吾银钱 :" + balance);
            int cost = 0;
            if (balance < Main.practiceMoney * 10000)
            {
                practiceIntelligence = (int)(balance / 10000);
            }
            else
            {
                practiceIntelligence = Main.practiceMoney;
            }
            cost = practiceIntelligence * 10000;
            logger.Log("培训班费用" + cost);
            logger.Log("培训班技能增强" + practiceIntelligence);
            //增强内功身法绝技
            int neiGongIntelligence = 80 + practiceIntelligence + (int)(Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, 601)) * 0.2);
            int shenFaIntelligence = 80 + practiceIntelligence + (int)(Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, 602)) * 0.2);
            int jueJiIntelligence = 80 + practiceIntelligence + (int)(Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, 603)) * 0.2);
            Characters.SetCharProperty(id, 601, neiGongIntelligence.ToString());
            Characters.SetCharProperty(id, 602, shenFaIntelligence.ToString());
            Characters.SetCharProperty(id, 603, jueJiIntelligence.ToString());

            // 增强基础属性
            for (int i = 61; i <= 66; i++)
            {
                int baseIntelligence = Int32.Parse(Characters.GetCharProperty(id, i));
                if (baseIntelligence < 60)
                {
                    baseIntelligence = 60;
                }
                baseIntelligence = baseIntelligence + practiceIntelligence + (int)(Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, i)) * 0.1);
                Characters.SetCharProperty(id, i, baseIntelligence.ToString());
            }
            // 增强技艺
            for (int i = 501; i <= 516; i++)
            {
                int baseIntelligence = Int32.Parse(Characters.GetCharProperty(id, i));
                if (baseIntelligence < 60)
                {
                    baseIntelligence = 60;
                }
                // 厨艺锻造不能差
                if (i == 515 || i == 507)
                {
                    if (baseIntelligence < 80)
                    {
                        baseIntelligence = 80;
                    }
                }
                baseIntelligence = baseIntelligence + practiceIntelligence + (int)(Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, i)) * 0.1);
                Characters.SetCharProperty(id, i, baseIntelligence.ToString());
            }
            // 增强武学
            for (int i = 604; i <= 614; i++)
            {
                int baseIntelligence = Int32.Parse(Characters.GetCharProperty(id, i));
                if (baseIntelligence < 60)
                {
                    baseIntelligence = 60;
                }
                baseIntelligence = baseIntelligence + practiceIntelligence + (int)(Int32.Parse(Characters.GetCharProperty(DateFile.instance.mianActorId, i)) * 0.1);
                Characters.SetCharProperty(id, i, baseIntelligence.ToString());
            }

            //扣除学习班费用
            if (cost >= 10000)
            {
                UIDate.instance.ChangeResource(DateFile.instance.mianActorId, 5, -1 * cost, true);
                logger.Log("交完学费所剩银钱:" + Characters.GetCharProperty(DateFile.instance.mianActorId, 406));
            }
            //刷新特性
            DateFile.instance.ActorFeaturesCacheReset();
            logger.Log("npc change finished");
            //工作服 73703 劲衣 工作车 83503 下泽车
            DateFile.instance.SetActorEquip(id, 305, DateFile.instance.MakeNewItem(73703));
            DateFile.instance.SetActorEquip(id, 311, DateFile.instance.MakeNewItem(83503));
        }


        //debug遍历输出Dictionary<int, Dictionary<int, string>>
        public static void debugLogIntIntString(Dictionary<int, Dictionary<int, string>> dic, bool savelog)
        {
            String logText = "";
            int tmpnum = 0;
            foreach (KeyValuePair<int, Dictionary<int, string>> e in dic)
            {
                logText += "\nkey:" + e.Key + " value: ";
                foreach (KeyValuePair<int, string> kv in e.Value)
                {
                    logText += kv.Value + ",";
                }
                tmpnum++;
                if (tmpnum > 10000)
                {
                    break;
                }
            }
            logger.Log(logText);
        }
        //debug遍历输出List<int>
        public static void debugLogListInt(List<int> list)
        {
            String logText = "";
            for (int i = 0; i < list.Count; i++)
            {
                logText += list[i] + ",";
            }
            logger.Log(logText);
        }

    }
}