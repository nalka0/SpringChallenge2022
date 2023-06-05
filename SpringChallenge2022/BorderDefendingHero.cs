using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SpringChallenge2022;

public class BorderDefendingHero : FriendlyHero
{
    public bool PlayCarefully { get; set; }

    public List<Monster> ConsideredMonsters { get; set; }

    public int Angle { get; set; }

    public Point WindPoint { get; set; }

    public BorderDefendingHero(int order)
    {
        Angle = order switch
        {
            0 => 45,
            1 => 15,
            2 => 75,
            _ => 45
        };
        ConsideredMonsters = new List<Monster>();
        WindPoint = Helpers.CoordsConverter(Game.Me.BaseCoords, Helpers.PointOnCircle(new Point(), 5001, Angle));
    }

    public void Prepare()
    {
        int rallyPointDistance = 7801;
        PlayCarefully = Game.Ennemy.Heroes.Any(x => (Math.Abs(x.AngleToBase - Angle) <= 30 && x.DistanceToBase < 7000) ||
                                                    (!Helpers.IsCircleCollision(x.Position, 2200, Game.Me.BaseCoords, 5000) && x.DistanceToBase < 5000));
        if (Game.Ennemy.Heroes.Count > 0)
        {
            rallyPointDistance -= Game.CurrentTurn / 10 * 200;
        }
        if (PlayCarefully)
        {
            rallyPointDistance -= 800;
        }

        rallyPointDistance = Math.Max(rallyPointDistance, 5001);
        RallyPoint = Helpers.CoordsConverter(Game.Me.BaseCoords, Helpers.PointOnCircle(new Point(), rallyPointDistance, Angle == 75 ? Angle - 5 : Angle));
    }

    public override Point GetWindTarget()
    {
        if (Game.Ennemy.Heroes.Count == 0 && Id != 0 && Id != 3)
        {
            return Game.Me.Heroes[0].RallyPoint;
        }
        else
        {
            return Helpers.CoordsConverter(Game.Me.BaseCoords, Helpers.PointOnCircle(new Point(), 50000, Angle));
        }
    }

