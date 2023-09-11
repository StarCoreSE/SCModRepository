using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace MIG.Shared.SE {
    public static class EconomyHelper {
        public static MyObjectBuilderType COMPONENT = MyObjectBuilderType.Parse("MyObjectBuilder_Component");
        public static MyObjectBuilderType INGOT = MyObjectBuilderType.Parse("MyObjectBuilder_Ingot");
        public static MyObjectBuilderType ORE = MyObjectBuilderType.Parse("MyObjectBuilder_Ore");
        public static MyDefinitionId ASSEMBLER_TIME = new MyDefinitionId(ORE, "AssemblerTime");
        public static MyDefinitionId REFINERY_TIME = new MyDefinitionId(ORE , "RefineryTime");
        public static MyDefinitionId WORTH = new MyDefinitionId(ORE, "Money");
        public static MyDefinitionId WORTH_OK = new MyDefinitionId(ORE, "MoneyOk");
        
        
        public static long changeMoney (this IMyPlayer pl, double perc, long amount = 0) {
             long bal;
             if (pl.TryGetBalanceInfo (out bal)) {
                var fee = (long)(bal * perc + amount);
                var take = fee - bal > 0? bal : fee;
                pl.RequestChangeBalance (fee);
                return fee;
             }
             return 0;
        }

        public static long changeMoney (this IMyFaction pl, double perc, long amount = 0) {
             long bal;
             if (pl.TryGetBalanceInfo (out bal)) {
                var fee = (long)(bal * perc + amount) ;
                var take = fee - bal > 0? bal : fee;
                pl.RequestChangeBalance (take);
                return take;
             }
             return 0;
        }
        
        
        

        private static void FormWorth(Dictionary<MyDefinitionId, Dictionary<MyDefinitionId, double>> dict, MyBlueprintDefinitionBase b, Dictionary<MyDefinitionId, double> mapper, bool isRef, MyBlueprintDefinitionBase.Item result)
        {
            var from = isRef ? b.Results: b.Results;
            var to = isRef ? b.Prerequisites : b.Prerequisites;
            var time = isRef ? REFINERY_TIME : ASSEMBLER_TIME;

            var key = result.Id;
            var am = result.Amount;
            var d = new Dictionary<MyDefinitionId, double>();

            if (result.Id.SubtypeName.Contains("Scrap")) return;
            
            double w;
            var money = 0.0;
            var hasAllMappings = true;
            
            foreach (var y in to)
            {
                if (y.Id.SubtypeName.Contains("Scrap")) return;
                w = (double)y.Amount / (double)am;
                d[y.Id] = w;// * assemblerMutliplier;

                if (mapper.ContainsKey(y.Id))
                {
                    money += mapper[y.Id] * w;
                }
                else
                {
                    hasAllMappings = false;
                }
            }

            w = b.BaseProductionTimeInSeconds / (double)am;
            d[time] = w;

            if (hasAllMappings && mapper.ContainsKey(time))
            {
                money += w * mapper[time];
            }
            else
            {
                hasAllMappings = false;
            }

            d[WORTH] = money;
            if (hasAllMappings)
            {
                d[WORTH_OK] = 1;
            }

            dict[key] = d;
        }
    }
}
