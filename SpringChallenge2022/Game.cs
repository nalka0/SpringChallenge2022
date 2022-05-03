using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public static class Game
{
    public static Point VerticalCorner { get; set; }

    public static bool IsTopLeft { get; set; }

    public static Player<FriendlyHero> Me { get; set; }

    public static Player<EnnemyHero> Ennemy { get; set; }

    public static int CurrentTurn { get; set; }

    public static bool EnnemyHeroInSight { get; set; }

    public static int TurnsLeft => 220 - CurrentTurn;

    public static readonly int PredictedTurns = 6;

    public static void Main(string[] args)
    {
        Ennemy = new Player<EnnemyHero>();
        Me = new Player<FriendlyHero>();
        string[] inputs = Console.ReadLine().Split(' ');
        Console.ReadLine(); // Always 3 (number of heroes)
        Me.BaseCoords = new Point(int.Parse(inputs[0]), int.Parse(inputs[1]));
        VerticalCorner = new Point(Me.BaseCoords.X, IsTopLeft ? 9000 : 0);
        IsTopLeft = Me.BaseCoords.X == 0;
        Ennemy.BaseCoords = IsTopLeft ? new Point(17630, 9000) : new Point(0, 0);
        CurrentTurn = 1;
        while (true)
        {
            EnnemyHeroInSight = false;
            List<Monster> monsters = new List<Monster>();
            Me.Heroes.Clear();
            Ennemy.Heroes.Clear();
            inputs = Console.ReadLine().Split(' ');
            Me.Health = int.Parse(inputs[0]);
            Me.Mana = int.Parse(inputs[1]);
            inputs = Console.ReadLine().Split(' ');
            Ennemy.Health = int.Parse(inputs[0]);
            Ennemy.Mana = int.Parse(inputs[1]);
            int entityCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                bool nearBase = int.Parse(inputs[9]) == 1;
                int id = int.Parse(inputs[0]);
                int type = int.Parse(inputs[1]);
                Point position = new Point(int.Parse(inputs[2]), int.Parse(inputs[3]));
                int shieldDuration = int.Parse(inputs[4]);
                bool isControlled = int.Parse(inputs[5]) == 1;
                if (type == 0)
                {
                    Threat threat = int.Parse(inputs[10]) switch
                    {
                        0 => Threat.None,
                        1 => Threat.Me,
                        2 => Threat.Ennemy
                    };
                    Point speed = new Point(int.Parse(inputs[7]), int.Parse(inputs[8]));
                    monsters.Add(new Monster
                    {
                        Id = id,
                        Position = position,
                        Health = int.Parse(inputs[6]),
                        IsNearBase = nearBase,
                        ThreatFor = threat,
                        IsControlled = isControlled,
                        ShieldDuration = shieldDuration,
                        Speed = speed,
                        AngleToBase = (Helpers.AngleFromPoint(Me.BaseCoords, position) + (IsTopLeft ? 0 : 180)) % 360
                    });
                }
                else if (type == 1)
                {
                    Me.Heroes.Add(new BorderDefendingHero(Me.Heroes.Count)
                    {
                        Id = id,
                        Position = position,
                        IsControlled = isControlled,
                        ShieldDuration = shieldDuration,
                        DistanceToBase = Helpers.GetCircleRadius(position, Me.BaseCoords),
                    });
                }
                else
                {
                    EnnemyHeroInSight = true;
                    Ennemy.Heroes.Add(new EnnemyHero
                    {
                        Id = id,
                        Position = position,
                        IsControlled = isControlled,
                        ShieldDuration = shieldDuration,
                        DistanceToBase = Helpers.GetCircleRadius(Me.BaseCoords, position),
                        AngleToBase = (Helpers.AngleFromPoint(Me.BaseCoords, position) + (IsTopLeft ? 0 : 180)) % 360
                    });
                }
            }
            foreach (FriendlyHero hero in Me.Heroes)
            {
                if (hero is BorderDefendingHero borderDefendingHero)
                {
                    borderDefendingHero.Prepare();
                }
            }
            foreach (Monster monster in monsters)
            {
                monster.ComputeNextPositions(PredictedTurns);
            }

            for (int i = 0; i < Me.Heroes.Count; i++)
            {
                FriendlyHero hero = Me.Heroes[i];
                if (hero is BorderDefendingHero borderDef)
                {
                    foreach (Monster monster in monsters)
                    {
                        if (monster.IsDangerous ||
                            (Helpers.IsInRange(monster.Position, borderDef.RallyPoint, 4000) &&
                            (borderDef.Angle != 45 || Math.Abs(monster.AngleToBase - 45) <= 20) &&
                            (!borderDef.PlayCarefully || Math.Abs(monster.AngleToBase - borderDef.Angle) <= 15)))
                        {
                            borderDef.ConsideredMonsters.Add(monster);
                        }
                    }
                }

                hero.Play();
            }

            CurrentTurn++;
        }
    }
}

