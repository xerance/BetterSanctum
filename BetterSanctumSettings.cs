using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory;
using ExileCore.Shared.Attributes;
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

    // JsonIgnore matters here: Newtonsoft appends to an existing collection rather than
    // replacing it, so a serialised copy grew by five entries every time settings loaded.
    [JsonIgnore]
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

                Hint("A profile holds the tier values, the run type and the currency cutoff. Colours and display settings are shared across all profiles.");

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
                var runType = profile.RunType;
                if (ImGui.Combo("Run type", ref runType, RunTypeNames, RunTypeNames.Length))
                {
                    profile.RunType = runType;
                }

                Hint("Both relics duplicate the final reward, so either marks the offers not worth taking." +
                     "\n\nHour of Divinity blocks boons: BoonFountain drops to neutral and the early Treasure and Merchant bias is dropped." +
                     "\nGilded Chalice blocks resolve recovery: Fountain drops to neutral. CurseFountain is never adjusted.");

                var hideCurrencyBelowTier = profile.HideCurrencyBelowTier;
                if (ImGui.SliderInt("Hide currency below tier", ref hideCurrencyBelowTier, PrioritizeValue, BlockValue))
                {
                    profile.HideCurrencyBelowTier = hideCurrencyBelowTier;
                }

                Hint("Currencies rated worse than this are left out of the room text on the map. It does not affect routing.");

                ImGui.TextDisabled("0 = always route through, 1-3 = good, 4 = neutral, 5-7 = bad (7 heavily so), 8 = never route through");
                Hint("Below 4 adds to a route, above 4 subtracts, and the further from 4 the more it counts." +
                     "\nWeights: 1 = +100, 2 = +10, 3 = +5, 5 = -5, 6 = -10, 7 = -120." +
                     "\n0 and 8 are absolute: a 0 is always routed to, an 8 never is unless a 0 lies beyond it.");

                if (ImGui.TreeNode("Currency tiering"))
                {
                    ImGui.TextDisabled("Rated per reward slot. Only the best slot of a room counts, and the third slot counts twice since it pays double.");
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
                    ImGui.TextDisabled("Applies to both the fight room and the reward room, so a room is counted twice from this one list.");
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
                    ImGui.TextDisabled("Filter matches names and descriptions, and several words all have to match.");
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
    public const int BlockValue = 8;
    // Bare names apply to every reward slot; a "name/slot" key overrides one slot.
    public static readonly IReadOnlyDictionary<string, int> DefaultCurrencyTiers = new Dictionary<string, int>
    {
        ["Mirrors of Kalandra"] = PrioritizeValue,
        ["Divine Orbs"] = 1,
        ["Fracturing Orbs"] = 1,
        ["Volatile Vaal Orbs"] = 1,
        ["Chaos Orbs"] = 2,
        ["Stacked Decks"] = 2,
        ["Veiled Chaos Orbs"] = 2,
        ["Orbs of Annulment"] = 2,
        ["Exalted Orbs"] = 2,
    };

    // Fight rooms are graded on how much resolve they tend to cost, reward rooms on what
    // they hand you. Boss and Final sit at neutral deliberately: they are in the last
    // layer, which every route passes through, so their value cannot separate two routes.
    // Deferral is mildly positive rather than neutral - it is the only room type that
    // carries currency, and the currency itself is counted separately.
    public static readonly IReadOnlyDictionary<string, int> DefaultRoomTiers = new Dictionary<string, int>
    {
        // Fight rooms
        ["Explore"] = 3,
        ["Maze"] = 3,
        ["Puzzle"] = 3,
        ["Gauntlet"] = 3,
        ["Lair"] = 4,
        ["Vault"] = 4,
        ["Boss"] = 4,
        ["Miniboss"] = 5,
        ["Arena"] = 6,

        // Reward rooms
        ["Merchant"] = 2,
        ["BoonFountain"] = 2,
        ["RainbowFountain"] = 2,
        ["Deferral"] = 3,
        ["Fountain"] = 3,
        ["Treasure"] = 3,
        ["TreasureMinor"] = 4,
        ["Deal"] = 4,
        ["Final"] = 4,
        ["CurseFountain"] = 6,
    };

    public const int RunTypeNormal = 0;
    public const int RunTypeHourOfDivinity = 1;
    public const int RunTypeGildedChalice = 2;

    public static readonly string[] RunTypeNames = { "Normal", "The Hour of Divinity", "The Gilded Chalice" };

    // Floors are identified by the prefix on their room ids; the area name does not
    // track the floor. Nave and Crypt are the last two in some order, which no rule
    // distinguishes, so their relative order does not matter.
    private static readonly Dictionary<string, int> FloorsByRoomPrefix = new()
    {
        ["Cellar"] = 1,
        ["Vaults"] = 2,
        ["Nave"] = 3,
        ["Crypt"] = 4,
    };

    public static int GetFloorForRoomPrefix(string prefix)
    {
        return prefix != null && FloorsByRoomPrefix.TryGetValue(prefix, out var floor) ? floor : 0;
    }

    public const int CurrentScaleVersion = 6;

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
        if (profile.ScaleVersion < 3 && profile.DuplicateRun)
        {
            // The old checkbox meant Hour of Divinity specifically
            profile.RunType = RunTypeHourOfDivinity;
        }

        if (profile.ScaleVersion < 4)
        {
            // Seed only the room types the profile never had an entry for. Upstream set
            // four and left the rest implicitly neutral, which was minimalism rather than
            // a judgement, so filling them in does not overwrite anything you chose.
            foreach (var (roomType, tier) in DefaultRoomTiers)
            {
                if (!profile.RoomTiers.ContainsKey(roomType))
                {
                    profile.RoomTiers[roomType] = tier;
                }
            }
        }

        if (profile.ScaleVersion < 5 && profile.HideCurrencyBelowTier == 7)
        {
            // 7 was the top of the scale and meant "hide nothing" until 8 was added
            profile.HideCurrencyBelowTier = BlockValue;
        }

        if (profile.ScaleVersion < 6)
        {
            foreach (var (currency, tier) in DefaultCurrencyTiers)
            {
                if (!profile.CurrencyTiers.ContainsKey(currency))
                {
                    profile.CurrencyTiers[currency] = tier;
                }
            }

            // Only lift entries still holding the value they were shipped with, so a
            // rating you actually chose is never overwritten by a change of default.
            if (profile.CurrencyTiers.GetValueOrDefault("Mirrors of Kalandra") == 1)
            {
                profile.CurrencyTiers["Mirrors of Kalandra"] = PrioritizeValue;
            }

            if (profile.RoomTiers.GetValueOrDefault("Explore") == 2)
            {
                profile.RoomTiers["Explore"] = 3;
            }
        }

        profile.ScaleVersion = CurrentScaleVersion;
    }

    // Hover marker after the control it explains
    private static void Hint(string text)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
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
    public bool DuplicateRun => GetCurrentProfile().profile.RunType != RunTypeNormal;

    [JsonIgnore]
    public int RunType => GetCurrentProfile().profile.RunType;

    [JsonIgnore]
    public int HideCurrencyBelowTier => GetCurrentProfile().profile.HideCurrencyBelowTier;

    public int GetAfflictionTier(string type)
    {
        return GetCurrentProfile().profile.AfflictionTiers.GetValueOrDefault(type ?? "", NeutralValue);
    }



    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    public RoutingSettings Routing { get; set; } = new RoutingSettings();
    public MapDisplaySettings MapDisplay { get; set; } = new MapDisplaySettings();
    public TierColorSettings TierColors { get; set; } = new TierColorSettings();
    public InRoomSettings InRoom { get; set; } = new InRoomSettings();
    public DebugSettings Debug { get; set; } = new DebugSettings();

    public Dictionary<string, ProfileContent> Profiles = new Dictionary<string, ProfileContent>
    {
        ["Normal run"] = ProfileContent.CreateNormalRunProfile(),
        ["Hour of Divinity dupe run"] = ProfileContent.CreateHourOfDivinityProfile(),
    };

    public string CurrentProfile = "Normal run";

    [JsonIgnore]
    public CustomNode TieringNode { get; set; }

}

