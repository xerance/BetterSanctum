using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using Newtonsoft.Json;
using Color = SharpDX.Color;

namespace BetterSanctum;

public class BetterSanctumSettings : ISettings
{
    private static readonly IReadOnlyList<string> CurrencyTypes = new List<string>
    {
        "Orbs of Alteration",
        "Orbs of Chance",
        "Glassblower's Baubles",
        "Chromatic Orbs",
        "Jeweller's Orbs",
        "Orbs of Alchemy",
        "Orbs of Fusing",
        "Orbs of Scouring",
        "Cartographer's Chisels",
        "Chaos Orbs",
        "Orbs of Binding",
        "Orbs of Regret",
        "Gemcutter's Prisms",
        "Blessed Orbs",
        "Vaal Orbs",
        "Orbs of Horizon",
        "Instilling Orbs",
        "Regal Orbs",
        "Enkindling Orbs",
        "Orbs of Unmaking",
        "Awakened Sextants",
        "Stacked Decks",
        "Veiled Chaos Orbs",
        "Orbs of Annulment",
        "Divine Orbs",
        "Exalted Orbs",
        "Divine Vessels",
        "Sacred Orbs",
        "Mirrors of Kalandra",
        "Blacksmith's Whetstones",
        "Armourer's Scraps",
        "Orbs of Transmutation",
        "Orbs of Augmentation",
        "Fracturing Orbs",
        "Volatile Vaal Orbs",
    };

    public readonly IReadOnlyList<string> CurrencyDuplicate = new List<string>
    {
        "Divine Orb",
        "Divine Orbs",
        "Mirror of kalandra",
        "Mirror",
        "Mirrors",
    };

    private static readonly IReadOnlyList<string> RoomTypes = new List<string>
    {
        "Explore",
        "Arena",
        "Lair",
        "Maze",
        "Gauntlet",
        "Miniboss",
        "Vault",
        "Puzzle",
        "Boss",
        "Merchant",
        "Fountain",
        "Deal",
        "Deferral",
        "CurseFountain",
        "BoonFountain",
        "RainbowFountain",
        "Treasure",
        "TreasureMinor",
        "Final",
    };

