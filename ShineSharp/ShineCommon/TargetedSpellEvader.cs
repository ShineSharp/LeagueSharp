using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace ShineCommon
{
    public class TargetedSpellEvader
    {
        private Menu m_Menu;
        private Action<DetectedTargetedSpellArgs> m_fnEvade;
        
        /// <summary>
        /// Initializes TargetedSpellEvader class
        /// </summary>
        /// <param name="fn">The evade function.</param>
        /// <param name="menuToAttach">The menu to attach.</param>
        public TargetedSpellEvader(Action<DetectedTargetedSpellArgs> fn, Menu menuToAttach)
        {
            TargetedSpellDetector.OnDetected += TargetedSpellDetector_OnDetected;
            RegisterEvadeFunction(fn);

            m_Menu = new Menu("Targeted Spell Evader", "SCommon.TargetedSpellEvader.Root");
            foreach (var enemy in HeroManager.Enemies)
            {
                foreach (var spell in SpellDatabase.TargetedSpells.Where(p => p.ChampionName == enemy.ChampionName))
                    m_Menu.AddItem(new MenuItem(String.Format("SCommon.TargetedSpellEvader.Spell.{0}", spell.SpellName), String.Format("{0} ({1})", spell.ChampionName, spell.Slot) + (spell.IsDangerous ? " (Dangerous)" : "")).SetValue(true));
            }
            m_Menu.AddItem(new MenuItem("SCommon.TargetedSpellEvader.DisableInCombo", "Disable In Combo Mode").SetValue(false));
            m_Menu.AddItem(new MenuItem("SCommon.TargetedSpellEvader.OnlyDangerous", "Only Dangerous").SetValue(false));
            m_Menu.AddItem(new MenuItem("SCommon.TargetedSpellEvader.Enabled", "Enabled").SetValue(true));
            menuToAttach.AddSubMenu(m_Menu);
        }

        /// <summary>
        /// Registers evade function
        /// </summary>
        /// <param name="fn">The function.</param>
        public void RegisterEvadeFunction(Action<DetectedTargetedSpellArgs> fn)
        {
            m_fnEvade = fn;
        }

        /// <summary>
        /// Unregisters the evade function
        /// </summary>
        public void UnregisterEvadeFunction()
        {
            m_fnEvade = null;
        }

        /// <summary>
        /// Event callback which fired when targeted spell is detected
        /// </summary>
        /// <param name="args">The args.</param>
        private void TargetedSpellDetector_OnDetected(DetectedTargetedSpellArgs args)
        {
            if (IsEnabled)
            {
                if (OnlyDangerous && !args.SpellData.IsDangerous)
                    return;

                if (m_fnEvade != null && m_Menu.Item("SCommon.TargetedSpellEvader.Spell." + args.SpellData.SpellName).GetValue<bool>())
                    m_fnEvade(args);
            }
        }

        /// <summary>
        /// Gets TargetedSpellEvader is enabled
        /// </summary>
        public bool IsEnabled
        {
            get { return m_Menu.Item("SCommon.TargetedSpellEvader.Enabled").GetValue<bool>(); }
        }

        /// <summary>
        /// Gets TargetedSpellEvader is disabled while combo mode
        /// </summary>
        public bool DisableInComboMode
        {
            get { return m_Menu.Item("SCommon.TargetedSpellEvader.DisableInCombo").GetValue<bool>(); }
        }

        /// <summary>
        /// Gets TargetedSpellEvader is enabled for only dangerous spells
        /// </summary>
        public bool OnlyDangerous
        {
            get { return m_Menu.Item("SCommon.TargetedSpellEvader.OnlyDangerous").GetValue<bool>(); }
        }

    }
}