public class ProfileContent
{
    // Profiles written before the 0-7 scale have no ScaleVersion, so Newtonsoft leaves
    // this at 1 and MigrateProfile knows to remap them. Code-created profiles are stamped
    // current by CreateNew.
    public int ScaleVersion = 1;

    public int RunType = BetterSanctumSettings.RunTypeNormal;

    // Superseded by RunType. Read once by MigrateProfile, unused after.
    public bool DuplicateRun = false;
    public int HideCurrencyBelowTier = BetterSanctumSettings.BlockValue;

    public Dictionary<string, int> CurrencyTiers = new(BetterSanctumSettings.DefaultCurrencyTiers);

    public Dictionary<string, int> RoomTiers = new(BetterSanctumSettings.DefaultRoomTiers);

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

    public static ProfileContent CreateNormalRunProfile()
    {
        return new ProfileContent
        {
            ScaleVersion = BetterSanctumSettings.CurrentScaleVersion,
            RunType = 0,
            HideCurrencyBelowTier = 3,
            CurrencyTiers = new Dictionary<string, int>
            {
                ["Mirrors of Kalandra"] = 0,
                ["Divine Orbs"] = 1,
                ["Fracturing Orbs"] = 1,
                ["Volatile Vaal Orbs"] = 1,
                ["Chaos Orbs"] = 2,
                ["Stacked Decks"] = 2,
                ["Veiled Chaos Orbs"] = 2,
                ["Orbs of Annulment"] = 2,
                ["Exalted Orbs"] = 2,
                ["Volatile Vaal Orbs/0"] = 1,
                ["Volatile Vaal Orbs/1"] = 1,
                ["Volatile Vaal Orbs/2"] = 1,
                ["Fracturing Orbs/2"] = 1,
                ["Fracturing Orbs/1"] = 1,
                ["Fracturing Orbs/0"] = 1,
                ["Sacred Orbs/1"] = 2,
                ["Sacred Orbs/2"] = 2,
                ["Sacred Orbs/0"] = 2,
                ["Exalted Orbs/2"] = 3,
                ["Exalted Orbs/0"] = 3,
                ["Orbs of Annulment/0"] = 4,
                ["Orbs of Annulment/2"] = 3,
                ["Chaos Orbs/2"] = 3,
                ["Chaos Orbs/0"] = 3,
                ["Chaos Orbs/1"] = 3,
                ["Gemcutter's Prisms/0"] = 3,
                ["Gemcutter's Prisms/1"] = 3,
                ["Gemcutter's Prisms/2"] = 3,
                ["Stacked Decks/0"] = 4,
                ["Stacked Decks/1"] = 4,
                ["Stacked Decks/2"] = 4,
                ["Orbs of Annulment/1"] = 3,
                ["Divine Orbs/0"] = 1,
                ["Exalted Orbs/1"] = 3,
                ["Ancient Orbs/0"] = 3,
                ["Ancient Orbs/1"] = 3,
                ["Ancient Orbs/2"] = 3,
            },
            RoomTiers = new Dictionary<string, int>
            {
                ["Explore"] = 3,
                ["Maze"] = 4,
                ["Puzzle"] = 4,
                ["Gauntlet"] = 4,
                ["Lair"] = 4,
                ["Vault"] = 4,
                ["Boss"] = 4,
                ["Miniboss"] = 4,
                ["Arena"] = 6,
                ["Merchant"] = 3,
                ["BoonFountain"] = 3,
                ["RainbowFountain"] = 3,
                ["Deferral"] = 3,
                ["Fountain"] = 4,
                ["Treasure"] = 3,
                ["TreasureMinor"] = 4,
                ["Deal"] = 3,
                ["Final"] = 4,
                ["CurseFountain"] = 4,
            },
            AfflictionTiers = new Dictionary<string, int>
            {
                ["Accursed Prism"] = 7,
                ["Poisoned Water"] = 7,
                ["Cutpurse"] = 6,
                ["Purple Smoke"] = 7,
                ["Veiled Sight"] = 6,
                ["Red Smoke"] = 6,
                ["Golden Smoke"] = 8,
                ["Deadly Snare"] = 8,
                ["Floor Tax"] = 6,
                ["Liquid Cowardice"] = 7,
                ["Unhallowed Amulet"] = 6,
                ["Rusted Coin"] = 7,
                ["Chiselled Stone"] = 6,
                ["Fiendish Wings"] = 6,
                ["Empty Trove"] = 6,
                ["Demonic Skull"] = 7,
                ["Unassuming Brick"] = 7,
                ["Worn Sandals"] = 5,
                ["Ghastly Scythe"] = 8,
                ["Rapid Quicksand"] = 6,
                ["Black Smoke"] = 6,
                ["Deceptive Mirror"] = 7,
                ["Tattered Blindfold"] = 6,
                ["Unhallowed Ring"] = 6,
                ["Blunt Sword"] = 5,
                ["Charred Coin"] = 6,
                ["Door Tax"] = 6,
                ["Spilt Purse"] = 6,
                ["Tight Choker"] = 5,
                ["Concealed Anomaly"] = 5,
                ["Death Toll"] = 6,
                ["Haemorrhage"] = 5,
                ["Unholy Urn"] = 6,
                ["Orb of Negation"] = 7,
                ["Anomaly Attractor"] = 5,
                ["Glass Shard"] = 7,
                ["Phantom Illusion"] = 5,
                ["Honed Claws"] = 5,
                ["Spiked Shell"] = 6,
                ["Chains of Binding"] = 5,
                ["Mark of Terror"] = 5,
            },
        };
    }

