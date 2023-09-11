using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using Exception = System.Exception;

namespace MIG.SpecCores
{
    public partial class SpecBlock
    {
        /// <summary>
        /// Отвечает за выбранные элементы в списке.
        /// </summary>
        private List<int> SelectedUpgrades = new List<int>();

        /// <summary>
        /// Текущие тестовые настройки
        /// </summary>
        private List<int> AddedUpgrades = new List<int>();
        
        private static void UpgradesListBoxItemSelected(IMyTerminalBlock a, List<MyTerminalControlListBoxItem> b)
        {
            var specBlock = (SpecBlock)a.GetSpecBlock();
            specBlock.SelectedUpgrades.Clear();
            foreach (var item in b)
            {
                specBlock.SelectedUpgrades.Add((int)item.UserData);
            }

            UpdateControls(a.GetUpgradableSpecBlock());
        }

       public static void InitControls<Z>() where Z : IMyCubeBlock
        {
            try
            {
                var upgradesListBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, Z>("AvailableUpgrades");
                upgradesListBox.Visible = (x) => x.GetUpgradableSpecBlock()?.CanPickUpgrades() ?? false;
                upgradesListBox.VisibleRowsCount = 12;
                upgradesListBox.Multiselect = false;
                upgradesListBox.ItemSelected = UpgradesListBoxItemSelected;
                upgradesListBox.ListContent = UpgradeListContentGui;
                MyAPIGateway.TerminalControls.AddControl<Z>(upgradesListBox);
                
                MyAPIGateway.TerminalControls.CreateButton<SpecBlock, Z>("AddUpgrade",  T.Translation(T.Button_AddUpgrade),  T.Translation(T.Button_AddUpgrade_Tooltip), 
                    AddUpgradeGui, 
                    Sugar2.GetUpgradableSpecBlock, 
                    enabled: AddUpgrade_Enabled, 
                    visible: UpgradeGui_IsVisible);

                MyAPIGateway.TerminalControls.CreateButton<SpecBlock, Z>("RemoveUpgrade",  T.Translation(T.Button_RemoveUpgrade),  T.Translation(T.Button_RemoveUpgrade_Tooltip), 
                    RemoveUpgradeGui, 
                    Sugar2.GetUpgradableSpecBlock, 
                    visible: UpgradeGui_IsVisible);

                MyAPIGateway.TerminalControls.CreateButton<SpecBlock, Z>("ApplyUpgrades", T.Translation(T.Button_ApplyUpgrade), T.Translation(T.Button_ApplyUpgrade_Tooltip), 
                    ApplyUpgradesGui, 
                    Sugar2.GetUpgradableSpecBlock, 
                    enabled: ApplyUpgrades_Enabled, 
                    visible: UpgradeGui_IsVisible).DoubleClick();
                
                //MyAPIGateway.TerminalControls.CreateButton<SpecBlock, Z>("ApplyRandomUpgrades", 
                //    T.Translation(T.Button_ApplyRandomUpgrade), 
                //    T.Translation(T.Button_ApplyRandomUpgrade_Tooltip), 
                //    ApplyRandomUpgradesGui, 
                //    Sugar2.GetUpgradableSpecBlock,
                //    enabled: (x) => AddedTest.Count == 0 && x.CanPickUpgrades(),
                //    visible: (x) => x.info.ExtraRandomUpgrades > 0 && x.CanPickUpgrades()).DoubleClick();
                
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }

        
        
        
        
        private static void AddUpgradeGui(SpecBlock x)
        {
            //TODO
            if (x.SelectedUpgrades.Count != 1) { return; }

            if (new UpgradeHelper(x, x.AddedUpgrades, x.SelectedUpgrades[0]).CanBeApplied())
            {
                x.AddedUpgrades.Add(x.SelectedUpgrades[0]);
            }
            
            x.SelectedUpgrades.Clear();
            x.block.RefreshCustomInfo();
            UpdateControls(x);
        }
        
        private static void RemoveUpgradeGui(SpecBlock x)
        {
            foreach (var a in x.SelectedUpgrades)
            {
                for (int i = x.AddedUpgrades.Count - 1; i >= 0; i--)
                {
                    if (x.AddedUpgrades[i] == a)
                    {
                        x.AddedUpgrades.RemoveAt(i);
                        break;
                    }
                }
            }

            var newPossible = new UpgradeHelper(x, x.AddedUpgrades).GetMaxPossibleUpgradeList();
            x.AddedUpgrades.Clear();
            x.AddedUpgrades.AddRange(newPossible);
            
            x.SelectedUpgrades.Clear();
            x.block.RefreshCustomInfo();
            UpdateControls(x);
        }
        
        private static void ApplyUpgradesGui(SpecBlock x)
        {
            var settings = new UpgradableSpecBlockSettings();
            settings.Upgrades.AddRange(x.AddedUpgrades);
            x.AddedUpgrades.Clear();
            x.SelectedUpgrades.Clear();
            Sync.SendMessageToServer(x.block.EntityId, settings, type: APPLY_SELECTED);
            FrameExecutor.addDelayedLogic(30, (xx) => UpdateControls(x)); //1/2 sec
            if (!MyAPIGateway.Session.IsServer)
            {
                x.Settings = settings; //Remove gui lag
            }
        }
        
        private static void ApplyRandomUpgradesGui(SpecBlock x)
        {
            x.AddedUpgrades.Clear();
            x.SelectedUpgrades.Clear();
                
            var settings = new UpgradableSpecBlockSettings();
            settings.Upgrades = new List<int>();
            Sync.SendMessageToServer(x.block.EntityId, x.Settings, type: APPLY_RANDOM); //1/2 sec
            FrameExecutor.addDelayedLogic(30, (xx) => UpdateControls(x));
            if (!MyAPIGateway.Session.IsServer) {
                x.Settings = settings; //Remove gui lag
            }
        }

        public static Dictionary<int, float> GetTotalUpgradeCost(Dictionary<int, int> upgrades)
        {
            var sum = new Dictionary<int, float>();
            foreach (var u in upgrades)
            {
                var upgrade = OriginalSpecCoreSession.Instance.Upgrades.GetOr(u.Key, null);
                sum.Sum(upgrade.Levels[u.Value-1].TotalUpgradeCost);
            }

            return sum;
        }

        private static void UpgradeListContentGui(IMyTerminalBlock a, List<MyTerminalControlListBoxItem> all, List<MyTerminalControlListBoxItem> selected)
        {
            try
            {
                UpgradeListContentGui2(a, all, selected);
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }
        private static void UpgradeListContentGui2(IMyTerminalBlock a, List<MyTerminalControlListBoxItem> all, List<MyTerminalControlListBoxItem> selected)
        {
            var specBlock = a.GetUpgradableSpecBlock();
            all.Clear();
            selected.Clear();
            
            Limits originalStatic, originalDynamic;

            var upgrades = specBlock.AddedUpgrades.SumDuplicates();
            specBlock.GetUpgradedValues(upgrades, out originalStatic, out originalDynamic, false);
            var tooltipSb = new StringBuilder();

            
            
            
            var maxPoints = specBlock.GetLimits();
            OriginalSpecCoreSession.RemoveNonUpgradePoints(maxPoints);
            

            var possibleUpgrades = OriginalSpecCoreSession.Instance.Upgrades;
            foreach (var u in specBlock.info.PossibleUpgrades)
            {
                if (!possibleUpgrades.ContainsKey(u))
                {
                    Log.ChatError($"Wasn't able to find upgrade with id {u}");
                    continue;
                }
                
                var up = possibleUpgrades[u];
                if (!new UpgradeHelper(specBlock, specBlock.AddedUpgrades, u).CanBeApplied())
                {
                    if (specBlock.AddedUpgrades.Contains(u))
                    {
                        var item = GetRowItem(specBlock, up, specBlock.AddedUpgrades, upgrades, maxPoints, u, "");
                        all.Add(item);
                        if (specBlock.SelectedUpgrades.Contains(u))
                        {
                            selected.Add(item);
                        }
                    }
                }
                else
                {
                    var tooltip = GetTooltip(specBlock, u, upgrades, originalStatic, originalDynamic, tooltipSb);
                    var item = GetRowItem(specBlock, up, specBlock.AddedUpgrades, upgrades, maxPoints, u, tooltip);
                    all.Add(item);

                    if (specBlock.SelectedUpgrades.Contains(u))
                    {
                        selected.Add(item);
                    }
                }
            }
        }

        private static MyTerminalControlListBoxItem GetRowItem(SpecBlock specBlock, Upgrade up, List<int> upgradeOrder, Dictionary<int, int> upgrades, Limits maxPoints, int u, string tooltip)
        {
            var lvl = upgrades.GetOr(up.NId, 0) - 1;

            var upgradeName = T.Translation(up.DisplayName);

            String upText = "";
            int maxLevel = up.Levels.Length;
            int nextLevel = lvl + 1;
            if (nextLevel >= up.Levels.Length)
            {
                upText = T.Translation(T.UpgradesList_UpgradeTooltip_MaxUpgrades);
            }
            else
            {
                var upgradesAfter = new Dictionary<int, int>(upgrades);
                upgradesAfter.Sum(up.NId, 1);
                
                var sumBeforeUpgrade = GetTotalUpgradeCost(upgrades);
                var sumAfterUpgrade = GetTotalUpgradeCost(upgradesAfter);
                
                var totalCost = new Dictionary<int, float>(up.Levels[nextLevel].TotalUpgradeCost);
                
                var upgradeCost = new Dictionary<int, float>(sumAfterUpgrade);
                upgradeCost.Minus(sumBeforeUpgrade);
                upgradeCost.RemoveWhere((x,y)=>y == 0);
                
                var wouldHavePointsLeft = new Dictionary<int, float>(maxPoints);
                wouldHavePointsLeft.Minus(sumAfterUpgrade);
                wouldHavePointsLeft.RemoveWhere((x,y)=>y == 0);
                
                var wouldMissPoints = new Dictionary<int, float>(wouldHavePointsLeft);
                wouldMissPoints.RemoveWhere((x,y)=>y >= 0);

                var sb = new StringBuilder();
                if (wouldMissPoints.Count > 0)
                {
                    var missing = PrintUpgradePoints(wouldMissPoints, T.UpgradesList_UpgradeTooltip_NotEnoughPoints_Row, sb);
                    upText = T.Translation(T.UpgradesList_UpgradeTooltip_NotEnoughPoints, missing);
                }
                else
                {
                    var wouldHavePointsText = PrintUpgradePoints(wouldHavePointsLeft, T.UpgradesList_UpgradeTooltip_WouldHavePointsLeft, T.UpgradesList_UpgradeTooltip_WouldHavePointsLeftZero, T.UpgradesList_UpgradeTooltip_Row, sb.Clear());
                    var upgradeCostText = PrintUpgradePoints(upgradeCost, T.UpgradesList_UpgradeTooltip_UpgradeCost, T.UpgradesList_UpgradeTooltip_UpgradeCostZero,T.UpgradesList_UpgradeTooltip_Row, sb.Clear());
                    var totalCostText = PrintUpgradePoints(totalCost, T.UpgradesList_UpgradeTooltip_TotalCost, T.UpgradesList_UpgradeTooltip_TotalCostZero,T.UpgradesList_UpgradeTooltip_Row, sb.Clear());

                    var was = new UpgradeHelper(specBlock, upgradeOrder);
                    var wouldBe = new UpgradeHelper(specBlock, upgradeOrder, up.NId);
                    var locksAndUnlocks = was.GetLocksAndUnlocks(wouldBe);

                    var sbLocks = new StringBuilder();
                    var sbUnlocks = new StringBuilder();
                    foreach (var locksAndUnlock in locksAndUnlocks)
                    {
                        if (locksAndUnlock.Value)
                        {
                            sbLocks.Append(locksAndUnlock.Key);
                        }
                        else
                        {
                            sbUnlocks.Append(locksAndUnlock.Key);
                        }
                    }
                    
                    upText = T.Translation(T.UpgradesList_UpgradeTooltip, 
                        nextLevel, nextLevel+1, maxLevel, 
                        wouldHavePointsText, upgradeCostText,totalCostText, upgradeName,
                        sbLocks.ToString(), sbUnlocks.ToString());
                }
            }

            tooltip = tooltip ?? "";

            
            var rowText = T.Translation(T.UpgradesList_RowText, upgradeName, nextLevel, nextLevel+1, maxLevel);

            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(rowText), MyStringId.GetOrCompute(upText + tooltip), u);
            return item;
        }


        private static bool AddUpgrade_Enabled(SpecBlock specBlock)
        {
            if (specBlock.SelectedUpgrades.Count == 0) return false;
            var copy = new List<int>(specBlock.AddedUpgrades);
            copy.AddRange(specBlock.SelectedUpgrades);
            return new UpgradeHelper(specBlock, copy).CanBeApplied();
        }
        
        private static bool ApplyUpgrades_Enabled(SpecBlock specBlock) { return specBlock.AddedUpgrades.Count > 0; }
        
        private static bool UpgradeGui_IsVisible(SpecBlock specBlock)
        {
            return specBlock.CanPickUpgrades();
        }




        #region Helpers

        private static string PrintUpgradePoints(Dictionary<int, float> points, string translationHasPoints, string translationNoPoints, string rowTranslation, StringBuilder sb)
        {
            var s = PrintUpgradePoints(points, rowTranslation, sb);
            return T.Translation(points.Count == 0 ? translationNoPoints : translationHasPoints, s);
        }
        
        private static string PrintUpgradePoints(Dictionary<int, float> points, string rowTranslation, StringBuilder sb)
        {
            sb = sb ?? new StringBuilder();
            
            foreach (var point in points)
            {
                var up = OriginalSpecCoreSession.Instance.Points[point.Key];
                var name = T.Translation(up.Name);
                sb.AppendT(rowTranslation, name, point.Value);
            }

            return sb.ToString();
        }

        #endregion
        
        
        
        private static List<int> GenerateRandomUpgrades(List<int> possibleUpgrades, int upgradesLeft)
        {
            //TODO 
            
            var upgrades = OriginalSpecCoreSession.Instance.Upgrades;
            for (var index = 0; index < possibleUpgrades.Count; index++)
            {
                if (!upgrades.ContainsKey(possibleUpgrades[index]))
                {
                    possibleUpgrades.RemoveAt(index);
                    index--;
                }
            }

            Dictionary<int, int> outUpgrades = new Dictionary<int, int>(); 
            var r = new Random();
            while (possibleUpgrades.Count > 0 && upgradesLeft > 0)
            {
                var u = r.Next(possibleUpgrades);
                var lvl = outUpgrades.GetOr(u, 0);
                var up = upgrades[u];

                var nextUpgradeCost = 0; //up.GetNextUpgradeCost(lvl);
                if (nextUpgradeCost > upgradesLeft)
                {
                    //Log.ChatError($"Skipping {up.Name} {lvl}lvl {nextUpgradeCost} : {upgradesLeft}");
                    possibleUpgrades.Remove(u);
                }
                else
                {
                    //Log.ChatError($"Adding {up.Name} {lvl}lvl {nextUpgradeCost} : {upgradesLeft}");
                    outUpgrades.Sum(u, 1);
                    //if (lvl >= up.MaxUpgrades)
                    //{
                    //    possibleUpgrades.Remove(u);
                    //    //Log.ChatError($"Max ugrade: {up.Name}");
                    //}
                    upgradesLeft -= nextUpgradeCost;
                }
            }

            var upgradesList = new List<int>();
            foreach (var upgrade in outUpgrades)
            {
                for (int i = 0; i < upgrade.Value; i++)
                {
                    upgradesList.Add(upgrade.Key);
                }
            }
            
            return upgradesList;
        }
    }
}