    private static readonly IReadOnlyList<(string, string)> AfflictionTypes = new List<(string, string)>
    {
        ("Corrosive Concoction", "No Resolve Mitigation, chance to Avoid Resolve loss or Resolve Aegis"),
        ("Shattered Shield", "Cannot have Resolve Aegis"),
        ("Sharpened Arrowhead", "Enemy Hits ignore your Resolve Mitigation"),
        ("Iron Manacles", "Cannot Avoid Resolve Loss from Enemy Hits"),
        ("Accursed Prism", "When you gain an Affliction, gain an additional random Minor Affliction"),
        ("Poisoned Water", "Gain a random Minor Affliction when you use a Fountain"),
        ("Glass Shard", "The next Boon you gain is converted into a random Minor Affliction"),
        ("Cutpurse", "You cannot gain Aureus coins"),
        ("Corrupted Lockpick", "Chests in rooms explode when opened"),
        ("Voodoo Doll", "100% more Resolve lost while Resolve is below 50%"),
        ("Phantom Illusion", "Every room grants a random Minor Affliction, Afflictions granted this way are removed on room completion"),
        ("Gargoyle Totem", "Guards are accompanied by a Gargoyle"),
        ("Purple Smoke", "Afflictions are unknown on the Sanctum Map"),
        ("Veiled Sight", "Rooms are unknown on the Sanctum Map"),
        ("Red Smoke", "Room types are unknown on the Sanctum Map"),
        ("Golden Smoke", "Rewards are unknown on the Sanctum Map"),
        ("Blunt Sword", "You and your Minions deal 25% less Damage"),
        ("Charred Coin", "50% less Aureus coins found"),
        ("Deadly Snare", "Traps impact infinite Resolve"),
        ("Spiked Exit", "Lose 5% of current Resolve on room completion"),
        ("Floor Tax", "Lose all Aureus on floor completion"),
        ("Door Tax", "Lose 30 Aureus coins on room completion"),
        ("Spilt Purse", "Lose 20 Aureus coins when you lose Resolve from a Hit"),
        ("Liquid Cowardice", "Lose 10 Resolve when you use a Flask"),
        ("Tight Choker", "You can have a maximum of 5 Boons"),
        ("Unhallowed Ring", "50% increased Merchant prices"),
        ("Unhallowed Amulet", "The Merchant offers 50% fewer choices"),
        ("Rusted Coin", "The Merchant only offers one choice"),
        ("Honed Claws", "Monsters deal 25% more Damage"),
        ("Spiked Shell", "Monsters have 30% increased Maximum Life"),
        ("Chiselled Stone", "Monsters Petrify on Hit"),
        ("Hungry Fangs", "Monsters impact 25% increased Resolve"),
        ("Chains of Binding", "Monsters inflict Binding Chains on Hit"),
        ("Rusted Mallet", "Monsters always Knockback, Monsters have increased Knockback Distance"),
        ("Fiendish Wings", "Monsters' Action Speed cannot be slowed below base, Monsters have 30% increased Attack, Cast and Movement Speed"),
        ("Mark of Terror", "Monsters inflict Resolve Weakness on Hit"),
        ("Concealed Anomaly", "Guards release a Volatile Anomaly on Death"),
        ("Empty Trove", "Chests no longer drop Aureus coins"),
        ("Death Toll", "Monsters no longer drop Aureus coins"),
        ("Tattered Blindfold", "90% reduced Light Radius, Minimap is hidden"),
        ("Haemorrhage", "You cannot recover Resolve (removed after killing the next Floor Boss)"),
        ("Demonic Skull", "Cannot recover Resolve"),
        ("Unassuming Brick", "You cannot gain any more Boons"),
        ("Unholy Urn", "50% reduced Effect of your Relics"),
        ("Weakened Flesh", "-100 to Maximum Resolve"),
        ("Worn Sandals", "40% reduced Movement Speed"),
        ("Orb of Negation", "Relics have no Effect"),
        ("Ghastly Scythe", "Losing Resolve ends your Sanctum"),
        ("Unquenched Thirst", "50% reduced Resolve recovered"),
        ("Dark Pit", "Traps impact 100% increased Resolve"),
        ("Rapid Quicksand", "Traps are faster"),
        ("Anomaly Attractor", "Rooms spawn Volatile Anomalies"),
        ("Black Smoke", "You can see one fewer room ahead on the Sanctum Map"),
        ("Deceptive Mirror", "You are not always taken to the room you select"),
    };