    public static ProfileContent CreateHourOfDivinityProfile()
    {
        return new ProfileContent
        {
            ScaleVersion = BetterSanctumSettings.CurrentScaleVersion,
            RunType = 1,
            HideCurrencyBelowTier = 3,
            CurrencyTiers = new Dictionary<string, int>
            {
                ["Mirrors of Kalandra"] = 0,
                ["Divine Orbs"] = 1,
                ["Fracturing Orbs"] = 1,
                ["Volatile Vaal Orbs"] = 1,
                ["Chaos Orbs"] = 2,
                ["Stacked Decks"] = 2,
                ["Veiled Chaos Orbs"] = 2,
                ["Orbs of Annulment"] = 2,
                ["Exalted Orbs"] = 2,
                ["Chaos Orbs/0"] = 4,
                ["Chaos Orbs/1"] = 4,
                ["Chaos Orbs/2"] = 4,
                ["Orbs of Annulment/0"] = 4,
                ["Orbs of Annulment/1"] = 4,
                ["Orbs of Annulment/2"] = 4,
                ["Gemcutter's Prisms/1"] = 4,
                ["Gemcutter's Prisms/0"] = 4,
                ["Exalted Orbs/0"] = 4,
                ["Exalted Orbs/1"] = 4,
                ["Exalted Orbs/2"] = 4,
                ["Orbs of Augmentation/2"] = 4,
                ["Ancient Orbs/0"] = 4,
                ["Ancient Orbs/1"] = 4,
                ["Stacked Decks/0"] = 4,
                ["Stacked Decks/1"] = 4,
                ["Stacked Decks/2"] = 4,
            },
            RoomTiers = new Dictionary<string, int>
            {
                ["Explore"] = 3,
                ["Maze"] = 3,
                ["Puzzle"] = 3,
                ["Gauntlet"] = 3,
                ["Lair"] = 4,
                ["Vault"] = 4,
                ["Boss"] = 4,
                ["Miniboss"] = 4,
                ["Arena"] = 6,
                ["Merchant"] = 4,
                ["BoonFountain"] = 4,
                ["RainbowFountain"] = 3,
                ["Deferral"] = 3,
                ["Fountain"] = 3,
                ["Treasure"] = 4,
                ["TreasureMinor"] = 4,
                ["Deal"] = 3,
                ["Final"] = 4,
                ["CurseFountain"] = 4,
            },
            AfflictionTiers = new Dictionary<string, int>
            {
                ["Accursed Prism"] = 7,
                ["Poisoned Water"] = 6,
                ["Cutpurse"] = 6,
                ["Purple Smoke"] = 7,
                ["Veiled Sight"] = 6,
                ["Red Smoke"] = 6,
                ["Golden Smoke"] = 7,
                ["Deadly Snare"] = 6,
                ["Floor Tax"] = 5,
                ["Liquid Cowardice"] = 7,
                ["Unhallowed Amulet"] = 4,
                ["Rusted Coin"] = 4,
                ["Chiselled Stone"] = 6,
                ["Fiendish Wings"] = 6,
                ["Empty Trove"] = 5,
                ["Demonic Skull"] = 6,
                ["Unassuming Brick"] = 4,
                ["Worn Sandals"] = 6,
                ["Ghastly Scythe"] = 8,
                ["Rapid Quicksand"] = 6,
                ["Black Smoke"] = 5,
                ["Deceptive Mirror"] = 7,
                ["Tattered Blindfold"] = 4,
                ["Death Toll"] = 5,
                ["Spilt Purse"] = 5,
                ["Door Tax"] = 5,
                ["Charred Coin"] = 5,
                ["Phantom Illusion"] = 5,
                ["Unholy Urn"] = 7,
                ["Orb of Negation"] = 8,
                ["Anomaly Attractor"] = 5,
                ["Blunt Sword"] = 5,
                ["Honed Claws"] = 5,
                ["Spiked Shell"] = 5,
                ["Iron Manacles"] = 4,
            },
        };
    }

