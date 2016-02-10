using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace SPrediction
{
    public static class ConfigMenu
    {
        #region Private Properties

        private static Menu s_Menu;

        #endregion

        #region Initalizer Method

        /// <summary>
        /// Creates the sprediciton menu
        /// </summary>
        /// <param name="menuToAttach">The menu to attach.</param>
        public static void Initialize(Menu menuToAttach)
        {
            s_Menu = new Menu("SPrediction", "SPRED");
            s_Menu.AddItem(new MenuItem("PREDICTONLIST", "Prediction Method").SetValue(new StringList(new[] { "SPrediction", "Common Predicion" }, 0)));
            s_Menu.AddItem(new MenuItem("SPREDWINDUP", "Check for target AA Windup").SetValue(false));
            s_Menu.AddItem(new MenuItem("SPREDMAXRANGEIGNORE", "Max Range Dodge Ignore (%)").SetValue(new Slider(50, 0, 100)));
            s_Menu.AddItem(new MenuItem("SPREDREACTIONDELAY", "Ignore Rection Delay").SetValue<Slider>(new Slider(0, 0, 200)));
            s_Menu.AddItem(new MenuItem("SPREDDELAY", "Spell Delay").SetValue(new Slider(0, 0, 200)));
            s_Menu.AddItem(new MenuItem("SPREDHC", "Count HitChance").SetValue(new KeyBind(32, KeyBindType.Press)));
            s_Menu.AddItem(new MenuItem("SPREDDRAWINGX", "Drawing Pos X").SetValue(new Slider(Drawing.Width - 200, 0, Drawing.Width)));
            s_Menu.AddItem(new MenuItem("SPREDDRAWINGY", "Drawing Pos Y").SetValue(new Slider(0, 0, Drawing.Height)));
            s_Menu.AddItem(new MenuItem("SPREDDRAWINGS", "Enable Drawings").SetValue(false));
            menuToAttach.AddSubMenu(s_Menu);
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
