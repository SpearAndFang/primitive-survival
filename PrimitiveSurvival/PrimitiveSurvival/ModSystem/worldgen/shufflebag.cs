namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;

    // Data structure for picking random items
    public class ShuffleBag<T>
    {
        private readonly Random random = new Random();
        private readonly List<T> data;
        private T currentItem;
        private int currentPosition = -1;

        public int Size => this.data.Count;

        public ShuffleBag(int initCapacity)
        {
            this.data = new List<T>(initCapacity);
        }

        public ShuffleBag(int initCapacity, Random random)
        {
            this.random = random;
            this.data = new List<T>(initCapacity);
        }

        // Adds the specified number of the given item to the bag
        public void Add(T item, int amount)
        {
            for (var i = 0; i < amount; i++)
            { this.data.Add(item); }
            this.currentPosition = this.Size - 1;
        }

        // Returns the next random item from the bag
        public T Next()
        {
            if (this.currentPosition < 1)
            {
                this.currentPosition = this.Size - 1;
                this.currentItem = this.data[0];
                return this.currentItem;
            }
            var pos = this.random.Next(this.currentPosition);
            this.currentItem = this.data[pos];
            this.data[pos] = this.data[this.currentPosition];
            this.data[this.currentPosition] = this.currentItem;
            this.currentPosition--;
            return this.currentItem;
        }
    }
}
