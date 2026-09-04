using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Elements.Sanctum;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace BetterSanctum;

public class BetterSanctumPlugin : BaseSettingsPlugin<BetterSanctumSettings>
{
    private readonly Stopwatch _sinceLastReloadStopwatch = Stopwatch.StartNew();
    private Random rndColor = new Random();
    private bool _debugDumpPending = true;
    // Remembered from the floor map: the reward window can be open when the map is not,
    // and the area name follows the room you stand in rather than the floor.
    private string _lastKnownFloorPrefix;
    private List<RectangleF> _obstructions = new List<RectangleF>();
    private EffectHelper _effectHelper;

    public override bool Initialise()
    {
        _effectHelper = new EffectHelper(GameController, Graphics, Settings);
        return base.Initialise();
    }

    // Returns the size whether or not it draws, so a suppressed line still advances the
    // layout and the rest of the block stays where it belongs. Tested per line because a
    // room's text runs well below its own box and can reach a tooltip the box does not.
    private Vector2 DrawTextWithBackground(string text, Vector2 position, Color color, Color backgroundColor)
    {
        var textSize = Graphics.MeasureText(text);
        if (IsObstructed(_obstructions, new RectangleF(position.X, position.Y, textSize.X, textSize.Y)))
        {
            return textSize;
        }

        Graphics.DrawBox(position, textSize + position, backgroundColor);
        Graphics.DrawText(text, position, color);
        return textSize;
    }

    // The overlay already gave way to a room tooltip. Panels the game opens over the map
    // are the same problem, so they are collected here and treated identically.
    private List<RectangleF> CollectObstructions(RectangleF tooltipRect)
    {
        var obstructions = new List<RectangleF>();
        if (tooltipRect.Width > 0 && tooltipRect.Height > 0)
        {
            obstructions.Add(tooltipRect);
        }

        if (!Settings.MapDisplay.HideUnderGameUi)
        {
            return obstructions;
        }

        var ui = GameController.IngameState.IngameUi;
        // UIHover is deliberately absent: it is the room under the cursor as often as it
        // is a panel, and blanking the room you are pointing at helps nobody.
        foreach (var panel in new[] { ui.OpenLeftPanel, ui.OpenRightPanel, ui.ChatBox })
        {
            if (panel is not { IsVisible: true })
            {
                continue;
            }

            var rect = panel.GetClientRectCache;
            if (rect.Width > 0 && rect.Height > 0)
            {
                obstructions.Add(rect);
            }
        }

        return obstructions;
    }

    private static bool IsObstructed(List<RectangleF> obstructions, RectangleF rect)
    {
        foreach (var obstruction in obstructions)
        {
            if (obstruction.Intersects(rect))
            {
                return true;
            }
        }

        return false;
    }

    private void PreventLastOffer()
    {
        if (!GameController.IngameState.IngameUi.SanctumRewardWindow.IsVisible)
            return;

        var pathToOfferWindowRows = new int[] { 0, 1, 0, 1 };
        var sanctumOfferWindow = GameController.IngameState.IngameUi.SanctumRewardWindow.GetChildFromIndices(pathToOfferWindowRows);
        if (!sanctumOfferWindow.IsVisible)
        {
            sanctumOfferWindow = GameController.IngameState.IngameUi.SanctumRewardWindow.GetChildFromIndices(new int[] { 0, 1, 0, 2 });

            if (!sanctumOfferWindow.IsVisible)
            {
                return;
            }
        }
            

        var dupOffer = sanctumOfferWindow.Children.Where(x => Settings.CurrencyDuplicate.Any(y => x.Children[1].Text.Contains(y)));
        var noDupOffer = sanctumOfferWindow.Children.Where(x => Settings.CurrencyDuplicate.Any(y => !x.Children[1].Text.Contains(y)));
        var entitiesByType = GameController.EntityListWrapper.ValidEntitiesByType;
        var floorFinalChest = entitiesByType.TryGetValue(EntityType.Chest, out var chests)
            ? chests
            : Enumerable.Empty<Entity>();

        foreach (var offer in dupOffer)
        {
            Graphics.DrawFrame(offer.GetClientRect(), RandomUtil.NextColor(rndColor), 6);
        }

        foreach (var offer in noDupOffer.Where(x => !dupOffer.Contains(x)))
        {
            // The end-of-sanctum slot is never worth taking on a duplicate run, and on the
            // last floors the end-of-floor slot is not either. Keyed on the floor's room
            // prefix, since the area name tracks the room you stand in, not the floor.
            var crossOut = offer.IndexInParent == 2 ||
                           _lastKnownFloorPrefix == "Crypt" && offer.IndexInParent is 1 or 2 ||
                           _lastKnownFloorPrefix == "Nave" && offer.IndexInParent is 1 or 2 &&
                           floorFinalChest.FirstOrDefault(x => x.Metadata.Contains("FloorFinalRewardChest")) != null;
            if (!crossOut)
            {
                continue;
            }

            var rect = offer.Children[1].Parent.GetClientRect();
            Graphics.DrawLine(rect.TopLeft.ToVector2Num(), rect.BottomRight.ToVector2Num(), 4, Color.Red);
            Graphics.DrawLine(rect.TopRight.ToVector2Num(), rect.BottomLeft.ToVector2Num(), 4, Color.Red);
            Graphics.DrawFrame(rect, Color.Red, 4);
        }
    }

