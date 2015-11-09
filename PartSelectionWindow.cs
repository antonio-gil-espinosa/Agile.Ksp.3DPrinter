using Agile.Ksp.Collections;
using Agile.Ksp.Extensions;
using Agile.Ksp.UI;
using System;

namespace Agile.Ksp.Printer3D
{
    public partial class Printer
    {
        public class PartSelectionWindow : KspUIWindow
        {
            private readonly Printer _printer;

            public PartSelectionWindow(Printer printer, ListMapping<PartCategories, AvailablePart> availableParts) : base(printer.part)
            {
                _printer = printer;
                Title = "Choose part to print";

                AddControl(new KspUIVerticalLayout().Tap(xx =>
                {
                    xx.AddControl(new KspUIHorizontalLayout().Tap(x =>
                    {
                        x.AddControl(new KspUIScroll().Tap(y =>
                        {
                            y.MinWidth = 300;
                            y.MinHeight = 600;
                            y.AddControl(KspUIRepeat.Create(() => availableParts.Keys, z => new KspUIButton(z.ToString(), () =>
                            {
                                _selectedCategory = z;
                            })));
                        }));

                        x.AddControl(new KspUIScroll().Tap(y =>
                        {
                            y.MinWidth = 600;
                            y.MinHeight = 600;

                            y.AddControl(KspUIRepeat.Create(() => availableParts[_selectedCategory], z =>
                            {
                                float oreUnitsRequired = _printer.GetRequiredOreUnits(z);
                                return new KspUIButton(z.title + Environment.NewLine + oreUnitsRequired + " ore units required", () =>
                                {
                                    Close();
                                    _printer.BuildingPart = z;
                                    _printer.MassPrinted = 0;
                                    _printer.StartWork();
                                });
                            }));
                        }));
                    }));

                    xx.AddControl(new KspUIButton("Close", Close));
                }));
            }

            public PartCategories _selectedCategory { get; set; }
        }
    }
}