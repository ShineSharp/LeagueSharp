/*
 Copyright 2015 - 2015 SPrediction
 ConfigMenu.cs is part of SPrediction
 
 SPrediction is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 SPrediction is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with SPrediction. If not, see <http://www.gnu.org/licenses/>.
*/

using LeagueSharp;
using LeagueSharp.Common;

namespace SPrediction
{
    /// <summary>
    /// SPrediction Config Menu class
    /// </summary>
    public static class ConfigMenu
    {
        #region Private Properties

        private static Menu s_Menu = null;

        #endregion

        #region Initalizer Methods

        /// <summary>
        /// Creates the sprediciton menu and attach to the given menu
        /// </summary>
        /// <param name="menuToAttach">The menu to attach.</param>
        public static void Initialize(Menu menuToAttach, string prefMenuName)
        {
            Initialize(prefMenuName);
            if (menuToAttach == null)
                return;
            menuToAttach.AddSubMenu(s_Menu);
        }

        /// <summary>
        /// Creates the sprediciton menu
        /// </summary>
        public static Menu Initialize(string prefMenuName = "SPRED")
        {
            s_Menu = new Menu("SPrediction", prefMenuName);
            s_Menu.AddItem(new MenuItem("PREDICTONLIST", "Prediction Method").SetValue(new StringList(new[] { "SPrediction", "Common Prediction" }, 0)));
            s_Menu.AddItem(new MenuItem("SPREDWINDUP", "Check for target AA Windup").SetValue(false));
            s_Menu.AddItem(new MenuItem("SPREDMAXRANGEIGNORE", "Max Range Dodge Ignore (%)").SetValue(new Slider(50, 0, 100)));
            s_Menu.AddItem(new MenuItem("SPREDREACTIONDELAY", "Ignore Rection Delay").SetValue<Slider>(new Slider(0, 0, 200)));
            s_Menu.AddItem(new MenuItem("SPREDDELAY", "Spell Delay").SetValue(new Slider(0, 0, 200)));
            s_Menu.AddItem(new MenuItem("SPREDHC", "Count HitChance").SetValue(new KeyBind(32, KeyBindType.Press)));
            s_Menu.AddItem(new MenuItem("SPREDDRAWINGX", "Drawing Pos X").SetValue(new Slider(Drawing.Width - 200, 0, Drawing.Width)));
            s_Menu.AddItem(new MenuItem("SPREDDRAWINGY", "Drawing Pos Y").SetValue(new Slider(0, 0, Drawing.Height)));
            s_Menu.AddItem(new MenuItem("SPREDDRAWINGS", "Enable Drawings").SetValue(false));

            return s_Menu;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets selected prediction for spell extensions
        /// </summary>
        public static StringList SelectedPrediction
        {
            get { return s_Menu.Item("PREDICTONLIST").GetValue<StringList>(); }
        }

        /// <summary>
        /// Gets or sets Check AA WindUp value
        /// </summary>
        public static bool CheckAAWindUp
        {
            get { return s_Menu.Item("SPREDWINDUP").GetValue<bool>(); }
            set { s_Menu.Item("SPREDWINDUP").SetValue(value); }
        }

        /// <summary>
        /// Gets or sets max range ignore value
        /// </summary>
        public static int MaxRangeIgnore
        {
            get { return s_Menu.Item("SPREDMAXRANGEIGNORE").GetValue<Slider>().Value; }
            set { s_Menu.Item("SPREDMAXRANGEIGNORE").SetValue(new Slider(value, 0, 100)); }
        }

        /// <summary>
        /// Gets or sets ignore reaction delay value
        /// </summary>
        public static int IgnoreReactionDelay
        {
            get { return s_Menu.Item("SPREDREACTIONDELAY").GetValue<Slider>().Value; }
            set { s_Menu.Item("SPREDREACTIONDELAY").SetValue(new Slider(value, 0, 200)); }
        }

        /// <summary>
        /// Gets or sets spell delay value
        /// </summary>
        public static int SpellDelay
        {
            get { return s_Menu.Item("SPREDDELAY").GetValue<Slider>().Value; }
            set { s_Menu.Item("SPREDDELAY").SetValue(new Slider(value, 0, 200)); }
        }

        /// <summary>
        /// Gets count hitchance key is enabled
        /// </summary>
        public static bool CountHitChance
        {
            get { return s_Menu.Item("SPREDHC").GetValue<KeyBind>().Active; }
        }

        /// <summary>
        /// Gets or sets drawing x pos for hitchance drawings
        /// </summary>
        public static int HitChanceDrawingX
        {
            get { return s_Menu.Item("SPREDDRAWINGX").GetValue<Slider>().Value; }
            set { s_Menu.Item("SPREDDRAWINGX").SetValue(new Slider(value, 0, Drawing.Width)); }
        }

        /// <summary>
        /// Gets or sets drawing y pos for hitchance drawings
        /// </summary>
        public static int HitChanceDrawingY
        {
            get { return s_Menu.Item("SPREDDRAWINGY").GetValue<Slider>().Value; }
            set { s_Menu.Item("SPREDDRAWINGY").SetValue(new Slider(value, 0, Drawing.Height)); }
        }

        /// <summary>
        /// Gets or sets drawings are enabled
        /// </summary>
        public static bool EnableDrawings
        {
            get { return s_Menu.Item("SPREDDRAWINGS").GetValue<bool>(); }
            set { s_Menu.Item("SPREDDRAWINGS").SetValue(value); }
        }

        #endregion
    }
}