    public override void Render()
    {
        if (Settings.DuplicateRun)
        {
            PreventLastOffer();
        }
        
        // Only inside a Sanctum, and before the floor-map return below, since these draw
        // in the room rather than on the map
        if (GameController.Area.CurrentArea.Area.RawName.StartsWith("Sanctum"))
        {
            _effectHelper.DrawEffects();
        }

        var floorWindow = GameController.IngameState.IngameUi.SanctumFloorWindow;
        if (!floorWindow.IsVisible)
        {
            _debugDumpPending = true;
            return;
        }

        if (!GameController.Files.SanctumRooms.EntriesList.Any() && _sinceLastReloadStopwatch.Elapsed > TimeSpan.FromSeconds(5))
        {
            GameController.Files.LoadFiles();
            _sinceLastReloadStopwatch.Restart();
        }

        var hoveredRoom = floorWindow.Rooms.FirstOrDefault(x =>
            ImGui.IsMouseHoveringRect(x.GetClientRectCache.TopLeft.ToVector2Num(), x.GetClientRectCache.BottomRight.ToVector2Num(), false));
        var tooltipRect = RectangleF.Empty;
        if (hoveredRoom != null)
        {
            tooltipRect = hoveredRoom.Tooltip.GetClientRectCache;
        }

        _obstructions = CollectObstructions(tooltipRect);

        var tierMap = new Dictionary<(int, int), (List<int> CurrencyTier, int? RoomTier, int? AfflictionTier)>();
        var roomsByLayer = floorWindow.RoomsByLayer;

        foreach (var probeLayer in roomsByLayer)
        {
            foreach (var probeRoom in probeLayer)
            {
                var probeId = probeRoom.Data?.FightRoom?.Id ?? probeRoom.Data?.RewardRoom?.Id;
                if (probeId != null)
                {
                    _lastKnownFloorPrefix = probeId.Split('_')[0];
                    break;
                }
            }
        }

        if (Settings.Debug.DebugDumpRoomData && _debugDumpPending)
        {
            _debugDumpPending = false;
            // Next to Loader.exe rather than the plugin folder: DirectoryFullName is not
            // dependable for source-compiled plugins, and the HUD root is easy to find.
            var dumpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "room-dump.txt");
            try
            {
                // Room ids carry the floor name; the area name does not reliably
                var floorPrefix = "unknown";
                foreach (var probeLayer in roomsByLayer)
                {
                    foreach (var probeRoom in probeLayer)
                    {
                        var probeId = probeRoom.Data?.FightRoom?.Id ?? probeRoom.Data?.RewardRoom?.Id;
                        if (!string.IsNullOrEmpty(probeId))
                        {
                            floorPrefix = probeId.Split('_')[0];
                            break;
                        }
                    }

                    if (floorPrefix != "unknown")
                    {
                        break;
                    }
                }

                var lines = new List<string>
                {
                    $"{DateTime.Now:s} floorPrefix={floorPrefix} area={GameController.Area.CurrentArea.Area.RawName} layers={roomsByLayer.Count}",
                    "WINDOW " + string.Join(", ", DebugWindowMemberNames.Select(name => DescribeMember(floorWindow, name))),
                    "FLOORDATA " + string.Join(", ", DebugWindowMemberNames.Select(name => DescribeMember(floorWindow.FloorData, name))),
                };
                for (var layerIndex = 0; layerIndex < roomsByLayer.Count; layerIndex++)
                {
                    var roomLayer = roomsByLayer[layerIndex];
                    for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
                    {
                        var data = roomLayer[roomIndex].Data;
                        lines.Add($"L{layerIndex}R{roomIndex} " +
                                  string.Join(", ", DebugMemberNames.Select(name => DescribeMember(data, name))));
                    }
                }

                File.WriteAllLines(dumpPath, lines);
                LogMessage($"[BetterSanctum] wrote {lines.Count - 1} rooms to {dumpPath}", 30);
            }
            catch (Exception e)
            {
                LogError($"[BetterSanctum] could not write {dumpPath}: {e.Message}", 30);
            }
        }

