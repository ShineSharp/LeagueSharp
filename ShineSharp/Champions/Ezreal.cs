using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;
using SPrediction;
using SharpDX;

namespace ShineSharp.Champions
{
    public class Ezreal : BaseChamp
    {
        public Ezreal()
            : base ("Ezreal")
        {
            
        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(true));
            //
            ult = new Menu("R Settings (BETA)", "rsetting");
            ult.AddItem(new MenuItem("CUSER", "Use R").SetValue(true));
            ult.AddItem(new MenuItem("CUSERHIT", "If Can Hit Enemies Count >=").SetValue(new Slider(3, 2, 5)));
            ult.AddItem(new MenuItem("CUSERHP", "If Target HP Percent <=").SetValue(new Slider(30, 1, 100)));
            ult.AddItem(new MenuItem("CUSEHPHIT", "Use Both ^ (x enemies with less than y% hp)").SetValue(true));
            ult.AddItem(new MenuItem("CUSERRANGE", "Don't Use R If Enemy Count >= 2 In Range: ").SetValue(new Slider(600, 1, 2500)));
            ult.AddItem(new MenuItem("CRHITCHANCE", "Hit Chance").SetValue<StringList>(new StringList(ShineCommon.Utility.HitchanceNameArray, 2)));
            //
            combo.AddSubMenu(ult);

            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 0, 100)));

            laneclear = new Menu("LaneClear", "LaneClear");
            laneclear.AddItem(new MenuItem("LUSEQ", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("LMANA", "Min. Mana Percent").SetValue(new Slider(50, 0, 100)));

            misc = new Menu("Misc", "Misc");
            misc.AddItem(new MenuItem("MLASTQ", "Last Hit Q").SetValue(true));
            misc.AddItem(new MenuItem("MAUTOQ", "Auto Harass Q").SetValue(true));
            misc.AddItem(new MenuItem("MUSER", "Use R If Killable").SetValue(false));

            m_evader = new Evader(out evade, EvadeMethods.Blink);

            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(evade);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            base.OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Combo] += Combo;
            base.OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.Mixed] += Harass;
            base.OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
            base.OrbwalkingFunctions[(int)Orbwalking.OrbwalkingMode.LastHit] += LastHit;
            base.BeforeOrbWalking += BeforeOrbwalk;
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 1190);
            Spells[Q].SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            Spells[W] = new Spell(SpellSlot.W, 800f);
            Spells[W].SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            Spells[E] = new Spell(SpellSlot.E, 475f);

            Spells[R] = new Spell(SpellSlot.R, 3000f);
            Spells[R].SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            m_evader.SetEvadeSpell(Spells[E]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override double CalculateDamageR(Obj_AI_Hero target)
        {
            double dmg = 0.0;
            if (Config.Item("CUSER").GetValue<bool>() && Spells[R].IsReady())
            {
                dmg = ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
                int collCount = Spells[R].GetCollision(ObjectManager.Player.ServerPosition.To2D(), new List<Vector2>() { target.ServerPosition.To2D() }).Count();
                int percent = 10 - collCount > 7 ? 7 : collCount;
                dmg = dmg / 10 * percent;
            }
            return dmg;
        }

        public override void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 300)
            {
                Spells[E].Cast(gapcloser.End.Extend(ObjectManager.Player.ServerPosition, ObjectManager.Player.Distance(gapcloser.End) + Spells[E].Range));
            }
        }

        public void BeforeOrbwalk()
        {
            #region Auto Harass
            if (Spells[Q].IsReady() && Config.Item("MAUTOQ").GetValue<bool>() && !ObjectManager.Player.UnderTurret() && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                var t = (from enemy in HeroManager.Enemies where enemy.IsValidTarget(Spells[Q].Range) orderby TargetSelector.GetPriority(enemy) descending select enemy).FirstOrDefault();
                if (t != null)
                    CastSkillshot(t, Spells[Q]);
            }
            #endregion

            #region Auto Ult
            if (Config.Item("MUSER").GetValue<bool>() && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo && ObjectManager.Player.CountEnemiesInRange(600) == 0)
            {
                var t = (from enemy in HeroManager.Enemies where enemy.IsValidTarget(2500f) && CalculateDamageR(enemy) > enemy.Health orderby enemy.ServerPosition.Distance(ObjectManager.Player.ServerPosition) descending select enemy).FirstOrDefault();
                if (t != null)
                    CastSkillshot(t, Spells[R], HitChance.High);
            }
            #endregion
        }
        
        public void Combo()
        {
            if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Physical);
                if (t != null)
                    CastSkillshot(t, Spells[Q]);
            }

            if (Spells[W].IsReady() && Config.Item("CUSEW").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(Spells[W].Range, TargetSelector.DamageType.Magical);
                if (t != null)
                    CastSkillshot(t, Spells[W]);
            }

            if (Spells[R].IsReady() && ult.Item("CUSER").GetValue<bool>() && ObjectManager.Player.CountEnemiesInRange(ult.Item("CUSERRANGE").GetValue<Slider>().Value) < 2)
            {
                var t = HeroManager.Enemies.Where(p => p.IsValidTarget(2500f)).OrderBy(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                if (t != null)
                {
                    HitChance ulthc = ShineCommon.Utility.HitchanceArray[Config.Item("CRHITCHANCE").GetValue<StringList>().SelectedIndex];
                    if (Config.Item("CUSEHPHIT").GetValue<bool>())
                    {
                        if (Spells[R].SPredictionCast(t, ulthc, 0, (byte)Config.Item("CUSERHIT").GetValue<Slider>().Value, null, Config.Item("CUSERHP").GetValue<Slider>().Value))
                            return;
                    }

                    if (t.Health / t.MaxHealth * 100 <= Config.Item("CUSERHP").GetValue<Slider>().Value)
                        if (Spells[R].SPredictionCast(t, ulthc))
                            return;

                    Spells[R].SPredictionCast(t, ulthc, 0, (byte)Config.Item("CUSERHIT").GetValue<Slider>().Value, null, 100);
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Spells[Q].Range, TargetSelector.DamageType.Physical);
                if (target != null)
                    CastSkillshot(target, Spells[Q]);
            }

            if (Spells[W].IsReady() && Config.Item("HUSEW").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Spells[W].Range, TargetSelector.DamageType.Magical);
                if (target != null)
                    CastSkillshot(target, Spells[W]);
            }

        }

        public void LaneClear()
        {
            if (!Spells[Q].IsReady() || ObjectManager.Player.ManaPercent < Config.Item("LMANA").GetValue<Slider>().Value || !Config.Item("LUSEQ").GetValue<bool>())
                return;

            var t = (from minion in MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth) where minion.IsValidTarget(Spells[Q].Range) && Spells[Q].GetDamage(minion) >= minion.Health orderby minion.Distance(ObjectManager.Player.Position) descending select minion).FirstOrDefault();
            if (t != null)
                Spells[Q].CastIfHitchanceEquals(t, HitChance.High);
        }

        public void LastHit()
        {
            if (Spells[Q].IsReady() && Config.Item("MLASTQ").GetValue<bool>())
            {
                var t = (from minion in MinionManager.GetMinions(Spells[Q].Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth) where minion.IsValidTarget(Spells[Q].Range) && Spells[Q].GetDamage(minion) >= minion.Health && (!minion.UnderTurret() && minion.Distance(ObjectManager.Player.Position) > ObjectManager.Player.AttackRange) orderby minion.Health ascending select minion).FirstOrDefault();
                if (t != null)
                    Spells[Q].CastIfHitchanceEquals(t, HitChance.High);
            }
        }
    }
}