    protected override bool PlayInternal()
    {
        if (ConsideredMonsters.Count > 0)
        {
            int turnsToWindPoint = (int)Math.Ceiling(Helpers.GetCircleRadius(WindPoint, Position) / 800d);
            int i = 0;
            int? closestFarmable = null;
            List<Monster> possibleTargets = new();
            while (i < Game.PredictedTurns)
            {
                bool defenseNeeded = false;
                foreach (Monster monster in ConsideredMonsters)
                {
                    if ((monster.IsDangerous || monster.TurnsToBase < turnsToWindPoint) &&
                        (Math.Abs(monster.AngleToBase - Angle) <= 15 || (monster.IsDangerous && Helpers.IsInRange(monster.Position, Position, 1280))))
                    {
                        if (!defenseNeeded)
                        {
                            defenseNeeded = true;
                            possibleTargets.Clear();
                        }

                        possibleTargets.Add(monster);
                    }
                    else if ((closestFarmable == null || closestFarmable == i) && monster.NextPositions.Count > i && Helpers.IsInRange(monster.NextPositions[i], Position, 800 * (i + 2)) && !defenseNeeded)
                    {
                        closestFarmable = i;
                        possibleTargets.Add(monster);
                    }
                }
                if ((possibleTargets.Count > 0 && Game.PredictedTurns - 1 == i) || defenseNeeded)
                {
                    Monster targetMonster = possibleTargets.OrderByDescending(x => x.IsDangerous).ThenByDescending(x => x.ThreatFor).ThenBy(x => x.TurnsToBase ?? int.MaxValue).ThenBy(x => Helpers.GetCircleRadius(x.Position, Game.Me.BaseCoords)).First();
                    if (defenseNeeded)
                    {
                        if (i == 0)
                        {
                            foreach (Point nextPosition in targetMonster.NextPositions)
                            {
                                if (!Helpers.IsInRange(nextPosition, Position, 800 * (i + 1)))
                                {
                                    i++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (i >= targetMonster.NextPositions.Count)
                            {
                                i = targetMonster.NextPositions.Count - 1;
                            }
                        }
                        int minDistanceToWind;
                        if (PlayCarefully)
                        {
                            minDistanceToWind = 8000;
                        }
                        else if (Game.Ennemy.Heroes.Count > 0)
                        {
                            minDistanceToWind = 7500;
                        }
                        else
                        {
                            minDistanceToWind = 5001;
                        }
                        Debug($"defending vs {targetMonster.Id} among {string.Join(',', possibleTargets.Select(x => x.Id))}");
                        if (targetMonster.NextPositions.Count > 1 && (targetMonster.Health > 2 || ConsideredMonsters.Any(x => x != targetMonster && x.IsDangerous && x.ShieldDuration == 0 && Helpers.IsInRange(x.Position, Position, 1280)))
                            && !targetMonster.IsWinded && Game.Me.Mana >= 10 && Helpers.IsInRange(targetMonster.Position, Position, 1280) && targetMonster.ShieldDuration == 0 && Helpers.GetCircleRadius(targetMonster.Position, Game.Me.BaseCoords) <= minDistanceToWind)
                        {
                            if (Helpers.GetCircleRadius(targetMonster.NextPositions[1], Position) < 1280 && !targetMonster.IsNearBase && PlayWithAlreadyOptimalPosition(targetMonster, false, true))
                            {
                                return true;
                            }
                            else
                            {
                                foreach (Monster winded in ConsideredMonsters.Where(x => Helpers.IsInRange(x.Position, Position, 1280)))
                                {
                                    winded.IsWinded = true;
                                }

                                return Wind();
                            }
                        }
                        else if (Game.Me.Mana >= 10 && targetMonster.TurnsToDamage == 2 && (targetMonster.TurnsToDamage - i) * 2 > targetMonster.Health && targetMonster.ShieldDuration == 0 && Helpers.IsInRange(targetMonster.Position, Position, 2200))
                        {
                            return Control(targetMonster, Position);
                        }
                        else
                        {
                            return (i == 0 && TryAttackMultipleMonsters(targetMonster, possibleTargets)) || AttackSingleMonster(targetMonster, i, true);
                        }
                    }
                    else
                    {
                        Debug($"farming {string.Join(',', possibleTargets.Select(x => x.Id))}");
                        if (Game.Me.Mana >= 30 && ShieldDuration == 0 && Game.Ennemy.Heroes.Any(x => Helpers.IsInRange(x.Position, Position, 2200)))
                        {
                            return Shield(this);
                        }
                        else if (Game.Me.Mana >= 20 && Game.Me.Heroes.FirstOrDefault(friend => friend.ShieldDuration == 0 && friend.IsControlled && Helpers.IsInRange(friend.Position, Position, 2200) && Game.Ennemy.Heroes.Any(x => Helpers.IsInRange(friend.Position, x.Position, 2200))) is FriendlyHero saved)
                        {
                            return Shield(saved);
                        }
                        else if (closestFarmable == 0)
                        {
                            foreach (Monster farmed in possibleTargets)
                            {
                                if (TryAttackMultipleMonsters(farmed, possibleTargets))
                                {
                                    return true;
                                }
                            }
                        }

                        return AttackSingleMonster(targetMonster, closestFarmable.Value, false);
                    }
                }

                i++;
            }
        }

        return false;
    }

    public bool TryAttackMultipleMonsters(Monster firstTarget, List<Monster> possibleTargets)
    {
        foreach (Monster otherTarget in possibleTargets.OrderBy(x => Helpers.GetCircleRadius(x.Position, firstTarget.Position)))
        {
            if (firstTarget != otherTarget)
            {
                List<Point> intersections = Helpers.CircleIntersections(firstTarget.Position, 798, otherTarget.Position, 798);
                if (intersections.Count == 2)
                {
                    intersections.Add(Helpers.PointFromRounding((firstTarget.Position.X + otherTarget.Position.X) / 2d, (firstTarget.Position.Y + otherTarget.Position.Y) / 2d));
                }

                foreach (Point intersection in intersections.Where(x => Helpers.IsInRange(x, Position, 799)).OrderByDescending(i => possibleTargets.Count(m => m != firstTarget && m != otherTarget && Helpers.IsInRange(i, m.Position, 799))).ThenBy(x => Helpers.GetCircleRadius(x, RallyPoint)))
                {
                    Debug($"multihitting {otherTarget.Id} from {firstTarget.Id}");
                    return Move(intersection);
                }
            }
        }

        return false;
    }

    public bool AttackSingleMonster(Monster target, int turnsToReach, bool isDefending)
    {
        if (turnsToReach == 0)
        {
            return TryAttackFromOutsideBase(target) || PlayWithAlreadyOptimalPosition(target, true, isDefending);
        }
        else
        {
            return Move(target.NextPositions[turnsToReach]);
        }
    }

    public bool TryAttackFromOutsideBase(Monster target)
    {
        if (Game.Ennemy.Heroes.All(x => x.DistanceToBase > 8000) && target.IsNearBase && !Helpers.IsInRange(target.Position, Game.Me.BaseCoords, 4200))
        {
            if (Helpers.IsInRange(Position, target.Position, 799) && !Helpers.IsInRange(Position, Game.Me.BaseCoords, 5000))
            {
                return PlayWithAlreadyOptimalPosition(target, false, true) || Move(Position);
            }
            foreach (Point outsideBaseHit in Helpers.CircleIntersections(Game.Me.BaseCoords, 5001, Position, 800))
            {
                if (Helpers.IsInRange(outsideBaseHit, target.Position, 800))
                {
                    return Move(outsideBaseHit);
                }
            }
        }

        return false;
    }

    protected override bool PlayWithAlreadyOptimalPosition(Monster target, bool allowMove, bool isDefending)
    {
        if (isDefending && ShieldDuration == 0 && Game.Me.Mana >= 18 && Game.Ennemy.Heroes.Any(x => Helpers.IsInRange(x.Position, Position, 2200)))
        {
            return Shield(this);
        }
        else if (target != null && allowMove)
        {
            double monsterAngle = Helpers.AngleFromPoint(target.Position, RallyPoint);
            return Move(Helpers.PointOnCircle(target.Position, target.IsDangerous && target.Health > 2 ? 398 : 798, monsterAngle));
        }
        else
        {
            return false;
        }
    }

    protected override bool Explore()
    {
        if (Game.Ennemy.Heroes.Count == 0)
        {
            return Move(Helpers.CoordsConverter(Position, new Point(800, 0)));
        }
        else
        {
            return false;
        }
    }
}
