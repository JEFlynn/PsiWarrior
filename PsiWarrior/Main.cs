using System;
using System.IO;
using System.Reflection;
using UnityModManagerNet;
using HarmonyLib;
using I2.Loc;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using SolastaModApi;
using System.Collections.Generic;
using UnityEngine;

namespace PsiWarrior
{
    public class Main
    {

        public static Guid ModGuidNamespace = new Guid("62565155-4d2e-4d72-a651-f8b0749f22a0");
        // This is showing one way to create guids for a database entry. Once a guid is released it should stay consistent.
        // One way to maintain this consistincy is to manually set each individual guid. This is showing a second option where
        // you generate a guid for the mod, and then use the GuidHelper to generate the guid for the database entry using the
        // mod guid and the string name of the item. The process is deterministic given the same inputs, so once you've
        // released with a generated guid you should not change the inputs to the GuidHelper.
        [Conditional("DEBUG")]
        internal static void Log(string msg) => Logger.Log(msg);

        internal static void Error(Exception ex) => Logger?.Error(ex.ToString());
        internal static void Error(string msg) => Logger?.Error(msg);
        internal static UnityModManager.ModEntry.ModLogger Logger { get; private set; }

        internal static void LoadTranslations()
        {
            var languageSourceData = LocalizationManager.Sources[0];
            var translationsPath = Path.Combine(UnityModManager.modsPath, @"PsiWarrior\Translations.json");
            var translations = JObject.Parse(File.ReadAllText(translationsPath));
            foreach (var translationKey in translations)
            {
                foreach (var translationLanguage in (JObject)translationKey.Value)
                {
                    try
                    {
                        var languageIndex = languageSourceData.GetLanguageIndex(translationLanguage.Key);
                        languageSourceData.AddTerm(translationKey.Key).Languages[languageIndex] = translationLanguage.Value.ToString();
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Error($"language {translationLanguage.Key} not installed");
                    }
                    catch (KeyNotFoundException e)
                    {
                        Error($"term {translationKey.Key} not found");
                    }
                }
            }
        }

        internal static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Logger = modEntry.Logger;

                ModBeforeDBReady();

                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Error(ex);
                throw;
            }