    public static ProfileContent CreateNew()
    {
        return new ProfileContent { ScaleVersion = BetterSanctumSettings.CurrentScaleVersion };
    }
}

// A short description drawn at the top of a settings group. ExileCore renders the nodes
// themselves, so this is where the explanation of what a group does has to live.
public static class SettingsHelp
{
    public static CustomNode Block(params string[] lines)
    {
        return new CustomNode
        {
            DrawDelegate = () =>
            {
                foreach (var line in lines)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.ToImgui());
                    ImGui.TextWrapped(line);
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();
            }
        };
    }
}

[Submenu(CollapsedByDefault = false)]
public class RoutingSettings
{
    [JsonIgnore]
    public CustomNode Help { get; set; } = SettingsHelp.Block(
        "Picks one room per layer from where you stand to the boss and frames it.",
        "Rooms are counted by tier and weighted per axis, because the same tier means different things: a tier-1 reward is 100 while a tier-2 is only 3, so lesser rewards never outweigh a calmer route. A bad affliction is -70, so one tier-1 reward is worth one but not two. Room type sits between the two.",
        "Tier 0 is always routed to and tier 8 never is, unless a 0 lies beyond it.",
        "Bias strength scales the floor adjustments only, each point being one tier step: on floors 3-4 good currency improves and Deal gains 50 points, on floors 1-2 Treasure and Merchant improve unless you are running Hour of Divinity. Set it to 0 to score purely on the tiers you assigned.",
        "Relic adjustments are not scaled and apply at any strength: Hour of Divinity flattens BoonFountain to neutral, Gilded Chalice flattens Fountain.");

