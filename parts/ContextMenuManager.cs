// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Windows.Forms;

/// <summary>
///       Namespace Name - CSgen.
/// </summary>
namespace PowerSimBuilder
{
    /// <summary>
    /// MenuItemDefinition
    /// </summary>
    public class MenuItemDefinition
    {
        /// <summary>
        ///      Property Name - Text.
        ///  </summary>
        public string Text { get; set; }

        /// <summary>
        ///      Property Name - Handler.
        ///  </summary>
        public EventHandler Handler { get; set; }

        /// <summary>
        ///  Function Name :  MenuItemDefinition.
        /// </summary>
        /// <param name="text">This text's Datatype is : string.</param>
        /// <param name="handler">This handler's Datatype is :
        ///     System.EventHandler.</param>
        /// <returns>void.</returns>
        public MenuItemDefinition(string text, EventHandler handler)
        {
            Text = text;
            Handler = handler;
        }
    }

    /// <summary>
    /// ContextMenuManager
    /// </summary>
    public class ContextMenuManager
    {
        private Dictionary<string, ContextMenuStrip> _menus;

        /// <summary>
        ///      Property Name - Menus.
        /// </summary>
        public Dictionary<string, ContextMenuStrip> Menus
        {
            get => _menus;
            set => _menus = value;
        }

        /// <summary>
        ///  Function Name :  ContextMenuManager.
        /// </summary>
        /// <returns>void.</returns>
        public ContextMenuManager()
        {
            _menus = new Dictionary<string, ContextMenuStrip>();
        }

        /// <summary>
        ///  Function Name :  CreateMenu.
        /// </summary>
        /// <param name="menuName">This menuName's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void CreateMenu(string menuName)
        {
            if (!_menus.ContainsKey(menuName))
            {
                _menus[menuName] = new ContextMenuStrip();
            }
        }

        /// <summary>
        ///  Function Name :  AddMenuItem.
        /// </summary>
        /// <param name="menuName">This menuName's Datatype is : string.</param>
        /// <param name="text">This text's Datatype is : string.</param>
        /// <param name="handler">This handler's Datatype is :
        ///     System.EventHandler.</param>
        /// <returns>void.</returns>
        public void AddMenuItem(string menuName, string text, EventHandler handler)
        {
            if (!_menus.ContainsKey(menuName))
            {
                CreateMenu(menuName);
            }

            ToolStripMenuItem menuItem = new ToolStripMenuItem(text);
            menuItem.Click += handler;
            _menus[menuName].Items.Add(menuItem);
        }

        /// <summary>
        ///  Function Name :  AddMenuItems.
        /// </summary>
        /// <param name="menuName">This menuName's Datatype is : string.</param>
        /// <param name="items">This items's Datatype is :
        ///     System.Collections.Generic.List CSgen.MenuItemDefinition&gt;.
        ///     </param>
        /// <returns>void.</returns>
        public void AddMenuItems(string menuName, List<MenuItemDefinition> items)
        {
            if (!_menus.ContainsKey(menuName))
            {
                CreateMenu(menuName);
            }

            foreach (MenuItemDefinition item in items)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem(item.Text);
                menuItem.Click += item.Handler;
                _menus[menuName].Items.Add(menuItem);
            }
        }

        /// <summary>
        ///  Function Name :  RemoveMenuItem.
        /// </summary>
        /// <param name="menuName">This menuName's Datatype is : string.</param>
        /// <param name="text">This text's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void RemoveMenuItem(string menuName, string text)
        {
            if (_menus.ContainsKey(menuName))
            {
                foreach (ToolStripMenuItem item in _menus[menuName].Items)
                {
                    if (item.Text == text)
                    {
                        _menus[menuName].Items.Remove(item);
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///  Function Name :  GetMenu.
        /// </summary>
        /// <param name="menuName">This menuName's Datatype is : string.</param>
        /// <returns>System.Windows.Forms.ContextMenuStrip.</returns>
        public ContextMenuStrip GetMenu(string menuName)
        {
            if (_menus.ContainsKey(menuName))
            {
                return _menus[menuName];
            }
            return null;
        }

        /// <summary>
        ///  Function Name :  ClearMenu.
        /// </summary>
        /// <param name="menuName">This menuName's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void ClearMenu(string menuName)
        {
            if (_menus.ContainsKey(menuName))
            {
                _menus[menuName].Items.Clear();
            }
        }
    }
}