        if (Settings.MapDisplay.ConnectionLineThickness > 0)
        {
            for (var layerIndex = roomsByLayer.Count - 2; layerIndex >= 0; layerIndex--)
            {
                var roomLayer = roomsByLayer[layerIndex];
                for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
                {
                    var room = roomLayer[roomIndex];
                    (List<int> CurrencyTier, int? RoomTier, int? AfflictionTier) thisRoomData = (
                        room.GetRoomsWithOrder().Select(x => Settings.GetCurrencyTier(x.room.CurrencyName, x.order)).ToList(),
                        room.Data.RewardRoom?.RoomType?.Id switch
                        {
                            null => null,
                            var o => Settings.GetRoomTier(o)
                        },
                        (room.Data.RewardRoom?.RoomType?.Id, room.Data.RoomEffect?.ReadableName) switch
                        {
                            (not null, null) => 1,
                            (null, _) => null,
                            (not null, { } o) => Settings.GetAfflictionTier(o)
                        }
                    );
                    var connections = floorWindow.FloorData.RoomLayout[layerIndex][roomIndex];
                    var connectedRoomData = connections.Select(x => tierMap.GetValueOrDefault((layerIndex + 1, x)))
                        .Where(x => x != default).ToList();
                    if (connectedRoomData.Any())
                    {
                        var aggregateConnectionData = connectedRoomData
                            .Aggregate((current, connectionData) => (
                                current.CurrencyTier.Union(connectionData.CurrencyTier).ToList(),
                                (current.RoomTier, connectionData.RoomTier) switch
                                {
                                    ({ } tier1, { } tier2) => Math.Min(tier1, tier2),
                                    var (tier1, tier2) => tier1 ?? tier2
                                },
                                (current.AfflictionTier, connectionData.AfflictionTier) switch
                                {
                                    ({ } tier1, { } tier2) => Math.Min(tier1, tier2),
                                    var (tier1, tier2) => tier1 ?? tier2
                                }));
                        thisRoomData = (
                            thisRoomData.CurrencyTier.Union(aggregateConnectionData.CurrencyTier).ToList(),
                            (thisRoomData.RoomTier, aggregateConnectionData.RoomTier) switch
                            {
                                ({ } tier1, { } tier2) => Math.Min(tier1, tier2),
                                var (tier1, tier2) => tier1 ?? tier2
                            },
                            (thisRoomData.AfflictionTier, aggregateConnectionData.AfflictionTier) switch
                            {
                                ({ } tier1, { } tier2) => Math.Max(tier1, tier2),
                                var (tier1, tier2) => tier1 ?? tier2
                            });
                    }

                    tierMap[(layerIndex, roomIndex)] = thisRoomData;
                }
            }
        }


