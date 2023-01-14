using UnityEngine;

namespace NBG.DebugUI
{
    internal abstract class Navigator
    {
        public float FirstTimeSeconds = 0.4f;
        public float HoldTimeSeconds = 0.15f;

        int _holdDirection = 0; // Hold direction (-1, 0, 1)
        float _timeToWait = 0.0f; // How long to hold before next activation

        public float HoldDuration { get; private set; }

        // <direction> of 0 resets timers
        // Returns true of position changed
        public bool Move(int direction)
        {
            Debug.Assert(direction == 1 || direction == -1 || direction == 0);
            if (direction == 0)
            {
                HoldDuration = 0.0f;
                _holdDirection = 0;
                return false;
            }

            _timeToWait -= Time.deltaTime;
            HoldDuration += Time.deltaTime;

            var changed = false;
            var performChange = false;

            if (_holdDirection != direction)
            {
                _timeToWait = FirstTimeSeconds;
                performChange = true;
            }
            else if (_timeToWait <= 0.0f)
            {
                _timeToWait = HoldTimeSeconds;
                performChange = true;
            }

            if (performChange)
                changed = PerformChange(direction);

            _holdDirection = direction;

            return changed;
        }

        protected abstract bool PerformChange(int direction);
    }

    // Wraps around a given item count.
    // No acceleration.
    internal class WrappingNavigator : Navigator
    {
        public int Count { get; private set; }
        public int Position { get; private set; }

        public WrappingNavigator(int count)
        {
            Reset(count);
        }

        public void Reset(int count)
        {
            Count = count;
            Position = 0;
        }

        protected override bool PerformChange(int direction)
        {
            if (Count == 0)
                return false;

            var newPos = Position + direction;
            if (newPos < 0)
            {
                newPos = Count - 1;
            }
            else if (newPos >= Count)
            {
                newPos = 0;
            }

            if (Position != newPos)
            {
                Position = newPos;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // Changes item value.
    // Accelerates.
    internal class ValueNavigator : Navigator
    {
        public uint MultiplySpeedAfter = 2;
        public uint MultiplySpeedBy = 10;
        public uint BaseSpeed = 1;
        public uint Speed
        {
            get
            {
                uint currentSpeed = (uint)(Mathf.Pow(BaseSpeed * MultiplySpeedBy, (uint)(HoldDuration / MultiplySpeedAfter)));
                return currentSpeed;
            }
        }

        public Item Item { get; private set; }

        public ValueNavigator(Item item)
        {
            Reset(item);
        }

        public void Reset(Item item)
        {
            Item = item;
        }

        protected override bool PerformChange(int direction)
        {
            Debug.Assert(direction == 1 || direction == -1);

            if (direction == 1)
            {
                return Item.OnIncrement(Speed);
            }
            else
            {
                return Item.OnDecrement(Speed);
            }
        }
    }
}
