using System;
using System.Collections.Generic;

namespace VRTraining
{
    public sealed class TrainingSession
    {
        private readonly EquipmentId[] requiredOrder;

        private readonly HashSet<EquipmentId> completedItems =
            new HashSet<EquipmentId>();

        public int CurrentIndex { get; private set; }

        public int ErrorCount { get; private set; }

        public int TotalSteps => requiredOrder.Length;

        public bool IsComplete =>
            CurrentIndex >= requiredOrder.Length;

        public EquipmentId CurrentExpectedItem
        {
            get
            {
                if (IsComplete)
                {
                    throw new InvalidOperationException(
                        "The training session is already complete.");
                }

                return requiredOrder[CurrentIndex];
            }
        }

        public TrainingSession(EquipmentId[] requiredOrder)
        {
            if (requiredOrder == null ||
                requiredOrder.Length == 0)
            {
                throw new ArgumentException(
                    "At least one training step is required.");
            }

            this.requiredOrder =
                (EquipmentId[])requiredOrder.Clone();
        }

        public SubmissionStatus Submit(
            EquipmentId itemId,
            EquipmentId socketId)
        {
            if (IsComplete)
            {
                return SubmissionStatus.AlreadyComplete;
            }

            if (completedItems.Contains(itemId))
            {
                return SubmissionStatus.Duplicate;
            }

            if (itemId != socketId)
            {
                ErrorCount++;
                return SubmissionStatus.WrongSocket;
            }

            if (itemId != CurrentExpectedItem)
            {
                ErrorCount++;
                return SubmissionStatus.WrongOrder;
            }

            completedItems.Add(itemId);
            CurrentIndex++;

            return SubmissionStatus.Correct;
        }
    }
}