        // Route planning. Sanctum floors are layered and you enter exactly one room per
        // layer, so every route holds the same number of rooms and their tier counts are
        // directly comparable. Routes are ranked by comparing those counts tier by tier
        // rather than by summing points, so two tier-1 rewards beat one tier-1 however
        // much middling filler sits behind it.
        var bestRoute = new HashSet<(int, int)>();
        var bestRouteOrder = new List<(int Layer, int Room)>();
        if (Settings.Routing.EnablePathfinding && Settings.Routing.BestPathFrameThickness > 0 && roomsByLayer.Count > 0)
        {
            var floor = BetterSanctumSettings.GetFloorForRoomPrefix(_lastKnownFloorPrefix);

            var routeValue = new Dictionary<(int, int), (int[] Counts, int Next)>();
            for (var layerIndex = roomsByLayer.Count - 1; layerIndex >= 0; layerIndex--)
            {
                var roomLayer = roomsByLayer[layerIndex];
                for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
                {
                    var own = EvaluateRoom(roomLayer[roomIndex], floor);
                    if (layerIndex == roomsByLayer.Count - 1)
                    {
                        routeValue[(layerIndex, roomIndex)] = (own, -1);
                        continue;
                    }

                    var next = -1;
                    int[] nextCounts = null;
                    foreach (var connection in floorWindow.FloorData.RoomLayout[layerIndex][roomIndex])
                    {
                        if (!routeValue.TryGetValue((layerIndex + 1, connection), out var candidate))
                        {
                            continue;
                        }

                        if (nextCounts == null || CompareRoutes(candidate.Counts, nextCounts) > 0)
                        {
                            next = connection;
                            nextCounts = candidate.Counts;
                        }
                    }

                    // Nothing onward exists, so this room leads nowhere
                    if (next < 0)
                    {
                        continue;
                    }

                    var total = new int[RouteValueSize];
                    for (var tier = 0; tier < RouteValueSize; tier++)
                    {
                        total[tier] = own[tier] + nextCounts[tier];
                    }

                    routeValue[(layerIndex, roomIndex)] = (total, next);
                }
            }

            // Anchor the route to where you actually stand. FloorData.RoomChoices holds
            // the room index taken in each completed layer, so its count is the layer you
            // are choosing from next and its last entry is your current room. Empty at the
            // start of a floor, where every room in layer 0 is a candidate.
            var roomChoices = floorWindow.FloorData.RoomChoices is IEnumerable rawChoices
                ? rawChoices.Cast<object>().Select(x => Convert.ToInt32(x)).ToList()
                : new List<int>();
            var startLayer = roomChoices.Count;
            IEnumerable<int> startCandidates;
            if (startLayer == 0)
            {
                startCandidates = Enumerable.Range(0, roomsByLayer[0].Count);
            }
            else
            {
                // Only rooms connected to the current one can be entered next
                startCandidates = floorWindow.FloorData.RoomLayout[startLayer - 1][roomChoices[startLayer - 1]]
                    .Select(x => (int)x);
            }

            var routeRoom = -1;
            int[] routeCounts = null;
            if (startLayer < roomsByLayer.Count)
            {
                foreach (var roomIndex in startCandidates)
                {
                    if (!routeValue.TryGetValue((startLayer, roomIndex), out var candidate))
                    {
                        continue;
                    }

                    if (routeCounts == null || CompareRoutes(candidate.Counts, routeCounts) > 0)
                    {
                        routeRoom = roomIndex;
                        routeCounts = candidate.Counts;
                    }
                }
            }

            // routeRoom goes negative at the last layer, ending the walk
            for (var layerIndex = startLayer; routeRoom >= 0 && layerIndex < roomsByLayer.Count; layerIndex++)
            {
                bestRoute.Add((layerIndex, routeRoom));
                bestRouteOrder.Add((layerIndex, routeRoom));
                routeRoom = routeValue[(layerIndex, routeRoom)].Next;
            }
        }

