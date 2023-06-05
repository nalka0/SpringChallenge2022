using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SpringChallenge2022
{
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
                    Point added = new(Position.X + (Speed.X * i), Position.Y + (Speed.Y * i));
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
}
