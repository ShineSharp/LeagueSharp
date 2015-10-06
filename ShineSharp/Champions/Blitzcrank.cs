using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SPrediction;
using ShineCommon;

namespace ShineSharp.Champions
{
    public class Blitzcrank : BaseChamp
    {
        public Blitzcrank()
            : base ("Blitzcrank")
        {

        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("CUSEE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("CQHITCHANCE", "Q Hit Chance").SetValue<StringList>(new StringList(ShineCommon.Utility.HitchanceNameArray, 2)));
            combo.AddItem(new MenuItem("CUSERGRAB", "Use R If Grabbed").SetValue(true));
            combo.AddItem(new MenuItem("CUSERHIT", "Use R If Enemies >=").SetValue(new Slider(2, 1, 5)));
            
            //
            Menu nograb = new Menu("Grab Filter", "autograb");
            foreach (Obj_AI_Hero enemy in HeroManager.Enemies)
                nograb.AddItem(new MenuItem("nograb" + enemy.ChampionName, string.Format("Dont Grab {0}", enemy.ChampionName)).SetValue(false));
            //
            combo.AddSubMenu(nograb);

            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));
            harass.AddItem(new MenuItem("HQHITCHANCE", "Q Hit Chance").SetValue<StringList>(new StringList(ShineCommon.Utility.HitchanceNameArray, 2)));

