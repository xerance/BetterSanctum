using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private Vector2 DrawTextWithBackground(string text, Vector2 position, Color color, Color backgroundColor)
    {
        var textSize = Graphics.MeasureText(text);
        Graphics.DrawBox(position, textSize + position, backgroundColor);
        Graphics.DrawText(text, position, color);
        return textSize;
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
        var floorFinalChest = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Chest];

        foreach (var offer in dupOffer)
        {
            Graphics.DrawFrame(offer.GetClientRect(), RandomUtil.NextColor(rndColor), 6);
        }
        
        foreach (var offer in noDupOffer.Where(x => !dupOffer.Contains(x)))
        {
            if ((offer.IndexInParent == 2) || (GameController.Area.CurrentArea.Area.RawName == "SanctumCrypt" && (offer.IndexInParent == 2 || offer.IndexInParent == 1)
                    || GameController.Area.CurrentArea.Area.RawName == "SanctumNave" && (offer.IndexInParent == 2 || offer.IndexInParent == 1) && floorFinalChest.FirstOrDefault<Entity>(x => x.Metadata.Contains("FloorFinalRewardChest")) != null ))
                {
                    Graphics.DrawLine(offer.Children[1].Parent.GetClientRect().TopLeft.ToVector2Num(), offer.Children[1].Parent.GetClientRect().BottomRight.ToVector2Num(), 4, Color.Red);
                    Graphics.DrawLine(offer.Children[1].Parent.GetClientRect().TopRight.ToVector2Num(), offer.Children[1].Parent.GetClientRect().BottomLeft.ToVector2Num(), 4, Color.Red);
                    Graphics.DrawFrame(offer.Children[1].Parent.GetClientRect(), Color.Red, 4);
                }
        }
    }

    public override void Render()
    {
        if (Settings.DuplicateRun)
        {
            PreventLastOffer();
        }
        
        var floorWindow = GameController.IngameState.IngameUi.SanctumFloorWindow;
        if (!floorWindow.IsVisible)
        {
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

        var tierMap = new Dictionary<(int, int), (List<int> CurrencyTier, int? RoomTier, int? AfflictionTier)>();
        var roomsByLayer = floorWindow.RoomsByLayer;
        if (Settings.ConnectionLineThickness > 0)
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
        // layer, so every route is the same length and total scores compare directly with
        // no normalisation. Walking backwards, each room records the best continuation:
        // more must-take rooms wins, and ties break on the higher total score.
        var bestRoute = new HashSet<(int, int)>();
        if (Settings.EnablePathfinding && Settings.BestPathFrameThickness > 0 && roomsByLayer.Count > 0)
        {
            var routeValue = new Dictionary<(int, int), (int MustCount, int Score, int Next)>();
            for (var layerIndex = roomsByLayer.Count - 1; layerIndex >= 0; layerIndex--)
            {
                var roomLayer = roomsByLayer[layerIndex];
                for (var roomIndex = 0; roomIndex < roomLayer.Count; roomIndex++)
                {
                    if (EvaluateRoom(roomLayer[roomIndex]) is not { } own)
                    {
                        continue;
                    }

                    if (layerIndex == roomsByLayer.Count - 1)
                    {
                        routeValue[(layerIndex, roomIndex)] = (own.MustCount, own.Score, -1);
                        continue;
                    }

                    var next = -1;
                    var nextMustCount = 0;
                    var nextScore = 0;
                    foreach (var connection in floorWindow.FloorData.RoomLayout[layerIndex][roomIndex])
                    {
                        if (!routeValue.TryGetValue((layerIndex + 1, connection), out var candidate))
                        {
                            continue;
                        }

                        if (next < 0 ||
                            candidate.MustCount > nextMustCount ||
                            (candidate.MustCount == nextMustCount && candidate.Score > nextScore))
                        {
                            next = connection;
                            nextMustCount = candidate.MustCount;
                            nextScore = candidate.Score;
                        }
                    }

                    // Every way onward is blocked, so this room leads nowhere
                    if (next < 0)
                    {
                        continue;
                    }

                    routeValue[(layerIndex, roomIndex)] = (own.MustCount + nextMustCount, own.Score + nextScore, next);
                }
            }

            var routeRoom = -1;
            var routeMustCount = 0;
            var routeScore = 0;
            for (var roomIndex = 0; roomIndex < roomsByLayer[0].Count; roomIndex++)
            {
                if (!routeValue.TryGetValue((0, roomIndex), out var candidate))
                {
                    continue;
                }

                if (routeRoom < 0 ||
                    candidate.MustCount > routeMustCount ||
                    (candidate.MustCount == routeMustCount && candidate.Score > routeScore))
                {
                    routeRoom = roomIndex;
                    routeMustCount = candidate.MustCount;
                    routeScore = candidate.Score;
                }
            }

            // routeRoom goes negative at the last layer, ending the walk
            for (var layerIndex = 0; routeRoom >= 0 && layerIndex < roomsByLayer.Count; layerIndex++)
            {
                bestRoute.Add((layerIndex, routeRoom));
                routeRoom = routeValue[(layerIndex, routeRoom)].Next;
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
                if (fightRoomId != null && Settings.ConnectionLineThickness > 0)
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
                            if (tooltipRect.Intersects(new RectangleF(leftPoint.X, Math.Min(leftPoint.Y, rightPoint.Y),
                                    rightPoint.X - leftPoint.X,
                                    Math.Max(leftPoint.Y, rightPoint.Y) -
                                    Math.Min(leftPoint.Y, rightPoint.Y))))
                            {
                                continue;
                            }

                            var leftPointOffset = new Vector2(0, (rightPoint.Y - leftPoint.Y) * 0.25f);
                            var overlapOffsetVector = new Vector2(0,
                                Settings.ConnectionLineThickness * (0.5f + 0.5f * (rightPoint - leftPoint).Length() / (rightPoint.X - leftPoint.X)));
                            Graphics.DrawLine(leftPoint + leftPointOffset - overlapOffsetVector,
                                rightPoint - leftPointOffset - overlapOffsetVector,
                                Settings.ConnectionLineThickness,
                                currencyTier.Any() ? GetTierColor(currencyTier.Min()) : Settings.EmptyColor);
                            Graphics.DrawLine(leftPoint + leftPointOffset,
                                rightPoint - leftPointOffset,
                                Settings.ConnectionLineThickness,
                                roomTier is { } ? GetTierColor(roomTier.Value) : Settings.EmptyColor);
                            Graphics.DrawLine(leftPoint + leftPointOffset + overlapOffsetVector,
                                rightPoint - leftPointOffset + overlapOffsetVector,
                                Settings.ConnectionLineThickness,
                                afflictionTier is { } ? GetTierColor(afflictionTier.Value) : Settings.EmptyColor);
                        }
                    }
                }

                if (room.GetClientRectCache.Intersects(tooltipRect))
                {
                    continue;
                }

                if (bestRoute.Contains((layerIndex, roomIndex)))
                {
                    Graphics.DrawFrame(room.GetClientRectCache, Settings.BestPathColor, Settings.BestPathFrameThickness.Value);
                }

                var textTopLeft = room.GetClientRectCache.TopLeft.ToVector2Num();
                var lineLocation = textTopLeft;
                var textSize = DrawTextWithBackground(fightRoomId ?? "??", lineLocation, GetRoomColor(fightRoomId), Settings.BackgroundColor);
                lineLocation.Y += textSize.Y;
                var rewardRoomId = room.Data.RewardRoom?.RoomType?.Id;
                textSize = DrawTextWithBackground($"->{rewardRoomId ?? "??"}", lineLocation, GetRoomColor(rewardRoomId), Settings.BackgroundColor);
                lineLocation.Y += textSize.Y;

                if (room.GetRoomsWithOrder() is { Count: > 0 } rewards)
                {
                    textSize = DrawTextWithBackground("\nRewards:", lineLocation, Settings.TextColor, Settings.BackgroundColor);
                    lineLocation.Y += textSize.Y;
                    foreach (var reward in rewards)
                    {
                        var currencyName = reward.room.CurrencyName;
                        var tier = Settings.GetCurrencyTier(currencyName, reward.order);
                        if (tier <= Settings.HideCurrencyBelowTier)
                        {
                            textSize = DrawTextWithBackground(currencyName, lineLocation, GetTierColor(tier), Settings.BackgroundColor);
                            lineLocation.Y += textSize.Y;
                        }
                    }
                }

                if (room.Data.RoomEffect is { } effect)
                {
                    var text = "";
                    if (Settings.ShowEffectId)
                    {
                        text += $"{effect.Id}\n";
                    }

                    var effectName = effect.ReadableName;
                    if (Settings.ShowEffectName)
                    {
                        text += $"{effectName}\n";
                    }

                    if (Settings.ShowEffectDescription)
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

                    textSize = DrawTextWithBackground(text, lineLocation, GetAfflictionColor(effectName), Settings.BackgroundColor);
                    lineLocation.Y += textSize.Y;
                }
            }
        }

    }

    // Null means the room cannot be routed through at all.
    //
    // Currency scores on its best slot only: the three offers are the same reward at
    // different timings and you take one, so summing them would count rewards you never
    // receive. A blocked currency drops that slot rather than the room - a bad offer is
    // no reason to avoid a room, whereas a bad room type or affliction is.
    private (int MustCount, int Score)? EvaluateRoom(SanctumRoomElement room)
    {
        var mustCount = 0;
        var score = 0;

        int? bestSlotScore = null;
        foreach (var (reward, order) in room.GetRoomsWithOrder())
        {
            var value = Settings.GetCurrencyTier(reward.CurrencyName, order);
            if (value == BetterSanctumSettings.BlockValue)
            {
                continue;
            }

            if (value == BetterSanctumSettings.PrioritizeValue)
            {
                mustCount++;
                continue;
            }

            var slotScore = BetterSanctumSettings.ScoreOf(value);
            if (slotScore > 0 && order == 2)
            {
                // Third slot is the end-of-sanctum deferral, which pays out larger
                slotScore += Settings.ThirdSlotBonus.Value;
            }

            if (bestSlotScore == null || slotScore > bestSlotScore)
            {
                bestSlotScore = slotScore;
            }
        }

        score += (bestSlotScore ?? 0) * Settings.CurrencyWeightMultiplier.Value;

        foreach (var roomTypeId in new[] { room.Data.FightRoom?.RoomType?.Id, room.Data.RewardRoom?.RoomType?.Id })
        {
            if (roomTypeId == null)
            {
                continue;
            }

            var value = Settings.GetRoomTier(roomTypeId);
            if (value == BetterSanctumSettings.BlockValue)
            {
                return null;
            }

            if (value == BetterSanctumSettings.PrioritizeValue)
            {
                mustCount++;
            }
            else
            {
                score += BetterSanctumSettings.ScoreOf(value) * Settings.RoomWeightMultiplier.Value;
            }
        }

        if (room.Data.RoomEffect?.ReadableName is { } effectName)
        {
            var value = Settings.GetAfflictionTier(effectName);
            if (value == BetterSanctumSettings.BlockValue)
            {
                return null;
            }

            if (value == BetterSanctumSettings.PrioritizeValue)
            {
                mustCount++;
            }
            else
            {
                score += BetterSanctumSettings.ScoreOf(value) * Settings.AfflictionWeightMultiplier.Value;
            }
        }

        return (mustCount, score);
    }

    private Color GetAfflictionColor(string effectName) => GetTierColor(Settings.GetAfflictionTier(effectName));
    private Color GetRoomColor(string fightRoomId) => GetTierColor(Settings.GetRoomTier(fightRoomId));

    private ColorNode GetTierColor(int value)
    {
        return value switch
        {
            0 => Settings.Tier0Color,
            1 => Settings.Tier1Color,
            2 => Settings.Tier2Color,
            3 => Settings.Tier3Color,
            4 => Settings.Tier4Color,
            5 => Settings.Tier5Color,
            6 => Settings.Tier6Color,
            7 => Settings.Tier7Color,
            _ => Settings.EmptyColor,
        };
    }
}