    public ToggleNode EnablePathfinding { get; set; } = new ToggleNode(true);
    public ColorNode BestPathColor { get; set; } = new(Color.Cyan);
    public RangeNode<int> BestPathFrameThickness { get; set; } = new RangeNode<int>(3, 0, 10);
    public RangeNode<int> BestPathLineThickness { get; set; } = new RangeNode<int>(4, 0, 10);
    // Scales the floor-based adjustments by one tier step per point. Relic nullification
    // is a constraint rather than a preference and is deliberately not scaled by it.
    // What a deal room is worth from floor 3, in the same units as the tier weights:
    // a tier-1 reward is 100, a bad affliction is -70.
    public RangeNode<int> DealValueLateFloors { get; set; } = new RangeNode<int>(80, 0, 200);

    public RangeNode<int> ContextBiasStrength { get; set; } = new RangeNode<int>(1, 0, 5);
}

[Submenu(CollapsedByDefault = true)]
public class MapDisplaySettings
{
    [JsonIgnore]
    public CustomNode Help { get; set; } = SettingsHelp.Block(
        "Text and connection lines drawn over the Sanctum floor map.",
        "Each connection carries three stacked lines - currency, room type, affliction - coloured by the best of that kind reachable through it. Set line thickness to 0 to hide them and leave only the route frame.",
        "Hide under game UI drops any text, frame or line that would be covered by an open panel or the chat box, the same way the overlay already gives way to a room tooltip.");

