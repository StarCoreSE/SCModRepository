using System;
using System.Collections.Generic;
using System.Text;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using VRage;

namespace MIG.SpecCores
{
    public static class T
    {
        public static string Infinity = "SpecCores_Infinity";
        
        public static string BlockInfo_StatusRow = "SpecCores_BlockInfo_StatusRow";//STATUS: {0}\r\n==============\r\n
        public static string BlockInfo_WarningSign = "SpecCores_BlockInfo_WarningSign";
        public static string BlockInfo_NoSpecializations = "SpecCores_BlockInfo_NoSpecializations";//"Block hasn't picked specializations"
        public static string BlockInfo_UpgradesHeader = "SpecCores_BlockInfo_UpgradesHeader";//"\r\n==============\r\nUpgrades:"
        public static string BlockInfo_UpgradesRow = "SpecCores_BlockInfo_UpgradesRow";//"{0}: {1}"
        public static string BlockInfo_StaticOnly = "SpecCores_BlockInfo_StaticOnly";//"Static:"
        public static string BlockInfo_DynamicOnly = "SpecCores_BlockInfo_DynamicOnly";//"Dynamic:"
        public static string BlockInfo_StaticOrDynamic = "SpecCores_BlockInfo_StaticOrDynamic";//"Static/Dynamic:"
        public static string BlockInfo_CantWorkOnSubGrids = "SpecCores_BlockInfo_CantWorkOnSubGrids"; //Cant work on sub grids
        public static string BlockInfo_Header = "SpecCores_BlockInfo_Header";
        public static string BlockInfo_UpgradeCosts = "Upgrade Costs";
        
        public static string ActivationStatus_CurrentCore = "SpecCores_ActivationStatus_CurrentCore";
        public static string ActivationStatus_ErrorNotWorking = "SpecCores_ActivationStatus_NotWorking";
        public static string ActivationStatus_ErrorNotEnabled = "SpecCores_ActivationStatus_NotEnabled";
        public static string ActivationStatus_ErrorNotFunctional = "SpecCores_ActivationStatus_NotFunctional";
        public static string ActivationStatus_ErrorOtherCore = "SpecCores_ActivationStatus_ErrorOtherCore";
        public static string ActivationStatus_ErrorUsingGridGroupDefault = "SpecCores_ActivationStatus_ErrorUsingGridGroupDefault";
        public static string ActivationStatus_ErrorEvenGridGroupFail = "SpecCores_ActivationStatus_ErrorEvenGridGroupFail";

        public static string GUI_AutoEnable = "SpecCores_GUI_AutoEnable"; 
        public static string GUI_AutoEnableToolTip = "SpecCores_GUI_AutoEnableToolTip";
        public static string GUI_SmartAutoEnable = "SpecCores_GUI_SmartAutoEnable";
        public static string GUI_SmartAutoEnableToolTip = "SpecCores_GUI_SmartAutoEnableToolTip";
        
        public static string UpgradesList_RowText = "SpecCores_UpgradesList_RowText";//$"{0} {1}/{2}"
        
        public static string UpgradesList_UpgradeTooltip = "SpecCores_UpgradesList_UpgradeTooltip";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_Row = "SpecCores_UpgradesList_UpgradeTooltip_Row";//$"{0} {1}/{2}"
        
        public static string UpgradesList_UpgradeTooltip_WouldHavePointsLeft = "SpecCores_UpgradesList_UpgradeTooltip_WouldHavePointsLeft";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_WouldHavePointsLeftZero = "SpecCores_UpgradesList_UpgradeTooltip_WouldHavePointsLeftZero";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_UpgradeCost = "SpecCores_UpgradesList_UpgradeTooltip_UpgradeCost";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_UpgradeCostZero = "SpecCores_UpgradesList_UpgradeTooltip_UpgradeCostZero";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_TotalCost = "SpecCores_UpgradesList_UpgradeTooltip_TotalCost";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_TotalCostZero = "SpecCores_UpgradesList_UpgradeTooltip_TotalCostZero";//$"{0} {1}/{2}"
        
        

