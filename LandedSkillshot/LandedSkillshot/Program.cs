using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LeagueSharp;
using LeagueSharp.Common;

namespace LandedSkillshot
{
    class Program
    {
        private static Menu s_Menu;
        private static int hitCount = 0;
        private static int castCount = 0;

        private struct _lastSpells
        {
            public string name;
            public int tick;

            public _lastSpells(string n, int t)
            {
                name = n;
                tick = t;
            }
        }

        private static List<_lastSpells> LastSpells = new List<_lastSpells>();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            s_Menu = new Menu("Landed SkillShots", "landedskillshot", true);
            s_Menu.AddItem(new MenuItem("drawingx", "Drawing Pos X").SetValue(new Slider(Drawing.Width - 200, 0, Drawing.Width)));
            s_Menu.AddItem(new MenuItem("drawingy", "Drawing Pos Y").SetValue(new Slider(0, 0, Drawing.Height)));
            s_Menu.AddItem(new MenuItem("countonlycombo", "Count Only In Combo Mode").SetValue(false)).ValueChanged += (s, ar) => s_Menu.Item("combokey").Show(ar.GetNewValue<bool>());
            s_Menu.AddItem(new MenuItem("combokey", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press))).Show(s_Menu.Item("countonlycombo").GetValue<bool>());
            s_Menu.AddItem(new MenuItem("resetval", "Reset").SetValue(false))
                .ValueChanged += (s, ar) => 
                    {
                        if (ar.GetNewValue<bool>())
                        {
                            hitCount = 0;
                            castCount = 0;
                            (s as MenuItem).SetValue(false);
                        }
                    };
            s_Menu.AddItem(new MenuItem("landedskillshotsenabled", "Enabled").SetValue(true));
            s_Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Obj_AI_Hero.OnDamage += Obj_AI_Hero_OnDamage;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (IsEnabled)
            {
                Drawing.DrawText(s_Menu.Item("drawingx").GetValue<Slider>().Value, s_Menu.Item("drawingy").GetValue<Slider>().Value, System.Drawing.Color.Red, String.Format("Casted Spell Count: {0}", castCount));
                Drawing.DrawText(s_Menu.Item("drawingx").GetValue<Slider>().Value, s_Menu.Item("drawingy").GetValue<Slider>().Value + 20, System.Drawing.Color.Red, String.Format("Hit Spell Count: {0}", hitCount));
                Drawing.DrawText(s_Menu.Item("drawingx").GetValue<Slider>().Value, s_Menu.Item("drawingy").GetValue<Slider>().Value + 40, System.Drawing.Color.Red, String.Format("Hitchance (%): {0}%", castCount > 0 ? (((float)hitCount / castCount) * 100).ToString("00.00") : "n/a"));
            }
        }

        private static void Obj_AI_Hero_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            lock (LastSpells)
            {
                LastSpells.RemoveAll(p => Environment.TickCount - p.tick > 2000);
                if (args.SourceNetworkId == ObjectManager.Player.NetworkId && HeroManager.Enemies.Exists(p => p.NetworkId == sender.NetworkId))
                {
                    if (LastSpells.Count != 0)
                    {
                        LastSpells.RemoveAt(0);
                        hitCount++;
                    }
                }
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            lock (LastSpells)
            {
                LastSpells.RemoveAll(p => Environment.TickCount - p.tick > 2000);
                if (sender.IsMe && !args.SData.IsAutoAttack() && (!OnlyInCombo || ComboKeyActive))
                {
                    if (args.Target == null && !LastSpells.Exists(p => p.name == args.SData.Name))
                    {
                        LastSpells.Add(new _lastSpells(args.SData.Name, Environment.TickCount));
                        castCount++;
                    }
                }
            }
        }

        public static bool IsEnabled
        {
            get { return s_Menu.Item("landedskillshotsenabled").GetValue<bool>(); }
        }

        public static bool OnlyInCombo
        {
            get { return s_Menu.Item("countonlycombo").GetValue<bool>(); }
        }

        public static bool ComboKeyActive
        {
            get { return s_Menu.Item("combokey").GetValue<KeyBind>().Active; }
        }
    }
}
