using System;
using System.Drawing;

namespace SpringChallenge2022
{
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
}