    public BetterSanctumSettings()
    {
        var currencyFilter = "";
        var roomFilter = "";
        var afflictionFilter = "";
        var renameBuffer = "";
        string renameBufferOwner = null;
        TieringNode = new CustomNode
        {
            DrawDelegate = () =>
            {
                var (profileName, profile) = GetCurrentProfile();

                // The rename box edits a buffer that survives across frames and is only
                // written back on Enter. Committing every keystroke renamed the profile
                // out from under the widget, after which each further keystroke consumed
                // whichever profile had become current.
                if (renameBufferOwner != profileName)
                {
                    renameBufferOwner = profileName;
                    renameBuffer = profileName;
                }

                foreach (var key in Profiles.Keys.OrderBy(x => x).ToList())
                {
                    if (key == profileName)
                    {
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.DarkGreen.ToImgui());
                        if (ImGui.InputText("Current profile (Enter to rename)", ref renameBuffer, 200, ImGuiInputTextFlags.EnterReturnsTrue))
                        {
                            RenameProfile(profileName, renameBuffer);
                            renameBufferOwner = null;
                        }

                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        if (ImGui.Button($"Activate profile {key}##profile"))
                        {
                            CurrentProfile = key;
                        }
                    }
                }

                if (ImGui.Button("Add profile##addProfile"))
                {
                    var newProfileName = Enumerable.Range(0, 100).Select(x => $"New profile {x}").First(x => !Profiles.ContainsKey(x));
                    Profiles[newProfileName] = ProfileContent.CreateNew();
                    CurrentProfile = newProfileName;
                }

                // Deleting the last profile would leave nothing to fall back to
                if (Profiles.Count > 1)
                {
                    ImGui.SameLine();
                    if (ImGui.Button($"Delete profile {profileName}##deleteProfile"))
                    {
                        Profiles.Remove(profileName);
                        CurrentProfile = Profiles.Keys.First();
                    }
                }


                // Part of the profile rather than a display preference: both follow the
                // run strategy, and the tier cutoff is meaningless without the tiers it
                // is counted against.
                var duplicateRun = profile.DuplicateRun;
                if (ImGui.Checkbox("Duplicate run", ref duplicateRun))
                {
                    profile.DuplicateRun = duplicateRun;
                }

                var hideCurrencyBelowTier = profile.HideCurrencyBelowTier;
                if (ImGui.SliderInt("Hide currency below tier", ref hideCurrencyBelowTier, PrioritizeValue, BlockValue))
                {
                    profile.HideCurrencyBelowTier = hideCurrencyBelowTier;
                }

                ImGui.TextDisabled("0 = always route through, 1-3 = good, 4 = neutral, 5-6 = bad, 7 = never route through");

                if (ImGui.TreeNode("Currency tiering"))
                {
                    ImGui.InputTextWithHint("##CurrencyFilter", "Filter", ref currencyFilter, 100);
                    var (currencyTypes, fromGameFiles) = GetKnownCurrencyTypes();
                    ImGui.TextDisabled($"{currencyTypes.Count} currencies ({(fromGameFiles ? "from game files" : "fallback list")})");
                    foreach (var type in currencyTypes)
                    {
                        for (int order = 0; order < 3; order++)
                        {
                            // Filter against the full label so the slot words are searchable too
                            var label = $"{type} ({order switch { 0 => "first", 1 => "second", 2 => "third" }})";
                            if (!MatchesFilter(label, currencyFilter))
                            {
                                continue;
                            }

                            var currentValue = GetCurrencyTier(type, order);
                            if (ImGui.SliderInt(label, ref currentValue, PrioritizeValue, BlockValue))
                            {
                                profile.CurrencyTiers[$"{type}/{order}"] = currentValue;
                            }
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Room tiering"))
                {
                    ImGui.InputTextWithHint("##RoomFilter", "Filter", ref roomFilter, 100);
                    foreach (var type in RoomTypes.Where(t => t.Contains(roomFilter, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var currentValue = GetRoomTier(type);
                        if (ImGui.SliderInt(type, ref currentValue, PrioritizeValue, BlockValue))
                        {
                            profile.RoomTiers[type] = currentValue;
                        }
                    }

                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Affliction tiering"))
                {
                    ImGui.InputTextWithHint("##AfflictionFilter", "Filter", ref afflictionFilter, 100);
                    // Name and description are searched as one string, so terms can span both
                    foreach (var (type, description) in AfflictionTypes.Where(t => MatchesFilter($"{t.Item1} {t.Item2}", afflictionFilter)))
                    {
                        var currentValue = GetAfflictionTier(type);
                        if (ImGui.SliderInt(type, ref currentValue, PrioritizeValue, BlockValue))
                        {
                            profile.AfflictionTiers[type] = currentValue;
                        }

                        ImGui.SameLine();
                        ImGui.TextDisabled("(?)");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(description);
                        }
                    }

                    ImGui.TreePop();
                }
            }
        };
    }

    // The set of reward currencies is defined by the game's SanctumDeferredRewardCategory table,
    // which is also what room rewards report as their CurrencyName. Reading it here keeps the
    // tiering keys in sync with the lookup keys automatically. The static list below is only a
    // fallback for when the settings are drawn before the game files are loaded.
    // Space-separated terms, all of which must match, so "chaos second" narrows to one slot.
    private static bool MatchesFilter(string text, string filter)
    {
        return filter.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(term => text.Contains(term, StringComparison.InvariantCultureIgnoreCase));
    }

    private (IReadOnlyList<string> Types, bool FromGameFiles) GetKnownCurrencyTypes()
    {
        if (RemoteMemoryObject.pTheGame?.Files?.SanctumDeferredRewardCategories?.EntriesList is { Count: > 0 } entries)
        {
            var liveTypes = entries
                .Select(x => x.CurrencyName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            if (liveTypes.Count > 0)
            {
                return (liveTypes, true);
            }
        }

        return (CurrencyTypes, false);
    }

    // Silently ignores names that would collide with or erase another profile:
    // the old code assigned before removing, so renaming onto an existing name
    // overwrote that profile's contents.
    private void RenameProfile(string oldName, string newName)
    {
        newName = newName?.Trim();
        if (string.IsNullOrEmpty(newName) || newName == oldName || Profiles.ContainsKey(newName))
        {
            return;
        }

        if (!Profiles.Remove(oldName, out var content))
        {
            return;
        }

        Profiles[newName] = content;
        CurrentProfile = newName;
    }

    // Values run 0-7 with 4 neutral. The ends are constraints rather than weights:
    // 0 means route through this if at all possible, 7 means do not route through it.
    // 1-3 and 5-6 are ordinary positive and negative weights.
    public const int PrioritizeValue = 0;
    public const int NeutralValue = 4;
    public const int BlockValue = 7;
    public const int CurrentScaleVersion = 2;

    // 1 => +3 ... 6 => -2. The constraint values carry no weight of their own.
    public static int ScoreOf(int value)
    {
        return value is PrioritizeValue or BlockValue ? 0 : NeutralValue - value;
    }

    // The old scales were 1-5 for currency and 1-3 for rooms and afflictions, both with
    // 1 best. Nothing is mapped onto 0 or 7 - promoting a room to "never enter" is a
    // decision to make deliberately, not one to inherit from a rescale.
    private static int MigrateTriTier(int value) => value switch { 1 => 2, 2 => 4, 3 => 6, _ => NeutralValue };

    private static int MigrateCurrencyTier(int value) => value switch { 1 => 1, 2 => 2, 3 => 4, 4 => 5, 5 => 6, _ => NeutralValue };

    private static void MigrateProfile(ProfileContent profile)
    {
        if (profile.ScaleVersion >= CurrentScaleVersion)
        {
            return;
        }

        profile.CurrencyTiers = profile.CurrencyTiers.ToDictionary(x => x.Key, x => MigrateCurrencyTier(x.Value));
        profile.RoomTiers = profile.RoomTiers.ToDictionary(x => x.Key, x => MigrateTriTier(x.Value));
        profile.AfflictionTiers = profile.AfflictionTiers.ToDictionary(x => x.Key, x => MigrateTriTier(x.Value));
        // 5 was the old maximum and meant "hide nothing", which is 7 on the new scale
        profile.HideCurrencyBelowTier = profile.HideCurrencyBelowTier >= 5 ? BlockValue : MigrateCurrencyTier(profile.HideCurrencyBelowTier);
        profile.ScaleVersion = CurrentScaleVersion;
    }

    private (string profileName, ProfileContent profile) GetCurrentProfile()
    {
        var profileName = CurrentProfile != null && Profiles.ContainsKey(CurrentProfile) ? CurrentProfile : Profiles.Keys.FirstOrDefault() ?? "Default";
        if (!Profiles.ContainsKey(profileName))
        {
            Profiles[profileName] = ProfileContent.CreateNew();
        }

        // Pin it, so a null or stale CurrentProfile cannot drift to a different profile later
        CurrentProfile = profileName;

        MigrateProfile(Profiles[profileName]);

        var profile = Profiles[profileName];
        return (profileName, profile);
    }

    public int GetRoomTier(string type)
    {
        return GetCurrentProfile().profile.RoomTiers.GetValueOrDefault(type ?? "", NeutralValue);
    }

    public int GetCurrencyTier(string type, int order)
    {
        var currencyTiers = GetCurrentProfile().profile.CurrencyTiers;
        return currencyTiers.TryGetValue($"{type ?? ""}/{order}", out var tier) ||
               currencyTiers.TryGetValue(type ?? "", out tier)
            ? tier
            : NeutralValue;
    }

    // Read off the active profile, so the plugin keeps reading Settings.X unchanged.
    // JsonIgnore, or Newtonsoft would write these back out alongside the profiles.
    [JsonIgnore]
    public bool DuplicateRun => GetCurrentProfile().profile.DuplicateRun;

    [JsonIgnore]
    public int HideCurrencyBelowTier => GetCurrentProfile().profile.HideCurrencyBelowTier;

    public int GetAfflictionTier(string type)
    {
        return GetCurrentProfile().profile.AfflictionTiers.GetValueOrDefault(type ?? "", NeutralValue);
    }



    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public ColorNode TextColor { get; set; } = new ColorNode(Color.White);
    public ColorNode BackgroundColor { get; set; } = new ColorNode(Color.Black with { A = 128 });
    public ToggleNode ShowEffectId { get; set; } = new ToggleNode(false);
    public ToggleNode ShowEffectName { get; set; } = new ToggleNode(true);
    public ToggleNode ShowEffectDescription { get; set; } = new ToggleNode(true);

    // One ramp shared by every axis, so a value reads the same wherever it appears
    public ColorNode Tier0Color { get; set; } = new(Color.Magenta);
    public ColorNode Tier1Color { get; set; } = new(Color.Lime);
    public ColorNode Tier2Color { get; set; } = new(Color.GreenYellow);
    public ColorNode Tier3Color { get; set; } = new(Color.PaleGreen);
    public ColorNode Tier4Color { get; set; } = new(Color.White);
    public ColorNode Tier5Color { get; set; } = new(Color.Orange);
    public ColorNode Tier6Color { get; set; } = new(Color.OrangeRed);
    public ColorNode Tier7Color { get; set; } = new(Color.Red);
    public ColorNode EmptyColor { get; set; } = new(Color.Gray);

    public RangeNode<int> ConnectionLineThickness { get; set; } = new RangeNode<int>(5, 0, 10);

    public ToggleNode EnablePathfinding { get; set; } = new ToggleNode(true);
    public ToggleNode DrawConnectionLinesOnBestPathOnly { get; set; } = new ToggleNode(false);
    public ColorNode BestPathColor { get; set; } = new(Color.Cyan);
    public RangeNode<int> BestPathFrameThickness { get; set; } = new RangeNode<int>(4, 0, 10);

    // All default to no-ops: the per-slot values decide routes until you change these
    public RangeNode<int> CurrencyWeightMultiplier { get; set; } = new RangeNode<int>(1, 0, 10);
    public RangeNode<int> RoomWeightMultiplier { get; set; } = new RangeNode<int>(1, 0, 10);
    public RangeNode<int> AfflictionWeightMultiplier { get; set; } = new RangeNode<int>(1, 0, 10);
    public RangeNode<int> ThirdSlotBonus { get; set; } = new RangeNode<int>(0, 0, 5);

    public Dictionary<string, ProfileContent> Profiles = new Dictionary<string, ProfileContent>
    {
        ["Default"] = ProfileContent.CreateNew()
    };

    public string CurrentProfile;

    [JsonIgnore]
    public CustomNode TieringNode { get; set; }

}

public class ProfileContent
{
    // Profiles written before the 0-7 scale have no ScaleVersion, so Newtonsoft leaves
    // this at 1 and MigrateProfile knows to remap them. Code-created profiles are stamped
    // current by CreateNew.
    public int ScaleVersion = 1;

    public bool DuplicateRun = false;
    public int HideCurrencyBelowTier = 7;

    public Dictionary<string, int> CurrencyTiers = new()
    {
        ["Mirrors of Kalandra"] = 1,
        ["Divine Orbs"] = 1,
        ["Chaos Orbs"] = 2,
        ["Stacked Decks"] = 2,
        ["Veiled Chaos Orbs"] = 2,
        ["Orbs of Annulment"] = 2,
        ["Exalted Orbs"] = 2,
    };

    public Dictionary<string, int> RoomTiers = new()
    {
        ["Explore"] = 2,
        ["Merchant"] = 2,
        ["CurseFountain"] = 6,
        ["Arena"] = 6,
    };

    public Dictionary<string, int> AfflictionTiers = new()
    {
        ["Accursed Prism"] = 6,
        ["Poisoned Water"] = 6,
        ["Cutpurse"] = 6,
        ["Purple Smoke"] = 6,
        ["Veiled Sight"] = 6,
        ["Red Smoke"] = 6,
        ["Golden Smoke"] = 6,
        ["Deadly Snare"] = 6,
        ["Floor Tax"] = 6,
        ["Liquid Cowardice"] = 6,
        ["Unhallowed Amulet"] = 6,
        ["Rusted Coin"] = 6,
        ["Chiselled Stone"] = 6,
        ["Fiendish Wings"] = 6,
        ["Empty Trove"] = 6,
        ["Demonic Skull"] = 6,
        ["Unassuming Brick"] = 6,
        ["Worn Sandals"] = 6,
        ["Ghastly Scythe"] = 6,
        ["Rapid Quicksand"] = 6,
        ["Black Smoke"] = 6,
        ["Deceptive Mirror"] = 6,
        ["Tattered Blindfold"] = 2,
    };

    public static ProfileContent CreateNew()
    {
        return new ProfileContent { ScaleVersion = BetterSanctumSettings.CurrentScaleVersion };
    }
}
