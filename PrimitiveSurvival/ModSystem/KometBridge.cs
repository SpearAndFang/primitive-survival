#if KOMET
namespace PrimitiveSurvival.ModSystem
{
    using Komet.Api;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;

    // Komet.Core.dll loads only when this class runs, so PS still starts without Komet
    internal static class KometBridge
    {
        public static bool Start(Vintagestory.API.Common.ModSystem system, ICoreClientAPI api)
        {
            if (Kapi.Connect(system, "1.0") is not { } komet) return false;
            var src = api.Assets.TryGet(new AssetLocation("primitivesurvival", "shaders/kometvenom.fsh"))?.ToText();
            if (src is null) return false;
            var pass = komet.Shaders.TryAddPasses(new KapiPass("venom", src)
            {
                Order = 50,
                Enabled = () => VenomState.Venomed is { Length: > 0 } v && v == api.World.Player?.Entity?.GetName()
            });
            if (pass.Active) return true;
            api.Logger.Warning("[primitivesurvival] Komet venom pass: {0}", pass.Error);
            komet.Dispose();
            return false;
        }
    }
}
#endif
