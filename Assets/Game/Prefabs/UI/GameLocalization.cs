using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Mavis
{
    public enum GameLanguage { English, SimplifiedChinese }
    /// <summary>Shared by title screen, additive scenes and runtime-built HUDs.</summary>
    public static class GameLocalization
    {
        public const string PreferenceKey = "DarkBrine.Settings.Language";
        public static event Action Changed;
        static bool loaded;
        static GameLanguage language;
        static Font font;
        static TMP_FontAsset tmpFont;
        static TMP_FontAsset latinFont;
        static TMP_FontAsset uiFont;
        public static GameLanguage Language { get { EnsureLoaded(); return language; } }
        public static Font Font => font ? font : font = Resources.Load<Font>("Localization/NotoSansCJKsc-Regular");
        public static TMP_FontAsset TMPFont
        {
            get
            {
                if (!tmpFont && Font)
                {
                    tmpFont = Resources.Load<TMP_FontAsset>("Localization/NotoSansCJKsc SDF");
                }
                if(!latinFont)latinFont=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if(!uiFont && latinFont)
                {
                    // Keep Latin glyphs on their stable atlas in both languages. Chinese
                    // uses the bundled CJK fallback; the shared source assets are not edited.
                    uiFont=UnityEngine.Object.Instantiate(latinFont);
                    uiFont.name="Dark Brine UI";uiFont.hideFlags=HideFlags.DontSave;
                    uiFont.fallbackFontAssetTable=new List<TMP_FontAsset>();
                    if(tmpFont)uiFont.fallbackFontAssetTable.Add(tmpFont);
                    if(latinFont.fallbackFontAssetTable!=null)
                        foreach(var fallback in latinFont.fallbackFontAssetTable)
                            if(fallback && fallback!=tmpFont)uiFont.fallbackFontAssetTable.Add(fallback);
                }
                return uiFont ? uiFont : tmpFont;
            }
        }
        static void EnsureLoaded()
        {
            if (loaded) return;
            language = PlayerPrefs.GetInt(PreferenceKey, 0) == 1 ? GameLanguage.SimplifiedChinese : GameLanguage.English;
            loaded = true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(uiFont){if(Application.isPlaying)UnityEngine.Object.Destroy(uiFont);else UnityEngine.Object.DestroyImmediate(uiFont);}
            loaded=false;Changed=null;font=null;tmpFont=null;latinFont=null;uiFont=null;
        }
        public static void SetLanguage(GameLanguage value)
        {
            EnsureLoaded();
            value = value == GameLanguage.SimplifiedChinese ? value : GameLanguage.English;
            PlayerPrefs.SetInt(PreferenceKey, (int)value); PlayerPrefs.Save();
            if (language == value) return;
            language = value; Changed?.Invoke();
        }
        static readonly Dictionary<string, string[]> table = BuildTable();
        static Dictionary<string,string[]> BuildTable()
        {
            var result = new Dictionary<string,string[]>(StringComparer.Ordinal);
            void Add(string en, string zh, string key = null, params string[] aliases)
            {
                var pair = new[] {en,zh}; result[en] = pair; result[zh] = pair;
                if (key != null) result[key] = pair;
                foreach (string alias in aliases) result[alias] = pair;
            }
            Add("DARK BRINE: ROT", "暗潮：抽象之岛"); Add("MAIN MENU","主菜单"); Add("FIRST ISLAND","第一座岛");
            Add("01  /  FIRST ISLAND","01  /  第一座岛");
            Add("I / Esc to close","I / Esc 关闭");
            Add("CLICK TO SWITCH TERRAIN", "点击切换地形");
            Add("Fight the shark on the wreckage. Use your usual attacks and dodge.", "在木板上迎战鲨鱼。使用平时的攻击和闪避。");
            Add("The shark is winding up. Dodge away!", "鲨鱼正在蓄力，快闪开！");
            Add("Counterattack now!", "趁现在反击！");
            Add("A huge wave is coming!", "巨浪正在袭来！");
            Add("Climbing onto the board...", "正在爬上木板…");
            Add("{0}: climb onto the board", "{0}：爬上木板");
            Add("Swim near a board, then press {0} to climb", "游到木板旁，按 {0} 爬上去");
            Add("F: surf on this board  {0}: jump between boards", "F：操控木板冲浪  {0}：在木板之间跳跃");
            Add("{0}/{1}: forward/back  {2}/{3}: steer  {4}: jump off  F: walk on board", "{0}/{1}：前后移动  {2}/{3}：转向  {4}：跳离  F：恢复步行");
            Add("NEW GAME","开始游戏"); Add("CONTINUE","继续游戏"); Add("SAVE DATA FOUND","已有存档"); Add("NO SAVE DATA","暂无存档");
            Add("PAUSED","游戏暂停"); Add("RESUME","继续游戏"); Add("SETTINGS","设置"); Add("QUIT","退出游戏");
            Add("BACK","返回"); Add("DEFAULTS","恢复默认"); Add("APPLY & SAVE","应用并保存");
            Add("GAMEPLAY","游戏"); Add("CONTROLS","操作"); Add("AUDIO","声音"); Add("DISPLAY","显示"); Add("GRAPHICS","画质");
            Add("LANGUAGE","语言 / Language"); Add("Change language immediately; choice is saved","即时切换语言，自动保存选择");
            Add("CHALLENGE","难度"); Add("Karen Fairy health and chase; Sahur attack power","调整猪妖小仙人生命与追击、Sahur 攻击力", "Nailong health and chase; Sahur attack power", "调整奶龙生命与追击、Sahur 攻击力");
            Add("CAMERA DISTANCE","镜头距离"); Add("Third-person follow distance","第三人称镜头跟随距离"); Add("FIELD OF VIEW","视野范围"); Add("Camera perspective","镜头视野角度");
            Add("MOVE FORWARD","向前移动"); Add("MOVE BACK","向后移动"); Add("MOVE LEFT","向左移动"); Add("MOVE RIGHT","向右移动");
            Add("SPRINT","奔跑"); Add("JUMP","跳跃"); Add("DODGE","闪避"); Add("Click to rebind","点击修改按键");
            Add("HEALING PACK", "治疗包");
            Add("{0}  HEAL  {1}/2", "{0}  治疗包  {1}/2", "healing.count");
            Add("Healing pack: +{0} HP", "使用治疗包：+{0} HP", "healing.used");
            Add("Health is full", "生命值已满", "healing.full");
            Add("No healing packs remaining", "治疗包已用完", "healing.empty");
            Add("LOOK SENSITIVITY","鼠标灵敏度"); Add("Mouse camera speed","鼠标转动镜头的速度"); Add("INVERT VERTICAL LOOK","反转垂直视角"); Add("Reverse vertical mouse look","反转鼠标上下移动方向");
            Add("MASTER VOLUME","主音量"); Add("Overall game sound","所有游戏声音的音量"); Add("RESOLUTION","分辨率"); Add("Screen pixel dimensions","画面像素尺寸");
            Add("DISPLAY MODE","显示模式"); Add("Borderless or windowed","无边框或窗口模式"); Add("VERTICAL SYNC","垂直同步"); Add("Sync frames with the display","让帧率与显示器同步");
            Add("FRAME LIMIT","帧率上限"); Add("V-Sync can override this limit","开启垂直同步可能覆盖帧率上限");
            Add("RENDER SCALE","渲染比例"); Add("Internal resolution; lower is faster","内部渲染分辨率；调低可提高帧率");
            Add("TEXTURE DETAIL","贴图精度"); Add("Texture mipmap limit","贴图分辨率等级"); Add("TEXTURE FILTERING","贴图过滤"); Add("Sharper textures at oblique angles","让斜视角下的贴图更清晰");
            Add("OBJECT DETAIL","模型细节"); Add("Distance before lower-detail models appear","远处低精度模型的切换距离"); Add("ANTI-ALIASING","抗锯齿"); Add("Off, FXAA, or SMAA","关闭、FXAA 或 SMAA");
            Add("POST PROCESSING","后期处理"); Add("Camera image effects","镜头画面效果"); Add("BLOOM","泛光"); Add("Glow around bright parts of the image","画面亮部周围的柔光");
            Add("VIGNETTE","暗角"); Add("Darkening around the screen edge","让画面四周变暗"); Add("MOTION BLUR","动态模糊"); Add("Blur from camera and object movement","镜头与物体运动产生的模糊");
            Add("SHADOWS","阴影"); Add("Scene light shadows","场景光照阴影"); Add("SHADOW DETAIL","阴影精度"); Add("Shadow map resolution","阴影贴图分辨率"); Add("SHADOW DISTANCE","阴影距离"); Add("Maximum real-time shadow range","实时阴影的最远距离");
            Add("EASY","简单"); Add("NORMAL","普通"); Add("HARD","困难"); Add("ON","开启"); Add("OFF","关闭"); Add("BORDERLESS","无边框"); Add("WINDOWED","窗口"); Add("UNLIMITED","不限");
            Add("FULL","完整"); Add("HALF","一半"); Add("QUARTER","四分之一"); Add("PER TEXTURE","按贴图设置"); Add("FORCED","强制开启");
            Add("ESC  RESUME","Esc  继续游戏"); Add("ESC  BACK     /     APPLY TO SAVE","Esc  返回     /     应用以保存设置");
            Add("CONFIRM QUIT","确认退出"); Add("SELECT AGAIN TO EXIT  /  ESC TO CANCEL","再次点击退出  /  Esc 取消"); Add("SAVE FAILED  /  EXIT CANCELLED","存档失败  /  已取消退出");
            Add("PRESS A KEY...","请按下新按键…"); Add("PRESS A KEY  /  ESC TO CANCEL","按下新按键  /  Esc 取消"); Add("SETTINGS SAVED","设置已保存"); Add("DEFAULTS READY  /  APPLY TO SAVE","已恢复默认值  /  应用以保存");
            Add("Karen Fairy","猪妖小仙人", "enemy.name", "Nailong", "奶龙"); Add("YOU DIED","你倒下了 · 这把先寄了","你倒下了"); Add("Respawn at the island checkpoint","回到岛上的复活点，下一把继续","在岛上的复活点重新出发");
            Add("Health and stamina restored · Collected gear kept","生命与体力恢复 · 已拾取装备保留"); Add("RESPAWN  /  Enter","复活  /  Enter","复活  /  Enter");
            Add("Locked on · Middle click to release","锁定 · 中键解除");
            Add("ISLAND MAP","岛屿地图"); Add("NORTH  N","北  N"); Add("LEGEND","地图图例"); Add("▲  Player / Facing","▲  玩家 / 朝向"); Add("◆  Island checkpoint","◆  岛上复活点"); Add("●  Karen Fairy","●  猪妖小仙人","●  Nailong", "●  奶龙");
            Add("M / Esc to close · Game paused while viewing","M / Esc 关闭地图 · 查看时游戏暂停"); Add("BACK TO GAME","返回游戏");
            Add("Position\nX {0}   Z {1}\n\nMap width  {2} m","当前位置\nX {0}   Z {1}\n\n地图宽度  {2} 米","map.coordinates");
            Add("Head","头部"); Add("Body","身体"); Add("Legs","腿部"); Add("Feet","脚部"); Add("Weapon","武器"); Add("GEAR","装备");
            Add("Skills  /  1–4 to select","技能  /  1–4 选择"); Add("I  Gear / Skills","I  装备 / 技能"); Add("SAHUR  /  GEAR & SKILLS","SAHUR  /  装备与技能");
            Add("Close  [I]","关闭  [I]"); Add("Skill loadout  /  Four slots","技能配置  /  四个槽位"); Add("I / Esc to close · Gear collecting only; equipping and skills coming later","I / Esc 关闭   ·   装备可收集，穿戴与技能稍后加入");
            Add("{0}   Empty","{0}   空","skill.empty"); Add("{0}\nUnassigned","{0}\n未配置","skill.unassigned");
            Add("{0}     Empty","{0}     空","gear.empty"); Add("{0}  Collected {1}","{0}  已获得 {1}","gear.count");
            Add("SKILL SLOT {0}\n\nUnassigned.\n\n1–4  /  Select a slot","技能槽 {0}\n\n未配置。\n\n1–4  /  选择槽位","skill.details");
            Add("{0} collection\n\n{1}\n\nApproach dropped gear to collect it.","{0}收集\n\n{1}\n\n靠近掉落的装备可自动拾取。","gear.details");
            Add("No gear collected yet","暂无已获得装备","gear.none"); Add("Collected:","已获得：","gear.collected"); Add("Loot acquired: {0}","喜提战利品：{0}","gear.pickup");
            Add("Karen Fairy Clogs","猪妖小仙人洞洞鞋", "奶龙洞洞鞋", "Nailong Clogs"); Add("Karen Fairy Trousers","猪妖小仙人下装", "奶龙下装", "Nailong Trousers"); Add("Karen Fairy Round Glasses","猪妖小仙人圆框眼镜", "奶龙圆框眼镜", "Nailong Round Glasses");
            Add("SCROLL FOR MORE","向下滚动查看更多");
            Add("You lost consciousness.", "你失去了意识。");
            Add("The waves washed you ashore.", "海浪把你推上了岸。");
            Add("Your stick washed away. Find it along the beach.", "棍子被冲走了。沿着沙滩找到棍子。");
            Add("F  Pick up the stick", "按 F 拾起棍子");
            Add("You recovered your stick.", "找回了棍子。");
            Add("Come here, coward!","你过来呀！胆小鬼！","nailong.taunt");
            for (int i=1;i<=4;i++) { Add(i+"   Empty",i+"   空"); Add(i+"\nUnassigned",i+"\n未配置"); }
            string[] slotsEn={"Head","Body","Legs","Feet","Weapon"}, slotsZh={"头部","身体","腿部","脚部","武器"};
            for(int i=0;i<5;i++) Add(slotsEn[i]+"     Empty",slotsZh[i]+"     空");
            return result;
        }
        public static string Text(string key)
        {
            if (string.IsNullOrEmpty(key)) return key ?? "";
            return table.TryGetValue(key, out var pair) ? pair[(int)Language] : key;
        }
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Text(key), args);
    }
}