            laneclear = new Menu("LaneClear", "LaneClear");
            laneclear.AddItem(new MenuItem("LUSER", "Use R").SetValue(true));
            laneclear.AddItem(new MenuItem("LMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));


            misc = new Menu("Misc", "Misc");
            //
            Menu autograb = new Menu("Auto Grab (Q)", "autograb");
            foreach (Obj_AI_Hero enemy in HeroManager.Enemies)
                autograb.AddItem(new MenuItem("noautograb" + enemy.ChampionName, string.Format("Dont Grab {0}", enemy.ChampionName)).SetValue(false));
            autograb.AddItem(new MenuItem("MAUTOQIMMO", "Auto Grab Immobile Target").SetValue(true));
            autograb.AddItem(new MenuItem("MAUTOQRANGE", "Max. Grab Range").SetValue(new Slider(800, 1, 1000)));
            autograb.AddItem(new MenuItem("MAUTOQHITCHANCE", "Auto Grab Hit Chance").SetValue<StringList>(new StringList(ShineCommon.Utility.HitchanceNameArray, 2)));
            autograb.AddItem(new MenuItem("MAUTOQHP", "Min. HP Percent").SetValue(new Slider(40, 1, 100)));
            autograb.AddItem(new MenuItem("MAUTOQ", "Enabled").SetValue(true));
            //
            misc.AddSubMenu(autograb);
            //
            Menu interrupt = new Menu("Auto Interrupt", "aintrpt");
            interrupt.AddItem(new MenuItem("MINTQ", "Use Q").SetValue(true));
            interrupt.AddItem(new MenuItem("MINTE", "Use E").SetValue(true));
            interrupt.AddItem(new MenuItem("MINTR", "Use R").SetValue(true));
            interrupt.AddItem(new MenuItem("MINTEN", "Enabled").SetValue(true));
            //
            misc.AddSubMenu(interrupt);

            misc.AddItem(new MenuItem("MTGRAB", "Show Toggle Harass Status").SetValue(true))
                    .ValueChanged += (s, ar) =>
                    {
                        if (ar.GetNewValue<bool>())
                            Config.SubMenu("Orbwalking").Item("Farm").Permashow(true, "Grab Toggle Status");
                        else
                            Config.SubMenu("Orbwalking").Item("Farm").Permashow(false);
                    };

            Config.SubMenu("Orbwalking").Item("Farm").Permashow(true, "Grab Toggle Status");

            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.Combo] += Combo;
            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.Mixed] += Harass;
            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
            BeforeOrbWalking += BeforeOrbwalk;
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 1000f);
            Spells[Q].SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);
            Spells[W] = new Spell(SpellSlot.W, 0f);
            Spells[E] = new Spell(SpellSlot.E, 125f);
            Spells[R] = new Spell(SpellSlot.R, 550f);
        }

        
        public void BeforeOrbwalk()
        {
            #region Auto Harass

            if (Spells[Q].IsReady() && Config.Item("MAUTOQ").GetValue<bool>() && Config.Item("MAUTOQHP").GetValue<Slider>().Value <= ObjectManager.Player.HealthPercent && !ObjectManager.Player.UnderTurret())
            {
                var t = TargetSelector.GetTarget(Config.Item("MAUTOQRANGE").GetValue<Slider>().Value, TargetSelector.DamageType.Magical);
                if (t != null && !Config.Item("noautograb" + t.ChampionName).GetValue<bool>())
                    Spells[Q].SPredictionCast(t, ShineCommon.Utility.HitchanceArray[Config.Item("MAUTOQHITCHANCE").GetValue<StringList>().SelectedIndex]);
            }

            #endregion
        }

        public void Combo()
        {
            bool chase = false;
            if (Spells[R].IsReady() && Spells[Q].IsReady() && Config.Item("CUSERGRAB").GetValue<bool>())
            {
                var t = HeroManager.Enemies.Where(p => p.IsValidTarget(Spells[R].Range + 100)).OrderBy(q => TargetSelector.GetPriority(q)).LastOrDefault();
                if (t != null)
                {
                    if (Config.Item("nograb" + t.ChampionName).GetValue<bool>())
                        return;
                    chase = true;
                    if(Spells[W].IsReady())
                        Spells[W].Cast();
                    if(t.IsValidTarget(Spells[R].Range - 10))
                        Spells[R].Cast();
                }
            }
            if (!chase)
            {
                if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
                {
                    var t = TargetSelector.GetTarget(Spells[Q].Range - 30, TargetSelector.DamageType.Magical);
                    if (t != null && (!t.HasBuffOfType(BuffType.SpellImmunity) && !t.HasBuffOfType(BuffType.SpellShield)) && t.IsValidTarget(Spells[Q].Range - 30))
                    {
                        if (Config.Item("nograb" + t.ChampionName).GetValue<bool>())
                            return;
                        CastSkillshot(t, Spells[Q], ShineCommon.Utility.HitchanceArray[Config.Item("CQHITCHANCE").GetValue<StringList>().SelectedIndex]);
                    }
                }
                
                if (Spells[R].IsReady())
                {
                    var t = HeroManager.Enemies.Where(p => p.IsValidTarget(Spells[R].Range)).OrderBy(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                    if (t != null)
                    {
                        if (t.HasBuffOfType(BuffType.Knockup) && t.IsValidTarget(Spells[R].Range) && Config.Item("CUSERGRAB").GetValue<bool>())
                            Spells[R].Cast();
                        else
                            Spells[R].CastIfWillHit(t, Config.Item("CUSERHIT").GetValue<Slider>().Value);
                    }
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                Obj_AI_Hero target = null;
               
                //toggle grab
                if (TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <= Spells[Q].Range - 30)
                    target = TargetSelector.SelectedTarget;
                else
                    target = TargetSelector.GetTarget(Spells[Q].Range - 30, TargetSelector.DamageType.Magical);

                if (target != null)
                    CastSkillshot(target, Spells[Q], ShineCommon.Utility.HitchanceArray[Config.Item("HQHITCHANCE").GetValue<StringList>().SelectedIndex]);
            }
        }

        public void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("LMANA").GetValue<Slider>().Value)
                return;

            if (Spells[R].IsReady() && Config.Item("LUSER").GetValue<bool>())
            {
                var t = (from minion in MinionManager.GetMinions(Spells[R].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth) where minion.IsValidTarget(Spells[R].Range) && Spells[R].GetDamage(minion) >= minion.Health orderby minion.Health ascending select minion);
                if (t != null && t.Count() >= 3)
                    Spells[R].Cast(t.FirstOrDefault().ServerPosition);
            }
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe && HeroManager.Enemies.Exists(p => p.NetworkId == args.Target.NetworkId) && ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && combo.Item("CUSEE").GetValue<bool>()) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && harass.Item("HUSEE").GetValue<bool>())))
            {
                if (Spells[E].IsReady())
                    Spells[E].Cast();

                ObjectManager.Player.IssueOrder(GameObjectOrder.AutoAttack, args.Target);
            }
        }

        public override void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (sender.IsEnemy && sender.IsChampion() && ShineCommon.Utility.IsImmobileTarget(sender as Obj_AI_Hero) && Config.Item("MAUTOQHP").GetValue<Slider>().Value >= (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100)
            {
                if (Spells[Q].IsReady() && sender.IsValidTarget(Spells[Q].Range) && Config.Item("MAUTOQIMMO").GetValue<bool>() && !Config.Item("noautograb" + (sender as Obj_AI_Hero).ChampionName).GetValue<bool>())
                    Spells[Q].Cast(sender.ServerPosition);
            }
        }

        public override void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("MINTEN").GetValue<bool>())
            {
                if (Config.Item("MINTQ").GetValue<bool>() && Spells[Q].IsReady() && Config.Item("MAUTOQHP").GetValue<Slider>().Value >= (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100)
                    CastSkillshot(sender, Spells[Q], HitChance.Low);

                if (Config.Item("MINTE").GetValue<bool>() && Spells[E].IsReady() && sender.Distance(ObjectManager.Player.ServerPosition) <= Spells[E].RangeSqr)
                {
                    Spells[E].Cast();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
                }

                if (Config.Item("MINTR").GetValue<bool>() && Spells[R].IsReady() && sender.IsValidTarget(Spells[R].Range))
                    Spells[R].Cast();
            }
        }
    }
}



