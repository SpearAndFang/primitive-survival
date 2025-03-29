namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using PrimitiveSurvival.ModConfig;

    public class EntityPSAgent : EntityAgent
    {
        public EntityPSAgent()
        { }

        public override string GetInfoText()
        {
            var result = base.GetInfoText();
            if (ModConfig.Loaded.ShowModNameInHud)
            {
                result += "\n<font color=\"#D8EAA3\"><i>" + Lang.GetMatching("game:tabname-primitive") + "</i></font>\n\n";
            }
            return result;
        }

    }
}

