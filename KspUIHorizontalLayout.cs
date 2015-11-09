using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Agile.Collections.Generic;
using ClassLibrary1;
using UnityEngine;

namespace Agile.KSP.UI
{
    public class KspUIHorizontalLayoutX : KspUIContainer
    {
        private readonly ListMapping<PartCategories, AvailablePart> _availableParts;
        private readonly Printer _printer;
        private Vector2 categoriesScrollPosition;
        private PartCategories _selectedCategory;
        private Vector2 partScrollPosition;

        public KspUIHorizontalLayoutX(ListMapping<PartCategories, AvailablePart> availableParts, Printer printer)
        {
            _availableParts = availableParts;
            _printer = printer;

        }

        public override void Draw()
        {
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);

            GUILayout.BeginHorizontal();
            {
                categoriesScrollPosition = GUILayout.BeginScrollView(categoriesScrollPosition, GUILayout.MinWidth(300),
                                                                     GUILayout.MinHeight(600));
                {
                    foreach (PartCategories categories in _availableParts.Keys)
                    {

                        if (GUILayout.Button(categories.ToString())) //GUILayout.Button is "true" when clicked
                        {
                            _selectedCategory = categories;

                        }
                    }
                }
                GUILayout.EndScrollView();

                partScrollPosition = GUILayout.BeginScrollView(partScrollPosition, GUILayout.MinWidth(600), GUILayout.MinHeight(600));
                {
                    if (_selectedCategory != PartCategories.none)
                    {
                        foreach (AvailablePart availablePart in _availableParts[_selectedCategory])
                        {
                            string title = availablePart.title;
                            var oreUnitsRequired = _printer.GetRequiredOreUnits(availablePart);
                            if (GUILayout.Button(title + " (" + oreUnitsRequired + " ore units required)", mySty,
                                                 GUILayout.ExpandWidth(true))) //GUILayout.Button is "true" when clicked
                            {


                                ((KspUIWindow)Parent)?.Close();
                                _printer.BeginBuild(availablePart);
                  





                            }
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
            //DragWindow makes the window draggable. The Rect specifies which part of the window it can by dragged by, and is 
            //clipped to the actual boundary of the window. You can also pass no argument at all and then the window can by
            //dragged by any part of it. Make sure the DragWindow command is AFTER all your other GUI input stuff, or else
            //it may "cover up" your controls and make them stop responding to the mouse.
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