            return true;
        }

        [HarmonyPatch(typeof(MainMenuScreen), "RuntimeLoaded")]
        internal static class MainMenuScreen_RuntimeLoaded_Patch
        {
            internal static void Postfix()
            {
                ModAfterDBReady();
            }
        }

        // ENTRY POINT IF YOU NEED SERVICE LOCATORS ACCESS
        internal static void ModBeforeDBReady()
        {
            LoadTranslations();
        }

        // ENTRY POINT IF YOU NEED SAFE DATABASE ACCESS
        internal static void ModAfterDBReady()
        {
            // var cleric = DatabaseHelper.CharacterClassDefinitions.Cleric;
            // var skeleton = DatabaseHelper.MonsterDefinitions.Skeleton;



            SubClassBuilder psiWarrior = new SubClassBuilder();
            psiWarrior.SetName("PsiWarrior", GuidHelper.Create(ModGuidNamespace, "PsiWarrior").ToString());
            GuiPresentationBuilder psiGUI = new GuiPresentationBuilder(
               "Subclass/&ArchetypePsiWarriorDescription",
               "Subclass/&ArchetypePsiWarriorTitle");
            psiGUI.SetSpriteReference(DatabaseHelper.CharacterSubclassDefinitions.RoguishShadowCaster.GuiPresentation.SpriteReference);

            
            psiWarrior.SetGuiPresentation(psiGUI.Build());
            //FeatureDefinitionAdditionalDamage test =;

            GuiPresentationBuilder psionicDiceGui = new GuiPresentationBuilder("Subclass/&PsionicDiceDescription", "Subclass/&PsionicDiceTitle");

            //trying to see what things might be causing the object reference issues
            psionicDiceGui.SetSpriteReference(DatabaseHelper.FeatureDefinitionPowers.PowerDomainBattleDecisiveStrike.GuiPresentation.SpriteReference);

            FeatureDefinitionAttributeModifier PsionicDice = BuildAttributeMod(FeatureDefinitionAttributeModifier.AttributeModifierOperation.Set, AttributeDefinitions.ChannelDivinityNumber, 4, "AttributeModifierPsionicChannelDivinity", psionicDiceGui.Build());
            GuiPresentationBuilder psionicStrikeGui = new GuiPresentationBuilder("Spend a psionic dice to deal extra damage.", "Psionic Strike");

            //FeatureDefinitionPower psionicStrike = BuildFeaturePower("FeaturePowerPsionicStrike", RuleDefinitions.ActivationTime.OnAttackHit, RuleDefinitions.RechargeRate.ChannelDivinity, 1, PsiStrikeBuilder(), psionicStrikeGui.Build());
            FeatureDefinitionPower psionicStrike = BuildPower2(4, RuleDefinitions.UsesDetermination.Fixed, AttributeDefinitions.Wisdom, RuleDefinitions.ActivationTime.OnAttackHit, 1, RuleDefinitions.RechargeRate.ChannelDivinity, RuleDefinitions.RangeType.MeleeHit, RuleDefinitions.TargetType.Individuals, ActionDefinitions.ItemSelectionType.None, RuleDefinitions.DurationType.Instantaneous, 1, RuleDefinitions.TurnOccurenceType.EndOfTurn, AttributeDefinitions.Wisdom, AttributeDefinitions.Intelligence, 15, new List<SaveAffinityBySenseDescription>() { }, new ConditionDefinition(), new ConditionDefinition(), "FeaturePowerPsionicStrike", psionicStrikeGui.Build(), PsiStrikeBuilder());

            //trying to see what things might be causing the object reference issues
            psionicStrikeGui.SetSpriteReference(DatabaseHelper.FeatureDefinitionPowers.PowerDomainBattleDecisiveStrike.GuiPresentation.SpriteReference);
            

            psiWarrior.AddFeatureAtLevel(PsionicDice, 3);
            psiWarrior.AddFeatureAtLevel(psionicStrike, 3);


            GuiPresentationBuilder psionicDiceAddGui = new GuiPresentationBuilder("Gain another 2 psionic dice", "Additional Psionic Power");
            
            psionicDiceAddGui.SetSpriteReference(DatabaseHelper.FeatureDefinitionPowers.PowerDomainBattleDecisiveStrike.GuiPresentation.SpriteReference);
            
            FeatureDefinitionAttributeModifier psionicDiceIncreaseUses = BuildAttributeMod(FeatureDefinitionAttributeModifier.AttributeModifierOperation.Additive, AttributeDefinitions.ChannelDivinityNumber, 2, "AttributeModifierPsionicChannelDivinityAdd",psionicDiceAddGui.Build());
            psiWarrior.AddFeatureAtLevel(psionicDiceIncreaseUses, 5);

            psiWarrior.AddFeatureAtLevel(psionicDiceIncreaseUses, 9);
            DatabaseHelper.FeatureDefinitionSubclassChoices.SubclassChoiceFighterMartialArchetypes.Subclasses.Add(psiWarrior.AddToDB().Name);


            //EffectDescriptionBuilder;
        }

        // Below here are helpers that make creating specific Features easier. They also add the features to the database so
        // that the game can reference them.

        public static EffectDescription PsiStrikeBuilder()
        {
            EffectDescriptionBuilder effectBuild = new EffectDescriptionBuilder();

            effectBuild.SetTargetingData(RuleDefinitions.Side.Enemy, RuleDefinitions.RangeType.MeleeHit, 0, RuleDefinitions.TargetType.Individuals, 1, 0, ActionDefinitions.ItemSelectionType.None);
            effectBuild.SetDurationData(RuleDefinitions.DurationType.Instantaneous, 0, RuleDefinitions.TurnOccurenceType.EndOfTurn);

            SolastaModApi.BuilderHelpers.EffectFormBuilder damageFormBuilder = new SolastaModApi.BuilderHelpers.EffectFormBuilder();
            damageFormBuilder.SetDamageForm(false, RuleDefinitions.DieType.D1, RuleDefinitions.DamageTypeForce, 0, RuleDefinitions.DieType.D6, 1, RuleDefinitions.HealFromInflictedDamage.Never, new List<RuleDefinitions.TrendInfo> { });
            effectBuild.AddEffectForm(damageFormBuilder.Build());
            
            return effectBuild.Build();

        }

        #region OldEffectBuilder
        //public static EffectDescription BuildPsiStrikeEffect()
        //{
        //    EffectDescription effect = new EffectDescription();
        //    EffectForm effectForm = new EffectForm();
        //    effectForm.FormType = EffectForm.EffectFormType.Damage;

        //    DamageForm psiDamage = new DamageForm();
        //    psiDamage.DiceNumber = 1;
        //    psiDamage.DieType = RuleDefinitions.DieType.D6;

        //    EffectAdvancement effectAdvancement = new EffectAdvancement();
        //    Traverse.Create(effectAdvancement).Field("incrementMultiplier").SetValue(1);
        //    Traverse.Create(effect).Field("effectAdvancement").SetValue(effectAdvancement);

        //    EffectParticleParameters particleParams = new EffectParticleParameters(DatabaseHelper.SpellDefinitions.MagicWeapon.EffectDescription.EffectParticleParameters);
        //    Traverse.Create(effect).Field("effectParticleParameters").SetValue(particleParams);

        //    Traverse.Create(effectForm).Field("damageForm").SetValue(psiDamage);
        //    Traverse.Create(effectForm).Field("rangeType").SetValue(RuleDefinitions.RangeType.MeleeHit);
        //    Traverse.Create(effectForm).Field("durationType").SetValue(RuleDefinitions.DurationType.Instantaneous);
        //    effect.EffectForms.Add(effectForm);


        //    return effect;

        //}
        #endregion

        public static FeatureDefinitionPower BuildFeaturePower(string name, RuleDefinitions.ActivationTime activation, RuleDefinitions.RechargeRate recharge, int cost, EffectDescription effect, GuiPresentation guiPresentation)
        {
            FeatureDefinitionPower featurePower = ScriptableObject.CreateInstance<FeatureDefinitionPower>();
            Traverse.Create(featurePower).Field("activationTime").SetValue(activation);
            Traverse.Create(featurePower).Field("rechargeRate").SetValue(recharge);
            Traverse.Create(featurePower).Field("costPerUse").SetValue(cost);
            Traverse.Create(featurePower).Field("effectDescription").SetValue(effect);
            Traverse.Create(featurePower).Field("name").SetValue(name);


            Traverse.Create(featurePower).Field("shortTitleOverride").SetValue("test");
            Traverse.Create(featurePower).Field("contentCopyright").SetValue(BaseDefinition.Copyright.UserContent);

            Traverse.Create(featurePower).Field("usesDetermination").SetValue(RuleDefinitions.UsesDetermination.Fixed);

            featurePower.name = name;

            Traverse.Create(featurePower).Field("guiPresentation").SetValue(guiPresentation);
            Traverse.Create(featurePower).Field("guid").SetValue(GuidHelper.Create(Main.ModGuidNamespace, name).ToString());
            //DatabaseRepository.GetDatabase<FeatureDefinitionPower>().Add(featurePower);
            DatabaseRepository.GetDatabase<FeatureDefinition>().Add(featurePower);

            return featurePower;
        }

        public static FeatureDefinitionPower BuildPower2(int usesPerRecharge, RuleDefinitions.UsesDetermination usesDetermination, string usesAbilityScoreName,
            RuleDefinitions.ActivationTime activationTime, int costPerUse, RuleDefinitions.RechargeRate recharge, RuleDefinitions.RangeType rangeType,
            RuleDefinitions.TargetType targetType, ActionDefinitions.ItemSelectionType itemSelectionType, RuleDefinitions.DurationType durationtype, int durationparameter,
            RuleDefinitions.TurnOccurenceType endOfEffect, string savethrowtype, string savethrowDA, int savethrowDC,
            List<SaveAffinityBySenseDescription> saveAffinityBySenseDescriptions, ConditionDefinition condition1, ConditionDefinition condition2, string name, GuiPresentation guiPresentation, EffectDescription effect)
        {
            FeatureDefinitionPower power = ScriptableObject.CreateInstance<FeatureDefinitionPower>();
            Traverse.Create(power).Field("fixedUsesPerRecharge").SetValue(usesPerRecharge);
            Traverse.Create(power).Field("usesDetermination").SetValue(usesDetermination);
            Traverse.Create(power).Field("activationTime").SetValue(activationTime);
            Traverse.Create(power).Field("costPerUse").SetValue(costPerUse);
            Traverse.Create(power).Field("rechargeRate").SetValue(recharge);
            Traverse.Create(power).Field("usesAbilityScoreName").SetValue(usesAbilityScoreName);
            Traverse.Create(power).Field("uniqueInstance").SetValue(true);
            Traverse.Create(power).Field("effectDescription").SetValue(effect);



            Traverse.Create(power).Field("name").SetValue(name);
            power.name = name;
            Traverse.Create(power).Field("guiPresentation").SetValue(guiPresentation);
            Traverse.Create(power).Field("guid").SetValue(GuidHelper.Create(Main.ModGuidNamespace, name).ToString());
            DatabaseRepository.GetDatabase<FeatureDefinition>().Add(power);
            return power;
        }

        public static FeatureDefinitionAttributeModifier BuildAttributeMod(FeatureDefinitionAttributeModifier.AttributeModifierOperation modifierType, string attribute, int amount, string name, GuiPresentation guiPresentation)
        {
            FeatureDefinitionAttributeModifier attributeMod = ScriptableObject.CreateInstance<FeatureDefinitionAttributeModifier>();

            //Traverse.Create(attributeMod).Field("modifierType").SetValue(modifierType);
            Traverse.Create(attributeMod).Field("modifierType2").SetValue(modifierType);
            Traverse.Create(attributeMod).Field("modifiedAttribute").SetValue(attribute);
            Traverse.Create(attributeMod).Field("modifierValue").SetValue(amount);
            Traverse.Create(attributeMod).Field("name").SetValue(name);
            attributeMod.name = name;

            Traverse.Create(attributeMod).Field("guiPresentation").SetValue(guiPresentation);
            Traverse.Create(attributeMod).Field("guid").SetValue(GuidHelper.Create(Main.ModGuidNamespace, name).ToString());
            //DatabaseRepository.GetDatabase<FeatureDefinitionAttributeModifier>().Add(attributeMod);
            DatabaseRepository.GetDatabase<FeatureDefinition>().Add(attributeMod);
            return attributeMod;

        }
    }


}