        // Join the route up so it reads as a path rather than a row of separate frames
        if (Settings.Routing.BestPathLineThickness > 0)
        {
            for (var step = 1; step < bestRouteOrder.Count; step++)
            {
                var from = roomsByLayer[bestRouteOrder[step - 1].Layer][bestRouteOrder[step - 1].Room].GetClientRectCache;
                var to = roomsByLayer[bestRouteOrder[step].Layer][bestRouteOrder[step].Room].GetClientRectCache;
                if (IsObstructed(_obstructions, from) || IsObstructed(_obstructions, to))
                {
                    continue;
                }

                Graphics.DrawLine(
                    new Vector2(from.Right - 15, from.Center.Y),
                    new Vector2(to.Left + 15, to.Center.Y),
                    Settings.Routing.BestPathLineThickness.Value,
                    Settings.Routing.BestPathColor);
            }
        }

        for (var layerIndex = 0;
             layerIndex < roomsByLayer.Count;
             layerIndex++)
        {
            var roomLayer = roomsByLayer[layerIndex];
            for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
            {
                var room = roomLayer[roomIndex];
                var fightRoomId = room.Data.FightRoom?.RoomType?.Id;
                if (fightRoomId != null && Settings.MapDisplay.ConnectionLineThickness > 0)
                {
                    var connections = floorWindow.FloorData.RoomLayout[layerIndex][roomIndex];
                    var connectedRoomData = connections.Select(index => (index, tierMap.GetValueOrDefault((layerIndex + 1, index))))
                        .Where(x => x.Item2 != default).ToList();
                    if (connectedRoomData.Any())
                    {
                        var leftPoint = new Vector2(room.GetClientRectCache.Right - 15, room.GetClientRectCache.Center.Y);
                        foreach (var (index, (currencyTier, roomTier, afflictionTier)) in connectedRoomData)
                        {
                            var connectedRoom = roomsByLayer[layerIndex + 1][index];
                            if (connectedRoom.Data.FightRoom?.RoomType?.Id == null)
                            {
                                continue;
                            }

                            var rightPoint = new Vector2(connectedRoom.GetClientRectCache.Left + 15, connectedRoom.GetClientRectCache.Center.Y);
                            if (IsObstructed(_obstructions, new RectangleF(leftPoint.X, Math.Min(leftPoint.Y, rightPoint.Y),
                                    rightPoint.X - leftPoint.X,
                                    Math.Max(leftPoint.Y, rightPoint.Y) -
                                    Math.Min(leftPoint.Y, rightPoint.Y))))
                            {
                                continue;
                            }

                            var leftPointOffset = new Vector2(0, (rightPoint.Y - leftPoint.Y) * 0.25f);
                            var overlapOffsetVector = new Vector2(0,
                                Settings.MapDisplay.ConnectionLineThickness * (0.5f + 0.5f * (rightPoint - leftPoint).Length() / (rightPoint.X - leftPoint.X)));
                            Graphics.DrawLine(leftPoint + leftPointOffset - overlapOffsetVector,
                                rightPoint - leftPointOffset - overlapOffsetVector,
                                Settings.MapDisplay.ConnectionLineThickness,
                                currencyTier.Any() ? GetTierColor(currencyTier.Min()) : Settings.TierColors.EmptyColor);
                            Graphics.DrawLine(leftPoint + leftPointOffset,
                                rightPoint - leftPointOffset,
                                Settings.MapDisplay.ConnectionLineThickness,
                                roomTier is { } ? GetTierColor(roomTier.Value) : Settings.TierColors.EmptyColor);
                            Graphics.DrawLine(leftPoint + leftPointOffset + overlapOffsetVector,
                                rightPoint - leftPointOffset + overlapOffsetVector,
                                Settings.MapDisplay.ConnectionLineThickness,
                                afflictionTier is { } ? GetTierColor(afflictionTier.Value) : Settings.TierColors.EmptyColor);
                        }
                    }
                }

                if (IsObstructed(_obstructions, room.GetClientRectCache))
                {
                    continue;
                }

                if (bestRoute.Contains((layerIndex, roomIndex)))
                {
                    Graphics.DrawFrame(room.GetClientRectCache, Settings.Routing.BestPathColor, Settings.Routing.BestPathFrameThickness.Value);
                }

                var textTopLeft = room.GetClientRectCache.TopLeft.ToVector2Num();
                var lineLocation = textTopLeft;
                var textSize = DrawTextWithBackground(fightRoomId ?? "??", lineLocation, GetRoomColor(fightRoomId), Settings.MapDisplay.BackgroundColor);
                lineLocation.Y += textSize.Y;
                var rewardRoomId = room.Data.RewardRoom?.RoomType?.Id;
                textSize = DrawTextWithBackground($"->{rewardRoomId ?? "??"}", lineLocation, GetRoomColor(rewardRoomId), Settings.MapDisplay.BackgroundColor);
                lineLocation.Y += textSize.Y;

                if (room.GetRoomsWithOrder() is { Count: > 0 } rewards)
                {
                    textSize = DrawTextWithBackground("\nRewards:", lineLocation, Settings.MapDisplay.TextColor, Settings.MapDisplay.BackgroundColor);
                    lineLocation.Y += textSize.Y;
                    foreach (var reward in rewards)
                    {
                        var currencyName = reward.room.CurrencyName;
                        var tier = Settings.GetCurrencyTier(currencyName, reward.order);
                        if (tier <= Settings.HideCurrencyBelowTier)
                        {
                            textSize = DrawTextWithBackground(currencyName, lineLocation, GetTierColor(tier), Settings.MapDisplay.BackgroundColor);
                            lineLocation.Y += textSize.Y;
                        }
                    }
                }

                if (room.Data.RoomEffect is { } effect)
                {
                    var text = "";
                    if (Settings.MapDisplay.ShowEffectId)
                    {
                        text += $"{effect.Id}\n";
                    }

                    var effectName = effect.ReadableName;
                    if (Settings.MapDisplay.ShowEffectName)
                    {
                        text += $"{effectName}\n";
                    }

                    if (Settings.MapDisplay.ShowEffectDescription)
                    {
                        var maxWidth = room.GetClientRectCache.Width;
                        var splitDescription = effect.Description.Split(" ").Aggregate(new List<string> { "" }, (l, i) =>
                        {
                            if (l.Last().Length > 0 && Graphics.MeasureText(l.Last() + i).X > maxWidth)
                            {
                                return l.Append(i).ToList();
                            }

                            return l.SkipLast(1).Append($"{l.Last()} {i}").ToList();
                        });
                        text += $"{string.Join("\n", splitDescription)}\n";
                    }

                    textSize = DrawTextWithBackground(text, lineLocation, GetAfflictionColor(effectName), Settings.MapDisplay.BackgroundColor);
                    lineLocation.Y += textSize.Y;
                }
            }
        }

    }

    // Read by reflection on purpose: several of these are guesses from the ExileCore
    // metadata, and a name that turns out not to exist should report itself as absent
    // rather than stop the plugin compiling.
    // Floor-window level, to find which rooms are currently choosable
    private static readonly string[] DebugWindowMemberNames =
    {
        "RoomChoices", "Rooms", "RoomData", "RoomDataArray", "RoomName", "Room",
    };

    private static readonly string[] DebugMemberNames =
    {
        "FightRoom", "RewardRoom", "RewardRooms", "RoomEffect",
        "Reward1", "Reward2", "Reward3",
        "Cost", "CostStat", "CostMultiplier", "DeferralCategory",
    };

    private static string DescribeMember(object target, string name)
    {
        if (target == null)
        {
            return $"{name}=<no data>";
        }

        var member = target.GetType().GetProperty(name);
        if (member == null)
        {
            return $"{name}=<absent>";
        }

        try
        {
            return $"{name}={Describe(member.GetValue(target), 0)}";
        }
        catch (Exception e)
        {
            return $"{name}=<{e.GetType().Name}>";
        }
    }

    private static string Describe(object value, int depth)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string text)
        {
            return text;
        }

        var type = value.GetType();
        if (type.IsPrimitive || value is decimal)
        {
            return value.ToString();
        }

        if (depth > 2)
        {
            return type.Name;
        }

        if (value is IEnumerable items)
        {
            return "[" + string.Join("|", items.Cast<object>().Select(x => Describe(x, depth + 1))) + "]";
        }

        // Report every identifying member rather than the first one found. Returning
        // early on Id hid RoomType, which is the member the tiering actually keys on.
        var parts = new List<string>();
        foreach (var name in new[] { "Id", "ReadableName", "CurrencyName", "RoomType" })
        {
            try
            {
                if (type.GetProperty(name)?.GetValue(value) is { } inner)
                {
                    parts.Add($"{name}={Describe(inner, depth + 1)}");
                }
            }
            catch (Exception)
            {
                // an unreadable member tells us nothing useful, so try the next one
            }
        }

        return parts.Count > 0 ? $"{type.Name}({string.Join(" ", parts)})" : type.Name;
    }

    // Bonuses shift a value towards the good end rather than adding points, so they stay
    // meaningful under tier comparison. They never reach 0, which is yours to assign, and
    // never improve something already at or below neutral.
    private int AdjustCurrencyValue(int value, int floor)
    {
        if (value is BetterSanctumSettings.PrioritizeValue or BetterSanctumSettings.BlockValue ||
            value >= BetterSanctumSettings.NeutralValue)
        {
            return value;
        }

        var shift = 0;
        if (floor >= 3)
        {
            // Later floors roll higher reward tiers
            shift += Settings.Routing.ContextBiasStrength.Value;
        }

        return Math.Max(value - shift, 1);
    }

    // Folds the run type and floor into the room type's value. A relic that makes a room
    // type pointless flattens it to neutral; floor rules nudge it by the bias strength.
    // Neither ever overrides an explicit 0 or 8 - those are your decisions, not context.
    private int AdjustRoomValue(int value, string roomTypeId, int floor)
    {
        if (value is BetterSanctumSettings.PrioritizeValue or BetterSanctumSettings.BlockValue)
        {
            return value;
        }

        var runType = Settings.RunType;
        if (runType == BetterSanctumSettings.RunTypeHourOfDivinity && roomTypeId == "BoonFountain" ||
            runType == BetterSanctumSettings.RunTypeGildedChalice && roomTypeId == "Fountain")
        {
            // No boons to gain, or no resolve to recover: the room has nothing to offer.
            // CurseFountain is deliberately untouched - it stays bad on its own merits.
            return BetterSanctumSettings.NeutralValue;
        }

        var bias = Settings.Routing.ContextBiasStrength.Value;
        if (bias == 0)
        {
            return value;
        }

        // Deals gate the larger rewards late, and coins matter early - but only when
        // boons are buyable, which Hour of Divinity rules out.
        // Deals are handled separately, as flat points rather than a tier shift
        var favoured = floor is >= 1 and <= 2 &&
                       runType != BetterSanctumSettings.RunTypeHourOfDivinity &&
                       roomTypeId is "Treasure" or "Merchant";

        // Lower is better on this scale, and 1 is as good as a weight gets
        return favoured ? Math.Max(value - bias, 1) : value;
    }

    private const int TierCount = 9;

    // Tier counts, plus one trailing slot holding flat bonus points so both sum the same
    // way through the route.
    private const int DealLateFloorBonus = 50;
    private const int BonusIndex = TierCount;
    private const int RouteValueSize = TierCount + 1;

    // Distance from neutral, an order of magnitude per step, with 7 an outsized penalty
    // rather than a bar: it takes more than a tier-1 reward to justify entering one, but
    // enough value can still buy through. 0 and 8 are the constraints and score nothing.
    // The last entry scores the bonus slot, so bonuses use these same units.
    private static readonly int[] TierWeights = { 0, 100, 10, 5, 0, -5, -10, -120, 0, 1 };

    private static int WeighTiers(int[] counts)
    {
        var total = 0;
        for (var tier = 0; tier < RouteValueSize; tier++)
        {
            total += counts[tier] * TierWeights[tier];
        }

        return total;
    }

    // Positive when route a is preferable to route b. The two constraint tiers carry no
    // weight of their own and are compared ahead of the sum: a must-take outranks
    // everything, including any number of never-enter rooms in the way, and among routes
    // tied on must-takes the one entering fewest never-enter rooms wins.
    private static int CompareRoutes(int[] a, int[] b)
    {
        if (a[BetterSanctumSettings.PrioritizeValue] != b[BetterSanctumSettings.PrioritizeValue])
        {
            return a[BetterSanctumSettings.PrioritizeValue].CompareTo(b[BetterSanctumSettings.PrioritizeValue]);
        }

        if (a[BetterSanctumSettings.BlockValue] != b[BetterSanctumSettings.BlockValue])
        {
            return b[BetterSanctumSettings.BlockValue].CompareTo(a[BetterSanctumSettings.BlockValue]);
        }

        return WeighTiers(a).CompareTo(WeighTiers(b));
    }

    // How many rooms of each tier this room contributes. Currency counts its best slot
    // only, since the three offers are one reward at different timings and you take one.
    private int[] EvaluateRoom(SanctumRoomElement room, int floor)
    {
        var counts = new int[RouteValueSize];

        // The third slot is the end-of-sanctum deferral. It only pays double from floor 4;
        // before that it is an ordinary offer.
        var thirdSlotMultiplier = floor >= 4 ? 2 : 1;

        // Best slot only - the three offers are one reward at different timings and you
        // take one - chosen on what the slot is actually worth, multiplier included, so a
        // doubled tier-2 does not displace a tier-1 taken now.
        var bestSlotValue = -1;
        var bestSlotWorth = 0;
        var bestSlotMultiplier = 1;
        foreach (var (reward, order) in room.GetRoomsWithOrder())
        {
            var value = AdjustCurrencyValue(Settings.GetCurrencyTier(reward.CurrencyName, order), floor);
            if (value == BetterSanctumSettings.PrioritizeValue)
            {
                counts[BetterSanctumSettings.PrioritizeValue]++;
                continue;
            }

            // A currency you never want is a reason to skip the offer, not the room
            if (value == BetterSanctumSettings.BlockValue)
            {
                continue;
            }

            var multiplier = order == 2 ? thirdSlotMultiplier : 1;
            var worth = TierWeights[value] * multiplier;
            if (bestSlotValue < 0 || worth > bestSlotWorth)
            {
                bestSlotValue = value;
                bestSlotWorth = worth;
                bestSlotMultiplier = multiplier;
            }
        }

        if (bestSlotValue >= 0)
        {
            // Counting it twice is what doubles its weight
            counts[bestSlotValue] += bestSlotMultiplier;
        }

        foreach (var roomTypeId in new[] { room.Data.FightRoom?.RoomType?.Id, room.Data.RewardRoom?.RoomType?.Id })
        {
            if (roomTypeId != null)
            {
                counts[AdjustRoomValue(Settings.GetRoomTier(roomTypeId), roomTypeId, floor)]++;
                if (roomTypeId == "Deal" && floor >= 3)
                {
                    // Worth more than a tier-2 reward but less than a tier-1, since the
                    // terms are unknowable until entered and pay out best late
                    counts[BonusIndex] += DealLateFloorBonus * Settings.Routing.ContextBiasStrength.Value;
                }
            }
        }

        if (room.Data.RoomEffect?.ReadableName is { } effectName)
        {
            counts[Settings.GetAfflictionTier(effectName)]++;
        }

        return counts;
    }


    private Color GetAfflictionColor(string effectName) => GetTierColor(Settings.GetAfflictionTier(effectName));
    private Color GetRoomColor(string fightRoomId) => GetTierColor(Settings.GetRoomTier(fightRoomId));

    private ColorNode GetTierColor(int value)
    {
        return value switch
        {
            0 => Settings.TierColors.Tier0Color,
            1 => Settings.TierColors.Tier1Color,
            2 => Settings.TierColors.Tier2Color,
            3 => Settings.TierColors.Tier3Color,
            4 => Settings.TierColors.Tier4Color,
            5 => Settings.TierColors.Tier5Color,
            6 => Settings.TierColors.Tier6Color,
            7 => Settings.TierColors.Tier7Color,
            8 => Settings.TierColors.Tier8Color,
            _ => Settings.TierColors.EmptyColor,
        };
    }
}