public class Monster : Entity
{
    public bool IsWinded { get; set; }

    public double AngleToBase { get; set; }

    public Point Speed { get; set; }

    public int Health { get; set; }

    public List<Point> NextPositions { get; set; }

    public bool IsNearBase { get; set; }

    public Threat ThreatFor { get; set; }

    public int? TurnsToBase { get; set; }

    public int? TurnsToDamage { get; set; }

    public bool IsDangerous { get; set; }

    public void ComputeNextPositions(int count)
    {
        NextPositions = new List<Point>()
        {
            Position
        };
        int i = 1;
        while (!IsNearBase && ShieldDuration == 0 && Health > 2 && i < count && Game.Ennemy.Heroes.FirstOrDefault(x => Helpers.IsInRange(NextPositions[^1], x.Position, 1280) && x.DistanceToBase < 9000) is EnnemyHero winder)
        {
            IsDangerous = true;
            NextPositions.Add(Helpers.PointOnCircle(NextPositions[^1], 2200, Helpers.AngleFromPoint(winder.Position, Game.Me.BaseCoords)));
            //i++;
        }
        bool hasEnteredBase = IsNearBase;
        while (i < count)
        {
            if (!hasEnteredBase)
            {
                Point added = new Point(Position.X + (Speed.X * i), Position.Y + (Speed.Y * i));
                if (added.X <= 17635 && added.X >= -5 && added.Y >= -5 && added.Y <= 9005)
                {
                    NextPositions.Add(added);

                    if (Helpers.IsInRange(added, Game.Me.BaseCoords, 5000))
                    {
                        hasEnteredBase = true;
                        TurnsToBase = i;
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                Point added = IsControlled && i == 1 ? new Point(Position.X + Speed.X, Position.Y + Speed.Y) : Helpers.GetNextPosition(NextPositions[i - 1], 400, Game.Me.BaseCoords);
                NextPositions.Add(added);
                if (Helpers.IsInRange(added, Game.Me.BaseCoords, 300))
                {
                    TurnsToDamage = i;
                    break;
                }
            }

            i++;
        }
        if (IsNearBase)
        {
            TurnsToBase = 0;
            IsDangerous = true;
        }
        else
        {
            IsDangerous |= TurnsToBase <= 1 || NextPositions.Take(2).Any(p => Game.Ennemy.Heroes.Any(x => Helpers.IsInRange(p, x.Position, 2200)) && Helpers.GetCircleRadius(p, Game.Me.BaseCoords) <= 8500);
        }
    }
}

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
    }

    public void Prepare()
    {
        int rallyPointDistance;
        PlayCarefully = Game.Ennemy.Heroes.Any(x => (Math.Abs(x.AngleToBase - Angle) <= 30 && x.DistanceToBase < 7000) ||
                                                    (!Helpers.IsCircleCollision(x.Position, 2200, Game.Me.BaseCoords, 5000) && x.DistanceToBase < 5000));
        if (PlayCarefully)
        {
            rallyPointDistance = 5801;
        }
        else if (Game.EnnemyHeroInSight)
        {
            rallyPointDistance = 6601;
        }
        else
        {
            rallyPointDistance = 7401;
        }

        RallyPoint = Helpers.CoordsConverter(Game.Me.BaseCoords, Helpers.PointOnCircle(new Point(), rallyPointDistance, Angle == 75 ? Angle - 5 : Angle));
        WindPoint = Helpers.CoordsConverter(Game.Me.BaseCoords, Helpers.PointOnCircle(new Point(), 5001, Angle));
    }

    public override Point GetWindTarget()
    {
        return Helpers.CoordsConverter(Game.Me.BaseCoords, Helpers.PointOnCircle(new Point(), 50000, Angle));
    }

