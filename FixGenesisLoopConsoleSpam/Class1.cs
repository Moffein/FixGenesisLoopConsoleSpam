using UnityEngine;
using BepInEx;
using RoR2;
using System;
using MonoMod.Cil;
using System.Collections;
using System.Linq;
using BepInEx.Configuration;
using System.Runtime.CompilerServices;

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace FixGenesisLoopConsoleSpam
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Moffein.FixGenesisLoopConsoleSpam", "FixGenesisLoopConsoleSpam", "1.0.0")]
    public class FixGenesisLoopConsoleSpam : BaseUnityPlugin
    {
        public static ConfigEntry<bool> allowUnreadable;

        private void Awake()
        {
            allowUnreadable = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Allow Unreadable Meshes"), true, new ConfigDescription("Meshes with unreadable Tri-counts (usually custom skins) are allowed to keep the Genesis Loop effect. Set to false if you're still getting console spam (may cause Genesis Loop particles to get removed from valid custom skins)."));
            On.EntityStates.VagrantNovaItem.BaseVagrantNovaItemState.OnEnter += BaseVagrantNovaItemState_OnEnter;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(allowUnreadable));
        }

        private void BaseVagrantNovaItemState_OnEnter(On.EntityStates.VagrantNovaItem.BaseVagrantNovaItemState.orig_OnEnter orig, EntityStates.VagrantNovaItem.BaseVagrantNovaItemState self)
        {
            bool hasNullref = false;
            try
            {
                orig(self);
            }
            catch(NullReferenceException e)
            {
                hasNullref = true;
            }

            if (!self.chargeSparks) return;
            if (hasNullref)
            {
                Destroy(self.chargeSparks);
                return;
            }

            bool shouldDestroy = true;
            if (self.chargeSparks.shape.skinnedMeshRenderer && self.chargeSparks.shape.skinnedMeshRenderer.sharedMesh)
            {
                bool readable = self.chargeSparks.shape.skinnedMeshRenderer.sharedMesh.isReadable;
                if (readable)
                {
                    int tris = self.chargeSparks.shape.skinnedMeshRenderer.sharedMesh.triangles.Count();
                    if (tris > 0)
                    {
                        Debug.Log("FixGenesisLoopConsoleSpam: Valid mesh confirmed.");
                        shouldDestroy = false;
                    }
                }
                else
                {
                    if (allowUnreadable.Value)
                    {
                        Debug.LogWarning("FixGenesisLoopConsoleSpam: Could not validate mesh because isReadable is set to false. Allowing due to config settings.");
                        shouldDestroy = false;
                    }
                    else
                    {
                        Debug.LogWarning("FixGenesisLoopConsoleSpam: Could not validate mesh because isReadable is set to false.");
                    }
                }
            }

            if (shouldDestroy)
            {
                Debug.LogWarning("FixGenesisLoopConsoleSpam: Could not confirm mesh is valid. Destroying Gensis Loop particles.");
                Destroy(self.chargeSparks);
            }
        }
    }
}
