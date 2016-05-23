namespace Taliyah
{
    using System;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using SharpDX;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;


    class Program
    {
        private static Menu main_menu;
        private static Spell Q, W, E;
        private static Vector3 lastE;
        private static bool Q5x = true;
        static void Main(string[] args)
        {
            Events.OnLoad += OnLoad;
        }

        private static void OnLoad(object sender, EventArgs e)
        {
            main_menu = new Menu("taliyah", "Taliyah", true);

            Menu combo = new Menu("taliyah.combo", "Combo");
            combo.Add(new MenuBool("taliyah.combo.useq", "Use Q", true, ObjectManager.Player.ChampionName));
            combo.Add(new MenuBool("taliyah.combo.usew", "Use W", true, ObjectManager.Player.ChampionName));
            combo.Add(new MenuBool("taliyah.combo.usee", "Use E", true, ObjectManager.Player.ChampionName));
            main_menu.Add(combo);

            Menu harass = new Menu("taliyah.harass", "Harass");
            harass.Add(new MenuBool("taliyah.harass.useq", "Use Q", true, ObjectManager.Player.ChampionName));
            harass.Add(new MenuSlider("taliyah.harass.manaperc", "Min. Mana", 40, 0, 100, ObjectManager.Player.ChampionName));
            main_menu.Add(harass);

            Menu laneclear = new Menu("taliyah.laneclear", "LaneClear");
            laneclear.Add(new MenuBool("taliyah.laneclear.useq", "Use Q", true, ObjectManager.Player.ChampionName));
            laneclear.Add(new MenuSlider("taliyah.laneclear.minq", "Min. Q Hit", 3, 1, 6, ObjectManager.Player.ChampionName));
            laneclear.Add(new MenuSlider("taliyah.laneclear.manaperc", "Min. Mana", 40, 0, 100, ObjectManager.Player.ChampionName));
            main_menu.Add(laneclear);

            main_menu.Add(new MenuBool("taliyah.onlyq5", "Only cast 5x Q", false, ObjectManager.Player.ChampionName));
            main_menu.Add(new MenuBool("taliyah.antigap", "Auto E to Gapclosers", true, ObjectManager.Player.ChampionName));
            main_menu.Add(new MenuBool("taliyah.interrupt", "Auto W to interrupt spells", true, ObjectManager.Player.ChampionName));
            main_menu.Attach();
      
            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0f, 60f, Q.Instance.SData.MissileSpeed, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 800f);
            W.SetSkillshot(0.5f, 30f, 0, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 700f);
            E.SetSkillshot(0.25f, 150f, 2000f, false, SkillshotType.SkillshotLine);

            Game.OnUpdate += Game_OnUpdate;
            Events.OnGapCloser += Events_OnGapCloser;
            Events.OnInterruptableTarget += Events_OnInterruptableTarget;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Name == "Taliyah_Base_Q_aoe_bright.troy")
                Q5x = false;
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Name == "Taliyah_Base_Q_aoe_bright.troy")
                Q5x = true;
        }


        private static void Combo()
        {
            if (W.Instance.Name == "TaliyahWNoClick")
            {
                ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W, lastE, false);
            }
            else
            {
                if (E.IsReady() && main_menu["taliyah.combo"]["taliyah.combo.usee"].GetValue<MenuBool>().Value)
                {
                    if (W.IsReady() && main_menu["taliyah.combo"]["taliyah.combo.usew"].GetValue<MenuBool>().Value)
                    {
                        //e w combo
                        var target = E.GetTarget();
                        if (target != null)
                        {
                            var pred = W.GetPrediction(target);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                lastE = ObjectManager.Player.ServerPosition;
                                E.Cast(ObjectManager.Player.ServerPosition.ToVector2() + (pred.CastPosition.ToVector2() - ObjectManager.Player.ServerPosition.ToVector2()).Normalized() * (E.Range - 200));
                                DelayAction.Add(250, () => W.Cast(pred.UnitPosition));
                            }
                        }
                        return;
                    }
                    else
                    {
                        var target = E.GetTarget();
                        if (target != null)
                            E.Cast(target);
                    }
                }
                if (W.IsReady() && main_menu["taliyah.combo"]["taliyah.combo.usew"].GetValue<MenuBool>().Value)
                {
                    var target = W.GetTarget();
                    if (target != null)
                        W.CastIfHitchanceEquals(target, HitChance.High);
                }
            }
            var q_target = Q.GetTarget();
            if (q_target != null && main_menu["taliyah.combo"]["taliyah.combo.useq"].GetValue<MenuBool>().Value && (!main_menu["taliyah.onlyq5"].GetValue<MenuBool>().Value || Q5x))
                Q.Cast(q_target);
        }

        private static void Harass()
        {
            if (ObjectManager.Player.ManaPercent < main_menu["taliyah.harass"]["taliyah.harass.manaperc"].GetValue<MenuSlider>().Value)
                return;

            if (main_menu["taliyah.harass"]["taliyah.harass.useq"].GetValue<MenuBool>().Value)
            {
                var target = Q.GetTarget();
                if (target != null)
                    Q.Cast(target);
            }
        }

        private static void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < main_menu["taliyah.laneclear"]["taliyah.laneclear.manaperc"].GetValue<MenuSlider>().Value)
                return;
            
            if (main_menu["taliyah.laneclear"]["taliyah.laneclear.useq"].GetValue<MenuBool>().Value)
            {
                var farm = Q.GetCircularFarmLocation(ObjectManager.Get<Obj_AI_Minion>().Where(p => p.IsEnemy && p.DistanceToPlayer() < Q.Range).Select(q => (Obj_AI_Base)q).ToList());
                if (farm.MinionsHit >= main_menu["taliyah.laneclear"]["taliyah.laneclear.minq"].GetValue<MenuSlider>().Value)
                    Q.Cast(farm.Position);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            switch(Variables.Orbwalker.ActiveMode)
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Harass();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
            }
        }

        private static void Events_OnInterruptableTarget(object sender, Events.InterruptableTargetEventArgs e)
        {
            if (main_menu["taliyah.interrupt"].GetValue<MenuBool>().Value)
            {
                if (e.Sender.DistanceToPlayer() < W.Range)
                    W.Cast(e.Sender.ServerPosition);
            }
        }

        private static void Events_OnGapCloser(object sender, Events.GapCloserEventArgs e)
        {
            if (main_menu["taliyah.antigap"].GetValue<MenuBool>().Value)
            {
                if (e.End.DistanceToPlayer() < E.Range)
                    E.Cast(e.Sender.ServerPosition);
            }
        }
    }
}
