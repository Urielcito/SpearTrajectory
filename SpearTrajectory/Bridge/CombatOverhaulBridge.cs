using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SpearTrajectory.Bridge
{
    // Detects CO and helps integrate both mods
    public class CombatOverhaulBridge
    {
        public bool IsPresent { get; private set; } = false;
        public bool IsCOPresent { get; private set; } = false;

        private ICoreClientAPI _capi;

        private object _coSystem;

        private PropertyInfo _aimingSystemProp;

        private PropertyInfo _targetVecProp;

        private PropertyInfo _aimingProp;

        private Type _bowItemType;
        private Type _meleeWeaponItemType;

        public CombatOverhaulBridge(ICoreClientAPI capi)
        {
            _capi = capi;

            if (!capi.ModLoader.IsModEnabled("overhaullib"))
                return;

            try
            {
                InitReflection(capi);
            }
            catch (Exception e)
            {
                capi.Logger.Warning("[SpearTrajectory] CO está instalado pero falló la integración: " + e.Message);
                IsPresent = false;
            }
        }

        private void InitReflection(ICoreClientAPI capi) //Get CO and OL assemblies with reflection to get actual Item classes from the mod
        {
            Assembly overhaullibAsm = null;
            Assembly combatoverhaulAsm = null;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = asm.GetName().Name;
                if (name == "Overhaullib") { overhaullibAsm = asm; }
                if (name == "CombatOverhaul") combatoverhaulAsm = asm;
            }

            if (overhaullibAsm == null) { capi.Logger.Warning("[ST] No se encontró overhaullib."); return; }

            Type coSystemType = overhaullibAsm.GetType("CombatOverhaul.CombatOverhaulSystem");
            if (coSystemType == null) return;

            _coSystem = capi.ModLoader.GetModSystem(coSystemType.FullName);
            if (_coSystem == null) return;

            _aimingSystemProp = coSystemType.GetProperty("AimingSystem", BindingFlags.Public | BindingFlags.Instance);
            if (_aimingSystemProp == null) return;

            Type clientAimingType = overhaullibAsm.GetType("CombatOverhaul.RangedSystems.Aiming.ClientAimingSystem");
            if (clientAimingType == null) return;

            _targetVecProp = clientAimingType.GetProperty("TargetVec", BindingFlags.Public | BindingFlags.Instance);
            _aimingProp = clientAimingType.GetProperty("Aiming", BindingFlags.Public | BindingFlags.Instance);

            if (_targetVecProp == null || _aimingProp == null) return;

            Assembly itemAsm = overhaullibAsm;
            _bowItemType = itemAsm.GetType("CombatOverhaul.Implementations.BowItem");
            _meleeWeaponItemType = itemAsm.GetType("CombatOverhaul.Implementations.MeleeWeapon");

            IsPresent = true;
            IsCOPresent = combatoverhaulAsm != null;
            capi.Logger.Notification("[ST] Integración con Combat Overhaul activada.");
        }

        public bool IsCOItem(Item item)
        {
            if (!IsPresent || item == null) return false;

            Type t = item.GetType();

            if (_bowItemType != null && _bowItemType.IsAssignableFrom(t))
                return true;

            if (_meleeWeaponItemType != null && _meleeWeaponItemType.IsAssignableFrom(t))
                return true;

            return false;
        }

        public bool IsCOBow(Item item)
        {
            if (!IsPresent || item == null || _bowItemType == null) return false;
            return _bowItemType.IsAssignableFrom(item.GetType());
        }

        public bool IsCOThrowable(Item item)
        {
            if (!IsPresent || item == null || _meleeWeaponItemType == null) return false;
            if (!_meleeWeaponItemType.IsAssignableFrom(item.GetType())) return false;

            try
            {
                var modesToken = item.Attributes?["Modes"];
                if (modesToken == null) return false;

                // Buscar cualquier modo que tenga ThrowAttack
                var modesObj = modesToken.AsObject<JObject>();
                if (modesObj == null) return false;

                foreach (var mode in modesObj.Properties())
                {
                    if (mode.Value["ThrowAttack"] != null)
                        return true;
                }
            }
            catch { }

            return false;
        }

        public bool IsAiming()
        {
            if (!IsPresent) return false;

            try
            {
                object aimingSystem = _aimingSystemProp.GetValue(_coSystem);
                if (aimingSystem == null) return false;

                return (bool)_aimingProp.GetValue(aimingSystem);
            }
            catch { return false; }
        }

        public Vec3d GetTargetVec()
        {
            if (!IsPresent) return null;

            try
            {
                object aimingSystem = _aimingSystemProp.GetValue(_coSystem);
                if (aimingSystem == null) return null;

                object targetVec = _targetVecProp.GetValue(aimingSystem);
                if (targetVec == null) return null;

                Type vec3Type = targetVec.GetType();
                float x = (float)vec3Type.GetField("X").GetValue(targetVec);
                float y = (float)vec3Type.GetField("Y").GetValue(targetVec);
                float z = (float)vec3Type.GetField("Z").GetValue(targetVec);

                return new Vec3d(x, y, z);
            }
            catch { return null; }
        }

        public float? GetCOVelocity(Item item)
        {
            if (!IsPresent || item == null) return null;

            try
            {
                if (IsCOBow(item))
                {
                    float v = item.Attributes["ArrowVelocity"].AsFloat(0f);
                    if (v > 0f) return v;
                    return null;
                }

                if (IsCOThrowable(item))
                {
                    var modesToken = item.Attributes?["Modes"];
                    if (modesToken == null) return null;

                    var modesObj = modesToken.AsObject<JObject>();
                    if (modesObj == null) return null;

                    foreach (var mode in modesObj.Properties())
                    {
                        var throwAttack = mode.Value["ThrowAttack"];
                        if (throwAttack != null)
                        {
                            float v = throwAttack["Velocity"]?.Value<float>() ?? 0f;
                            if (v > 0f) return v;
                        }
                    }
                }
            }
            catch { }

            return null;
        }
    }
}