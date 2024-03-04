using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.ModAPI;
using Sandbox.Game;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace Example {
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Mod : MySessionComponentBase {

        public MyLanguagesEnum? Language { get; private set; }

        public override void LoadData() {
            LoadLocalization("Localization");
            LoadLocalization("Localization/Other");

            MyAPIGateway.Gui.GuiControlRemoved += OnGuiControlRemoved;
        }

        protected override void UnloadData() {
            MyAPIGateway.Gui.GuiControlRemoved -= OnGuiControlRemoved;
        }

        private void LoadLocalization(string folder) {
            var path = Path.Combine(ModContext.ModPathData, folder);
            var supportedLanguages = new HashSet<MyLanguagesEnum>();
            MyTexts.LoadSupportedLanguages(path, supportedLanguages);

            var currentLanguage = supportedLanguages.Contains(MyAPIGateway.Session.Config.Language) ? MyAPIGateway.Session.Config.Language : MyLanguagesEnum.English;
            if (Language != null && Language == currentLanguage) {
                return;
            }

            Language = currentLanguage;
            var languageDescription = MyTexts.Languages.Where(x => x.Key == currentLanguage).Select(x => x.Value).FirstOrDefault();
            if (languageDescription != null) {
                var cultureName = string.IsNullOrWhiteSpace(languageDescription.CultureName) ? null : languageDescription.CultureName;
                var subcultureName = string.IsNullOrWhiteSpace(languageDescription.SubcultureName) ? null : languageDescription.SubcultureName;
                MyTexts.LoadTexts(path, cultureName, subcultureName);
            }
        }

        private void OnGuiControlRemoved(object obj) {
            if (obj.ToString().EndsWith("ScreenOptionsSpace")) {
                LoadLocalization("Localization");
				LoadLocalization("Localization/Other");
            }
        }
    }
}