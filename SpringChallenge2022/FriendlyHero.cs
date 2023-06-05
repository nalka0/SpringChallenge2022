using System;
using System.Drawing;

namespace SpringChallenge2022
{
    public abstract class FriendlyHero : Hero
    {
        public Point RallyPoint { get; set; }

        public void Play()
        {
            if (!PlayInternal() && !PlayWithAlreadyOptimalPosition(null, true, false))
            {
                if (Position != RallyPoint || !Explore())
                {
                    Move(RallyPoint);
                }
            }
        }

        protected abstract bool PlayWithAlreadyOptimalPosition(Monster target, bool allwoMove, bool isDefending);

        protected abstract bool PlayInternal();

        protected abstract bool Explore();

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
                Game.Me.Mana -= 10;
                Console.WriteLine($"SPELL SHIELD {target.Id}");
                return true;
            }
            else
            {
                Debug($"failed to cast SHIELD on {target.Id} {Game.Me.Mana > 10}/{target.ShieldDuration == 0}");
                return false;
            }
        }

        public bool Control(Entity target, Point destination)
        {
            if (Game.Me.Mana >= 10)
            {
                Game.Me.Mana -= 10;
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
                Game.Me.Mana -= 10;
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
}