    public ColorNode TextColor { get; set; } = new ColorNode(Color.White);
    public ColorNode BackgroundColor { get; set; } = new ColorNode(Color.Black with { A = 128 });
    public RangeNode<int> ConnectionLineThickness { get; set; } = new RangeNode<int>(0, 0, 10);
    public ToggleNode HideUnderGameUi { get; set; } = new ToggleNode(true);
    public ToggleNode ShowEffectId { get; set; } = new ToggleNode(false);
    public ToggleNode ShowEffectName { get; set; } = new ToggleNode(true);
    public ToggleNode ShowEffectDescription { get; set; } = new ToggleNode(true);
}

[Submenu(CollapsedByDefault = true)]
public class TierColorSettings
{
    [JsonIgnore]
    public CustomNode Help { get; set; } = SettingsHelp.Block(
        "One ramp shared by currency, room types and afflictions, so a value reads the same wherever it appears.",
        "0 always route through, 1-3 good, 4 neutral, 5-7 bad, 8 never route through. Empty colours anything the map has not revealed.");

    public ColorNode Tier0Color { get; set; } = new(Color.Magenta);
    public ColorNode Tier1Color { get; set; } = new(Color.Cyan);
    public ColorNode Tier2Color { get; set; } = new(Color.GreenYellow);
    public ColorNode Tier3Color { get; set; } = new(Color.PaleGreen);
    public ColorNode Tier4Color { get; set; } = new(Color.White);
    public ColorNode Tier5Color { get; set; } = new(Color.Orange);
    public ColorNode Tier6Color { get; set; } = new(Color.OrangeRed);
    public ColorNode Tier7Color { get; set; } = new(Color.Red);
    public ColorNode Tier8Color { get; set; } = new(Color.DarkRed);
    public ColorNode EmptyColor { get; set; } = new(Color.Gray);
}

[Submenu(CollapsedByDefault = true)]
public class InRoomSettings
{
    [JsonIgnore]
    public CustomNode Help { get; set; } = SettingsHelp.Block(
        "Drawn in the room you are fighting in, rather than on the floor map.",
        "Spawners show lime while active and as a small marker while dormant. Hazards circle the meteor and holy beam telegraphs. Draw distance is in world units from your character.");

    public ToggleNode ShowGuardSpawners { get; set; } = new ToggleNode(true);
    public ToggleNode ShowHazards { get; set; } = new ToggleNode(true);
    public RangeNode<int> EffectDrawDistance { get; set; } = new RangeNode<int>(100, 20, 300);
    public ColorNode ActiveSpawnerColor { get; set; } = new(Color.Lime);
    public ColorNode DormantSpawnerColor { get; set; } = new(Color.LightBlue);
    public ColorNode HazardColor { get; set; } = new(Color.Red);
}

[Submenu(CollapsedByDefault = true)]
public class DebugSettings
{
    [JsonIgnore]
    public CustomNode Help { get; set; } = SettingsHelp.Block(
        "Writes room-dump.txt beside Loader.exe once each time the floor map is opened, listing the raw data behind every room.");

    public ToggleNode DebugDumpRoomData { get; set; } = new ToggleNode(false);
}
