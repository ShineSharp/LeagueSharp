using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace WhereDidHeGo
{
    public class Data
    {
        public static List<Tuple<int, Vector3>> StealthPoses = new List<Tuple<int, Vector3>>();
        public static List<Vector3> PingPoses = new List<Vector3>();
        public static List<Tuple<float, List<Vector2>>> StealthPaths = new List<Tuple<float, List<Vector2>>>();
        public static List<Tuple<int, string>> StealthSpells = new List<Tuple<int, string>>();
        public static List<_sdata> AntiStealthSpells = new List<_sdata>();
        public static List<_odata> StealthObjects = new List<_odata>();
        public static Obj_AI_Hero Teemo;
        public static Menu Config;

        public static void Initialize()
        {
            #region Spell Databases
            StealthSpells.Add(new Tuple<int, string>(3, "akalismokebomb"));
            StealthSpells.Add(new Tuple<int, string>(1, "khazixr"));
            StealthSpells.Add(new Tuple<int, string>(1, "khazixrlong"));
            StealthSpells.Add(new Tuple<int, string>(3, "talonshadowassault"));
            StealthSpells.Add(new Tuple<int, string>(1, "monkeykingdecoy"));
            StealthSpells.Add(new Tuple<int, string>(1, "hideinshadows"));

            AntiStealthSpells.Add(new _sdata { ChampionName = "caitlyn",    Spell = SpellSlot.W, SpellRange = 800,  StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "kogmaw",     Spell = SpellSlot.R, SpellRange = 1200, StealthDetectionLevel = 1 }); //range + level * 300
            AntiStealthSpells.Add(new _sdata { ChampionName = "leesin",     Spell = SpellSlot.Q, SpellRange = 1100, StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "nidalee",    Spell = SpellSlot.W, SpellRange = 900,  StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "nocturne",   Spell = SpellSlot.Q, SpellRange = 1200, StealthDetectionLevel = 1 });
            AntiStealthSpells.Add(new _sdata { ChampionName = "twistedfate",Spell = SpellSlot.R, SpellRange = 4000, StealthDetectionLevel = 3, SelfCast = true });
            #endregion
            #region Config Menu
            Config = new Menu("Where Did He Go", "wheredidhego", true);
            Config.AddItem(new MenuItem("DRAWCIRCLE", "Show Where did enemy go").SetValue(true));
            Config.AddItem(new MenuItem("PINGMODE", "Ping Enemy Missing").SetValue(new StringList(new string[] { "Only local", "Send to team", "Disabled" }, 0)));
            Config.AddItem(new MenuItem("PINGCOUNT", "Ping X Times").SetValue(new Slider(1, 1, 100)));
            Config.AddItem(new MenuItem("DRAWWAPOINTS", "Draw Enemy's Last Waypoint").SetValue(true));
            Config.AddItem(new MenuItem("ENABLEINCOMBO", "Only Enable In Combo").SetValue(false)).ValueChanged += (s, ar) => Config.Item("ENABLECOMBOKEY").Show(ar.GetNewValue<bool>());
            Config.AddItem(new MenuItem("ENABLECOMBOKEY", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press))).Show(Config.Item("ENABLEINCOMBO").GetValue<bool>());
            Config.AddItem(new MenuItem("ENABLEWDHG", "Enabled").SetValue(true));

            Menu antiStealth = new Menu("Anti-Stealth", "wdhgantistealth");
            antiStealth.AddItem(new MenuItem("USEVISIONWARD", "Use Vision Ward").SetValue(true));
            antiStealth.AddItem(new MenuItem("USEORACLESLENS", "Use Oracle's Lens").SetValue(true));
            antiStealth.AddItem(new MenuItem("USELIGHTBRINGER", "Use Lightbringer").SetValue(true));
            antiStealth.AddItem(new MenuItem("USEHEXTECHSWEEPER", "Use Hextech Sweeper").SetValue(true));
            antiStealth.AddItem(new MenuItem("USESNOWBALL", "Use ARAM Snowball").SetValue(true));

            Config.AddSubMenu(antiStealth);

            var spells = AntiStealthSpells.Where(p => p.ChampionName == ObjectManager.Player.ChampionName.ToLower());
            if (spells != null)
            {
                Menu antiStealthSpells = new Menu("Reveal Spells", "wdhgrevealspells");
                foreach (var spell in spells)
                {
                    Menu antiStealthSpell = new Menu(String.Format("{0} ({1})", ObjectManager.Player.Spellbook.GetSpell(spell.Spell).Name, spell.Spell), "wdhg" + spell.Spell.ToString());
                    antiStealthSpell.AddItem(new MenuItem(String.Format("DETECT{0}", spell.Spell.ToString()), "Detection Level").SetValue(new Slider(spell.StealthDetectionLevel, 1, 3)));
                    antiStealthSpell.AddItem(new MenuItem(String.Format("USE{0}", spell.Spell.ToString()), "Enabled").SetValue(true));
                    antiStealthSpells.AddSubMenu(antiStealthSpell);
                }
                Config.AddSubMenu(antiStealthSpells);
            }

            Config.AddToMainMenu();
            #endregion
            Teemo = HeroManager.Enemies.FirstOrDefault(p => p.ChampionName.ToLower() == "teemo");
        }

        public struct _sdata
        {
            public string ChampionName;
            public SpellSlot Spell;
            public float SpellRange;
            public int StealthDetectionLevel;
            public bool SelfCast;
        }

        public class _odata
        {
            public string DisplayName;
            public Vector2 Position;
            public int EndTick;
            public int Radius;
        }
    }
}