        public static string UpgradesList_UpgradeTooltip_NotEnoughPoints = "SpecCores_UpgradesList_UpgradeTooltip_NotEnoughPoints";//$"{0} {1}/{2}"
        public static string UpgradesList_UpgradeTooltip_NotEnoughPoints_Row = "SpecCores_UpgradesList_UpgradeTooltip_NotEnoughPoints_Row";//$"{0}:{1}"

        public static string UpgradesList_UpgradeTooltip_MaxUpgrades = "SpecCores_UpgradesList_UpgradeTooltip_MaxUpgrades";//$"{0} {1}/{2}"
        
        public static string Button_ApplyRandomUpgrade = "SpecCores_Button_ApplyRandomUpgrade";
        public static string Button_ApplyRandomUpgrade_Tooltip = "SpecCores_Button_ApplyRandomUpgrade_Tooltip";
        public static string Button_ApplyUpgrade = "SpecCores_Button_ApplyUpgrade";
        public static string Button_ApplyUpgrade_Tooltip = "SpecCores_Button_ApplyUpgrade_Tooltip";
        public static string Button_AddUpgrade = "SpecCores_Button_AddUpgrade";
        public static string Button_AddUpgrade_Tooltip = "SpecCores_Button_AddUpgrade_Tooltip";
        public static string Button_RemoveUpgrade = "SpecCores_Button_RemoveUpgrade";
        public static string Button_RemoveUpgrade_Tooltip = "SpecCores_Button_RemoveUpgrade_Tooltip";

        public static StringBuilder AppendT(this StringBuilder sb, string key, params object[] data)
        {
            sb.Append(Translation(key, data));
            return sb;
        }

        public static StringBuilder AppendT(this StringBuilder sb, string key)
        {
            sb.Append(Translation(key));
            return sb;
        }
        
        public static string Translation(string key)
        {
            return MyTexts.GetString(key);
        }

        public static string Translation(string key, params object[] data)
        {
            var s = MyTexts.GetString(key);
            return string.Format(s, data);
        }
        