    protected override bool PlayInternal()
    {
        if (ConsideredMonsters.Count > 0)
        {
            int turnsToWindPoint = (int)Math.Ceiling(Helpers.GetCircleRadius(WindPoint, Position) / 800d);
            int i = 0;
            int? closestFarmable = null;
            List<Monster> possibleTargets = new List<Monster>();
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
                            minDistanceToWind = 9000;
                        }
                        else if (Game.EnnemyHeroInSight)
                        {
                            minDistanceToWind = 8200;
                        }
                        else
                        {
                            minDistanceToWind = 7500;
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
                            if (i == 0 && TryAttackMultipleMonsters(targetMonster, possibleTargets))
                            {
                                return true;
                            }
                            else
                            {
                                return AttackSingleMonster(targetMonster, i, true);
                            }
                        }
                    }
                    else
                    {
                        Debug($"farming {string.Join(',', possibleTargets.Select(x => x.Id))}");
                        if (Game.Me.Mana >= 30 && ShieldDuration == 0 && Game.Ennemy.Heroes.Any(x => Helpers.IsInRange(x.Position, Position, 2200)))
                        {
                            return Shield(this);
                        }
                        else if (Game.Me.Mana >= 20 && Game.Me.Heroes.FirstOrDefault(friend => friend.IsControlled && Helpers.IsInRange(friend.Position, Position, 2200) && Game.Ennemy.Heroes.Any(x => Helpers.IsInRange(friend.Position, x.Position, 2200))) is FriendlyHero saved)
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
                List<Point> intersections = Helpers.CircleIntersections(firstTarget.Position, 799, otherTarget.Position, 799);
                if (intersections.Count == 2)
                {
                    intersections.Add(Helpers.PointFromRounding((firstTarget.Position.X + otherTarget.Position.X) / 2d, (firstTarget.Position.Y + otherTarget.Position.Y) / 2d));
                }

                foreach (Point intersection in intersections.OrderByDescending(i => possibleTargets.Count(m => m != firstTarget && m != otherTarget && Helpers.IsInRange(i, m.Position, 800))).ThenBy(x => Helpers.GetCircleRadius(x, Game.Me.BaseCoords)))
                {
                    if (Helpers.IsInRange(intersection, Position, 800))
                    {
                        Debug($"multihitting from {firstTarget.Id}");
                        return Move(intersection);
                    }
                }
            }
        }

        return false;
    }

    public bool AttackSingleMonster(Monster target, int turnsToReach, bool isDefending)
    {
        if (turnsToReach == 0)
        {
            if (TryAttackFromOutsideBase(target))
            {
                return true;
            }
            else
            {
                return PlayWithAlreadyOptimalPosition(target, true, isDefending);
            }
        }
        else
        {
            return Move(target.NextPositions[turnsToReach]);
        }
    }

    public bool TryAttackFromOutsideBase(Monster target)
    {
        if (!Game.Ennemy.Heroes.Any(x => x.DistanceToBase < 8000) && target.IsNearBase && !Helpers.IsInRange(target.Position, Game.Me.BaseCoords, 4200))
        {
            if (Helpers.IsInRange(Position, target.Position, 800) && !Helpers.IsInRange(Position, Game.Me.BaseCoords, 5000))
            {
                return PlayWithAlreadyOptimalPosition(target, false, true) || Move(Position);
            }
            List<Point> outsideBaseHits = Helpers.CircleIntersections(Game.Me.BaseCoords, 5001, Position, 800);
            foreach (Point outsideBaseHit in outsideBaseHits)
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
}

public abstract class FriendlyHero : Hero
{
    public Point RallyPoint { get; set; }

    public void Play()
    {
        if (!PlayInternal() && !PlayWithAlreadyOptimalPosition(null, true, false))
        {
            Move(RallyPoint);
        }
    }

    protected abstract bool PlayWithAlreadyOptimalPosition(Monster target, bool allwoMove, bool isDefending);

    protected abstract bool PlayInternal();

    public bool Move(Point target)
    {
        if (Helpers.IsInRange(target, Game.Me.BaseCoords, 799))
        {
            target = Helpers.PointOnCircle(Game.Me.BaseCoords, 799, Helpers.AngleFromPoint(Game.Me.BaseCoords, target));
        }
        else if (Helpers.IsInRange(target, Game.VerticalCorner, 799))
        {
            target = Helpers.PointOnCircle(target, 799, Helpers.AngleFromPoint(Game.VerticalCorner, target));
        }

        Console.WriteLine($"MOVE {target.X} {target.Y}");
        return true;
    }

    public bool Shield(Entity target)
    {
        if (Game.Me.Mana >= 10 && target.ShieldDuration == 0)
        {
            Console.WriteLine($"SPELL SHIELD {target.Id}");
            return true;
        }
        else
        {
            Debug($"failed to cast SHIELD {Game.Me.Mana > 10}/{target.ShieldDuration == 0}");
            return false;
        }
    }

    public bool Control(Entity target, Point destination)
    {
        if (Game.Me.Mana >= 10)
        {
            Console.WriteLine($"SPELL CONTROL {target.Id} {destination.X} {destination.Y}");
            return true;
        }
        else
        {
            Debug("failed to cast CONTROL because he didn't have enough mana");
            return false;
        }
    }

    public bool Wind()
    {
        if (Game.Me.Mana >= 10)
        {
            Point windTarget = GetWindTarget();
            Console.WriteLine($"SPELL WIND {windTarget.X} {windTarget.Y}");
            return true;
        }
        else
        {
            Debug("failed to cast WIND because he didn't have enough mana");
            return false;
        }
    }

    public virtual Point GetWindTarget()
    {
        return Helpers.CoordsConverter(Position, new Point(1, 1));
    }
}

public static class Helpers
{
    public static bool IsCircleCollision(Point center1, int radius1, Point center2, int radius2)
    {
        int dx = center2.X - center1.X;
        int dy = center2.Y - center1.Y;

        double D = Math.Sqrt(dx * dx + dy * dy);
        return Math.Abs(radius1 - radius2) < D && D < radius1 + radius2;
    }

    public static Point PointFromRounding(double x, double y)
    {
        if (x > 8815)
        {
            x = Math.Ceiling(x);
        }
        else
        {
            x = Math.Floor(x);
        }

        if (y > 4500)
        {
            y = Math.Ceiling(y);
        }
        else
        {
            y = Math.Floor(y);
        }

        return new Point((int)x, (int)y);
    }

    public static List<Point> CircleIntersections(Point center1, double radius1, Point center2, double radius2)
    {
        List<Point> ret = new List<Point>();
        double dx = center1.X - center2.X;
        double dy = center1.Y - center2.Y;
        double d = Math.Sqrt(dx * dx + dy * dy);

        if (d > (radius1 + radius2) || (d == 0 && radius1 == radius2) || (d + Math.Min(radius1, radius2)) < Math.Max(radius1, radius2))
        {
            return ret;
        }
        else
        {
            double a = (radius1 * radius1 - radius2 * radius2 + d * d) / (2 * d);
            double h = Math.Sqrt(radius1 * radius1 - a * a);

            double p2x = center1.X + a * (center2.X - center1.X) / d;
            double p2y = center1.Y + a * (center2.Y - center1.Y) / d;

            Point i1 = PointFromRounding(p2x + h * (center2.Y - center1.Y) / d, p2y - h * (center2.X - center1.X) / d);
            Point i2 = PointFromRounding(p2x - h * (center2.Y - center1.Y) / d, p2y + h * (center2.X - center1.X) / d);

            if (d == (radius1 + radius2))
            {
                ret.Add(i1);
            }
            else
            {
                ret.Add(i1);
                ret.Add(i2);
            }

            return ret;
        }
    }

    public static double AngleFromPoint(Point center, Point target)
    {
        return Math.Atan2(target.Y - center.Y, target.X - center.X) * (180 / Math.PI);
    }

    public static Point PointOnCircle(Point center, int radius, double angle)
    {
        return PointFromRounding(center.X + (radius * Math.Cos(angle * (Math.PI / 180))), center.Y + (radius * Math.Sin(angle * (Math.PI / 180))));
    }

    public static int GetCircleRadius(Point center, Point onCircle)
    {
        return (int)Math.Sqrt(Math.Pow(center.X - onCircle.X, 2) + Math.Pow(center.Y - onCircle.Y, 2));
    }

    public static Point GetNextPosition(Point currentPosition, int range, Point target)
    {
        double theta = Math.Atan2(target.Y - currentPosition.Y, target.X - currentPosition.X);
        return PointFromRounding(Math.Min(Math.Max(0, currentPosition.X + (range * Math.Cos(theta))), 17630), Math.Min(Math.Max(0, currentPosition.Y + (range * Math.Sin(theta))), 9000));
    }

    public static Point CoordsConverter(Point source, Point distance)
    {
        if (Game.IsTopLeft)
        {
            return new Point(source.X + distance.X, source.Y + distance.Y);
        }
        else
        {
            return new Point(source.X - distance.X, source.Y - distance.Y);
        }
    }

    public static bool IsInRange(Point source, Point center, int range)
    {
        return Math.Pow(source.X - center.X, 2) + Math.Pow(source.Y - center.Y, 2) <= Math.Pow(range, 2);
    }
}

public class EnnemyHero : Hero
{
    public double AngleToBase { get; set; }
}

public abstract class Hero : Entity
{
    public int DistanceToBase { get; set; }
}

public enum Threat
{
    Ennemy = 0,
    None = 1,
    Me = 2,
}

public class Player<THero> where THero : Hero
{
    public int Mana { get; set; }

    public int Health { get; set; }

    public Point BaseCoords { get; set; }

    public List<THero> Heroes { get; set; }

    public Player()
    {
        Heroes = new List<THero>();
    }
}

public abstract class Entity
{
    public int Id { get; set; }

    public Point Position { get; set; }

    public bool IsControlled { get; set; }

    public int ShieldDuration { get; set; }

    public void Debug(object message)
    {
        Console.Error.WriteLine($"{GetType().Name} {Id} : {message}");
    }
}