namespace PrimitiveSurvival.ModSystem
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

#nullable disable

    public class ItemSpearFromAttributes : ItemFishingSpear , IContainedMeshSource, IHandbookGrouping
    {
        public const string SpearMeshRefsKey = "spearmeshrefs";
        private ICoreClientAPI capi;
        private Dictionary<string, Item> fishItemCache = new Dictionary<string, Item>();
        private Dictionary<int, MultiTextureMeshRef> meshrefs => ObjectCacheUtil.GetOrCreate(api, SpearMeshRefsKey, () => new Dictionary<int, MultiTextureMeshRef>());
        private Vintagestory.API.Datastructures.OrderedDictionary<string, string[]> variantGroups = null;
        
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
            variantGroups = Attributes["variantGroups"].AsObject(variantGroups);
            if (variantGroups == null) throw new System.Exception("Spear " + Code + " has improperly defined variantGroups");
            AddEmptyToCreativeInventory();
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            if (api.ObjectCache.ContainsKey(SpearMeshRefsKey) && meshrefs.Count > 0)
            {
                foreach (var (_, meshRef) in meshrefs)
                {
                    meshRef.Dispose();
                }

                ObjectCacheUtil.Delete(api, SpearMeshRefsKey);
            }
            base.OnUnloaded(api);
        }

        private void AddEmptyToCreativeInventory()
        {
            List<JsonItemStack> stacks = new List<JsonItemStack>();
            var allowedVariants = Attributes["allowedVariants"].AsArray<Dictionary<string, string>>();

            foreach (var entry in allowedVariants)
            {
                Dictionary<string, string[]> attributes = [];
                foreach (var key in variantGroups.Keys)
                {
                    if (entry.TryGetValue(key, out var val))
                    {
                        attributes.Add(key, val == "*" ? [.. variantGroups[key]] : (val != null ? [val] : []));
                    }
                }

                string[] variants = [""];
                foreach (var key in attributes.Keys)
                {
                    var newVariants = variants.SelectMany(variant => attributes[key].Select(val => variant + $" {key}: \"{val}\","));
                    if (newVariants.Any()) variants = [.. newVariants];
                }

                variants = [.. variants.Select(var => $"{{{var} }}")];
                stacks.AddRange(variants.Select(genJstack));
            }

            CreativeInventoryStacks = [new() { Stacks = [.. stacks], Tabs = ["primitive", "tools"] }];
        }

        private JsonItemStack genJstack(string json)
        {
            var jstack = new JsonItemStack()
            {
                Code = this.Code,
                Type = EnumItemClass.Item,
                Attributes = new JsonObject(JToken.Parse(json))
            };

            jstack.Resolve(api.World, "spear type");

            return jstack;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string fish = itemstack.Attributes.GetString("fish", "none");
            if (fish != itemstack.TempAttributes.GetString("lastRenderedFish", "none"))
            {
                itemstack.TempAttributes.RemoveAttribute("meshRefId"); // Bust the cache
                itemstack.TempAttributes.SetString("lastRenderedFish", fish); // Update the tracker
            }

            int meshrefid = itemstack.TempAttributes.GetInt("meshRefId");
            if (meshrefid == 0 || !meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                int id = meshrefs.Count + 1;
                var modelref = capi.Render.UploadMultiTextureMesh(GenMesh(itemstack, capi.ItemTextureAtlas));
                renderinfo.ModelRef = meshrefs[id] = modelref;

                itemstack.TempAttributes.SetInt("meshRefId", id);
            }
            
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            if (genTextureSource(itemstack, targetAtlas) is not ContainedTextureSource cnts) return new MeshData();
            capi.Tesselator.TesselateItem(this, out MeshData fullMesh, cnts);
            string fishType = itemstack.Attributes.GetString("fish", "none");
            if (fishType != "none")
            {
                if (!fishItemCache.TryGetValue(fishType, out Item fishItem))
                {
                    fishItem = capi.World.Items.FirstOrDefault(i =>
                        i.Code != null && i.Code.Path.Contains(fishType+"-raw"));

                    if (fishItem != null) fishItemCache[fishType] = fishItem;
                }
                if (fishItem != null)
                {
                    var fishTexSource = new ContainedTextureSource(capi, targetAtlas, new Dictionary<string, AssetLocation>(), "fish-on-spear");
                    var fishShape = capi.TesselatorManager.GetCachedShape(fishItem.Shape.Base);
                    foreach (var tex in fishShape.Textures)
                    {
                        fishTexSource.Textures[tex.Key] = tex.Value;
                    }
                    capi.Tesselator.TesselateItem(fishItem, out MeshData fishMesh, fishTexSource);

                    if (fishMesh != null)
                    {
                        float[] modelMat = Mat4f.Create();
                        Mat4f.Translate(modelMat, modelMat, 0.8f, 0.6f, 0.65f);
                        Mat4f.RotateX(modelMat, modelMat, -1.5708f);
                        Mat4f.RotateY(modelMat, modelMat, 1.5708f);

                        fishMesh.MatrixTransform(modelMat);
                        fullMesh.AddMeshData(fishMesh);
                    }
                }
            }
            return fullMesh;
        }



        private ContainedTextureSource genTextureSource(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            var cnts = new ContainedTextureSource(api as ICoreClientAPI, targetAtlas, [], string.Format("For render in spear {0}", Code));
            cnts.Textures.Clear();

            Dictionary<string, string> attributes = getAttributesFromItemstack(itemstack);
            var shape = capi.TesselatorManager.GetCachedShape(this.Shape.Base);

            foreach (var ctex in shape.Textures)
            {
                cnts.Textures[ctex.Key] = ctex.Value;
            }

            if (attributes.Count == 0) return cnts;
            var texturesDict = Attributes["textures"].AsObject<Dictionary<string, Dictionary<string, string>>>();
            if (texturesDict != null && texturesDict.TryGetValue("fishingspear", out var resolvedTextures))
            {
                foreach (var textureEntry in resolvedTextures)
                {
                    string textureKey = textureEntry.Key;  
                    string texturePath = textureEntry.Value;

                    foreach (var attr in attributes)
                    {
                        texturePath = texturePath.Replace($"{{{attr.Key}}}", attr.Value);
                    }
                    cnts.Textures[textureKey] = new AssetLocation(texturePath);
                }
            }
            return cnts;
        }


        public override string GetHeldItemName(ItemStack itemStack)
        {
            Dictionary<string, string> attributes = getAttributesFromItemstack(itemStack);

            if (attributes.Count == 0) return base.GetHeldItemName(itemStack);

            string variant = Code.Domain + AssetLocation.LocationSeparator + "item-" + Code.Path + "-" + string.Join("-", attributes.Values);
            return Lang.GetMatchingIfExists(variant) ?? base.GetHeldItemName(itemStack);
        }


       
        private Dictionary<string, string> getAttributesFromItemstack(ItemStack itemStack)
        {
            Dictionary<string, string> attributes = [];
            foreach (var key in variantGroups.Keys)
            {
                if (itemStack.Attributes.GetAsString(key, null) is string val)
                {
                    attributes.Add(key, val);
                }
            }

            attributes.Add("material", "wood");
            return attributes;
        }

        public MeshData GenMesh(ItemSlot slot, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
        {
            return GenMesh(slot.Itemstack, targetAtlas);
        }

        public string GetMeshCacheKey(ItemSlot slot)
        {
            return Code.ToShortString() + "-" + string.Join("-", variantGroups.Keys.Select(code => slot.Itemstack.Attributes.GetString(code)).Where(code => code != null));
        }

        public AssetLocation GetCodeForHandbookGrouping(ItemStack stack)
        {
            Dictionary<string, string> attributes = getAttributesFromItemstack(stack);

            if (attributes.Count == 0) return Code;

            return AssetLocation.Create(Code.Path + "-" + string.Join("-", attributes.Values), Code.Domain);
        }

        public string GetWildcardForHandbookGrouping(string wildcard, ItemStack stack)
        {
            foreach (var variant in variantGroups)
            {
                wildcard = wildcard.Replace("{" + variant.Key + "}", stack.Attributes.GetAsString(variant.Key));
            }

            return wildcard;
        }
    }
}