        public static void GetLimitsInfo(Limits foundLimits, StringBuilder sb, Limits maxAvailable, Limits total)
        {
            int index = sb.Length;
            int line = 0;
            try
            {
                line = 1;
                var allKeys = new Limits(maxAvailable);
                if (foundLimits != null)
                {
                    allKeys.Sum(foundLimits);
                }
                line = 3;
                var keys = new List<int>(allKeys.Keys);
                foreach (var kv in OriginalSpecCoreSession.Instance.Points)
                {
                    if (kv.Value == null)
                    {
                        throw new Exception("Limit point is null");
                    }
                    
                    if (kv.Value.Format == null)
                    {
                        throw new Exception($"Limit point {kv.Key} format is null");
                    }
                    if (!kv.Value.Format.Visible) keys.Remove(kv.Key);
                }
                line = 4;
                keys.Sort((a, b) =>
                {
                    var points = OriginalSpecCoreSession.Instance.Points;
                    LimitPoint p1, p2;
                    if (points.TryGetValue(a, out p1) && points.TryGetValue(b, out p2))
                    {
                        var a1 = p1.DisplayOrder;
                        var a2 = p2.DisplayOrder;
                        return a1 > a2 ? 1 : a1 < a2 ? -1 : 0;
                    } 
                    else
                    {
                        Log.ChatError("Not found: " + a + " / " + b);
                        return a > b  ? 1 : a < b ? -1 : 0;
                    }
                });
                line = 4;
                if (foundLimits != null)
                {
                    foreach (var x in keys) {
                        var am1 = foundLimits.GetValueOrDefault(x, 0);
                        var am2 = maxAvailable.GetValueOrDefault(x, 0);
                        var am3 = total.GetValueOrDefault(x, 0);
                        
                        
                        line = 5;
                        var limitPoint = OriginalSpecCoreSession.Instance.Points[x];
                        LimitPointFormat format = limitPoint.Format;
                        
                        if (am1 == 0 && am2 == 0 && am3 == 0 && !format.VisibleIfAllZero)
                        {
                            continue;
                        }
                        
                        if (am2 >= OriginalSpecCoreSession.MaxValue && !format.VisibleIfInfinity)
                        {
                            continue;
                        }
                        line = 6;


                        bool isOverlimit = false;
                        bool isPossibleOverlimit = false;
                        
                        string formatS = Translation(format.Format);
                        switch (limitPoint.Behavior)
                        {
                            case PointBehavior.SumLessOrEqual:
                            case PointBehavior.LessOrEqual:
                                if (am1 > am2)
                                {
                                    formatS = Translation(format.FormatOverlimiting);
                                    isOverlimit = true;
                                }
                                else if (am3 > am2)
                                {
                                    formatS = Translation(format.FormatPossibleOverlimiting);
                                    isPossibleOverlimit = true;
                                }
                                break;
                            case PointBehavior.MoreOrEqual:
                                if (am1 < am2)
                                {
                                    formatS = Translation(format.FormatOverlimiting);
                                    isOverlimit = true;
                                }
                                else if (am3 < am2)
                                {
                                    formatS = Translation(format.FormatPossibleOverlimiting);
                                    isPossibleOverlimit = true;
                                }
                                break;
                            case PointBehavior.Property:
                                break;
                        }

                        line = 7;
                        
                        var txt = formatS.Replace((key) =>
                        {
                            switch (key)
                            {
                                case "PointName": return Translation(limitPoint.Name);
                                case "UnitName": return Translation(limitPoint.UnitName);
                                case "Max": return FormatNumber(format.NumberFormat, am2);
                                case "Current": return FormatNumber(format.NumberFormat, am1);
                                case "Total": return FormatNumber(format.NumberFormat, am3);
                                default: return Translation(key);
                            }
                        });
                        
                        
                        line = 8;


                        if ((isOverlimit && format.ShowOnTopIfOverlimit) || (isPossibleOverlimit && format.ShowOnTopIfPossibleOverlimit))
                        {
                            var s = txt + "\r\n";
                            sb.Insert(index, s);
                            index += s.Length;
                        }
                        else
                        {
                            sb.Append(txt);
                            sb.Append("\r\n");
                        }
                        line = 9;
                    }
                }
                else
                {
                    line = 20;
                    foreach (var x in keys) {
                        var am2 = maxAvailable.GetValueOrDefault(x, 0);
                        line = 24;
                        var limitPoint = OriginalSpecCoreSession.Instance.Points.GetOr(x, null);
                        if (limitPoint == null)
                        {
                            continue;
                        }
                        line = 26;
                        LimitPointFormat format = limitPoint.Format;
                        if (!format.VisibleIfAllZero && am2 == 0)
                        {
                            continue;
                        }
                        if (am2 > OriginalSpecCoreSession.MaxValue && !format.VisibleIfInfinity)
                        {
                            continue;
                        }
                        line = 28;
                        
                        sb.Append($"{Translation(limitPoint.Name)}: {FormatNumber(format.NumberFormat, am2)}{Translation(limitPoint.UnitName)}\r\n");
                        
                        line = 30;
                    }
                }
            }
            catch (Exception e)
            {
                Log.ChatError($"At line = {line} MAX={maxAvailable?.Print(" ") ?? "NULL" } TOTAL={total?.Print(" ") ?? "NULL"} FOUND={foundLimits?.Print(" ") ?? "NULL"}", e);
            }
        }

        private static string FormatNumber(string format, float value)
        {
            if (value >= OriginalSpecCoreSession.MaxValue)
            {
                return Translation(T.Infinity);
            }
            return string.Format(format,value);
        }
    }
}